using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using JLPTReference.Api.Data;
using JLPTReference.Api.Services.Interfaces;
using JLPTReference.Api.Services.Implementations;
using JLPTReference.Api.Repositories.Interfaces;
using JLPTReference.Api.Repositories.Implementations;

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

// Add DbContext
builder.Services.AddDbContext<ApplicationDBContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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

var app = builder.Build();

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



