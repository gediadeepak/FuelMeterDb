using System.Text;
using FuelMeter.Api.Options;
using FuelMeter.Api.Services;
using FuelMeter.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ── Config ─────────────────────────────────────────────────────
var jwtSection  = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtKey      = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key is missing.");
var jwtIssuer   = jwtSection["Issuer"]!;
var jwtAudience = jwtSection["Audience"]!;

builder.Services.Configure<JwtOptions>(jwtSection);

// ── Data + business services ───────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("FuelMeterDb")
    ?? throw new InvalidOperationException("FuelMeterDb connection string is missing.");

builder.Services.AddFuelMeterData(connectionString, builder.Configuration);

// ── JWT token generator ────────────────────────────────────────
builder.Services.AddSingleton<JwtTokenService>();

// ── JWT bearer authentication ──────────────────────────────────
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtIssuer,
            ValidAudience            = jwtAudience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// ── OpenAPI / Scalar ──────────────────────────────────────────
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info = new OpenApiInfo
        {
            Title       = "FuelMeter API",
            Version     = "v1",
            Description = "JWT-secured REST API for the FuelMeter application."
        };

        // Register the Bearer security scheme
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type         = SecuritySchemeType.Http,
            Scheme       = "bearer",
            BearerFormat = "JWT",
            Description  = "Enter your JWT token. Obtain it from POST /api/auth/login."
        };

        // Attach the Bearer requirement to every operation
        if (document.Paths is not null)
        {
            foreach (var path in document.Paths.Values)
            {
                foreach (var operation in path.Operations!.Values)
                {
                    operation.Security ??= [];
                    operation.Security.Add(new OpenApiSecurityRequirement
                    {
                        // v2 constructor: (id, owning document, externalResource)
                        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                    });
                }
            }
        }

        return Task.CompletedTask;
    });
});

// ── CORS (Blazor web + MAUI Android) ──────────────────────────
builder.Services.AddCors(options =>
    options.AddPolicy("BlazorClient", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

// ── OpenAPI + Scalar UI ───────────────────────────────────────
// Serves the raw OpenAPI JSON at /openapi/v1.json
app.MapOpenApi();

// Scalar UI at /scalar/v1
app.MapScalarApiReference(options =>
{
    options.Title             = "FuelMeter API";
    options.Theme             = ScalarTheme.DeepSpace;
    options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
    options.AddPreferredSecuritySchemes("Bearer");
});

app.UseCors("BlazorClient");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
