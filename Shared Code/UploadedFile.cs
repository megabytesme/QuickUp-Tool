using SQLite;

namespace QuickUp.Shared
{
    public class UploadedFile
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string FileName { get; set; }
        public string Status { get; set; }
        public string URL { get; set; }
    }
}
