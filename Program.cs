using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.DriveActivity.v2;
using Google.Apis.PeopleService.v1;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using GSheetTelegramBot.Helpers;
using GSheetTelegramBot.Models;
using GSheetTelegramBot.Services;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace GSheetTelegramBot
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            var botToken = builder.Configuration["BotConfiguration:BotToken"];

            builder.Services.Configure<GoogleCloudSettings>(
                builder.Configuration.GetSection("GoogleCloudSettings"));
            builder.Services.AddSingleton<SheetsService>(provider =>
            {
                var settings = provider.GetRequiredService<IOptions<GoogleCloudSettings>>().Value;
                return InitializeSheetsService(settings);
            });


            builder.Services.AddSingleton<PeopleServiceService>(provider =>
            {
                var settings = provider.GetRequiredService<IOptions<GoogleCloudSettings>>().Value;
                return InitializePeopleService(settings);
            });

            builder.Services.AddSingleton<DriveActivityService>(provider =>
            {
                var settings = provider.GetRequiredService<IOptions<GoogleCloudSettings>>().Value;
                return InitializeDriveActivityService(settings);
            });

            builder.Services.AddSingleton<DriveService>(provider =>
            {
                var settings = provider.GetRequiredService<IOptions<GoogleCloudSettings>>().Value;
                return InitializeDriveService(settings);
            });

            static SheetsService InitializeSheetsService(GoogleCloudSettings settings)
            {
                var gsettings = JsonConvert.SerializeObject(settings).Replace("\\n", "\n");
                var credential = GoogleCredential.FromJson(gsettings)
                    .CreateScoped(SheetsService.Scope.Spreadsheets);

                return new SheetsService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = settings.ProjectId,
                });
            }

            static PeopleServiceService InitializePeopleService(GoogleCloudSettings settings)
            {
                var gsettings = JsonConvert.SerializeObject(settings).Replace("\\n", "\n");
                var credential = GoogleCredential.FromJson(gsettings)
                    .CreateScoped(
                        PeopleServiceService.Scope.Contacts,
                        PeopleServiceService.Scope.UserAddressesRead,
                        PeopleServiceService.Scope.UserEmailsRead,
                        PeopleServiceService.Scope.UserPhonenumbersRead,
                        PeopleServiceService.Scope.UserinfoProfile,
                        PeopleServiceService.Scope.UserinfoEmail,
                        PeopleServiceService.Scope.UserOrganizationRead
                    );

                return new PeopleServiceService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = settings.ProjectId,
                });
            }

            static DriveService InitializeDriveService(GoogleCloudSettings settings)
            {
                var credential = GoogleCredential.FromJson(JsonConvert.SerializeObject(settings))
                    .CreateScoped(DriveService.Scope.Drive);

                return new DriveService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = settings.ProjectId, 
                });
            }

            static DriveActivityService InitializeDriveActivityService(GoogleCloudSettings settings)
            {
                var credential = GoogleCredential.FromJson(JsonConvert.SerializeObject(settings))
                    .CreateScoped(DriveActivityService.Scope.DriveActivity);

                return new DriveActivityService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = settings.ProjectId,
                });
            }

            builder.Services.AddSingleton<BotService>(provider =>
            {
                var sheetsService = provider.GetRequiredService<SheetsService>();
                var driveService = provider.GetRequiredService<DriveService>();
                var driveActivityService = provider.GetRequiredService<DriveActivityService>();
                var peopleService = provider.GetRequiredService<PeopleServiceService>();
                return new BotService(botToken, sheetsService, driveService, driveActivityService, peopleService);
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            var botService = app.Services.GetRequiredService<BotService>();
            await botService.StartReceivingAsync();

            app.Run();

            botService.StopReceiving();
        }
    }
}