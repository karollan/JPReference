using JLPTReference.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace JLPTReference.Api.Data;

public class ApplicationDBContext : DbContext
{
    public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options)
    {

    }

    public DbSet<Kanji> Kanji { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder) 
    {
        // Kanji table
        modelBuilder.Entity<Kanji>().ToTable("kanji", schema: "jlpt");

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