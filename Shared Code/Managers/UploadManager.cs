using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace QuickUp.Shared
{
    public class UploadManager
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly UploadRepository _uploadRepository;

        public UploadManager(UploadRepository uploadRepository)
        {
            _uploadRepository =
                uploadRepository ?? throw new ArgumentNullException(nameof(uploadRepository));
        }

        public async Task<string> UploadFile(
            StorageFile file,
            UploadedFile uploadedFileInfo,
            IProgress<double> progress
        )
        {
            if (uploadedFileInfo == null)
                throw new ArgumentNullException(nameof(uploadedFileInfo));

            try
            {
                const ulong twoGB = 2UL * 1024 * 1024 * 1024;
                BasicProperties properties = await file.GetBasicPropertiesAsync();
                ulong fileSize = properties.Size;

                if (fileSize > twoGB)
                {
                    throw new Exception("File size exceeds the limit of 2GB.");
                }

                uploadedFileInfo.Status = "Uploading";

                string binName = Guid.NewGuid().ToString();
                string fileName = file.Name;
                var uri = new Uri($"https://filebin.net/{binName}/{fileName}");

                var fileStream = await file.OpenReadAsync();
                var streamContent = new HttpStreamContent(fileStream);
                streamContent.Headers.ContentType = new HttpMediaTypeHeaderValue(
                    "application/octet-stream"
                );
                var postOperation = _httpClient.PostAsync(uri, streamContent);

                postOperation.Progress = (responseMessage, httpProgress) =>
                {
                    if (
                        httpProgress.TotalBytesToSend.HasValue
                        && httpProgress.TotalBytesToSend.Value > 0
                    )
                    {
                        double progressPercentage =
                            (double)httpProgress.BytesSent
                            / httpProgress.TotalBytesToSend.Value
                            * 100;
                        progress.Report(progressPercentage);
                    }
                };

                var response = await postOperation;
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.Created)
                    {
                        FilebinResponseObject responseObject = DeserializeFilebinResponse(
                            responseContent
                        );
                        string baseUrl = "https://filebin.net/";
                        string fileUrlString = $"{baseUrl}{binName}/{fileName}";

                        if (Uri.TryCreate(fileUrlString, UriKind.Absolute, out Uri validUri))
                        {
                            uploadedFileInfo.URL = validUri.ToString();
                            uploadedFileInfo.Status = "Uploaded";
                            uploadedFileInfo.ExpiryDate = responseObject?.Bin?.ExpiredAt;
                            uploadedFileInfo.FileSizeReadable = responseObject?.File?.BytesReadable;
                            uploadedFileInfo.ContentType = responseObject?.File?.ContentType;
                            return validUri.ToString();
                        }
                        else
                        {
                            throw new Exception(
                                "Upload was successful but the constructed URL is invalid."
                            );
                        }
                    }
                    else
                    {
                        throw new Exception(
                            $"Upload failed with unexpected HTTP success status code: {response.StatusCode}. Expected 201 Created."
                        );
                    }
                }
                else
                {
                    throw new Exception(
                        $"Upload failed with HTTP status code: {response.StatusCode}."
                    );
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private FilebinResponseObject DeserializeFilebinResponse(string responseContent)
        {
            try
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(
                    typeof(FilebinResponseObject)
                );
                using (
                    var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseContent))
                )
                {
                    FilebinResponseObject responseObject =
                        (FilebinResponseObject)serializer.ReadObject(ms) as FilebinResponseObject;
                    return responseObject;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("JSON deserialization failed: " + ex.Message);
            }
        }
    }
}
