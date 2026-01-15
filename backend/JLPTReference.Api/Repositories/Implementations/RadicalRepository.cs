using JLPTReference.Api.DTOs.Radical;
using JLPTReference.Api.Data;
using Microsoft.EntityFrameworkCore;
using JLPTReference.Api.Repositories.Interfaces;
using JLPTReference.Api.DTOs.Kanji;
using Npgsql;
using System.Text.Json;

namespace JLPTReference.Api.Repositories.Implementations;
public class RadicalRepository : IRadicalRepository {
    private readonly ApplicationDBContext _context;
    private readonly string _connectionString;

    private char NormalizeRadicalChar(char literal)
    {
        return NormMap.TryGetValue(literal, out var normalized)
            ? normalized
            : literal;
    }

    public RadicalRepository(ApplicationDBContext context, IConfiguration configuration) {
        _context = context;
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public async Task<List<RadicalSummaryDto>> GetRadicalsListAsync()
    {
        return await _context.Radicals
            .Select(r => new RadicalSummaryDto
            {
                Id = r.Id,
                Literal = r.Literal,
                StrokeCount = r.StrokeCount
            })
            .ToListAsync();
    }

    public async Task<RadicalDetailDto> GetRadicalByLiteralAsync(string literal) {

        // 1. Find the member (variant) matching the input literal
        var member = await _context.RadicalGroupMembers
            .Include(m => m.Group)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Literal == literal);
            
        if (member == null)
            return null;

        var group = member.Group;

        // 2. Find the "main" source radical entry for this group to get stroke count/code
        var mainSourceRadical = await _context.Radicals
            .Where(r => r.GroupId == group.Id)
            .AsNoTracking()
            .OrderByDescending(r => r.Literal == group.CanonicalLiteral)
            .FirstOrDefaultAsync();

        // 3. Get all variants (members) for the group
        var allMembers = await _context.RadicalGroupMembers
            .Where(m => m.GroupId == group.Id)
            .AsNoTracking()
            .ToListAsync();

        // 4. Fetch Kanji for each variant
        var variantsDtos = new List<RadicalGroupMemberDto>();
        var allKanjiForGroup = new HashSet<Guid>();
        var topLevelKanjiList = new List<KanjiSummaryDto>();

        foreach (var m in allMembers)
        {
            var kanji = await GetKanjisForLiteralAsync(m.Literal);
            variantsDtos.Add(new RadicalGroupMemberDto
            {
                Id = m.Id,
                Literal = m.Literal,
                Kanji = kanji.Take(10).ToList()
            });
            
            foreach(var k in kanji)
            {
                if (allKanjiForGroup.Add(k.Id))
                {
                    topLevelKanjiList.Add(k);
                }
            }
        }

        return new RadicalDetailDto
        {
            Id = mainSourceRadical?.Id ?? group.Id,
            Literal = group.CanonicalLiteral,
            StrokeCount = mainSourceRadical?.StrokeCount ?? 0,
            Code = mainSourceRadical?.Code,
            KangXiNumber = group.KangXiNumber,
            Meanings = group.Meanings,
            Readings = group.Readings,
            Notes = group.Notes,
            Variants = variantsDtos,
            Kanji = topLevelKanjiList.Take(10).ToList(),
            UpdatedAt = group.UpdatedAt
        };
    }

    private async Task<List<KanjiSummaryDto>> GetKanjisForLiteralAsync(string literal)
    {
        var sql = "SELECT * FROM jlpt.get_kanjis_for_radical_literal(@literal)";
        var results = new List<KanjiSummaryDto>();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.Add(new NpgsqlParameter("@literal", literal));

        await using var reader = await command.ExecuteReaderAsync();

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        while (await reader.ReadAsync())
        {
            results.Add(new KanjiSummaryDto
            {
                Id = reader.GetGuid(0),
                Literal = reader.GetString(1),
                Grade = await reader.IsDBNullAsync(2) ? null : reader.GetInt32(2),
                StrokeCount = reader.GetInt32(3),
                Frequency = await reader.IsDBNullAsync(4) ? null : reader.GetInt32(4),
                JlptLevel = await reader.IsDBNullAsync(5) ? null : reader.GetInt32(5),
                KunyomiReadings = await reader.IsDBNullAsync(6) ? new List<KanjiReadingDto>() :
                    JsonSerializer.Deserialize<List<KanjiReadingDto>>(reader.GetString(6), jsonOptions) ?? new List<KanjiReadingDto>(),
                OnyomiReadings = await reader.IsDBNullAsync(7) ? new List<KanjiReadingDto>() :
                    JsonSerializer.Deserialize<List<KanjiReadingDto>>(reader.GetString(7), jsonOptions) ?? new List<KanjiReadingDto>(),
                Meanings = await reader.IsDBNullAsync(8) ? new List<KanjiMeaningDto>() :
                    JsonSerializer.Deserialize<List<KanjiMeaningDto>>(reader.GetString(8), jsonOptions) ?? new List<KanjiMeaningDto>(),
                Radicals = await reader.IsDBNullAsync(9) ? new List<RadicalSummaryDto>() :
                    JsonSerializer.Deserialize<List<RadicalSummaryDto>>(reader.GetString(9), jsonOptions) ?? new List<RadicalSummaryDto>(),
            });
        }

        return results;
    }

    public async Task<RadicalSearchResultDto> SearchKanjiByRadicalsAsync(List<Guid> radicalIds)
    {
        if (radicalIds == null || !radicalIds.Any())
        {
            return new RadicalSearchResultDto();
        }

        // 1. Find Kanji IDs that match ALL selected radicals
        // We use GROUP BY/HAVING logic for intersection (items that have all specific tags)
        var query = _context.KanjiRadicals
            .Where(kr => radicalIds.Contains(kr.RadicalId))
            .GroupBy(kr => kr.KanjiId)
            .Where(g => g.Count() == radicalIds.Count)
            .Select(g => g.Key);

        // Fetch matching Kanji IDs
        var matchingKanjiIds = await query.ToListAsync();

        if (!matchingKanjiIds.Any()) 
        {
             return new RadicalSearchResultDto();
        }

        // 2. Fetch Kanji Details
        var kanjiResults = await _context.Kanji
            .Where(k => matchingKanjiIds.Contains(k.Id))
            .Select(k => new KanjiSimpleDto
            {
                Id = k.Id,
                Literal = k.Literal,
                StrokeCount = k.StrokeCount
            })
            .OrderBy(k => k.StrokeCount)
            .ToListAsync();

        // 3. Find "Compatible" Radicals
        // Fetch all radical IDs associated with ANY of the matching Kanjis.
        // This tells us which radicals are present in the current result set.
        // When the user selects one of these "compatible" radicals, the result set will be narrowed down further (or stay same).
        // Any radical NOT in this list would yield 0 results if added to the current selection (AND logic).
        
        var compatibleRadicalIds = await _context.KanjiRadicals
            .Where(kr => matchingKanjiIds.Contains(kr.KanjiId))
            .Select(kr => kr.RadicalId)
            .Distinct()
            .ToListAsync();

        return new RadicalSearchResultDto
        {
            Results = kanjiResults,
            CompatibleRadicalIds = compatibleRadicalIds
        };
    }
}