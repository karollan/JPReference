using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JLPTReference.Api.Data;

namespace JLPTReference.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ApplicationDBContext _context;

    public SearchController(ApplicationDBContext context)
    {
        _context = context;
    }
}