using Scalar.AspNetCore;
using Microsoft.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Budget.API.Helpers;
using Budget.API.Services;
using Budget.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSingleton<ExpenseService>();

builder.Services.AddTransient<BalanceService>();
builder.Services.AddTransient<IncomeService>();
builder.Services.AddTransient<TransferService>();
builder.Services.AddTransient<TransactionsService>();
builder.Services.AddTransient<CurrencyRateService>();

//var lol = new TelegramBotService();
builder.Services.AddSingleton<TelegramBotService>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddPooledDbContextFactory<BudgetDbContext>(options =>
{
    // server=localhost;database=stars;uid=root;password=admin
    var serverVersion = ServerVersion.AutoDetect(connString);
    options.UseMySql(connString, serverVersion);
    options.EnableServiceProviderCaching(false);
}, 2);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder => builder
                                .SetIsOriginAllowed((host) => true)
                                .AllowAnyOrigin()
                                .AllowAnyMethod()
                                .AllowAnyHeader()
                                .WithExposedHeaders(HeaderNames.AccessControlAllowOrigin));
});

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

_ = app.Services.GetService<TelegramBotService>();
_ = app.Services.GetService<ExpenseService>();

app.Run();
