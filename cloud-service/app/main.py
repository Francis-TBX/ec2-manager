import os
from typing import List, Optional
from fastapi import FastAPI, Header, HTTPException, Depends
from pydantic import BaseModel
from dotenv import load_dotenv

load_dotenv()

from app.providers.factory import get_provider
from app.config.account_config import load_accounts

app = FastAPI(title="Multi-Cloud Instance Manager - Cloud Service", version="0.1.0")


def verify_api_key(authorization: str = Header(None)):
    expected = os.environ.get("EC2MANAGER_INTERNAL_API_KEY")
    if not expected:
        raise HTTPException(status_code=500, detail="Server misconfigured: internal API key not set")
    if not authorization or not authorization.startswith("Bearer "):
        raise HTTPException(status_code=401, detail="Missing or malformed Authorization header")
    token = authorization.removeprefix("Bearer ").strip()
    if token != expected:
        raise HTTPException(status_code=401, detail="Invalid API key")


class ListInstancesRequest(BaseModel):
    accountKey: str
    region: Optional[str] = None
    statuses: Optional[List[str]] = None
    search: Optional[str] = None


class InstanceActionRequest(BaseModel):
    accountKey: str
    region: str
    instanceIds: List[str]
    dryRun: bool = False


@app.get("/health")
def health():
    return {"status": "ok"}


@app.post("/instances/list", dependencies=[Depends(verify_api_key)])
def list_instances(req: ListInstancesRequest):
    provider = get_provider("aws")
    return provider.list_instances(req.accountKey, req.region, req.statuses, req.search)


@app.post("/instances/start", dependencies=[Depends(verify_api_key)])
def start_instances(req: InstanceActionRequest):
    provider = get_provider("aws")
    return provider.start_instances(req.accountKey, req.region, req.instanceIds, req.dryRun)


@app.post("/instances/stop", dependencies=[Depends(verify_api_key)])
def stop_instances(req: InstanceActionRequest):
    provider = get_provider("aws")
    return provider.stop_instances(req.accountKey, req.region, req.instanceIds, req.dryRun)


@app.get("/accounts/{account_key}/regions", dependencies=[Depends(verify_api_key)])
def get_regions(account_key: str):
    provider = get_provider("aws")
    return {"accountKey": account_key, "regions": provider.get_regions(account_key)}


@app.get("/accounts", dependencies=[Depends(verify_api_key)])
def list_accounts():
    accounts = load_accounts()
    return [
        {"key": a["key"], "name": a["name"], "accountId": a["accountId"]}
        for a in accounts
    ]
