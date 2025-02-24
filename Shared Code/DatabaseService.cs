using SQLite;
using System;
using System.IO;

#if WINDOWS_UWP
using Windows.Storage;
#endif

namespace QuickUp.Shared
{
    public class DatabaseService
    {
        private static readonly string DbPath = GetDatabasePath();
        private SQLiteConnection _database;

        public DatabaseService()
        {
            _database = new SQLiteConnection(DbPath);
            _database.CreateTable<UploadedFile>();
        }

        public SQLiteConnection Connection => _database;

        private static string GetDatabasePath()
        {
#if WINDOWS_UWP
            return Path.Combine(ApplicationData.Current.LocalFolder.Path, "uploadHistory.db");
#else
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "uploadHistory.db");
#endif
        }
    }
}