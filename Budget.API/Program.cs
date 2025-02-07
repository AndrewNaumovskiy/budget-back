using System.Text;
using Scalar.AspNetCore;
using Budget.API.Helpers;
using Budget.API.Services;
using Budget.API.Middleware;
using Microsoft.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSingleton<ExpenseService>();

builder.Services.AddTransient<BalanceService>();
builder.Services.AddTransient<IncomeService>();
builder.Services.AddTransient<TransferService>();
builder.Services.AddTransient<TransactionsService>();
builder.Services.AddTransient<CurrencyRateService>();
builder.Services.AddTransient<AuthService>();

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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(""))
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            Console.WriteLine("Authentication failed: " + context.Exception.Message);
                            return Task.CompletedTask;
                        }
                    };
                });

builder.Services.AddAuthorization();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

_ = app.Services.GetService<TelegramBotService>();
_ = app.Services.GetService<ExpenseService>();

app.Run();
