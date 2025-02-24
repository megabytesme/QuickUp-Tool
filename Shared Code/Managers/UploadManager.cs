using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace QuickUp.Shared
{
    public class UploadManager
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly UploadRepository _uploadRepository;

        public UploadManager(UploadRepository uploadRepository)
        {
            _uploadRepository = uploadRepository ?? throw new ArgumentNullException(nameof(uploadRepository));
        }

        public async Task<string> UploadFile(StorageFile file, UploadedFile uploadedFileInfo, IProgress<double> progress)
        {
            if (uploadedFileInfo == null) throw new ArgumentNullException(nameof(uploadedFileInfo));

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
                var streamContent = new ProgressableStreamContent(fileStream.AsStreamForRead(), (bytesSent, totalBytes) =>
                {
                    if (totalBytes > 0)
                    {
                        double progressPercentage = (double)bytesSent / totalBytes * 100;
                        progress.Report(progressPercentage);
                    }
                });

                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                var response = await _httpClient.PostAsync(uri, streamContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.Created)
                    {
                        FilebinResponseObject responseObject = DeserializeFilebinResponse(responseContent);
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
                            throw new Exception("Upload was successful but the constructed URL is invalid.");
                        }
                    }
                    else
                    {
                        throw new Exception($"Upload failed with unexpected HTTP success status code: {response.StatusCode}. Expected 201 Created.");
                    }
                }
                else
                {
                    throw new Exception($"Upload failed with HTTP status code: {response.StatusCode}.");
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
            }
        }

        private FilebinResponseObject DeserializeFilebinResponse(string responseContent)
        {
            try
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(FilebinResponseObject));
                using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseContent)))
                {
                    FilebinResponseObject responseObject = (FilebinResponseObject)serializer.ReadObject(ms) as FilebinResponseObject;
                    return responseObject;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("JSON deserialization failed: " + ex.Message);
            }
        }


        public class ProgressableStreamContent : HttpContent
        {
            private const int DefaultBufferSize = 4096;
            private readonly Stream _content;
            private readonly int _bufferSize;
            private readonly Action<long, long> _progress;

            public ProgressableStreamContent(Stream content, Action<long, long> progress, int bufferSize = DefaultBufferSize)
            {
                _content = content ?? throw new ArgumentNullException(nameof(content));
                _progress = progress ?? throw new ArgumentNullException(nameof(progress));
                _bufferSize = bufferSize;
            }

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                try
                {
                    var buffer = new byte[_bufferSize];
                    long totalBytesRead = 0;
                    int bytesRead;

                    while ((bytesRead = await _content.ReadAsync(buffer, 0, buffer.Length)) != 0)
                    {
                        await stream.WriteAsync(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;
                        _progress(totalBytesRead, _content.Length);
                    }
                }
                catch (Exception ex)
                {
                    throw;
                }
            }

            protected override bool TryComputeLength(out long length)
            {
                length = _content.Length;
                return true;
            }
        }
    }
}