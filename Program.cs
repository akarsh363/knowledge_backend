using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Project_Version1.Data;
using Project_Version1.Helpers;
using Microsoft.OpenApi.Models;
using Project_Version1.Services;
using System.Text;
using Project_Version1.Profiles;
using Project_Version1.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<FnfKnowledgeBaseContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("myCon")));

// AutoMapper
builder.Services.AddAutoMapper(cfg => { cfg.AddMaps(typeof(MappingProfile).Assembly); });

// Helpers & Services
builder.Services.AddScoped<JwtHelper>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<VoteService>(); // Make sure this comes before CommentService
builder.Services.AddScoped<CommentService>(); // Now depends on VoteService
builder.Services.AddScoped<PostService>();
//builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<FileService>();

// SignalR
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, NameIdentifierUserIdProvider>();

// Controllers
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.WriteIndented = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FNF KnowledgeBase API", Version = "v1" });

    // JWT Bearer definition for Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your token.\n\nExample: \"Bearer abc123xyz\""
    });

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
});

// JWT
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(options =>
{
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwt["Issuer"],
        ValidAudience = jwt["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]))
    };

    // Allow access token in query string for SignalR hubs (dev-friendly)
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"].FirstOrDefault();
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/notifications"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// CORS: include Vite dev server origins plus your existing React ports
builder.Services.AddCors(o => o.AddPolicy("dev", p =>
    p.WithOrigins(
            "http://localhost:5173", // Vite default
            "http://localhost:5174", // alternate Vite port if used
            "http://localhost:5175", // another possible dev port
            "http://localhost:3000", // react dev (if used)
            "http://localhost:3001"  // alt
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials()
));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Serve static files (wwwroot) and configure uploads path
var uploadsDir = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "attachments");
app.UseStaticFiles(); // enables serving from wwwroot

// IMPORTANT: UseCors should be applied early so it affects controllers and hubs
app.UseCors("dev");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
