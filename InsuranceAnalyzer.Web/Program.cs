using InsuranceAnalyzer.Application.Policies;
using InsuranceAnalyzer.Infrastructure.Persistence;
using InsuranceAnalyzer.Infrastructure.Policies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var connectionString = builder.Configuration.GetConnectionString("InsuranceAnalyzer")
    ?? throw new InvalidOperationException("Connection string 'InsuranceAnalyzer' was not found.");

builder.Services.AddDbContextFactory<InsuranceAnalyzerDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IPolicyService, PolicyService>();

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

app.Run();
