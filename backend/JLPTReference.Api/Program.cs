using Npgsql;
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
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var dataSourceBuilder = new NpgsqlDataSourceBuilder(
    builder.Configuration.GetConnectionString("DefaultConnection")
);
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();

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

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
    
    // Resolve duplicate schema IDs by using full type names
    options.CustomSchemaIds(x => x.FullName);
});

// Add DbContext with pooled factory (allows both direct injection and factory pattern)
builder.Services.AddPooledDbContextFactory<ApplicationDBContext>(options =>
    options.UseNpgsql(dataSource));

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
builder.Services.AddScoped<IRadicalService, RadicalService>();
builder.Services.AddScoped<IRadicalRepository, RadicalRepository>();

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


// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("fixed", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
});

var app = builder.Build();

Log.Init(app.Services.GetRequiredService<ILoggerFactory>());

app.UseHttpsRedirection();

// Security Headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.UseStaticFiles();
app.UseCors("AllowVueApp");
app.UseRateLimiter();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "JLPT Reference API v1");
    });
}

app.UseAuthorization();

app.MapControllers().RequireRateLimiting("fixed");

app.Run();



