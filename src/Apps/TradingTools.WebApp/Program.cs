using MudBlazor.Services;
using TradingTools.Assistant.Application.Queries.AskAssistant;
using TradingTools.Infrastructure.Claude.IoC;
using TradingTools.MarketData.Application.Queries.GetDashboardPrices;
using TradingTools.Infrastructure.Binance.IoC;
using TradingTools.WebApp.Components;
using TradingTools.Infrastructure.FileStorage.IoC;
using TradingTools.StrategyEvaluation.Application;
using TradingTools.StrategyEvaluation.Application.Commands.RunStrategy;
using TradingTools.StrategyEvaluation.Application.Queries;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<GetDashboardPricesQueryHandler>();
builder.Services.RegisterBinanceApi(builder.Configuration);
builder.Services.AddScoped<AskAssistantQueryHandler>();
builder.Services.RegisterClaudeApi(builder.Configuration);
builder.Services.RegisterStrategyRunStorage();
builder.Services.AddSingleton<StrategySimulator>();
builder.Services.AddScoped<RunStrategyCommandHandler>();
builder.Services.AddScoped<GetRunHistoryQueryHandler>();
builder.Services.AddScoped<GetStrategyRunQueryHandler>();
builder.Services.AddScoped<CompareRunsQueryHandler>();

// Add MudBlazor services
builder.Services.AddMudServices();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
