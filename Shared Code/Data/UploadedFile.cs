using SQLite;
using System;

namespace QuickUp.Shared
{
    public class UploadedFile
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string FileName { get; set; }
        public string Status { get; set; }
        public string URL { get; set; }
        public string ExpiryDate { get; set; }
        public string FileSizeReadable { get; set; }
        public string ContentType { get; set; }

        public string DaysUntilExpiry
        {
            get
            {
                if (DateTime.TryParse(ExpiryDate, out DateTime expiryDateTime))
                {
                    TimeSpan timeUntilExpiry = expiryDateTime - DateTime.Now.ToLocalTime();

                    if (timeUntilExpiry <= TimeSpan.Zero)
                    {
                        return "Expired";
                    }

                    string formattedTime = "";

                    if (timeUntilExpiry.Days > 0)
                    {
                        formattedTime += $"{timeUntilExpiry.Days} day{(timeUntilExpiry.Days == 1 ? "" : "s")}, ";
                    }
                    if (timeUntilExpiry.Hours > 0)
                    {
                        formattedTime += $"{timeUntilExpiry.Hours} hour{(timeUntilExpiry.Hours == 1 ? "" : "s")}, ";
                    }
                    if (timeUntilExpiry.Minutes > 0 && timeUntilExpiry.Days == 0)
                    {
                        formattedTime += $"{timeUntilExpiry.Minutes} minute{(timeUntilExpiry.Minutes == 1 ? "" : "s")}";
                    }

                    formattedTime = formattedTime.TrimEnd(',', ' ');

                    if (string.IsNullOrEmpty(formattedTime))
                    {
                        return "Expires very soon";
                    }

                    return formattedTime;
                }
                else
                {
                    return "Invalid Date";
                }
            }
        }

        public bool IsExpired
        {
            get
            {
                return DateTime.TryParse(ExpiryDate, out var expiry) && expiry < DateTime.UtcNow;
            }
        }

        public bool IsSharable
        {
            get
            {
                return !string.IsNullOrEmpty(URL)
                    && !string.IsNullOrEmpty(FileName)
                    && !IsExpired;
            }
        }
    }
}
