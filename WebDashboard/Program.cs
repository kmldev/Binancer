using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using BinanceTradingBot.Infrastructure.Persistence.Contexts;
using BinanceTradingBot.WebDashboard.Infrastructure;
using BinanceTradingBot.WebDashboard.Infrastructure.Identity;
using BinanceTradingBot.WebDashboard.Hubs;
using BinanceTradingBot.WebDashboard.Middleware;
using Serilog;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using BinanceTradingBot.Application.Extensions; // Add this using statement
using BinanceTradingBot.WebDashboard.Extensions; // Add this using statement

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

// Enregistrement des services applicatifs
builder.Services.AddApplicationServices(); // Corrected call

// Enregistrement des services du Web Dashboard
builder.Services.AddWebDashboardServices(builder.Configuration);

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
    var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>();
    
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
    var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>();
    
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