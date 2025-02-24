using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace QuickUp.Shared
{
    [DataContract]
    public class BinInfo
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "readonly")]
        public bool ReadOnly { get; set; }

        [DataMember(Name = "bytes")]
        public int Bytes { get; set; }

        [DataMember(Name = "bytes_readable")]
        public string BytesReadable { get; set; }

        [DataMember(Name = "files")]
        public int Files { get; set; }

        [DataMember(Name = "updated_at")]
        public string UpdatedAt { get; set; }
        [DataMember(Name = "updated_at_relative")]
        public string UpdatedAtRelative { get; set; }
        [DataMember(Name = "created_at")]
        public string CreatedAt { get; set; }
        [DataMember(Name = "created_at_relative")]
        public string CreatedAtRelative { get; set; }
        [DataMember(Name = "expired_at")]
        public string ExpiredAt { get; set; }
        [DataMember(Name = "expired_at_relative")]
        public string ExpiredAtRelative { get; set; }
    }
}