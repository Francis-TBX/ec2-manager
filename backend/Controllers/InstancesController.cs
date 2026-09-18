using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ec2Manager.Api.DTOs;
using Ec2Manager.Api.Services;

namespace Ec2Manager.Api.Controllers;

[ApiController]
[Route("instances")]
[Authorize]
public class InstancesController : ControllerBase
{
    private readonly CloudServiceClient _cloud;

    public InstancesController(CloudServiceClient cloud)
    {
        _cloud = cloud;
    }

    private static bool IsDnsEnabled(System.Text.Json.JsonElement tags)
    {
        foreach (var tag in tags.EnumerateArray())
        {
            if (tag.GetProperty("Key").GetString() == "DNS")
            {
                var val = tag.GetProperty("Value").GetString() ?? "";
                return val.Equals("Yes", StringComparison.OrdinalIgnoreCase);
            }
        }
        return false;
    }

    private static InstanceDto ToDto(System.Text.Json.JsonElement inst)
    {
        var tags = inst.GetProperty("tags");
        return new InstanceDto(
            inst.GetProperty("instanceId").GetString()!,
            inst.GetProperty("name").GetString()!,
            inst.GetProperty("state").GetString()!,
            IsDnsEnabled(tags),
            inst.TryGetProperty("publicIp", out var pub) ? pub.GetString() : null,
            inst.TryGetProperty("privateIp", out var priv) ? priv.GetString() : null,
            inst.GetProperty("region").GetString()!,
            inst.GetProperty("accountKey").GetString()!,
            inst.TryGetProperty("launchTime", out var lt) ? lt.GetString() : null,
            inst.TryGetProperty("instanceType", out var it) ? it.GetString() : null
        );
    }

    [HttpGet]
    public async Task<ActionResult<List<InstanceDto>>> List(
        [FromQuery] string accountKey,
        [FromQuery] string? region,
        [FromQuery] string? statuses,
        [FromQuery] string? search,
        [FromQuery] bool? dnsOnly)
    {
        if (string.IsNullOrWhiteSpace(accountKey))
            return BadRequest("accountKey is required.");

        var statusList = string.IsNullOrWhiteSpace(statuses)
            ? null
            : statuses.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        var raw = await _cloud.ListInstancesRaw(accountKey, region, statusList, search);
        var instances = raw.Select(ToDto).ToList();

        if (dnsOnly == true)
            instances = instances.Where(i => i.DnsEnabled).ToList();

        return Ok(instances);
    }

    [HttpGet("{instanceId}")]
    public async Task<ActionResult<InstanceDetailDto>> GetDetail(
        string instanceId,
        [FromQuery] string accountKey,
        [FromQuery] string region)
    {
        if (string.IsNullOrWhiteSpace(accountKey) || string.IsNullOrWhiteSpace(region))
            return BadRequest("accountKey and region are required.");

        var raw = await _cloud.ListInstancesRaw(accountKey, region, null, null);
        var match = raw.FirstOrDefault(i => i.GetProperty("instanceId").GetString() == instanceId);

        if (match.ValueKind == System.Text.Json.JsonValueKind.Undefined)
            return NotFound();

        var tags = match.GetProperty("tags").EnumerateArray()
            .Select(t => new TagDto(t.GetProperty("Key").GetString()!, t.GetProperty("Value").GetString()!))
            .ToList();

        var dto = ToDto(match);
        return Ok(new InstanceDetailDto(
            dto.InstanceId, dto.Name, dto.State, dto.DnsEnabled, dto.PublicIp, dto.PrivateIp,
            dto.Region, dto.AccountKey, dto.LaunchTime, dto.InstanceType, tags
        ));
    }
}
