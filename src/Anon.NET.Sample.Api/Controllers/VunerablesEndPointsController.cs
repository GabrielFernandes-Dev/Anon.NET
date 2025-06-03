using Anon.NET.Sample.Api.Data;
using Anon.NET.Sample.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Anon.NET.Sample.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VunerablesEndPointsController : ControllerBase
{
    private readonly SampleDbContext _context;
    public VunerablesEndPointsController(SampleDbContext context)
    {
        _context = context;
    }

    // Rota para testar a interceptação de SQL
    // Entradas que são SQL Injections:
    // 1. ' OR '1'='1 / %' OR 1=1 OR '%'='
    // 2. '; DROP TABLE Users;
    // 3. João' AND (SELECT COUNT(*) FROM Users WHERE SLEEP(5)) > 0 AND '1'='1
    // 4. 
    [HttpGet("vulnerable-search")]
    public async Task<IActionResult> VulnerableSearch([FromQuery] string name)
    {
        var query = $"SELECT * FROM Users WHERE Name LIKE '%{name}%'";

        var users = await _context.Users.FromSqlRaw(query).ToListAsync();

        return Ok(new { Message = "Search completed", Users = users });
    }

    [HttpGet("safe-search")]
    public async Task<IActionResult> LinqSearch([FromQuery] string name)
    {
        var users = await _context.Users.Where(u => u.Name == name).ToListAsync();

        return Ok(new { Message = "Search completed", Users = users });
    }

    [HttpPost("register-user")]
    public async Task<IActionResult> RegisterUser([FromBody] User user)
    {
        await _context.Users.AddAsync(user);
        _context.SaveChanges();
        return Ok(new { Message = "User registred", User = user });
    }
}
