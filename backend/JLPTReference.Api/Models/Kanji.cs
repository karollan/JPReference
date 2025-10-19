using System.ComponentModel.DataAnnotations;

namespace JLPTReference.Api.Models;

public class Kanji
{
    public Guid Id { get; set; }
    public required string Character { get; set; }
    public required string[] Meanings { get; set; }
    public string[]? ReadingsOn { get; set; }
    public string[]? ReadingsKun { get; set; }
    public int? StrokeCount { get; set; }
    public int? Grade { get; set; }
    public int? Frequency { get; set; }
    public int? JlptOld { get; set; }
    public int? JlptNew { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class KanjiDto
{
    public required string Character { get; set; }
    public required string[] Meanings { get; set; }
    public string[]? ReadingsOn { get; set; }
    public string[]? ReadingsKun { get; set; }
    public int? StrokeCount { get; set; }
    public int? Grade { get; set; }
    public int? Frequency { get; set; }
    public int? JlptOld { get; set; }
    public int? JlptNew { get; set; }
}