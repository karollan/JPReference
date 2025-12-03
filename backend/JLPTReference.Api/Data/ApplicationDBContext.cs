using JLPTReference.Api.Entities.Kanji;
using JLPTReference.Api.Entities.Radical;
using JLPTReference.Api.Entities.Vocabulary;
using JLPTReference.Api.Entities.ProperNoun;
using JLPTReference.Api.Entities.Relations;
using JLPTReference.Api.DTOs.Search;
using Microsoft.EntityFrameworkCore;

namespace JLPTReference.Api.Data;

public class ApplicationDBContext : DbContext
{
    public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options)
    {

    }

    // Kanji
    public DbSet<Kanji> Kanji { get; set; }
    public DbSet<KanjiCodepoint> KanjiCodepoints { get; set; }
    public DbSet<KanjiDictionaryReference> KanjiDictionaryReferences { get; set; }
    public DbSet<KanjiQueryCode> KanjiQueryCodes { get; set; }
    public DbSet<KanjiReading> KanjiReadings { get; set; }
    public DbSet<KanjiMeaning> KanjiMeanings { get; set; }
    public DbSet<KanjiNanori> KanjiNanori { get; set; }

    // Radical
    public DbSet<Radical> Radicals { get; set; }

    // Vocabulary
    public DbSet<Tag> Tags { get; set; }
    public DbSet<Vocabulary> Vocabulary { get; set; }
    public DbSet<VocabularyKanji> VocabularyKanji { get; set; }
    public DbSet<VocabularyKanjiTag> VocabularyKanjiTags { get; set; }
    public DbSet<VocabularyKana> VocabularyKana { get; set; }
    public DbSet<VocabularyKanaTag> VocabularyKanaTags { get; set; }
    public DbSet<VocabularySense> VocabularySenses { get; set; }
    public DbSet<VocabularySenseTag> VocabularySenseTags { get; set; }
    public DbSet<VocabularySenseRelation> VocabularySenseRelations { get; set; }
    public DbSet<VocabularySenseLanguageSource> VocabularySenseLanguageSources { get; set; }
    public DbSet<VocabularySenseGloss> VocabularySenseGlosses { get; set; }
    public DbSet<VocabularySenseExample> VocabularySenseExamples { get; set; }
    public DbSet<VocabularySenseExampleSentence> VocabularySenseExampleSentences { get; set; }

    // Proper Nouns
    public DbSet<ProperNoun> ProperNouns { get; set; }
    public DbSet<ProperNounKanji> ProperNounKanji { get; set; }
    public DbSet<ProperNounKanjiTag> ProperNounKanjiTags { get; set; }
    public DbSet<ProperNounKana> ProperNounKana { get; set; }
    public DbSet<ProperNounKanaTag> ProperNounKanaTags { get; set; }
    public DbSet<ProperNounTranslation> ProperNounTranslations { get; set; }
    public DbSet<ProperNounTranslationType> ProperNounTranslationTypes { get; set; }
    public DbSet<ProperNounTranslationText> ProperNounTranslationTexts { get; set; }
    public DbSet<ProperNounTranslationRelated> ProperNounTranslationRelated { get; set; }

    // Relations
    public DbSet<KanjiRadical> KanjiRadicals { get; set; }
    public DbSet<VocabularyUsesKanji> VocabularyUsesKanji { get; set; }
    public DbSet<ProperNounUsesKanji> ProperNounUsesKanji { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder) 
    {
        // Kanji
        modelBuilder.Entity<Kanji>().ToTable("kanji", schema: "jlpt");
        modelBuilder.Entity<Kanji>().HasKey(k => k.Id);
        modelBuilder.Entity<Kanji>().HasIndex(k => k.Literal).IsUnique();

        modelBuilder.Entity<KanjiCodepoint>().ToTable("kanji_codepoint", schema: "jlpt");
        modelBuilder.Entity<KanjiCodepoint>().HasKey(kc => kc.Id);

        modelBuilder.Entity<KanjiDictionaryReference>().ToTable("kanji_dictionary_reference", schema: "jlpt");
        modelBuilder.Entity<KanjiDictionaryReference>().HasKey(kdr => kdr.Id);

        modelBuilder.Entity<KanjiQueryCode>().ToTable("kanji_query_code", schema: "jlpt");
        modelBuilder.Entity<KanjiQueryCode>().HasKey(kqc => kqc.Id);

        modelBuilder.Entity<KanjiReading>().ToTable("kanji_reading", schema: "jlpt");
        modelBuilder.Entity<KanjiReading>().HasKey(kr => kr.Id);

        modelBuilder.Entity<KanjiMeaning>().ToTable("kanji_meaning", schema: "jlpt");
        modelBuilder.Entity<KanjiMeaning>().HasKey(km => km.Id);

        modelBuilder.Entity<KanjiNanori>().ToTable("kanji_nanori", schema: "jlpt");
        modelBuilder.Entity<KanjiNanori>().HasKey(kn => kn.Id);

        // Radical
        modelBuilder.Entity<Radical>().ToTable("radical", schema: "jlpt");
        modelBuilder.Entity<Radical>().HasKey(r => r.Id);
        modelBuilder.Entity<Radical>().HasIndex(r => r.Literal).IsUnique();

        // Vocabulary
        modelBuilder.Entity<Tag>().ToTable("tag", schema: "jlpt");
        modelBuilder.Entity<Tag>().HasKey(t => t.Code);

        modelBuilder.Entity<Vocabulary>().ToTable("vocabulary", schema: "jlpt");
        modelBuilder.Entity<Vocabulary>().HasKey(v => v.Id);
        modelBuilder.Entity<Vocabulary>().HasIndex(v => v.JmdictId).IsUnique();

        modelBuilder.Entity<VocabularyKanji>().ToTable("vocabulary_kanji", schema: "jlpt");
        modelBuilder.Entity<VocabularyKanji>().HasKey(vk => vk.Id);

        modelBuilder.Entity<VocabularyKanjiTag>().ToTable("vocabulary_kanji_tag", schema: "jlpt");
        modelBuilder.Entity<VocabularyKanjiTag>().HasKey(vkt => vkt.Id);

        modelBuilder.Entity<VocabularyKana>().ToTable("vocabulary_kana", schema: "jlpt");
        modelBuilder.Entity<VocabularyKana>().HasKey(vka => vka.Id);

        modelBuilder.Entity<VocabularyKanaTag>().ToTable("vocabulary_kana_tag", schema: "jlpt");
        modelBuilder.Entity<VocabularyKanaTag>().HasKey(vkat => vkat.Id);

        modelBuilder.Entity<VocabularySense>().ToTable("vocabulary_sense", schema: "jlpt");
        modelBuilder.Entity<VocabularySense>().HasKey(vs => vs.Id);

        modelBuilder.Entity<VocabularySenseTag>().ToTable("vocabulary_sense_tag", schema: "jlpt");
        modelBuilder.Entity<VocabularySenseTag>().HasKey(vst => vst.Id);
        modelBuilder.Entity<VocabularySenseTag>().HasIndex(vst => new { vst.SenseId, vst.TagCode, vst.TagType }).IsUnique();

        modelBuilder.Entity<VocabularySenseRelation>().ToTable("vocabulary_sense_relation", schema: "jlpt");
        modelBuilder.Entity<VocabularySenseRelation>().HasKey(vsr => vsr.Id);

        modelBuilder.Entity<VocabularySenseLanguageSource>().ToTable("vocabulary_sense_language_source", schema: "jlpt");
        modelBuilder.Entity<VocabularySenseLanguageSource>().HasKey(vsls => vsls.Id);

        modelBuilder.Entity<VocabularySenseGloss>().ToTable("vocabulary_sense_gloss", schema: "jlpt");
        modelBuilder.Entity<VocabularySenseGloss>().HasKey(vsg => vsg.Id);

        modelBuilder.Entity<VocabularySenseExample>().ToTable("vocabulary_sense_example", schema: "jlpt");
        modelBuilder.Entity<VocabularySenseExample>().HasKey(vse => vse.Id);

        modelBuilder.Entity<VocabularySenseExampleSentence>().ToTable("vocabulary_sense_example_sentence", schema: "jlpt");
        modelBuilder.Entity<VocabularySenseExampleSentence>().HasKey(vses => vses.Id);

        // Proper Nouns
        modelBuilder.Entity<ProperNoun>().ToTable("proper_noun", schema: "jlpt");
        modelBuilder.Entity<ProperNoun>().HasKey(pn => pn.Id);
        modelBuilder.Entity<ProperNoun>().HasIndex(pn => pn.JmnedictId).IsUnique();

        modelBuilder.Entity<ProperNounKanji>().ToTable("proper_noun_kanji", schema: "jlpt");
        modelBuilder.Entity<ProperNounKanji>().HasKey(pnk => pnk.Id);

        modelBuilder.Entity<ProperNounKanjiTag>().ToTable("proper_noun_kanji_tag", schema: "jlpt");
        modelBuilder.Entity<ProperNounKanjiTag>().HasKey(pnkt => pnkt.Id);

        modelBuilder.Entity<ProperNounKana>().ToTable("proper_noun_kana", schema: "jlpt");
        modelBuilder.Entity<ProperNounKana>().HasKey(pnka => pnka.Id);

        modelBuilder.Entity<ProperNounKanaTag>().ToTable("proper_noun_kana_tag", schema: "jlpt");
        modelBuilder.Entity<ProperNounKanaTag>().HasKey(pnkat => pnkat.Id);

        modelBuilder.Entity<ProperNounTranslation>().ToTable("proper_noun_translation", schema: "jlpt");
        modelBuilder.Entity<ProperNounTranslation>().HasKey(pnt => pnt.Id);

        modelBuilder.Entity<ProperNounTranslationType>().ToTable("proper_noun_translation_type", schema: "jlpt");
        modelBuilder.Entity<ProperNounTranslationType>().HasKey(pntt => pntt.Id);

        modelBuilder.Entity<ProperNounTranslationText>().ToTable("proper_noun_translation_text", schema: "jlpt");
        modelBuilder.Entity<ProperNounTranslationText>().HasKey(pntt => pntt.Id);

        modelBuilder.Entity<ProperNounTranslationRelated>().ToTable("proper_noun_translation_related", schema: "jlpt");
        modelBuilder.Entity<ProperNounTranslationRelated>().HasKey(pntr => pntr.Id);

        // Relations
        modelBuilder.Entity<KanjiRadical>().ToTable("kanji_radical", schema: "jlpt");
        modelBuilder.Entity<KanjiRadical>().HasKey(kr => kr.Id);
        modelBuilder.Entity<KanjiRadical>().HasIndex(kr => new { kr.KanjiId, kr.RadicalId}).IsUnique();

        modelBuilder.Entity<VocabularyUsesKanji>().ToTable("vocabulary_uses_kanji", schema: "jlpt");
        modelBuilder.Entity<VocabularyUsesKanji>().HasKey(vuk => vuk.Id);
        modelBuilder.Entity<VocabularyUsesKanji>().HasIndex(vuk => new { vuk.VocabularyId, vuk.KanjiId }).IsUnique();

        modelBuilder.Entity<ProperNounUsesKanji>().ToTable("proper_noun_uses_kanji", schema: "jlpt");
        modelBuilder.Entity<ProperNounUsesKanji>().HasKey(pnuk => pnuk.Id);
        modelBuilder.Entity<ProperNounUsesKanji>().HasIndex(pnuk => new { pnuk.ProperNounId, pnuk.KanjiId }).IsUnique();

        // Apply snake case to all properties
        foreach(var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach(var property in entity.GetProperties()) 
            {
                property.SetColumnName(Utils.ToSnakeCase(property.Name));
            }
        }

        // Special case: "full" is a PostgreSQL reserved word - Npgsql handles quoting automatically
        modelBuilder.Entity<VocabularySenseLanguageSource>().Property(vsls => vsls.Full).HasColumnName("full");
    }
}
