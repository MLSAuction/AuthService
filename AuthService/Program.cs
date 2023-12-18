using AuthService.Repositories;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.Commons;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using System.Reflection;
using NLog.Web;
using NLog;

var logger = NLog.LogManager.Setup().LoadConfigurationFromFile("NLog.config").GetCurrentClassLogger();
logger.Debug("Starting Auth Service");
Console.WriteLine("Initiating Auth Service");

var builder = WebApplication.CreateBuilder(args);

#region Configuration

var vaultUrl = Environment.GetEnvironmentVariable("vaultUrl");

if (string.IsNullOrEmpty(vaultUrl)) //azure flow
{
    // need Salt, jwtSecret, jwtIssuer and LokiEndpoint, UserService in bicep env variables
}
else //compose flow
{
    var httpClientHandler = new HttpClientHandler();
    httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => { return true; };

    IAuthMethodInfo authMethod = new TokenAuthMethodInfo("00000000-0000-0000-0000-000000000000"); //undersøg om er korrekt

    var vaultClientSettings = new VaultClientSettings(vaultUrl, authMethod)
    {
        Namespace = "",
        MyHttpClientProviderFunc = handler => new HttpClient(httpClientHandler) { BaseAddress = new Uri(vaultUrl) }
    };

    IVaultClient vaultClient = new VaultClient(vaultClientSettings);

    Secret<SecretData> vaultSecret = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path: "Secrets", mountPoint: "secret");

    Environment.SetEnvironmentVariable("jwtSecret", vaultSecret.Data.Data["jwtSecret"].ToString());
    Environment.SetEnvironmentVariable("jwtIssuer", vaultSecret.Data.Data["jwtIssuer"].ToString());
    Environment.SetEnvironmentVariable("Salt", vaultSecret.Data.Data["Salt"].ToString());
}

#endregion   

#region Authentication

string jwtSecret = Environment.GetEnvironmentVariable("jwtSecret");
string jwtIssuer = Environment.GetEnvironmentVariable("jwtIssuer");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtIssuer,
                        ValidAudience = "http://localhost", //træk fra nginx
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
                    };
                });

#endregion

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();

#region Swagger

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
            },
            new List<string>()
        },
    });

    var xmlFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFileName));
});

#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
