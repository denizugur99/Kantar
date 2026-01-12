using Kantarv2.DAL;
using Kantarv2.Entities;
using Kantarv2.Middleware;
using Kantarv2.Services;
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
.AddRoles<IdentityRole<int>>()
.AddEntityFrameworkStores<KantarDbContext>()
.AddSignInManager()
.AddDefaultTokenProviders();

builder.Host.UseSerilog((context,LoggerConfiguration)=>
{
    LoggerConfiguration
    .ReadFrom.Configuration(context.Configuration);
});
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration ="localhost:6379";
    options.InstanceName = "Kantarv2_";
});
var redisConnection = ConnectionMultiplexer.Connect("localhost:6379");
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);
builder.Services.AddScoped<ITokenServiceInterface, TokenService>();
builder.Services.AddScoped<IExcelServiceInterface, ExcelService>();
builder.Services.AddScoped<RoleSeeder>();
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
        OnTokenValidated = async context =>
        {
            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<Kantarv2.Entities.User>>();

            var userIdClaim = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
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

app.Run();
