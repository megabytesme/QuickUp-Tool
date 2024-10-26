using System.Runtime.Serialization;

namespace QuickUp.Shared
{
    [DataContract]
    public class UploadedFile
    {
        [DataMember]
        public string FileName { get; set; }
        [DataMember]
        public string Status { get; set; }
        [DataMember]
        public string URL { get; set; }
    }
}
