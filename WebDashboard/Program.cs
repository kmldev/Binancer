using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using BinanceTradingBot.Infrastructure.Persistence.Contexts;
using BinanceTradingBot.WebDashboard.Infrastructure;
using BinanceTradingBot.WebDashboard.Infrastructure.Identity;
using BinanceTradingBot.WebDashboard.Hubs;
using BinanceTradingBot.WebDashboard.Middleware;
using Serilog;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

// Configuration de Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/binancebot-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Ajout des services au conteneur
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.AddRazorPages();

// Configuration de la compression des réponses
builder.Services.AddResponseCompression(options =>
{
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.EnableForHttps = true;
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "text/css", "application/javascript", "image/svg+xml" });
});

// SignalR pour les mises à jour en temps réel
builder.Services.AddSignalR().AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

// Configuration des CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "https://localhost:7001" })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Configuration de la base de données principale
builder.Services.AddDbContext<TradingDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking); // Améliore les performances en lecture
    
    // Active les logs SQL détaillés en développement
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Configuration de la base de données pour l'identité
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("IdentityConnection"));
    
    // Active les logs SQL détaillés en développement
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Configuration de l'identité
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Configuration des règles de mot de passe
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    
    // Configuration du verrouillage de compte
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    
    // Configuration de l'utilisateur
    options.User.RequireUniqueEmail = true;
}).AddEntityFrameworkStores<ApplicationDbContext>()
  .AddDefaultTokenProviders();

// Configuration de l'authentification et autorisation
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero, // Réduit le délai de grâce par défaut
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? throw new InvalidOperationException("La clé secrète JWT n'est pas configurée")))
    };
    
    // Permettre à SignalR de recevoir le token
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            
            if (!string.IsNullOrEmpty(accessToken) && (path.StartsWithSegments("/tradingHub")))
            {
                context.Token = accessToken;
            }
            
            return Task.CompletedTask;
        }
    };
});

// Configuration des autorisations
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserAccess", policy => policy.RequireRole("User", "Admin"));
});

// Enregistrement des services applicatifs
builder.Services.AddApplicationServices(builder.Configuration);

// Configuration de Swagger
builder.Services.AddSwaggerDocumentation();

// Configuration du monitoring
builder.Services.AddApplicationInsightsTelemetry();

// Construction de l'application
var app = builder.Build();

// Middleware de journalisation des requêtes
app.UseSerilogRequestLogging();

// Configuration du pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwaggerDocumentation();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Configuration du middleware d'exception pour l'API
app.UseApiExceptionHandling();

// Compression des réponses
app.UseResponseCompression();

// Sécurité et routing
app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Mise en cache côté client pour les fichiers statiques
        if (ctx.File.Name.EndsWith(".css") || 
            ctx.File.Name.EndsWith(".js") || 
            ctx.File.Name.EndsWith(".woff") || 
            ctx.File.Name.EndsWith(".woff2") ||
            ctx.File.Name.EndsWith(".png") || 
            ctx.File.Name.EndsWith(".jpg") || 
            ctx.File.Name.EndsWith(".gif"))
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=604800"); // 7 jours
        }
    }
});

app.UseCors("CorsPolicy");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Création des rôles par défaut si nécessaire
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Création des rôles
        var roles = new[] { "Admin", "User" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Rôle {Role} créé", role);
            }
        }
        
        // Création d'un utilisateur admin par défaut si nécessaire
        var adminEmail = builder.Configuration["DefaultAdmin:Email"];
        var adminPassword = builder.Configuration["DefaultAdmin:Password"];
        
        if (!string.IsNullOrEmpty(adminEmail) && !string.IsNullOrEmpty(adminPassword))
        {
            var admin = await userManager.FindByEmailAsync(adminEmail);
            
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    EmailConfirmed = true
                };
                
                var createResult = await userManager.CreateAsync(admin, adminPassword);
                
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                    logger.LogInformation("Utilisateur admin par défaut créé");
                }
                else
                {
                    logger.LogError("Échec de la création de l'utilisateur admin: {Errors}",
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erreur lors de l'initialisation des rôles et utilisateurs");
    }
}

// Initialisation de la configuration des indices dans la base de données
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TradingDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        if (app.Environment.IsDevelopment())
        {
            // Appliquer les migrations automatiquement en développement
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Base de données mise à jour avec les dernières migrations");
        }
        
        // Création des indices pour optimiser les requêtes fréquentes
        // Exemple d'indices SQL personnalisés (à adapter selon votre schéma)
        var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = @"
            CREATE INDEX IF NOT EXISTS idx_positions_symbol ON Positions(Symbol);
            CREATE INDEX IF NOT EXISTS idx_positions_status ON Positions(Status);
            CREATE INDEX IF NOT EXISTS idx_positions_open_time ON Positions(OpenTime);
            CREATE INDEX IF NOT EXISTS idx_trading_pairs_active ON TradingPairs(IsActive);
        ";
        
        await dbContext.Database.OpenConnectionAsync();
        await command.ExecuteNonQueryAsync();
        
        logger.LogInformation("Indices de base de données créés/vérifiés");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erreur lors de l'initialisation de la base de données");
    }
}

// Configuration des routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.MapHub<TradingHub>("/tradingHub");

app.Run();