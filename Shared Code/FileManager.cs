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
            try
            {
                var roamingFolder = ApplicationData.Current.RoamingFolder;
                StorageFile historyFile;

                if (await roamingFolder.TryGetItemAsync("uploadHistory.json") == null)
                {
                    historyFile = await roamingFolder.CreateFileAsync("uploadHistory.json", CreationCollisionOption.ReplaceExisting);
                    DataContractJsonSerializer writeSerializer = new DataContractJsonSerializer(typeof(List<UploadedFile>));
                    using (Stream stream = await historyFile.OpenStreamForWriteAsync())
                    {
                        writeSerializer.WriteObject(stream, new List<UploadedFile>());
                    }
                    return new List<UploadedFile>();
                }
                else
                {
                    historyFile = await roamingFolder.GetFileAsync("uploadHistory.json");
                }
                DataContractJsonSerializer readSerializer = new DataContractJsonSerializer(typeof(List<UploadedFile>));
                using (Stream stream = await historyFile.OpenStreamForReadAsync())
                {
                    var files = (List<UploadedFile>)readSerializer.ReadObject(stream);
                    return files ?? new List<UploadedFile>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LoadHistoryAsync: {ex.Message}\n{ex.StackTrace}");
                return new List<UploadedFile>();
            }
        }

        public static async Task SaveHistoryAsync(List<UploadedFile> uploadedFiles)
        {
            try
            {
                var roamingFolder = ApplicationData.Current.RoamingFolder;
                StorageFile historyFile = await roamingFolder.GetFileAsync("uploadHistory.json");
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(List<UploadedFile>));
                using (Stream stream = await historyFile.OpenStreamForWriteAsync())
                {
                    serializer.WriteObject(stream, uploadedFiles);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SaveHistoryAsync: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
