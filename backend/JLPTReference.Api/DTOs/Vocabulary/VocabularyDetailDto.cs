namespace JLPTReference.Api.DTOs.Vocabulary;

public class VocabularyDetailDto
{
    public Guid Id { get; set; }
    public required string JmdictId { get; set; }
    public List<KanjiFormDto> KanjiForms { get; set; } = new();
    public List<KanaFormDto> KanaForms { get; set; } = new();
    public List<SenseDto> Senses { get; set; } = new();
    public int? JlptLevel { get; set; }
    // Cross-references
    public List<KanjiInfoDto> ContainedKanji { get; set; } = new();
}

public class KanjiInfoDto
{
    public Guid Id { get; set; }
    public required string Literal { get; set; }
}
public class KanjiFormDto
{
    public required string Text { get; set; }
    public bool IsCommon { get; set; }
    public List<TagInfoDto> Tags { get; set; } = new();
}

public class KanaFormDto
{
    public required string Text { get; set; }
    public bool IsCommon { get; set; }
    public List<TagInfoDto> Tags { get; set; } = new();
    public List<string> AppliesToKanji { get; set; } = new();
}

public class SenseDto
{
    public List<string> AppliesToKanji { get; set; } = new();
    public List<string> AppliesToKana { get; set; } = new();
    public List<string> Info { get; set; } = new();
    public List<TagInfoDto> Tags { get; set; } = new();
    public List<SenseRelationDto> Relations { get; set; } = new();
    public List<SenseLanguageSourceDto> LanguageSources { get; set; } = new();
    public List<SenseGlossDto> Glosses { get; set; } = new();
    public List<SenseExampleDto> Examples { get; set; } = new();
}

public class TagInfoDto
{
    public required string Code { get; set; }
    public required string Description { get; set; }
    public required string Category { get; set; }
    public string? Type { get; set; } // For sense tags: 'pos', 'field', 'dialect', 'misc'
}

public class SenseRelationDto
{
    public Guid RelationId { get; set; }
    public Guid RelationSenseId { get; set; }
    public required string Term { get; set;}
    public string? Reading { get; set; }
    public required string RelationType { get; set; }
}

public class SenseLanguageSourceDto
{
    public required string Language { get; set; }
    public required string Text { get; set; }
    public bool? IsFull { get; set; }
    public bool? IsWaei { get; set; }
}

public class SenseGlossDto
{
    public required string Language { get; set; }
    public required string Text { get; set; }
    public string? Gender { get; set; }
    public string? Type { get; set; }
}

public class SenseExampleDto
{
    public required string SourceType { get; set; }
    public required string SourceValue { get; set; }
    public required string Text { get; set; }
    public List<SenseExampleSentenceDto> Sentences { get; set; } = new();
}

public class SenseExampleSentenceDto
{
    public required string Language { get; set; }
    public required string Text { get; set; }
}
