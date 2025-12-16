using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using JLPTReference.Api.Data;
using JLPTReference.Api.Services.Interfaces;
using JLPTReference.Api.Services.Implementations;
using JLPTReference.Api.Repositories.Interfaces;
using JLPTReference.Api.Repositories.Implementations;
using JLPTReference.Api.Services.Search.Parser;
using JLPTReference.Api.Services.Search.Variants;
using JLPTReference.Api.Services.Search.QueryBuilder;
using JLPTReference.Api.Services.Search.Ranking;
using JLPTReference.Api.Entities.Kanji;
using JLPTReference.Api.Entities.ProperNoun;
using JLPTReference.Api.Entities.Vocabulary;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Swagger/OpenAPI configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "JLPT Reference API",
        Version = "v1"
    });
});

// Add DbContext with pooled factory (allows both direct injection and factory pattern)
builder.Services.AddPooledDbContextFactory<ApplicationDBContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Allow direct DbContext injection (creates context from the pool)
builder.Services.AddScoped<ApplicationDBContext>(sp => 
    sp.GetRequiredService<IDbContextFactory<ApplicationDBContext>>().CreateDbContext());


// Add CORS
builder.Services.AddCors(options => {
    options.AddPolicy("AllowVueApp", policy => {
        var frontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:3000";
        policy.WithOrigins(frontendUrl)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders("X-Total-Count", "X-Page", "X-Page-Size");
    });
});

// Add services
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<ISearchRepository, SearchRepository>();
builder.Services.AddScoped<IKanjiService, KanjiService>();
builder.Services.AddScoped<IKanjiRepository, KanjiRepository>();
builder.Services.AddScoped<IVocabularyService, VocabularyService>();
builder.Services.AddScoped<IVocabularyRepository, VocabularyRepository>();
builder.Services.AddScoped<IProperNounService, ProperNounService>();
builder.Services.AddScoped<IProperNounRepository, ProperNounRepository>();

builder.Services.AddScoped<IQueryParser, QueryParser>();
builder.Services.AddScoped<IVariantGenerator, VariantGenerator>();

// Rankers
builder.Services.AddScoped<IVocabularyRanker, VocabularyRanker>();
builder.Services.AddScoped<IKanjiRanker, KanjiRanker>();
builder.Services.AddScoped<IProperNounRanker, ProperNounRanker>();

// Query builders (basic)
builder.Services.AddScoped<ISearchQueryBuilder<Kanji>, EfCoreKanjiQueryBuilder>();
builder.Services.AddScoped<ISearchQueryBuilder<ProperNoun>, EfCoreProperNounQueryBuilder>();
builder.Services.AddScoped<ISearchQueryBuilder<Vocabulary>, EfCoreVocabularyQueryBuilder>();

// Ranked query builders
builder.Services.AddScoped<IRankedQueryBuilder<Vocabulary, VocabularyRankingProfile>, EfCoreVocabularyQueryBuilder>();
builder.Services.AddScoped<IRankedQueryBuilder<Kanji, KanjiRankingProfile>, EfCoreKanjiQueryBuilder>();
builder.Services.AddScoped<IRankedQueryBuilder<ProperNoun, ProperNounRankingProfile>, EfCoreProperNounQueryBuilder>();

// Ranking profiles
builder.Services.AddSingleton(VocabularyRankingProfile.Default);
builder.Services.AddSingleton(KanjiRankingProfile.Default);
builder.Services.AddSingleton(ProperNounRankingProfile.Default);

// Search services - switch between EF Core and SQL implementations
// Set "Search:UseSqlSearch" to true in appsettings to use the optimized SQL functions
var useSqlSearch = builder.Configuration.GetValue<bool>("Search:UseSqlSearch", false);
if (useSqlSearch)
{
    builder.Services.AddScoped<IVocabularySearchService, SqlVocabularySearchService>();
    builder.Services.AddScoped<IKanjiSearchService, SqlKanjiSearchService>();
    builder.Services.AddScoped<IProperNounSearchService, SqlProperNounSearchService>();
}
else
{
    builder.Services.AddScoped<IVocabularySearchService, EfCoreVocabularySearchService>();
    builder.Services.AddScoped<IKanjiSearchService, EfCoreKanjiSearchService>();
    builder.Services.AddScoped<IProperNounSearchService, EfCoreProperNounSearchService>();
}

var app = builder.Build();

Log.Init(app.Services.GetRequiredService<ILoggerFactory>());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "JLPT Reference API v1");
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowVueApp");

app.UseAuthorization();

app.MapControllers();

app.Run();



