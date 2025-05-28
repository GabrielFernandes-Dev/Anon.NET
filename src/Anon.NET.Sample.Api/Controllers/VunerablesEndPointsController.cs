using Anon.NET.Sample.Api.Data;
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
    // 1. 
    [HttpGet("vulnerable-search")]
    public async Task<IActionResult> VulnerableSearch([FromQuery] string name)
    {
        var query = $"SELECT * FROM Users WHERE Name LIKE '%{name}%'";

        var teste = _context.Users.ToList();

        var users = await _context.Users.FromSqlRaw(query).ToListAsync();

        return Ok(new { Message = "Search completed", Users = users });
    }
}
