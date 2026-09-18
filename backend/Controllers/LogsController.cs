using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ec2Manager.Api.Data;
using Ec2Manager.Api.DTOs;

namespace Ec2Manager.Api.Controllers;

[ApiController]
[Route("logs")]
[Authorize]
public class LogsController : ControllerBase
{
    private readonly AppDbContext _db;

    public LogsController(AppDbContext db)
    {
        _db = db;
    }

    private static List<string>? SplitCsv(string? s) =>
        string.IsNullOrWhiteSpace(s)
            ? null
            : s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<AuditLogDto>>> Get(
        [FromQuery] string? instanceId,
        [FromQuery] string? accountKey,
        [FromQuery] string? region,
        [FromQuery] string? actionType,
        [FromQuery] string? result,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 25;
        if (pageSize > 100) pageSize = 100;

        var accountKeys = SplitCsv(accountKey);
        var regions = SplitCsv(region);
        var actionTypes = SplitCsv(actionType);
        var results = SplitCsv(result);

        var query = _db.AuditLogs.AsQueryable();

        if (accountKeys != null)
            query = query.Where(a => accountKeys.Contains(a.AccountKey));

        if (regions != null)
            query = query.Where(a => regions.Contains(a.Region));

        if (actionTypes != null)
            query = query.Where(a => actionTypes.Contains(a.ActionType));

        if (results != null)
            query = query.Where(a => results.Contains(a.Result));

        if (from.HasValue)
            query = query.Where(a => a.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.Timestamp <= to.Value);

        if (!string.IsNullOrWhiteSpace(instanceId))
        {
            var needle = $"\"{instanceId}\"";
            query = query.Where(a => EF.Functions.Like(a.InstanceIdsJson, $"%{needle}%"));
        }

        query = query.OrderByDescending(a => a.Timestamp);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(a => new AuditLogDto(
            a.Id,
            a.Timestamp,
            a.UserName,
            a.ActionType,
            a.AccountKey,
            a.Region,
            JsonSerializer.Deserialize<List<string>>(a.InstanceIdsJson) ?? new List<string>(),
            a.DryRun,
            a.Result,
            a.Message,
            a.Error
        )).ToList();

        return Ok(new PagedResultDto<AuditLogDto>(dtos, totalCount, page, pageSize));
    }
}
