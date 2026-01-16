using JLPTReference.Api.DTOs.Radical;
using JLPTReference.Api.Data;
using Microsoft.EntityFrameworkCore;
using JLPTReference.Api.Repositories.Interfaces;
using JLPTReference.Api.DTOs.Kanji;
using System.Text.Json;

namespace JLPTReference.Api.Repositories.Implementations;

public class RadicalRepository : IRadicalRepository
{
    private readonly IDbContextFactory<ApplicationDBContext> _contextFactory;

    public RadicalRepository(IDbContextFactory<ApplicationDBContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<RadicalSummaryDto>> GetRadicalsListAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Radicals
            .Select(r => new RadicalSummaryDto
            {
                Id = r.Id,
                Literal = r.Literal,
                StrokeCount = r.StrokeCount
            })
            .ToListAsync();
    }

    public async Task<RadicalDetailDto?> GetRadicalByLiteralAsync(string literal)
    {
        await using var mainContext = await _contextFactory.CreateDbContextAsync();
        
        // 1. Find the member (variant) matching the input literal
        var member = await mainContext.RadicalGroupMembers
            .Include(m => m.Group)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Literal == literal);

        if (member == null)
            return null;

        var group = member.Group;

        // 2. Find main source radical and all members using separate contexts for parallel execution
        var mainSourceRadicalTask = Task.Run(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.Radicals.Where(r => r.GroupId == group.Id).AsNoTracking()
                .OrderByDescending(r => r.Literal == group.CanonicalLiteral).FirstOrDefaultAsync();
        });

        var allMembersTask = Task.Run(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.RadicalGroupMembers.Where(m => m.GroupId == group.Id).AsNoTracking().ToListAsync();
        });

        await Task.WhenAll(mainSourceRadicalTask, allMembersTask);

        var mainSourceRadical = mainSourceRadicalTask.Result;
        var allMembers = allMembersTask.Result;

        // 3. Batch lookup: get all kanji for all radical literals in a single query
        var allLiterals = allMembers.Select(m => m.Literal).ToArray();
        var allKanjiByLiteral = await GetKanjisForMultipleLiteralsAsync(allLiterals);

        // 4. Build variant DTOs with pre-fetched kanji
        var variantsDtos = new List<RadicalGroupMemberDto>();
        var allKanjiForGroup = new HashSet<Guid>();
        var topLevelKanjiList = new List<KanjiSummaryDto>();

        foreach (var m in allMembers)
        {
            var kanji = allKanjiByLiteral.TryGetValue(m.Literal, out var list) ? list : new List<KanjiSummaryDto>();
            variantsDtos.Add(new RadicalGroupMemberDto
            {
                Id = m.Id,
                Literal = m.Literal,
                Kanji = kanji.Take(10).ToList()
            });

            foreach (var k in kanji)
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

    /// <summary>
    /// Batch lookup: gets kanjis for multiple radical literals in a single SQL call.
    /// </summary>
    private async Task<Dictionary<string, List<KanjiSummaryDto>>> GetKanjisForMultipleLiteralsAsync(string[] literals)
    {
        if (literals.Length == 0)
            return new Dictionary<string, List<KanjiSummaryDto>>();

        var results = new Dictionary<string, List<KanjiSummaryDto>>();
        
        var sql = @"
            SELECT 
                r.literal as literal_key,
                k.id,
                k.literal,
                k.grade,
                k.stroke_count,
                k.frequency,
                k.jlpt_level_new as jlpt_level,
                (SELECT COALESCE(json_agg(json_build_object('type', kr.type, 'value', kr.value, 'status', kr.status, 'onType', kr.on_type) ORDER BY kr.id), '[]'::json)
                 FROM jlpt.kanji_reading kr WHERE kr.kanji_id = k.id AND kr.type = 'ja_kun') AS kunyomi_readings,
                (SELECT COALESCE(json_agg(json_build_object('type', kr.type, 'value', kr.value, 'status', kr.status, 'onType', kr.on_type) ORDER BY kr.id), '[]'::json)
                 FROM jlpt.kanji_reading kr WHERE kr.kanji_id = k.id AND kr.type = 'ja_on') AS onyomi_readings,
                (SELECT COALESCE(json_agg(json_build_object('language', km.lang, 'meaning', km.value) ORDER BY km.id), '[]'::json)
                 FROM jlpt.kanji_meaning km WHERE km.kanji_id = k.id) AS meanings,
                (SELECT COALESCE(json_agg(json_build_object('id', rad.id, 'literal', rad.literal) ORDER BY krad.id), '[]'::json)
                 FROM jlpt.kanji_radical krad JOIN jlpt.radical rad ON krad.radical_id = rad.id WHERE krad.kanji_id = k.id) AS radicals
            FROM unnest(@literals) as lit(l)
            JOIN jlpt.radical r ON r.literal = lit.l
            JOIN jlpt.kanji_radical kr ON kr.radical_id = r.id
            JOIN jlpt.kanji k ON k.id = kr.kanji_id
            ORDER BY k.frequency NULLS LAST, k.jlpt_level_new NULLS LAST, k.grade NULLS LAST, k.id";

        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        
        var param = command.CreateParameter();
        param.ParameterName = "@literals";
        param.Value = literals;
        command.Parameters.Add(param);

        await context.Database.OpenConnectionAsync();
        
        try
        {
            await using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var literalKey = reader.GetString(0);
                var dto = new KanjiSummaryDto
                {
                    Id = reader.GetGuid(1),
                    Literal = reader.GetString(2),
                    Grade = await reader.IsDBNullAsync(3) ? null : reader.GetInt32(3),
                    StrokeCount = reader.GetInt32(4),
                    Frequency = await reader.IsDBNullAsync(5) ? null : reader.GetInt32(5),
                    JlptLevel = await reader.IsDBNullAsync(6) ? null : reader.GetInt32(6),
                    KunyomiReadings = await reader.IsDBNullAsync(7) ? new List<KanjiReadingDto>() :
                        JsonSerializer.Deserialize<List<KanjiReadingDto>>(reader.GetString(7), jsonOptions) ?? new List<KanjiReadingDto>(),
                    OnyomiReadings = await reader.IsDBNullAsync(8) ? new List<KanjiReadingDto>() :
                        JsonSerializer.Deserialize<List<KanjiReadingDto>>(reader.GetString(8), jsonOptions) ?? new List<KanjiReadingDto>(),
                    Meanings = await reader.IsDBNullAsync(9) ? new List<KanjiMeaningDto>() :
                        JsonSerializer.Deserialize<List<KanjiMeaningDto>>(reader.GetString(9), jsonOptions) ?? new List<KanjiMeaningDto>(),
                    Radicals = await reader.IsDBNullAsync(10) ? new List<RadicalSummaryDto>() :
                        JsonSerializer.Deserialize<List<RadicalSummaryDto>>(reader.GetString(10), jsonOptions) ?? new List<RadicalSummaryDto>(),
                };

                if (!results.ContainsKey(literalKey))
                    results[literalKey] = new List<KanjiSummaryDto>();
                results[literalKey].Add(dto);
            }
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }

        return results;
    }

    public async Task<RadicalSearchResultDto> SearchKanjiByRadicalsAsync(List<Guid> radicalIds)
    {
        if (radicalIds == null || !radicalIds.Any())
            return new RadicalSearchResultDto();

        await using var context = await _contextFactory.CreateDbContextAsync();
        
        // 1. Find Kanji IDs that match ALL selected radicals
        var matchingKanjiIds = await context.KanjiRadicals
            .Where(kr => radicalIds.Contains(kr.RadicalId))
            .GroupBy(kr => kr.KanjiId)
            .Where(g => g.Count() == radicalIds.Count)
            .Select(g => g.Key)
            .ToListAsync();

        if (!matchingKanjiIds.Any())
            return new RadicalSearchResultDto();

        // 2. Fetch Kanji Details and compatible radicals using separate contexts for parallel
        var kanjiResultsTask = Task.Run(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.Kanji.Where(k => matchingKanjiIds.Contains(k.Id))
                .Select(k => new KanjiSimpleDto { Id = k.Id, Literal = k.Literal, StrokeCount = k.StrokeCount })
                .OrderBy(k => k.StrokeCount).ToListAsync();
        });

        var compatibleRadicalIdsTask = Task.Run(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            return await ctx.KanjiRadicals.Where(kr => matchingKanjiIds.Contains(kr.KanjiId))
                .Select(kr => kr.RadicalId).Distinct().ToListAsync();
        });

        await Task.WhenAll(kanjiResultsTask, compatibleRadicalIdsTask);

        return new RadicalSearchResultDto
        {
            Results = kanjiResultsTask.Result,
            CompatibleRadicalIds = compatibleRadicalIdsTask.Result
        };
    }
}