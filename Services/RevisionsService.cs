using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;

namespace GSheetTelegramBot.Services
{
    public class RevisionsService
    {
        private readonly DriveService _driveService;

        public RevisionsService(DriveService driveService)
        {
            _driveService = driveService;
        }

        public async Task<IList<Revision>> ListRevisionsAsync(string fileId)
        {
            var request = _driveService.Revisions.List(fileId);
            var response = await request.ExecuteAsync();

            return response.Revisions;
        }
    }
}
