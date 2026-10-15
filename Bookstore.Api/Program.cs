
using Bookstore.Api.Authentication;
using Bookstore.Api.OpenApi;
using Bookstore.Application.Interfaces;
using Bookstore.Infrastructure.Data;
using Bookstore.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Database configuration
builder.Services.AddDbContext<BookstoreDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("BookstoreDatabase")));

// Application services
builder.Services.AddScoped<IBookService, BookService>();

// API configuration
builder.Services.AddControllers();

// Swagger configuration
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Bookstore API",
        Version = "v1",
        Description = "Bookstore REST API secured with Keycloak OAuth2"
    });

    var authority = builder.Configuration["Keycloak:Authority"]!;

    // CRUD - Bearer token obtained using Client Credentials
    options.AddSecurityDefinition("CrudOAuth", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter a JWT obtained using OAuth2 Client Credentials."
    });

    // Search - OAuth2 Implicit Flow
    options.AddSecurityDefinition("SearchOAuth", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Description = "Implicit authentication for Book Search.",
        Flows = new OpenApiOAuthFlows
        {
            Implicit = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri(
                    $"{authority}/protocol/openid-connect/auth"),
                Scopes = new Dictionary<string, string>
                {
                    ["bookstore.search"] = "Search books"
                }
            }
        }
    });

    options.OperationFilter<SecurityRequirementsOperationFilter>();
});

// Keycloak JWT authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var authority = builder.Configuration["Keycloak:Authority"]
            ?? throw new InvalidOperationException(
                "Keycloak Authority is not configured.");

        var audience = builder.Configuration["Keycloak:Audience"]
            ?? throw new InvalidOperationException(
                "Keycloak Audience is not configured.");

        var metadataAddress =
            builder.Configuration["Keycloak:MetadataAddress"]
            ?? $"{authority}/.well-known/openid-configuration";

        var internalBaseUrl =
            builder.Configuration["Keycloak:InternalBaseUrl"];

        options.Authority = authority;
        options.Audience = audience;

        // HTTP is allowed only for local development
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.MapInboundClaims = false;

        // Validate the external Keycloak issuer
        options.TokenValidationParameters.ValidIssuer = authority;

        if (!string.IsNullOrWhiteSpace(internalBaseUrl))
        {
            // Docker: retrieve metadata and signing keys
            // through the internal Docker network
            options.ConfigurationManager =
                new ConfigurationManager<OpenIdConnectConfiguration>(
                    metadataAddress,
                    new DockerKeycloakConfigurationRetriever(
                        new OpenIdConnectConfigurationRetriever(),
                        internalBaseUrl),
                    new HttpDocumentRetriever
                    {
                        RequireHttps = false
                    });
        }
        else
        {
            // Visual Studio: use the standard Keycloak metadata endpoint
            options.MetadataAddress = metadataAddress;
        }
    });

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("BookManage", policy =>
        policy.RequireAuthenticatedUser()
            .RequireClaim("azp", "bookstore-crud-client")
            .RequireAssertion(context =>
                context.User.FindAll("scope")
                    .SelectMany(c => c.Value.Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries))
                    .Contains("bookstore.manage")));

    options.AddPolicy("BookSearch", policy =>
        policy.RequireAuthenticatedUser()
            .RequireClaim("azp", "bookstore-search-client")
            .RequireAssertion(context =>
                context.User.FindAll("scope")
                    .SelectMany(c => c.Value.Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries))
                    .Contains("bookstore.search")));
});

var app = builder.Build();

// Development configuration
if (app.Environment.IsDevelopment())
{
    // Apply database migrations and seed initial data
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider
            .GetRequiredService<BookstoreDbContext>();

        await dbContext.Database.MigrateAsync();
        await DbSeeder.SeedAsync(dbContext);
    }

    // Swagger UI
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "Bookstore API v1");

        options.RoutePrefix = "swagger";

        // OAuth2 Implicit Flow for search
        options.OAuthClientId("bookstore-search-client");
        options.OAuthAppName("Bookstore Swagger UI");
        options.OAuthScopes("bookstore.search");
    });
}

// HTTP request pipeline
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
