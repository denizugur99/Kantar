using Kantarv2.Consumers;
using Kantarv2.DAL;
using Kantarv2.Entities;
using Kantarv2.Hubs;
using Kantarv2.Middleware;
using Kantarv2.Services;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using StackExchange.Redis;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

builder.Services.AddOpenApi();
builder.Services.AddLogging();
builder.Services.AddMediatR(config => config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
builder.Services.AddHttpClient<ILlmService, LlmService>();
builder.Services.AddDbContext<KantarDbContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Configure Identity (using AddIdentityCore for API with JWT)
builder.Services.AddIdentityCore<User>(options =>
{
    // Password settings
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    // User settings
    options.User.RequireUniqueEmail = true;

    // SignIn settings
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddRoles<IdentityRole<Guid>>()
.AddEntityFrameworkStores<KantarDbContext>()
.AddSignInManager()
.AddDefaultTokenProviders();

builder.Host.UseSerilog((context,LoggerConfiguration)=>
{
    LoggerConfiguration
    .ReadFrom.Configuration(context.Configuration);
});
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ;
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "Kantarv2_";
});
var redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);
builder.Services.AddScoped<ITokenServiceInterface, TokenService>();
builder.Services.AddScoped<IExcelServiceInterface, ExcelService>();
builder.Services.AddScoped<RoleSeeder>();
builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();

// MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<UserCreatedConsumer>();
    x.AddConsumer<ExcelExportConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMqUri = builder.Configuration["RabbitMQ:Uri"]
            ?? throw new ArgumentNullException("RabbitMQ:Uri configuration is missing");

        cfg.Host(new Uri(rabbitMqUri));

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false; // Set to true in production
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["AppSettings:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["AppSettings:Audience"],
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["AppSettings:Token"])),
        ClockSkew = TimeSpan.Zero // Remove default 5 minute clock skew
    };

    // SecurityStamp validation - Token'ın hala geçerli olup olmadığını kontrol et
    options.Events = new JwtBearerEvents
    {
        // SignalR için query string'den token al
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<Kantarv2.Entities.User>>();

            var userIdClaim = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
            {
                context.Fail("Invalid token: User ID claim missing");
                return;
            }

            var user = await userManager.FindByIdAsync(userId.ToString());

            if (user == null || user.IsDeleted)
            {
                context.Fail("User not found or deleted");
                return;
            }

            // SecurityStamp kontrolü - Logout, şifre değişimi vs. durumlarında geçersiz olur
            var securityStampClaim = context.Principal?.FindFirst("security_stamp")?.Value;

            if (string.IsNullOrEmpty(securityStampClaim) || securityStampClaim != user.SecurityStamp)
            {
                context.Fail("Invalid security stamp - Please login again");
                return;
            }
        }
    };
});

builder.Services.AddHttpContextAccessor();

// SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Seed roles on startup
using (var scope = app.Services.CreateScope())
{
    var roleSeeder = scope.ServiceProvider.GetRequiredService<RoleSeeder>();
    await roleSeeder.SeedRolesAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();

// Kullanıcı context'ini loglara ekle (Authentication'dan sonra!)
app.UseMiddleware<UserContextMiddleware>();

app.UseAuthorization();

app.MapControllers();

// SignalR Hub
app.MapHub<ExcelExportHub>("/hubs/excel-export");

app.Run();
