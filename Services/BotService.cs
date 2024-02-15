using Google.Apis.Drive.v3;
using Google.Apis.DriveActivity.v2;
using Google.Apis.DriveActivity.v2.Data;
using Google.Apis.PeopleService.v1;
using Google.Apis.Sheets.v4;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace GSheetTelegramBot.Services
{
    public class BotService
    {
        private readonly TelegramBotClient _botClient;
        private readonly RevisionsService _revisionsService;
        private readonly SheetsService _sheetsService;
        private readonly DriveService _driveService;
        private readonly DriveActivityService _activityService;
        private readonly PeopleServiceService _peopleService;
        private CancellationTokenSource _cts;
        private string _lastRevisionId;
        private string _fileId = "your_google_sheet_file_id_here";
        private long _chatId;

        public BotService(string token, SheetsService sheetsService, DriveService driveService, DriveActivityService activityService, PeopleServiceService peopleService)
        {
            _botClient = new TelegramBotClient(token);
            _sheetsService = sheetsService;
            _driveService = driveService;
            _activityService = activityService;
            _peopleService = peopleService;
        }

        public async Task StartReceivingAsync()
        {
            _cts = new CancellationTokenSource();

            Task.Run(() => ProcessBotUpdates());

            //Task.Run(() => CheckRevisionsPeriodically());
        }

        private async Task ProcessBotUpdates()
        {
            var offset = 0;
            while (!_cts.IsCancellationRequested)
            {
                var updates = await _botClient.GetUpdatesAsync(offset, cancellationToken: _cts.Token);
                foreach (var update in updates)
                {
                    if (update.Message != null)
                    {
                        await HandleMessageAsync(update.Message);
                    }
                    offset = update.Id + 1;
                }
                await Task.Delay(TimeSpan.FromSeconds(1), _cts.Token);
            }
        }

        private async Task CheckRevisionsPeriodically()
        {
            while (!_cts.IsCancellationRequested)
            {
                var revisions = await _revisionsService.ListRevisionsAsync(_fileId);
                foreach (var revision in revisions.Reverse()) 
                {
                    if (revision.Id == _lastRevisionId)
                        break; 

                    await _botClient.SendTextMessageAsync(_chatId, $"Обнаружена новая ревизия: {revision.Id}");

                    _lastRevisionId = revision.Id; 
                }

                await Task.Delay(TimeSpan.FromMinutes(1), _cts.Token); 
            }
        }

        public void StopReceiving()
        {
            _cts?.Cancel();
        }

        private async Task HandleMessageAsync(Message message)
        {
            switch (message.Text.Split(' ').First())
            {
                case "/start":
                    _chatId = message.Chat.Id;
                    var request = _driveService.Files.List();
                    var response = await request.ExecuteAsync();
                    string sheetFileId="";
                    var sheetName ="";
                    foreach (var file in response.Files)
                    {
                        if (file.Name == "TestSheet")
                        {
                            var user = file.LastModifyingUser;
                            var filename = file.OriginalFilename;
                            sheetFileId = file.Id;
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(sheetFileId))
                    {
                        var activityRequest = _activityService.Activity.Query(new QueryDriveActivityRequest()
                        {
                            ItemName = "items/" + sheetFileId
                        });

                        var activityResponse = await activityRequest.ExecuteAsync();
                        foreach (var activity in activityResponse.Activities)
                        {
                            var personGet=_peopleService.People.Get(activity.Actors[0].User?.KnownUser?.PersonName);
                            var personResponse = await personGet.ExecuteAsync();
                            Console.WriteLine($"Time: {activity.Timestamp} User: {personResponse.Addresses} {personResponse.EmailAddresses} {personResponse.Names} Action: {activity.Actions[0].Detail}");
                        }


                    }

                    if (!string.IsNullOrEmpty(sheetFileId))
                    {
                        var sheetRequest = _sheetsService.Spreadsheets.Get(sheetFileId);
                        var sheetResponse = await sheetRequest.ExecuteAsync();
                        sheetName = sheetResponse.Sheets[0].Properties.Title;
                        var fileRequest = _driveService.Files.Get(sheetFileId);
                        var file = await fileRequest.ExecuteAsync();
                        var lastModifyingUser = file.LastModifyingUser;
                        var lastModifyingUserName = lastModifyingUser?.DisplayName;
                        var lastModifyingUserEmail = lastModifyingUser?.EmailAddress;

                    }

                    var revisionsRequest = _driveService.Revisions.List(sheetFileId);
                    var revisionsResponse = await revisionsRequest.ExecuteAsync();
                    var revisions =revisionsResponse.Revisions;

                    foreach (var revision in revisions)
                    {
                        var user = revision.LastModifyingUser;
                        var filename = revision.OriginalFilename;
                    }

                    var responseMessage = ($"Sheet Id:{sheetFileId} Sheet Name:{sheetName}");
                    await _botClient.SendTextMessageAsync(_chatId, responseMessage);
                    break;

            }
        }
    }
}
