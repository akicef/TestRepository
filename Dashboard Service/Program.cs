using Dashboard_Service.Repositories;
using Dashboard_Service.Security;
using Dashboard_Service.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Npgsql;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

// Add Repositories and Services
builder.Services.AddScoped<INoteService, NoteService>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddScoped<INoteRepository>(_ => new NoteRepository(connectionString));
builder.Services.AddScoped<IDashboardSummaryRepository>(_ => new DashboardSummaryRepository(connectionString));
builder.Services.AddScoped<IDashboardSummaryService, DashboardSummaryService>();
builder.Services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Secret"];

if (!string.IsNullOrEmpty(secretKey))
{
    var key = Encoding.ASCII.GetBytes(secretKey);

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                var result = System.Text.Json.JsonSerializer.Serialize(new { message = "Unauthorized" });
                return context.Response.WriteAsync(result);
            }
        };
    });

    // Add Authorization policies
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AuthenticatedOnly", policy =>
            policy.RequireAuthenticatedUser());
    });
}

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    if (!string.IsNullOrEmpty(secretKey))
    {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme.\n\n" +
                         "Enter 'Bearer' [space] and then your token in the text input below.\n\n" +
                         "Example: 'Bearer eyJhbGciOiJIUzI1NiIs...'",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    }
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();
// Run database migrations
await RunMigrationsAsync(app.Configuration);
// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dashboard Service API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Add authentication and authorization middleware (if JWT is configured)
if (!string.IsNullOrEmpty(secretKey))
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapControllers();

app.Run();
static async Task RunMigrationsAsync(IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    using var db = new NpgsqlConnection(connectionString);
    await db.OpenAsync();

    var sql = @"
        CREATE TABLE IF NOT EXISTS notes (
            id BIGSERIAL PRIMARY KEY,
            title VARCHAR(255) NOT NULL,
            text TEXT,
            editable BOOLEAN NOT NULL DEFAULT false,
            user_id BIGINT NOT NULL,
            creator_id BIGINT NOT NULL,
            last_modified TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
            disabled BOOLEAN NOT NULL DEFAULT false,
            created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP

        );

        CREATE INDEX IF NOT EXISTS idx_notes_user_id ON notes(user_id);
        CREATE INDEX IF NOT EXISTS idx_notes_creator_id ON notes(creator_id);
        CREATE INDEX IF NOT EXISTS idx_notes_last_modified ON notes(last_modified DESC);
        CREATE INDEX IF NOT EXISTS idx_notes_disabled ON notes(disabled);
    ";

    await db.ExecuteAsync(sql);
}