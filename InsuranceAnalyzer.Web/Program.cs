using InsuranceAnalyzer.Application.Policies;
using InsuranceAnalyzer.Infrastructure.Persistence;
using InsuranceAnalyzer.Infrastructure.Policies;
using Microsoft.EntityFrameworkCore;
using InsuranceAnalyzer.Application.Documents;
using InsuranceAnalyzer.Infrastructure.Documents;
using InsuranceAnalyzer.Application.Documents.Extraction;
using InsuranceAnalyzer.Infrastructure.Documents.Extraction;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var connectionString = builder.Configuration.GetConnectionString("InsuranceAnalyzer")
    ?? throw new InvalidOperationException("Connection string 'InsuranceAnalyzer' was not found.");

builder.Services.AddDbContextFactory<InsuranceAnalyzerDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IPolicyService, PolicyService>();
builder.Services.Configure<DocumentStorageOptions>(
    builder.Configuration.GetSection(
        DocumentStorageOptions.SectionName));

builder.Services.AddScoped<IDocumentStorageService,
    LocalDocumentStorageService>();

builder.Services.AddScoped<IPolicyDocumentService,
    PolicyDocumentService>();
builder.Services.AddScoped<IPdfPageOcrService, PaddleOcrPageService>();

builder.Services.AddScoped<HybridPdfTextExtractionService>();
builder.Services.Configure<DocumentQualityValidationOptions>(
    builder.Configuration.GetSection(
        DocumentQualityValidationOptions.SectionName));

builder.Services.AddScoped<
    IDocumentExtractionQualityValidator,
    DocumentExtractionQualityValidator>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<InsuranceAnalyzer.Web.Components.App>()
    .AddInteractiveServerRenderMode();

app.MapGet(
    "/documents/{id:int}/file",
    async (
        int id,
        bool? download,
        IPolicyDocumentService documentService,
        CancellationToken cancellationToken) =>
    {
        var file =
            await documentService.GetFileAsync(
                id,
                cancellationToken);

        if (file is null)
            return Results.NotFound();

        return Results.File(
            file.Content,
            file.ContentType,
            download == true
                ? file.OriginalFileName
                : null,
            enableRangeProcessing: true);
    });

app.Run();
