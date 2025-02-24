using System;

namespace QuickUp.Shared
{
    public class UploadResult
    {
        public string Url { get; set; }
        public UploadedFile UploadedFile { get; set; }
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; }
        public Exception OriginalException { get; set; }
    }
}