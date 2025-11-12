using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BlogApp.Api.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSingleton<MongoDbService>();
builder.Services.AddSingleton<AuthService>();
builder.Services.AddHttpClient();

// Register Supabase.Client in DI
builder.Services.AddSingleton(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var supabaseUrl = configuration["Supabase:Url"];
    var supabaseKey = configuration["Supabase:AnonKey"];
    return new Supabase.Client(supabaseUrl, supabaseKey);
});

// Configure Swagger with JWT Authentication
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "BlogApp.Api",
        Version = "v1"
    });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Please enter the JWT token with the 'Bearer ' prefix, e.g., 'Bearer <token>'",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

// Configure JWT Authentication for Supabase
var supabaseSecretKey = builder.Configuration["Supabase:JwtSecret"];
var supabaseSignatureKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(supabaseSecretKey));
var validIssuer = builder.Configuration["Supabase:Issuer"];
var validAudiences = new List<string> { "authenticated" };

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = supabaseSignatureKey,
        ValidAudiences = validAudiences,
        ValidIssuer = validIssuer,
        ValidateIssuer = true,
        ValidateAudience = true,
        NameClaimType = "sub" // Map 'sub' to ClaimTypes.NameIdentifier
    };
    // Log token claims for debugging
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            Console.WriteLine("---- JWT TOKEN CLAIMS ----");
            foreach (var claim in context.Principal.Claims)
            {
                Console.WriteLine($"Claim Type: {claim.Type}, Value: {claim.Value}");
            }
            Console.WriteLine("---- END JWT TOKEN CLAIMS ----");
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        }
    };
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", builder =>
        builder.WithOrigins("https://localhost:7111")
               .AllowAnyMethod()
               .AllowAnyHeader());
});

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddMongoDb(mongodbConnectionString: builder.Configuration["MongoDbSettings:ConnectionString"], name: "MongoDB", failureStatus: HealthStatus.Unhealthy)
    .AddUrlGroup(new Uri(builder.Configuration["Supabase:Url"] + "/health"), name: "Supabase", failureStatus: HealthStatus.Unhealthy);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowBlazorClient");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();