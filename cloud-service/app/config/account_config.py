import json
import os
from typing import List, Dict, Any
from cryptography.fernet import Fernet

_cached_accounts: List[Dict[str, Any]] | None = None


def _get_decryption_key() -> bytes:
    key = os.environ.get("EC2MANAGER_DECRYPTION_KEY")
    if not key:
        raise RuntimeError("EC2MANAGER_DECRYPTION_KEY is not set")
    return key.encode()


def load_accounts() -> List[Dict[str, Any]]:
    """
    Decrypts EC2MANAGER_AWS_ACCOUNTS_ENCRYPTED using EC2MANAGER_DECRYPTION_KEY.
    Result is cached in memory for the process lifetime.
    Never log or return raw credentials.
    """
    global _cached_accounts
    if _cached_accounts is not None:
        return _cached_accounts

    encrypted_env = os.environ.get("EC2MANAGER_AWS_ACCOUNTS_ENCRYPTED")
    if not encrypted_env:
        raise RuntimeError("EC2MANAGER_AWS_ACCOUNTS_ENCRYPTED is not set")

    payload = json.loads(encrypted_env)
    token = payload["encryptedPayload"].encode()

    fernet = Fernet(_get_decryption_key())
    decrypted_bytes = fernet.decrypt(token)
    accounts = json.loads(decrypted_bytes)

    _cached_accounts = accounts
    return accounts


def get_account(account_key: str) -> Dict[str, Any]:
    accounts = load_accounts()
    for acc in accounts:
        if acc["key"] == account_key:
            return acc
    raise KeyError(f"Unknown account key: {account_key}")
