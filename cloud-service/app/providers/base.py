from typing import Protocol, List, Dict, Any, Optional


class Instance(Protocol):
    @property
    def instance_id(self) -> str: ...
    @property
    def state(self) -> str: ...
    @property
    def tags(self) -> List[Dict[str, str]]: ...
    @property
    def public_ip(self) -> Optional[str]: ...
    @property
    def private_ip(self) -> Optional[str]: ...
    @property
    def region(self) -> str: ...
    @property
    def account_key(self) -> str: ...


class CloudProvider(Protocol):
    def list_instances(
        self,
        account_key: str,
        region: Optional[str],
        statuses: Optional[List[str]],
        search: Optional[str],
    ) -> List[Instance]: ...

    def start_instances(
        self,
        account_key: str,
        region: str,
        instance_ids: List[str],
        dry_run: bool = False,
    ) -> Dict[str, Any]: ...

    def stop_instances(
        self,
        account_key: str,
        region: str,
        instance_ids: List[str],
        dry_run: bool = False,
    ) -> Dict[str, Any]: ...

    def get_regions(self, account_key: str) -> List[str]: ...


def is_dns_enabled(tags: List[Dict[str, str]]) -> bool:
    """Tag-based policy: only DNS=Yes (case-insensitive) instances are actionable."""
    tag_map = {t["Key"]: t["Value"] for t in (tags or [])}
    return tag_map.get("DNS", "").lower() == "yes"
