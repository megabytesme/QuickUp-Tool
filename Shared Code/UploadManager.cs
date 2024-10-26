using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace QuickUp.Shared
{
    public static class UploadManager
    {
        public static async Task<string> UploadFile(StorageFile file, Action<long, long> reportProgress)
        {
            try
            {
                const ulong twoGB = 2UL * 1024 * 1024 * 1024;
                BasicProperties properties = await file.GetBasicPropertiesAsync();
                ulong fileSize = properties.Size;

                if (fileSize > twoGB)
                {
                    return null;
                }

                var uri = new Uri("https://file.io/");
                var httpClient = new HttpClient();
                var content = new MultipartFormDataContent();
                var fileStream = await file.OpenReadAsync();
                var streamContent = new ProgressableStreamContent(fileStream.AsStreamForRead(), reportProgress);

                streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                content.Add(streamContent, "file", file.Name);

                var response = await httpClient.PostAsync(uri, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(ResponseObject));
                    using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseContent)))
                    {
                        var responseObject = (ResponseObject)serializer.ReadObject(ms);
                        string urlString = responseObject.link;

                        if (Uri.TryCreate(urlString, UriKind.Absolute, out Uri validUri))
                        {
                            return validUri.ToString();
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Invalid URL: {urlString}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UploadFile: {ex.Message}");
            }
            return null;
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
                    System.Diagnostics.Debug.WriteLine($"Error in SerializeToStreamAsync: {ex.Message}");
                    throw;
                }
            }

            protected override bool TryComputeLength(out long length)
            {
                length = _content.Length;
                return true;
            }
        }

        [DataContract]
        private class ResponseObject
        {
            [DataMember]
            public string link { get; set; }
        }
    }
}
