using System.Text;
using CreditReporting.Api.Data;
using CreditReporting.Api.Metro2;
using CreditReporting.Api.Reports;
using CreditReporting.Api.Reports.Definitions;
using CreditReporting.Api.Repositories;
using CreditReporting.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Repositories
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ICreditReportService, CreditReportService>();

// Report catalog: register each definition here to make it available to users
builder.Services.AddScoped<IReportDefinition, DelinquentAccountsReport>();
builder.Services.AddScoped<IReportCatalog, ReportCatalog>();
builder.Services.AddScoped<ISavedReportService, SavedReportService>();

// Metro 2 module
builder.Services.AddSingleton<IMetro2Writer, Metro2Writer>();
builder.Services.AddSingleton<IMetro2Parser, Metro2Parser>();
builder.Services.AddSingleton<IMetro2Validator, Metro2Validator>();
builder.Services.AddScoped<IMetro2Service, Metro2Service>();

// JWT auth
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!))
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Credit Reporting API",
        Version = "v1",
        Description = "Portfolio demo: customer search, credit reports, and Metro 2 file generation/parsing. All data is synthetic."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT from POST /api/auth/login."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Create and seed the demo database on startup. EnsureCreated is enough for a
// demo; a production system would use migrations.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    DbSeeder.Seed(db);
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
