using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using RussianNationalMessengerServer.Models;
using RussianNationalMessengerServer.Services;
using RussianNationalMessengerServer.Services.AmazonService;
using RussianNationalMessengerServer.Services.ChatHub;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

//автоматически описывает все Endpoints
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(x =>
{
    //добавляем возможность авторизации прямо через Swagger
    x.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Авторизация",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    x.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

//подключаем БД (контекст)
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));

builder.Services.AddSingleton<MongoService>();
builder.Services.AddScoped<IAuthService, AuthService>();


builder.Services.Configure<AmazonConfig>(builder.Configuration.GetSection("AmazonS3Client"));

//подкючаем сервера амазона для файлов
builder.Services.AddSingleton<IAmazonS3>(serviceProvider =>
{
    IOptions<AmazonConfig> options = serviceProvider.GetRequiredService<IOptions<AmazonConfig>>();
    AmazonConfig config = options.Value;

    return new AmazonS3Client(config.KeyID, config.ApplicationKey, 
        new AmazonS3Config()
        {
            ServiceURL = config.ClientConfig.ServiceURL,
            ForcePathStyle = true
        }
    );
});

// Сервис файлов
builder.Services.AddScoped<IFileServices, FileServices>();


var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

ArgumentNullException.ThrowIfNull(jwtSettings, nameof(jwtSettings));

var key = Encoding.UTF8.GetBytes(jwtSettings.Key);
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    x.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/rnmhub"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddSignalR();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseExceptionHandler();

//аутентификация - кто зашёл?
app.UseAuthentication();
//авторизация - какие у него права?
app.UseAuthorization();

app.MapControllers();

app.MapHub<RNMHub>("/rnmhub");

app.Run();
