using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ec2Manager.Api.DTOs;

namespace Ec2Manager.Api.Controllers;

[ApiController]
[Route("accounts")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly IConfiguration _config;

    public AccountsController(IConfiguration config)
    {
        _config = config;
    }

    [HttpGet]
    public ActionResult<List<AccountMetadataDto>> List()
    {
        // Phase 1: account metadata mirrors cloud-service's encrypted config.
        // For now, single known account; will be replaced by a shared config source.
        var accounts = new List<AccountMetadataDto>
        {
            new("AWS", "AWS", "083141433636")
        };
        return Ok(accounts);
    }
}
