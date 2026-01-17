namespace JLPTReference.Api.Repositories.Interfaces;

public interface ITagRepository
{
    Task<List<TagDto>> GetAllTagsAsync();
}

public class TagDto
{
    public required string Code { get; set; }
    public required string Description { get; set; }
    public required string Category { get; set; }
    public required List<string> Source { get; set; }
}
