using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ec2Manager.Api.DTOs;
using Ec2Manager.Api.Services;

namespace Ec2Manager.Api.Controllers;

[ApiController]
[Route("accounts")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly CloudServiceClient _cloud;

    public AccountsController(CloudServiceClient cloud)
    {
        _cloud = cloud;
    }

    [HttpGet]
    public async Task<ActionResult<List<AccountMetadataDto>>> List()
    {
        var accounts = await _cloud.GetAccounts();
        return Ok(accounts);
    }

    [HttpGet("{accountKey}/regions")]
    public async Task<ActionResult<List<string>>> GetRegions(string accountKey)
    {
        var regions = await _cloud.GetRegions(accountKey);
        return Ok(regions);
    }
}
