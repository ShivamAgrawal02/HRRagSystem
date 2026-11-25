using HRRagSystem.Database;
using HRRagSystem.Models;
using HRRagSystem.Services.Implementation;
using HRRagSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
// Registering services
builder.Services.AddScoped<IDocumentScanner, DocumentScanner>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IChangeDetector, ChangeDetector>();
builder.Services.AddScoped<IIngestionService, IngestionService>();
builder.Services.AddScoped<IPdfReader, PdfReader>();
builder.Services.AddScoped<ITextCleaner, TextCleaner>();
builder.Services.AddSingleton<IEmbeddingGenerator, EmbeddingGenerator>();
builder.Services.AddScoped<IChunker, ChunkerUsingTextChunker>();


//Adding DbContext 
var SQLiteConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<HRRagSystemSQLiteDbContext>(options =>
    options.UseSqlite(SQLiteConnectionString));

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();


app.Run();
