using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
using GSheetTelegramBot.Services;

namespace GSheetTelegramBot.Helpers
{
    public class GoogleHelper
    {
        private readonly string _token;
        private readonly string _sheetFileName;
        private UserCredential _credentials;
        private DriveService _driveService;
        private SheetsService _sheetsService;
        private string _sheetFileId;
        private string _sheetName;

        public GoogleHelper(string token, string sheetFileName)
        {
            _token=token;
            _sheetFileName = sheetFileName;
        }

        public string ApplicationName { get; private set; } = "";
        public string[] Scopes { get; set; } = new string[] { DriveService.Scope.Drive, SheetsService.Scope.Spreadsheets};

        public async Task <bool> Start()
        {
            string credentialPath = Path.Combine(Environment.CurrentDirectory, ".credentials", ApplicationName);
            
            using (var strm = new MemoryStream(Encoding.UTF8.GetBytes(_token)))
            {
                _credentials = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    clientSecrets: GoogleClientSecrets.FromStream(strm).Secrets, 
                    scopes:Scopes, 
                    user:"user", 
                    taskCancellationToken:CancellationToken.None);

                new FileDataStore(credentialPath, true);
            }
           _driveService = new DriveService(new Google.Apis.Services.BaseClientService.Initializer
           {
               HttpClientInitializer = _credentials,
               ApplicationName = ApplicationName,
           });
           _sheetsService = new SheetsService(new Google.Apis.Services.BaseClientService.Initializer
           {
               HttpClientInitializer = _credentials,
               ApplicationName = ApplicationName,
           });

           var request = _driveService.Files.List();
           var response = await request.ExecuteAsync();

           foreach (var file in response.Files)
           {
               if (file.Name == _sheetFileName)
               {
                  _sheetFileId= file.Id;
                  break;
               }
           }

           if (!string.IsNullOrEmpty(_sheetFileId))
           {
               var sheetRequest = _sheetsService.Spreadsheets.Get(_sheetFileId);
               var sheetResponse = await sheetRequest.ExecuteAsync();
               _sheetName = sheetResponse.Sheets[0].Properties.Title;

               return true;
           }
           return false;
        }
    }
}
