using KWAcademics.Api.Configuration;
using KWAcademics.Api.Services;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Configure logging for Azure App Service
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddAzureWebAppDiagnostics();

// Add Azure Key Vault if configured
var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];
if (!string.IsNullOrEmpty(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential());
}

// Configure Microsoft Entra External ID authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(options =>
    {
        builder.Configuration.Bind("AzureAd", options);
        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.ValidateAudience = true;

        // Set valid audiences
        var audience = builder.Configuration["AzureAd:Audience"];
        if (!string.IsNullOrEmpty(audience))
        {
            options.TokenValidationParameters.ValidAudiences = new[] {
                audience,
                builder.Configuration["AzureAd:ClientId"] ?? ""
            };
        }
    },
    options =>
    {
        builder.Configuration.Bind("AzureAd", options);
    });

// Add authorization services
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireTtsConvertScope", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("http://schemas.microsoft.com/identity/claims/scope", "tts.convert");
    });
});

// Add services to the container
builder.Services.Configure<AzureSpeechOptions>(
    builder.Configuration.GetSection(AzureSpeechOptions.SectionName));

builder.Services.Configure<AzureAdOptions>(
    builder.Configuration.GetSection(AzureAdOptions.SectionName));

// Add HttpClient for REST API calls
builder.Services.AddHttpClient();

builder.Services.AddSingleton<SpeechSynthesisService>();

builder.Services.AddControllers();

// Add CORS
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "https://localhost:7047", "http://localhost:5197" };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()  // Required for authentication tokens
            .WithExposedHeaders("*");
    });
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
