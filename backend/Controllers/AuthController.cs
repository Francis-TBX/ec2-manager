using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ec2Manager.Api.Data;
using Ec2Manager.Api.DTOs;
using Ec2Manager.Api.Models;
using Ec2Manager.Api.Services;

namespace Ec2Manager.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AuthService _auth;

    public AuthController(AppDbContext db, AuthService auth)
    {
        _db = db;
        _auth = auth;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Username and password are required.");

        var exists = await _db.Users.AnyAsync(u => u.Username == req.Username || u.Email == req.Email);
        if (exists)
            return Conflict("A user with that username or email already exists.");

        var user = new User
        {
            Username = req.Username,
            Email = req.Email,
            PasswordHash = _auth.HashPassword(req.Password),
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = _auth.GenerateJwtToken(user);
        return Ok(new AuthResponse(token, user.Username, user.Email));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
        if (user is null || !_auth.VerifyPassword(req.Password, user.PasswordHash))
            return Unauthorized("Invalid username or password.");

        var token = _auth.GenerateJwtToken(user);
        return Ok(new AuthResponse(token, user.Username, user.Email));
    }
}
