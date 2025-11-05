using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ServiceRequestAPI.Auth;
using ServiceRequestAPI.Data;
using ServiceRequestAPI.Profiles;
using ServiceRequestAPI.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ------------------------
// Configure Serilog
// ------------------------
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ------------------------
// Add Services
// ------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ------------------------
// Swagger Configuration
// ------------------------
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Service Request Management API",
        Version = "v1.0.0",
        Description = "A REST API for managing service requests with JWT authentication.",
        Contact = new OpenApiContact
        {
            Name = "Development Team",
            Email = "support@servicerequest.com",
            Url = new Uri("https://www.example.com")
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // JWT Bearer Security Definition
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid JWT token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });

    // Security Requirement for all endpoints
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

    // Enable XML Comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Enable Annotations
    c.EnableAnnotations();
});

// ------------------------
// MySQL Database
// ------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// ------------------------
// JWT Authentication
// ------------------------
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(secretKey),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// ------------------------
// AutoMapper
// ------------------------
builder.Services.AddAutoMapper(typeof(MappingProfile));

// ------------------------
// Custom Services
// ------------------------
builder.Services.AddScoped<IServiceRequestService, ServiceRequestService>();
builder.Services.AddScoped<JwtTokenHandler>();

// ------------------------
// CORS
// ------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ------------------------
// Build App
// ------------------------
var app = builder.Build();

// ------------------------
// Middleware Pipeline
// ------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Service Request API v1");
        c.RoutePrefix = string.Empty; // Swagger as default page
        c.DocumentTitle = "Service Request Management API";
        c.DefaultModelsExpandDepth(2);
        c.DefaultModelExpandDepth(2);
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
