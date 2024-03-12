using GSheetTelegramBot.DataLayer.Context;
using GSheetTelegramBot.DataLayer.DbModels;
using GSheetTelegramBot.DataLayer.Repositories.Implementations;
using GSheetTelegramBot.DataLayer.Repositories.Interfaces;
using GSheetTelegramBot.Web.Helpers;
using GSheetTelegramBot.Web.Interfaces;
using GSheetTelegramBot.Web.Services;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace GSheetTelegramBot.Web;

public class Program
{
    public static Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        ConfigureServices(builder);

        var app = builder.Build();
        ConfigureHttpPipeline(app);

        var botService = app.Services.GetRequiredService<TelegramService>();
         botService.StartReceivingAsync();

        var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
        lifetime.ApplicationStopping.Register(() => botService.StopReceiving());

        app.Run();
        return Task.CompletedTask;
    }

    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
        var dbName = Environment.GetEnvironmentVariable("DB_NAME");
        var dbPassword = Environment.GetEnvironmentVariable("DB_SA_PASSWORD");

        var connectionString = $"Data Source={dbHost};Initial Catalog={dbName};User ID=sa;Password={dbPassword}";
        services.AddDbContext<GSheetTelegramBotDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IDataRepo<User>, DataRepo<User>>();
        services.AddScoped<IDataRepo<GoogleTable>, DataRepo<GoogleTable>>();
        services.AddScoped<IDataRepo<Subscription>, DataRepo<Subscription>>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IGoogleTableService, GoogleTableService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<INotificationService, NotificationsService>();

        services.AddScoped<IEmailService, EmailService>(provider =>
            new EmailService(configuration.GetSection("EmailConfiguration").Get<EmailConfiguration>()));

        var botToken = configuration["BotConfiguration:BotToken"];
        services.AddSingleton<TelegramService>(provider =>
            new TelegramService(botToken, provider));

        services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true
            }));

        services.AddHangfireServer();

        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
    }

    private static void ConfigureHttpPipeline(WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.UseHangfireDashboard();

        app.UseCors(policy => policy.AllowAnyOrigin());
    }
}