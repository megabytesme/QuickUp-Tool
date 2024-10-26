using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using Windows.Storage.Pickers;
using Windows.Storage;
using Microsoft.UI.Xaml.Media;
using System.Threading.Tasks;
using QuickUp.Shared;
using Microsoft.UI.Dispatching;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Windows.ApplicationModel.DataTransfer;

namespace QuickUp
{
    public sealed partial class MainWindow : Window
    {
        FontIcon uploadIcon = new FontIcon
        {
            Glyph = "\uE898",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        public ObservableCollection<UploadedFile> UploadedFiles { get; } = new ObservableCollection<UploadedFile>();
        private DispatcherQueue dispatcherQueue;

        public MainWindow()
        {
            this.InitializeComponent();
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(AppTitleBar);
            _ = LoadHistoryAsync();
        }

        private async Task LoadHistoryAsync()
        {
            var uploadedFiles = await FileManager.LoadHistoryAsync();
            if (uploadedFiles.Any())
            {
                this.noHistory.Visibility = Visibility.Collapsed;
                foreach (var file in uploadedFiles)
                {
                    UploadedFiles.Add(file);
                    System.Diagnostics.Debug.WriteLine($"Loaded: {file.FileName}, {file.Status}, {file.URL}");
                }
            }
            else
            {
                this.noHistory.Visibility = Visibility.Visible;
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is UploadedFile file)
            {
                UploadedFiles.Remove(file);
                if (UploadedFiles.Count == 0)
                {
                    this.noHistory.Visibility = Visibility.Visible;
                }
                await FileManager.SaveHistoryAsync(UploadedFiles.ToList());
            }
        }

        private async void AddToHistory(string fileName, string status, string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var validUri))
            {
                UploadedFiles.Add(new UploadedFile { FileName = fileName, Status = status, URL = validUri.ToString() });
                await FileManager.SaveHistoryAsync(UploadedFiles.ToList());
                this.noHistory.Visibility = Visibility.Collapsed;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Invalid URL: {url}");
            }
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
            dispatcherQueue.TryEnqueue(() =>
            {
                progressRingButton.IsEnabled = false;
                progressRingButton.Content = "Uploading" + Environment.NewLine + file.Name;
            });

            string url = await UploadManager.UploadFile(file, ReportProgress);
            dispatcherQueue.TryEnqueue(() =>
            {
                progressRingButton.IsEnabled = true;
                this.progressRingButton.Content = uploadIcon;
                if (!string.IsNullOrEmpty(url))
                {
                    AddToHistory(file.Name, "Successful", url);
                    var dataPackage = new DataPackage();
                    dataPackage.SetText(url);
                    Clipboard.SetContent(dataPackage);
                }
                else
                {
                    AddToHistory(file.Name, "Unsuccessful - Failed", "N/A");
                }
                this.progressRing.Value = 0;
            });
        }

        private void ReportProgress(long bytesSent, long totalBytes)
        {
            double progress = (double)bytesSent / totalBytes * 100;
            dispatcherQueue.TryEnqueue(() => { progressRing.Value = progress; });
        }

        private void ProgressRingButton_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
        }
    }
}