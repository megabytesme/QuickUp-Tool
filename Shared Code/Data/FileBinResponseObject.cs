using System.IO;
using System.Runtime;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace QuickUp.Shared
{
    [DataContract]
    public class FilebinResponseObject
    {
        [DataMember(Name = "bin")]
        public BinInfo Bin { get; set; }

        [DataMember(Name = "file")]
        public FileInfo File { get; set; }
    }
}