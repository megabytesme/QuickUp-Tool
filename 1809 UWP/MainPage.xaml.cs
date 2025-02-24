using Microsoft.UI.Xaml.Controls;
using QuickUp.Shared;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace _1809_UWP
{
    public sealed partial class MainPage : Page
    {
        FontIcon uploadIcon = new FontIcon
        {
            Glyph = "\uE898",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        public ObservableCollection<UploadedFile> UploadedFiles { get; } = new ObservableCollection<UploadedFile>();

        public MainPage()
        {
            this.InitializeComponent();
            ExtendViewIntoTitleBar();
            SetTitleBarBackground();
            ApplyBackdropOrAcrylic();
            _ = LoadHistoryAsync();
        }

        private void ExtendViewIntoTitleBar()
        {
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
        }

        private void SetTitleBarBackground()
        {
            var appTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            appTitleBar.ButtonBackgroundColor = Colors.Transparent;
            appTitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            Window.Current.SetTitleBar(AppTitleBar);
        }

        private void ApplyBackdropOrAcrylic()
        {
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 12))
            {
            muxc: BackdropMaterial.SetApplyToRootOrPageBackground(this, true);
            }
            else
            {
                this.Background = new AcrylicBrush
                {
                    BackgroundSource = AcrylicBackgroundSource.HostBackdrop,
                    TintColor = Colors.Transparent,
                    TintOpacity = 0.6,
                    FallbackColor = Colors.Gray
                };
            }
        }

        private async Task LoadHistoryAsync()
        {
            this.filesGrid.Visibility = Visibility.Collapsed;
            var uploadedFiles = await FileManager.LoadHistoryAsync();
            if (uploadedFiles.Any())
            {
                this.noHistory.Visibility = Visibility.Collapsed;
                this.filesGrid.Visibility = Visibility.Visible;
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
                    this.filesGrid.Visibility = Visibility.Collapsed;
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
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add("*");
            var storageFile = await picker.PickSingleFileAsync();
            if (storageFile != null)
            {
                await HandleFile(storageFile);
            }
        }

        private async void ProgressRingButton_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
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
            progressRingButton.IsEnabled = false;
            buttonText.FontFamily = new FontFamily("XamlAutoFontFamily");
            buttonText.Text = "Uploading" + Environment.NewLine + file.Name;

            string url = await UploadManager.UploadFile(file, ReportProgress);

            progressRingButton.IsEnabled = true;
            buttonText.FontFamily = uploadIcon.FontFamily;
            buttonText.Text = uploadIcon.Glyph;

            this.filesGrid.Visibility = Visibility.Visible;

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

            this.progressBar.Value = 0;
        }

        private async void ReportProgress(long bytesSent, long totalBytes)
        {
            if (totalBytes == 0)
            {
                return;
            }

            double progressPercentage = (double)bytesSent / totalBytes * 100;

            await progressBar.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                progressBar.Value = (int)Math.Min(progressPercentage, 100);
            });
        }

        private void ProgressRingButton_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }
    }
}
