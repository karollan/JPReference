namespace JLPTReference.Api.Entities.ProperNoun;

public class ProperNoun
{
    public Guid Id {get; set;}
    public required string JmnedictId {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}

    public List<ProperNounKanji> KanjiForms {get; set;} = new();
    public List<ProperNounKana> KanaForms {get; set;} = new();
    public List<ProperNounTranslation> Translations {get; set;} = new();
}