using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using Windows.Storage.Pickers;
using Windows.Storage;
using Microsoft.UI.Xaml.Media;
using System.Threading.Tasks;
using Windows.Storage.FileProperties;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using Microsoft.UI.Dispatching;
using Windows.ApplicationModel.DataTransfer;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Text.Json;

namespace QuickUp
{
    public sealed partial class MainWindow : Window
    {
        public ObservableCollection<UploadedFile> UploadedFiles { get; } = new ObservableCollection<UploadedFile>();

        FontIcon uploadIcon = new FontIcon
        {
            Glyph = "\uE898",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        public MainWindow()
        {
            this.InitializeComponent();
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(AppTitleBar);
            this.progressRingButton.Content = uploadIcon;
            LoadHistoryAsync();
        }

        private async Task LoadHistoryAsync()
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            StorageFile historyFile;
            if (await localFolder.TryGetItemAsync("uploadHistory.json") == null)
            {
                historyFile = await localFolder.CreateFileAsync("uploadHistory.json", CreationCollisionOption.ReplaceExisting);
                string initialJson = JsonSerializer.Serialize(new List<UploadedFile>());
                await FileIO.WriteTextAsync(historyFile, initialJson);
            }
            else
            {
                historyFile = await localFolder.GetFileAsync("uploadHistory.json");
            }

            string json = await FileIO.ReadTextAsync(historyFile);
            var uploadedFiles = JsonSerializer.Deserialize<List<UploadedFile>>(json);
            if (uploadedFiles != null)
            {
                this.noHistory.Visibility = Visibility.Collapsed;
                foreach (var file in uploadedFiles)
                {
                    UploadedFiles.Add(file);
                }
            }
        }

        private async Task SaveHistoryAsync()
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            StorageFile historyFile = await localFolder.GetFileAsync("uploadHistory.json");

            string json = JsonSerializer.Serialize(UploadedFiles);
            await FileIO.WriteTextAsync(historyFile, json);
        }

        private async void AddToHistory(string fileName, string status, string url)
        {
            UploadedFiles.Add(new UploadedFile { FileName = fileName, Status = status, URL = url });
            await SaveHistoryAsync();
            this.noHistory.Visibility = Visibility.Collapsed;
        }

        private async void ProgressRingButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            picker.FileTypeFilter.Add("*");
            var storageFile = await picker.PickSingleFileAsync();
            if (storageFile != null)
            {
                await HandleFile(storageFile);
            }
        }

        private void ProgressRingButton_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
        }

        private async void ProgressRingButton_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Any())
                {
                    var storageFile = items[0] as StorageFile;
                    if (storageFile != null)
                    {
                        await HandleFile(storageFile);
                    }
                }
            }
        }

        private async Task HandleFile(StorageFile file)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                progressRingButton.IsEnabled = false;
                progressRingButton.Content = "Uploading" + Environment.NewLine + file.Name;
            });

            BasicProperties properties = await file.GetBasicPropertiesAsync();
            ulong fileSize = properties.Size;
            const ulong twoGB = 2UL * 1024 * 1024 * 1024;

            if (fileSize > twoGB)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    progressRingButton.Content = "File is too big!";
                    progressRingButton.IsEnabled = true;
                });
                return;
            }

            await UploadFile(file);
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

            protected override bool TryComputeLength(out long length)
            {
                length = _content.Length;
                return true;
            }
        }

        private async Task UploadFile(StorageFile file)
        {
            var uri = new Uri("https://file.io/");

            using (var httpClient = new HttpClient())
            {
                using (var content = new MultipartFormDataContent())
                {
                    var fileStream = await file.OpenReadAsync();
                    var streamContent = new ProgressableStreamContent(fileStream.AsStreamForRead(), ReportProgress);
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    content.Add(streamContent, "file", file.Name);

                    var response = await httpClient.PostAsync(uri, content);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            var responseObject = System.Text.Json.JsonDocument.Parse(responseContent).RootElement;
                            var fileLink = responseObject.GetProperty("link").GetString();
                            var dataPackage = new DataPackage();
                            dataPackage.SetText(fileLink);
                            Clipboard.SetContent(dataPackage);
                            progressRingButton.IsEnabled = true;
                            progressRingButton.Content = "File uploaded!";
                            AddToHistory(file.Name, "Successful", fileLink);
                        }
                        else
                        {
                            progressRingButton.IsEnabled = true;
                            progressRingButton.Content = "File failed to upload.";
                            AddToHistory(file.Name, "Unsuccessful", "N/A");
                        }
                    });
                }
            }
        }

        private void ReportProgress(long bytesSent, long totalBytes)
        {
            double progress = (double)bytesSent / totalBytes * 100;
            DispatcherQueue.TryEnqueue(() =>
            {
                progressRing.Value = progress;
            });
        }
    }
    public class UploadedFile {
        public string FileName { get; set; }
        public string Status { get; set; }
        public string URL { get; set; }
    }
}
