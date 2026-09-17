using AudiobookConverter.Application.Abstractions;
using AudiobookConverter.Application.Jobs;
using AudiobookConverter.Application.Services;
using AudiobookConverter.Domain.Entities;
using AudiobookConverter.Domain.Enums;
using AudiobookConverter.Infrastructure.Audio;
using AudiobookConverter.Infrastructure.BackgroundServices;
using AudiobookConverter.Infrastructure.Epub;
using AudiobookConverter.Infrastructure.Persistence;
using AudiobookConverter.Infrastructure.Persistence.Repositories;
using AudiobookConverter.Infrastructure.FileStorage;
using AudiobookConverter.Infrastructure.TextProcessing;
using AudiobookConverter.Infrastructure.Tts;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar Banco SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=audiobook.db";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// 2. Registros de Serviços
builder.Services.AddScoped<IConversionRepository, ConversionRepository>();
builder.Services.AddSingleton<ConversionQueue>();
builder.Services.AddHostedService<ConversionBackgroundWorker>();

builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();
builder.Services.AddSingleton<ITextProcessor, TextProcessor>();
builder.Services.AddSingleton<IChunkGenerator, ChunkGenerator>();
builder.Services.AddScoped<IEpubReader, EpubReader>();

builder.Services.AddScoped<ITextToSpeechEngine, PiperTtsEngine>();
builder.Services.AddScoped<IAudioProcessor, FfmpegAudioProcessor>();
builder.Services.AddScoped<ConversionService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Audiobook Converter API",
        Version = "v1"
    });
});

var app = builder.Build();

// 3. Migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// 4. Arquivos Estáticos
app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Audiobook Converter API v1");
    });
}

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

// Endpoints da Aplicação (Dia 5)

// POST: Criar e enfileirar conversão
app.MapPost("/api/conversions", async (
    IFormFile file,
    IFileStorage fileStorage,
    IConversionRepository repository,
    ConversionQueue queue,
    CancellationToken cancellationToken) =>
{
    if (file is null || file.Length == 0)
        return Results.BadRequest("Nenhum arquivo enviado.");

    if (!Path.GetExtension(file.FileName).Equals(".epub", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest("O arquivo precisa ter a extensão .epub.");

    var conversion = new Conversion(file.FileName, string.Empty);
    var inputPath = fileStorage.GetInputPath(conversion.Id, file.FileName);

    await using (var stream = File.Create(inputPath))
    {
        await file.CopyToAsync(stream, cancellationToken);
    }

    conversion.SetStoredFilePath(inputPath);

    await repository.AddAsync(conversion, cancellationToken);
    await queue.QueueConversionAsync(conversion.Id);

    return Results.Ok(new { id = conversion.Id, status = conversion.Status.ToString() });
})
.DisableAntiforgery()
.Accepts<IFormFile>("multipart/form-data");

// GET: Consultar status e progresso
app.MapGet("/api/conversions/{id:guid}", async (Guid id, IConversionRepository repository, CancellationToken cancellationToken) =>
{
    var conversion = await repository.GetByIdAsync(id, cancellationToken);
    if (conversion is null) return Results.NotFound();

    return Results.Ok(new
    {
        id = conversion.Id,
        status = conversion.Status.ToString(),
        progress = conversion.Progress,
        errorMessage = conversion.ErrorMessage,
        downloadUrl = conversion.Status == ConversionStatus.Completed
            ? $"/api/conversions/{conversion.Id}/download"
            : null
    });
});

// GET: Download do MP3 gerado
app.MapGet("/api/conversions/{id:guid}/download", async (Guid id, IConversionRepository repository, CancellationToken cancellationToken) =>
{
    var conversion = await repository.GetByIdAsync(id, cancellationToken);
    if (conversion is null || conversion.Status != ConversionStatus.Completed || conversion.OutputFilePath is null)
        return Results.NotFound();

    if (!File.Exists(conversion.OutputFilePath))
        return Results.NotFound("Arquivo MP3 não encontrado.");

    var bytes = await File.ReadAllBytesAsync(conversion.OutputFilePath, cancellationToken);
    var fileName = Path.GetFileNameWithoutExtension(conversion.OriginalFileName) + ".mp3";
    return Results.File(bytes, "audio/mpeg", fileName);
});

app.Run();