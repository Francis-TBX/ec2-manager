from app.providers.gcp import GcpCloudProvider
from app.providers.azure import AzureCloudProvider
from app.providers.oracle import OracleCloudProvider


def get_provider(cloud: str):
    if cloud == "aws":
        from app.providers.aws import AwsCloudProvider
        return AwsCloudProvider()
    elif cloud == "gcp":
        return GcpCloudProvider()
    elif cloud == "azure":
        return AzureCloudProvider()
    elif cloud == "oracle":
        return OracleCloudProvider()
    else:
        raise ValueError(f"Unsupported cloud: {cloud}")
