using Backend.Class;
using Backend.DataContext;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

DotNetEnv.Env.Load();

var firebaseJson = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS");

if (string.IsNullOrWhiteSpace(firebaseJson))
{
    throw new Exception("Falta la variable GOOGLE_CREDENTIALS");
}

var credential = GoogleCredential.FromJson(firebaseJson);
FirebaseApp.Create(new AppOptions
{
    Credential = credential
});

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication("Firebase")
    .AddScheme<AuthenticationSchemeOptions, FirebaseAuthenticationHandler>("Firebase", null);

var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .Build();
string? cadenaConexion = configuration.GetConnectionString("mysqlLocal");

//configuración de inyección de dependencias del DBContext
builder.Services.AddDbContext<AgoraContext>(
    dbOptions => dbOptions.UseMySql(
        cadenaConexion,
        ServerVersion.AutoDetect(cadenaConexion),
        mySqlOptions => mySqlOptions
            .EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: System.TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null)
            .EnableStringComparisonTranslations() // Habilita traducción de StringComparison (Contains, StartsWith, etc.)
    )
);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configurar una política de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins",
        builder => builder
            .WithOrigins("https://localhost:8000", "https://agora20.azurewebsites.net")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors("AllowSpecificOrigins");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
