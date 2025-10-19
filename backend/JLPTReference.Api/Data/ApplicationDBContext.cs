using JLPTReference.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace JLPTReference.Api.Data;

public class ApplicationDBContext : DbContext
{
    public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options)
    {

    }

    public DbSet<Kanji> Kanji { get; set; }
    public DbSet<KanjiRadical> KanjiRadicals { get; set; }
    public DbSet<KanjiDecomposition> KanjiDecompositions { get; set; }
    public DbSet<Vocabulary> Vocabulary { get; set; }
    public DbSet<VocabularyExample> VocabularyExamples { get; set; }
    public DbSet<Radical> Radicals { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder) 
    {
        // Kanji table
        modelBuilder.Entity<Kanji>().ToTable("kanji", schema: "jlpt");
        modelBuilder.Entity<Kanji>().HasKey(k => k.Id);
        modelBuilder.Entity<Kanji>().HasIndex(k => k.Character).IsUnique();

        // KanjiRadical table
        modelBuilder.Entity<KanjiRadical>().ToTable("kanji_radicals", schema: "jlpt");
        modelBuilder.Entity<KanjiRadical>().HasKey(kr => kr.Id);
        modelBuilder.Entity<KanjiRadical>()
            .HasOne(kr => kr.Kanji)
            .WithMany(k => k.KanjiRadicals)
            .HasForeignKey(kr => kr.KanjiId)
            .OnDelete(DeleteBehavior.Cascade);

        // KanjiDecomposition table
        modelBuilder.Entity<KanjiDecomposition>().ToTable("kanji_decompositions", schema: "jlpt");
        modelBuilder.Entity<KanjiDecomposition>().HasKey(kd => kd.Id);
        modelBuilder.Entity<KanjiDecomposition>()
            .HasOne(kd => kd.Kanji)
            .WithMany(k => k.KanjiDecompositions)
            .HasForeignKey(kd => kd.KanjiId)
            .OnDelete(DeleteBehavior.Cascade);

        // Vocabulary table
        modelBuilder.Entity<Vocabulary>().ToTable("vocabulary", schema: "jlpt");
        modelBuilder.Entity<Vocabulary>().HasKey(v => v.Id);
        modelBuilder.Entity<Vocabulary>().HasIndex(v => v.JmdictId).IsUnique();

        // VocabularyExample table
        modelBuilder.Entity<VocabularyExample>().ToTable("vocabulary_examples", schema: "jlpt");
        modelBuilder.Entity<VocabularyExample>().HasKey(ve => ve.Id);
        modelBuilder.Entity<VocabularyExample>()
            .HasOne(ve => ve.Vocabulary)
            .WithMany(v => v.Examples)
            .HasForeignKey(ve => ve.VocabularyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Radical table
        modelBuilder.Entity<Radical>().ToTable("radicals", schema: "jlpt");
        modelBuilder.Entity<Radical>().HasKey(r => r.Id);
        modelBuilder.Entity<Radical>().HasIndex(r => r.Character).IsUnique();

        // Apply snake case to all properties
        foreach(var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach(var property in entity.GetProperties()) 
            {
                property.SetColumnName(Utils.ToSnakeCase(property.Name));
            }
        }
    }

}