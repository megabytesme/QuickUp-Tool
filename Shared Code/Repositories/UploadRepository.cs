using SQLite;
using System.Collections.ObjectModel;
using System.Linq;

namespace QuickUp.Shared
{
    public class UploadRepository
    {
        private static UploadRepository _instance;
        private readonly SQLiteConnection _database;
        public SQLiteConnection Database => _database;
        public ObservableCollection<UploadedFile> UploadedFiles { get; private set; }

        private UploadRepository()
        {
            var dbService = new DatabaseService();
            _database = dbService.Connection;
            LoadUploadedFiles();
        }

        public static UploadRepository Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new UploadRepository();
                }
                return _instance;
            }
        }

        private void LoadUploadedFiles()
        {
            var files = _database.Table<UploadedFile>().ToList();
            UploadedFiles = new ObservableCollection<UploadedFile>(files);
        }

        public void AddUploadedFile(UploadedFile uploadedFile)
        {
            _database.Insert(uploadedFile);
            UploadedFiles.Add(uploadedFile);
        }

        public void AddUploadedFileToDatabaseOnly(UploadedFile uploadedFile)
        {
            _database.Insert(uploadedFile);
        }

        public void DeleteUploadedFile(UploadedFile uploadedFile)
        {
            _database.Delete(uploadedFile);
            UploadedFiles.Remove(uploadedFile);
        }

        public void EditUploadedFile(UploadedFile uploadedFile)
        {
            _database.Update(uploadedFile);
            var existingFile = UploadedFiles.FirstOrDefault(f => f.Id == uploadedFile.Id);
            if (existingFile != null)
            {
                int index = UploadedFiles.IndexOf(existingFile);
                if (index >= 0 && index < UploadedFiles.Count)
                {
                    UploadedFiles[index] = uploadedFile;
                }
            }
        }

        public void SaveUploadedFiles()
        {
            _database.DeleteAll<UploadedFile>();
            _database.InsertAll(UploadedFiles.ToList());
        }
    }
}