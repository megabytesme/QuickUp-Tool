using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace QuickUp.Shared
{
    public static class FileManager
    {
        public static async Task<List<UploadedFile>> LoadHistoryAsync()
        {
            var roamingFolder = ApplicationData.Current.RoamingFolder;
            StorageFile historyFile = await roamingFolder.TryGetItemAsync("uploadHistory.json") as StorageFile;

            if (historyFile == null)
            {
                historyFile = await roamingFolder.CreateFileAsync("uploadHistory.json", CreationCollisionOption.ReplaceExisting);
                DataContractJsonSerializer writeSerializer = new DataContractJsonSerializer(typeof(List<UploadedFile>));
                using (Stream stream = await historyFile.OpenStreamForWriteAsync())
                {
                    writeSerializer.WriteObject(stream, new List<UploadedFile>());
                }
            }

            DataContractJsonSerializer readSerializer = new DataContractJsonSerializer(typeof(List<UploadedFile>));
            using (Stream stream = await historyFile.OpenStreamForReadAsync())
            {
                var files = (List<UploadedFile>)readSerializer.ReadObject(stream);
                return files ?? new List<UploadedFile>();
            }
        }

        public static async Task SaveHistoryAsync(List<UploadedFile> uploadedFiles)
        {
            var roamingFolder = ApplicationData.Current.RoamingFolder;
            StorageFile historyFile = await roamingFolder.GetFileAsync("uploadHistory.json");
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(List<UploadedFile>));
            using (Stream stream = await historyFile.OpenStreamForWriteAsync())
            {
                serializer.WriteObject(stream, uploadedFiles);
            }
        }
    }
}
