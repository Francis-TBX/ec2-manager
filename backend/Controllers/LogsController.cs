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

        var query = _db.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(accountKey))
            query = query.Where(a => a.AccountKey == accountKey);

        if (!string.IsNullOrWhiteSpace(region))
            query = query.Where(a => a.Region == region);

        if (!string.IsNullOrWhiteSpace(actionType))
            query = query.Where(a => a.ActionType == actionType);

        if (!string.IsNullOrWhiteSpace(result))
            query = query.Where(a => a.Result == result);

        if (from.HasValue)
            query = query.Where(a => a.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.Timestamp <= to.Value);

        if (!string.IsNullOrWhiteSpace(instanceId))
        {
            // InstanceIdsJson is a serialized string array; quote-anchored LIKE
            // avoids depending on MySQL JSON functions via Pomelo.
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
