using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace QuickUp.Shared
{
    [DataContract]
    public class FileInfo
    {
        [DataMember(Name = "filename")]
        public string Filename { get; set; }
        [DataMember(Name = "content-type")]
        public string ContentType { get; set; }
        [DataMember(Name = "bytes")]
        public int Bytes { get; set; }
        [DataMember(Name = "bytes_readable")]
        public string BytesReadable { get; set; }
        [DataMember(Name = "md5")]
        public string Md5 { get; set; }
        [DataMember(Name = "sha256")]
        public string Sha256 { get; set; }
        [DataMember(Name = "updated_at")]
        public string UpdatedAt { get; set; }
        [DataMember(Name = "updated_at_relative")]
        public string UpdatedAtRelative { get; set; }
        [DataMember(Name = "created_at")]
        public string CreatedAt { get; set; }
        [DataMember(Name = "created_at_relative")]
        public string CreatedAtRelative { get; set; }
    }
}