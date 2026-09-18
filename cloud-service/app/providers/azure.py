class AzureCloudProvider:
    def list_instances(self, account_key, region, statuses, search):
        raise NotImplementedError("Azure provider not yet implemented")

    def start_instances(self, account_key, region, instance_ids, dry_run=False):
        raise NotImplementedError("Azure provider not yet implemented")

    def stop_instances(self, account_key, region, instance_ids, dry_run=False):
        raise NotImplementedError("Azure provider not yet implemented")

    def get_regions(self, account_key):
        raise NotImplementedError("Azure provider not yet implemented")
