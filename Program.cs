//using System.Text;
//using cvAts;
//using cvAts.Services;
//using Microsoft.AspNetCore.Authentication.Cookies;
//using Microsoft.AspNetCore.Authentication.Google;
//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.AspNetCore.HttpOverrides;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Options;
//using Microsoft.IdentityModel.Tokens;
//using Microsoft.OpenApi.Models;


//var builder = WebApplication.CreateBuilder(args);

//var emailService = new EmailService(
//    smtpServer: "smtp.gmail.com",   // SMTP сервер Gmail
//    port: 465,                      // SSL порт
//    fromEmail: "karimovysh@gmail.com",
//    password: "vytp kqib fprc hrgz"   // не обычный пароль, а App Password!
//);

//builder.Services.AddSingleton(emailService);

//var jwtSecret = builder.Configuration["Jwt:Key"]
//                ?? "super_long_secret_key_with_32+chars!";


////builder.Services.Configure<CookiePolicyOptions>(options =>
////{
////    options.Secure = CookieSecurePolicy.Always;
////});

//// Регистрируем JwtService (DI)
//builder.Services.AddSingleton(new JwtService(jwtSecret));

//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme; // для OAuth-сессий
//    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;      // чтобы при вызове Challenge() шёл Google
//})
//.AddCookie(options =>
//{
//    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // cookie через HTTPS
//    options.Cookie.SameSite = SameSiteMode.Lax;
//}) // обязательно для хранения state
//.AddGoogle(googleOptions =>
//{
//    googleOptions.ClientId = builder.Configuration["Google:ClientId"];
//    googleOptions.ClientSecret = builder.Configuration["Google:ClientSecret"];
//    googleOptions.CallbackPath = "/signin-google"; // должен совпадать с Google Console
//})
//.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
//{
//    var jwtSecret = builder.Configuration["Jwt:Secret"];
//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuer = false,
//        ValidateAudience = false,
//        ValidateLifetime = true,
//        ValidateIssuerSigningKey = true,
//        IssuerSigningKey = new SymmetricSecurityKey(
//            Encoding.UTF8.GetBytes(jwtSecret))
//    };
//});

//builder.Services.Configure<CookiePolicyOptions>(options =>
//{
//    options.Secure = CookieSecurePolicy.Always;
//});
//builder.Services.AddMemoryCache();
//builder.Services.AddScoped<TempStorageService>();


//builder.Services.AddAuthorization();
//// jwt service

//// Add services to the container.

//builder.Services.AddControllers();



//// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen(c =>
//{
//    c.SwaggerDoc("v1", new() { Title = "My API", Version = "v1" });

//    // Настройка JWT Bearer
//    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//    {
//        Description = "JWT Authorization header. Введите 'Bearer {token}'",
//        Name = "Authorization",
//        In = ParameterLocation.Header,
//        Type = SecuritySchemeType.ApiKey,
//        Scheme = "Bearer"
//    });

//    c.AddSecurityRequirement(new OpenApiSecurityRequirement{
//        {
//            new OpenApiSecurityScheme{
//                Reference = new OpenApiReference{
//                    Type = ReferenceType.SecurityScheme,
//                    Id = "Bearer"
//                }
//            },
//            Array.Empty<string>()
//        }
//    });
//});
////builder.Services.AddSwaggerGen();

//builder.Services.AddDbContext<AppDbContext>(options =>
//    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


////var ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

////builder.Services.AddDbContext<AppDbContext>(options =>
////    options.UseNpgsql(ConnectionString).LogTo(Console.WriteLine, LogLevel.Information));
////builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(ConnectionString));

//var app = builder.Build();

//app.MapGet("/test-connection-with-database", async (AppDbContext db) =>
//{
//    try
//    {
//        var canConnect = await db.Database.CanConnectAsync();
//        return canConnect
//            ? Results.Ok("✅ Connection Successfully")
//            : Results.Problem("❌ CanConnect вернул false (но соединение не упало).");
//    }
//    catch (Exception ex)
//    {
//        return Results.Problem("❌ Connection error: " + ex.Message);
//    }
//});


//if (app.Environment.IsDevelopment())
//{
//    app.UseDeveloperExceptionPage(); // Показывает полную ошибку 500 в браузере для отладки
//}

//app.UseForwardedHeaders(new ForwardedHeadersOptions
//{
//    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
//});

//app.UseAuthentication(); // ✅ обязательно
//app.UseAuthorization();

//app.UseSwagger();
//app.UseSwaggerUI();

//app.UseHttpsRedirection();

//app.MapControllers();

//app.Run();

using System.Text;
using cvAts;

using cvAts.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);


// Email service
var emailService = new EmailService(
    smtpServer: "smtp.gmail.com",
    port: 465,
    fromEmail: "karimovysh@gmail.com",
    password: "vytp kqib fprc hrgz"
);
builder.Services.AddSingleton(emailService);

// JWT secret
var jwtSecret = builder.Configuration["Jwt:Key"] ?? "super_long_secret_key_with_32+chars!";
builder.Services.AddSingleton(new JwtService(jwtSecret));

// Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
})
.AddGoogle(googleOptions =>
{
    googleOptions.ClientId = builder.Configuration["Google:ClientId"];
    googleOptions.ClientSecret = builder.Configuration["Google:ClientSecret"];
    googleOptions.CallbackPath = "/signin-google";
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSecret))
    };
});

// Forwarded headers (важно для Nginx + OAuth)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthorization();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<TempStorageService>();
builder.Services.AddScoped<CoverLetterService>();
builder.Services.AddScoped<GroqCoverLetterService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddHttpClient<GroqServiceForAtsCheck>();
builder.Services.AddSingleton<CvStorageService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<AuditService>();

builder.Services.AddHttpClient<GeminiCoverLetterService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});
// Регистрируем сервис через фабрику



builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "My API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Use 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement{
        {
            new OpenApiSecurityScheme{
                Reference = new OpenApiReference{
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddHttpClient();

//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowMakeCV",
//        policy => policy
//            .WithOrigins("https://makecv.pro")
//            .AllowAnyHeader()
//            .AllowAnyMethod());
//});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://makecv.pro", "http://makecv.pro")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});


var app = builder.Build();

app.UseCors();
// Developer exception page (для отладки 500 ошибок)
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Forwarded headers middleware (для правильного scheme)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    // Важно, если Nginx на другом сервере
    RequireHeaderSymmetry = false
});
// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// HTTPS redirection
app.UseHttpsRedirection();

app.MapControllers();

// Test DB connection
app.MapGet("/test-connection-with-database", async (AppDbContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        return canConnect
            ? Results.Ok("✅ Connection Successfully")
            : Results.Problem("❌ CanConnect вернул false");
    }
    catch (Exception ex)
    {
        return Results.Problem("❌ Connection error: " + ex.Message);
    }
});


//app.UseCors("AllowMakeCV");



app.Run();
