import json
import getpass
from cryptography.fernet import Fernet

def main():
    key = Fernet.generate_key()
    fernet = Fernet(key)

    print("Enter details for AWS account (you can add more accounts later by re-running this).")
    account_key = input("Account key (short label, e.g. 'prod'): ").strip() or "prod"
    name = input("Display name (e.g. 'Production'): ").strip() or "Production"
    account_id = input("AWS Account ID [083141433636]: ").strip() or "083141433636"
    access_key_id = input("AWS Access Key ID: ").strip()
    secret_access_key = getpass.getpass("AWS Secret Access Key (hidden input): ").strip()

    accounts = [
        {
            "key": account_key,
            "name": name,
            "accountId": account_id,
            "credentials": {
                "accessKeyId": access_key_id,
                "secretAccessKey": secret_access_key,
            },
        }
    ]

    plaintext = json.dumps(accounts).encode()
    token = fernet.encrypt(plaintext)

    encrypted_env_value = json.dumps({"encryptedPayload": token.decode()})

    print("\n--- Add these to your .env file (cloud-service/.env) ---\n")
    print(f"EC2MANAGER_DECRYPTION_KEY={key.decode()}")
    print(f"EC2MANAGER_AWS_ACCOUNTS_ENCRYPTED={encrypted_env_value}")
    print(f"EC2MANAGER_INTERNAL_API_KEY=change-me-to-a-random-shared-secret")
    print("\n----------------------------------------------------------\n")

if __name__ == "__main__":
    main()
