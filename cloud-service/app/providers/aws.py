import boto3
from typing import List, Dict, Any, Optional
from app.config.account_config import get_account
from app.providers.base import is_dns_enabled

VALID_STATES = {"pending", "running", "shutting-down", "terminated", "stopping", "stopped"}

# Session and region caches (in-memory, per-process)
_session_cache: Dict[str, boto3.Session] = {}
_region_cache: Dict[str, List[str]] = {}


class AwsCloudProvider:
    def _get_session(self, account_key: str) -> boto3.Session:
        if account_key in _session_cache:
            return _session_cache[account_key]

        account = get_account(account_key)
        creds = account["credentials"]
        session = boto3.Session(
            aws_access_key_id=creds["accessKeyId"],
            aws_secret_access_key=creds["secretAccessKey"],
        )
        _session_cache[account_key] = session
        return session

    def get_regions(self, account_key: str) -> List[str]:
        if account_key in _region_cache:
            return _region_cache[account_key]

        session = self._get_session(account_key)
        # describe_regions needs *a* region to call from; us-east-1 is always valid
        ec2 = session.client("ec2", region_name="us-east-1")
        resp = ec2.describe_regions(AllRegions=False)
        regions = sorted(r["RegionName"] for r in resp["Regions"])
        _region_cache[account_key] = regions
        return regions

    def _describe_instances(self, account_key: str, region: str) -> List[Dict[str, Any]]:
        session = self._get_session(account_key)
        ec2 = session.client("ec2", region_name=region)
        paginator = ec2.get_paginator("describe_instances")

        instances = []
        for page in paginator.paginate():
            for reservation in page["Reservations"]:
                for inst in reservation["Instances"]:
                    tags = inst.get("Tags", [])
                    name_tag = next((t["Value"] for t in tags if t["Key"] == "Name"), None)
                    instances.append({
                        "instanceId": inst["InstanceId"],
                        "name": name_tag or inst["InstanceId"],
                        "state": inst["State"]["Name"],
                        "tags": tags,
                        "publicIp": inst.get("PublicIpAddress"),
                        "privateIp": inst.get("PrivateIpAddress"),
                        "region": region,
                        "accountKey": account_key,
                        "launchTime": inst["LaunchTime"].isoformat() if inst.get("LaunchTime") else None,
                        "instanceType": inst.get("InstanceType"),
                    })
        return instances

    def list_instances(
        self,
        account_key: str,
        region: Optional[str],
        statuses: Optional[List[str]],
        search: Optional[str],
    ) -> List[Dict[str, Any]]:
        regions_to_query = [region] if region else self.get_regions(account_key)

        results = []
        if len(regions_to_query) > 1:
            from concurrent.futures import ThreadPoolExecutor, as_completed
            with ThreadPoolExecutor(max_workers=min(10, len(regions_to_query))) as executor:
                futures = {executor.submit(self._describe_instances, account_key, r): r for r in regions_to_query}
                for future in as_completed(futures):
                    results.extend(future.result())
        else:
            for r in regions_to_query:
                results.extend(self._describe_instances(account_key, r))

        if statuses:
            status_set = {s.lower() for s in statuses}
            results = [i for i in results if i["state"].lower() in status_set]

        if search:
            needle = search.lower()
            def matches(i):
                return (
                    needle in i["instanceId"].lower()
                    or needle in (i["name"] or "").lower()
                    or needle in (i["publicIp"] or "").lower()
                    or needle in (i["privateIp"] or "").lower()
                )
            results = [i for i in results if matches(i)]

        return results

    def _validate_dns_and_split(self, account_key: str, region: str, instance_ids: List[str]):
        """Returns (allowed_ids, skipped) where skipped = [{instanceId, reason}]."""
        all_instances = self._describe_instances(account_key, region)
        by_id = {i["instanceId"]: i for i in all_instances}

        allowed = []
        skipped = []
        for iid in instance_ids:
            inst = by_id.get(iid)
            if inst is None:
                skipped.append({"instanceId": iid, "reason": "Instance not found in region"})
            elif not is_dns_enabled(inst["tags"]):
                skipped.append({"instanceId": iid, "reason": "DNS tag missing or not Yes"})
            else:
                allowed.append(iid)
        return allowed, skipped

    def start_instances(
        self,
        account_key: str,
        region: str,
        instance_ids: List[str],
        dry_run: bool = False,
    ) -> Dict[str, Any]:
        allowed, skipped = self._validate_dns_and_split(account_key, region, instance_ids)

        if dry_run:
            return {"wouldStart": allowed, "wouldSkip": skipped, "errors": []}

        errors = []
        started = []
        if allowed:
            session = self._get_session(account_key)
            ec2 = session.client("ec2", region_name=region)
            try:
                ec2.start_instances(InstanceIds=allowed)
                started = allowed
            except Exception as e:
                errors.append(str(e))

        return {"started": started, "skipped": skipped, "errors": errors}

    def stop_instances(
        self,
        account_key: str,
        region: str,
        instance_ids: List[str],
        dry_run: bool = False,
    ) -> Dict[str, Any]:
        allowed, skipped = self._validate_dns_and_split(account_key, region, instance_ids)

        if dry_run:
            return {"wouldStop": allowed, "wouldSkip": skipped, "errors": []}

        errors = []
        stopped = []
        if allowed:
            session = self._get_session(account_key)
            ec2 = session.client("ec2", region_name=region)
            try:
                ec2.stop_instances(InstanceIds=allowed)
                stopped = allowed
            except Exception as e:
                errors.append(str(e))

        return {"stopped": stopped, "skipped": skipped, "errors": errors}
