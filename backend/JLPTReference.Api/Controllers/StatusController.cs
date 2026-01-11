using JLPTReference.Api.Data;
using JLPTReference.Api.Entities.Meta;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JLPTReference.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    private readonly ApplicationDBContext _context;

    public StatusController(ApplicationDBContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<DatabaseStatus>> GetStatus()
    {
        var status = await _context.DatabaseStatus.FindAsync(1);

        if (status == null)
        {
            // If the table is empty for some reason, return a default or error
             return NotFound("Status not found");
        }

        return Ok(status);
    }
}
