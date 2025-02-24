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

        public ObservableCollection<UploadedFile> UploadedFiles { get; } = UploadRepository.Instance.UploadedFiles;
        private readonly UploadService _uploadService;

        public MainPage()
        {
            this.InitializeComponent();
            ExtendViewIntoTitleBar();
            SetTitleBarBackground();
            ApplyBackdropOrAcrylic();
            LoadHistory();
            var uploadManager = new UploadManager(UploadRepository.Instance);
            _uploadService = new UploadService(uploadManager, UploadRepository.Instance);
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

        private void LoadHistory()
        {
            this.filesListView.Visibility = Visibility.Collapsed;

            if (UploadedFiles.Any())
            {
                this.noHistory.Visibility = Visibility.Collapsed;
                this.filesListView.Visibility = Visibility.Visible;
            }
            else
            {
                this.noHistory.Visibility = Visibility.Visible;
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is UploadedFile file)
            {
                UploadRepository.Instance.DeleteUploadedFile(file);
                if (!UploadedFiles.Any())
                {
                    this.noHistory.Visibility = Visibility.Visible;
                    this.filesListView.Visibility = Visibility.Collapsed;
                }
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
                    foreach (var item in items)
                    {
                        if (item is StorageFile storageFile)
                        {
                            await HandleFile(storageFile);
                        }
                    }
                }
            }
        }

        private async Task HandleFile(StorageFile file)
        {
            progressRingButton.IsEnabled = false;
            progressRingButton.FontFamily = new FontFamily("XamlAutoFontFamily");
            progressRingButton.Content = "Uploading" + Environment.NewLine + file.Name;
            string url = null;
            UploadResult uploadResult = null;

            var progressIndicator = new Progress<double>(ReportProgress);

            try
            {
                uploadResult = await _uploadService.ProcessFileAsync(file, progressIndicator);
                if (uploadResult.IsSuccessful && !string.IsNullOrEmpty(uploadResult.Url))
                {
                    url = uploadResult.Url;
                    var dataPackage = new DataPackage();
                    dataPackage.SetText(url);
                    Clipboard.SetContent(dataPackage);
                }
                else
                {
                    await ShowUploadErrorDialog(uploadResult.ErrorMessage ?? "Unknown upload error.");
                }
            }
            finally
            {
                progressRingButton.IsEnabled = true;
                this.progressRingButton.Content = uploadIcon;
                this.progressBar.Value = 0;
            }
        }

        private async Task ShowUploadErrorDialog(string errorMessage)
        {
            ContentDialog errorDialog = new ContentDialog
            {
                Title = "Upload Error",
                Content = $"File upload failed. Error: {errorMessage}",
                CloseButtonText = "OK"
            };

            await errorDialog.ShowAsync();
        }

        private void ReportProgress(double percentage)
        {
            progressBar.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                progressBar.Value = (int)Math.Min(percentage, 100);
            });
        }

        private void ProgressRingButton_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }
    }
}