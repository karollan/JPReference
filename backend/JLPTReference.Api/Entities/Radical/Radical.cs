namespace JLPTReference.Api.Entities.Radical;

public class Radical
{
    public Guid Id {get; set;}
    public Guid GroupId {get; set;}
    public required string Literal {get; set;}
    public required int StrokeCount {get; set;}
    public string? Code {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}

    public RadicalGroup Group {get; set;}
}

public class RadicalGroup
{
    public Guid Id {get; set;}
    public required string CanonicalLiteral {get; set;}
    public int? KangXiNumber {get; set;}
    public List<string> Meanings {get; set;}
    public List<string> Readings {get; set;}
    public List<string> Notes {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}
}

public class RadicalGroupMember
{
    public Guid Id {get; set;}
    public required Guid GroupId {get; set;}
    public required string Literal {get; set;}
    public bool IsCanonical {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}

    public RadicalGroup Group {get; set;}
}