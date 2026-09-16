using AudiobookConverter.Application.Abstractions;
using AudiobookConverter.Application.Jobs;
using AudiobookConverter.Infrastructure.BackgroundServices;
using AudiobookConverter.Infrastructure.Epub;
using AudiobookConverter.Infrastructure.Persistence;
using AudiobookConverter.Infrastructure.Persistence.Repositories;
using AudiobookConverter.Infrastructure.TextProcessing;
using Microsoft.EntityFrameworkCore;
using AudiobookConverter.Infrastructure.Audio;
using AudiobookConverter.Infrastructure.Tts;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar Banco SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=audiobook.db";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// 2. Registrar Dependências da Aplicação e Infraestrutura
builder.Services.AddScoped<IConversionRepository, ConversionRepository>();
builder.Services.AddSingleton<ConversionQueue>();
builder.Services.AddHostedService<ConversionBackgroundWorker>();

// Registros do Dia 2
builder.Services.AddSingleton<ITextProcessor, TextProcessor>();
builder.Services.AddScoped<IEpubReader, EpubReader>();

// Registros do Dia 3
builder.Services.AddScoped<ITextToSpeechEngine, PiperTtsEngine>();
builder.Services.AddScoped<IAudioProcessor, FfmpegAudioProcessor>();

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

// 3. Aplicar Migrations na inicialização
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// 4. Configurar Arquivos Estáticos (para o Frontend no wwwroot)
app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "Audiobook Converter API v1");
    });
}

// 5. Endpoint de HealthCheck
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

// 6. Endpoint de Teste do Dia 2 - Extração de EPUB (com upload de arquivo)
app.MapPost("/api/test/extract-epub", async (IFormFile file, IEpubReader epubReader, CancellationToken cancellationToken) =>
{
    if (file == null || file.Length == 0)
        return Results.BadRequest("Nenhum arquivo enviado.");

    if (!Path.GetExtension(file.FileName).Equals(".epub", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest("O arquivo precisa ter a extensão .epub.");

    using var stream = file.OpenReadStream();
    var bookContent = await epubReader.ReadAsync(stream, cancellationToken);

    return Results.Ok(new
    {
        bookContent.Title,
        bookContent.Author,
        TotalChapters = bookContent.Chapters.Count,
        ChaptersPreview = bookContent.Chapters.Select(c => new
        {
            c.Order,
            c.Title,
            CharacterCount = c.Text.Length,
            PreviewText = c.Text.Length > 200 ? c.Text[..200] + "..." : c.Text
        })
    });
})
.DisableAntiforgery()
.Accepts<IFormFile>("multipart/form-data")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

// Endpoint de Teste do Dia 3 - Síntese de Voz e Conversão para MP3
app.MapPost("/api/test/tts", async (string text, ITextToSpeechEngine tts, IAudioProcessor audioProcessor, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(text))
        return Results.BadRequest("Informe um texto.");

    var tempWav = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.wav");
    var tempMp3 = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mp3");

    try
    {
        // 1. Gera WAV via Piper (usando o método correto da sua interface)
        await tts.SynthesizeAudioChunkAsync(text, tempWav, cancellationToken);

        // 2. Converte WAV em MP3 via FFmpeg
        await audioProcessor.ConvertWavToMp3Async(tempWav, tempMp3, cancellationToken);

        var bytes = await File.ReadAllBytesAsync(tempMp3, cancellationToken);
        return Results.File(bytes, "audio/mpeg", "teste.mp3");
    }
    finally
    {
        if (File.Exists(tempWav)) File.Delete(tempWav);
        if (File.Exists(tempMp3)) File.Delete(tempMp3);
    }
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

app.Run();