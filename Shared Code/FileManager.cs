using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuickUp.Shared
{
    public static class FileManager
    {
        private static DatabaseService _databaseService = new DatabaseService();

        public static Task<List<UploadedFile>> LoadHistoryAsync()
        {
            return Task.Run(() =>
            {
                return _databaseService.Connection.Table<UploadedFile>().ToList();
            });
        }

        public static Task SaveHistoryAsync(List<UploadedFile> uploadedFiles)
        {
            return Task.Run(() =>
            {
                _databaseService.Connection.DeleteAll<UploadedFile>();
                _databaseService.Connection.InsertAll(uploadedFiles);
            });
        }
    }
}
