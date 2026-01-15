using JLPTReference.Api.Data;
using JLPTReference.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JLPTReference.Api.Repositories.Implementations;

public class TagRepository : ITagRepository
{
    private readonly ApplicationDBContext _context;

    public TagRepository(ApplicationDBContext context)
    {
        _context = context;
    }

    public async Task<List<TagDto>> GetAllTagsAsync()
    {
        return await _context.Tags
            .AsNoTracking()
            .Select(t => new TagDto
            {
                Code = t.Code,
                Description = t.Description,
                Category = t.Category,
                Source = t.Source.ToList()
            })
            .ToListAsync();
    }
}
