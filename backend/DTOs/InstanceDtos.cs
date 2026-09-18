namespace Ec2Manager.Api.DTOs;

public record InstanceDto(
    string InstanceId,
    string Name,
    string State,
    bool DnsEnabled,
    string? PublicIp,
    string? PrivateIp,
    string Region,
    string AccountKey,
    string? LaunchTime,
    string? InstanceType
);

public record InstanceDetailDto(
    string InstanceId,
    string Name,
    string State,
    bool DnsEnabled,
    string? PublicIp,
    string? PrivateIp,
    string Region,
    string AccountKey,
    string? LaunchTime,
    string? InstanceType,
    List<TagDto> Tags
);

public record TagDto(string Key, string Value);

public record AccountMetadataDto(string Key, string Name, string AccountId);
