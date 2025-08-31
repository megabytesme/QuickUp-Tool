using Microsoft.UI.Xaml.Controls;
using QuickUp.Shared;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace _1809_UWP
{
    public sealed partial class SharePage : Page
    {
        private readonly UploadService _uploadService;
        private readonly UploadRepository _uploadRepository;

        public SharePage()
        {
            this.InitializeComponent();
            ApplyBackdropOrAcrylic();
            _uploadRepository = UploadRepository.Instance;
            var uploadManager = new UploadManager(_uploadRepository);
            _uploadService = new UploadService(uploadManager, _uploadRepository);
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

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is ShareTargetActivatedEventArgs args)
            {
                await ProcessShareOperationAsync(args.ShareOperation);
            }
        }

        private async Task ProcessShareOperationAsync(Windows.ApplicationModel.DataTransfer.ShareTarget.ShareOperation shareOperation)
        {
            if (shareOperation.Data.Contains(StandardDataFormats.StorageItems))
            {
                var items = await shareOperation.Data.GetStorageItemsAsync();
                var file = items.FirstOrDefault(i => i is StorageFile) as StorageFile;

                if (file != null)
                {
                    await HandleFile(file);
                }
                else
                {
                    await ShowResultDialog("Error", "No valid file was shared.", "Close");
                }
            }
        }

        private async Task HandleFile(StorageFile file)
        {
            StatusText.Text = $"Uploading {file.Name}...";
            var progressIndicator = new Progress<double>(ReportProgress);
            UploadResult uploadResult = null;

            try
            {
                uploadResult = await _uploadService.ProcessSharedFileAsync(file, progressIndicator);

                if (uploadResult.IsSuccessful && !string.IsNullOrEmpty(uploadResult.Url))
                {
                    var dataPackage = new DataPackage();
                    dataPackage.SetText(uploadResult.Url);
                    Clipboard.SetContent(dataPackage);

                    await ShowResultDialog("Upload Successful", $"{file.Name} is ready to share.\nThe URL has been copied to your clipboard.", "Close");
                }
                else
                {
                    await ShowResultDialog("Upload Failed", uploadResult?.ErrorMessage ?? "An unknown error occurred.", "Close");
                }
            }
            catch (Exception ex)
            {
                await ShowResultDialog("Upload Failed", ex.Message, "Close");
            }
        }

        private void ReportProgress(double percentage)
        {
            progressBar.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                int value = (int)Math.Min(percentage, 100);
                progressBar.Value = value;
                if (value >= 100)
                {
                    progressBar.IsIndeterminate = true;
                    StatusText.Text = "Processing...";
                }
            });
        }

        private async Task ShowResultDialog(string title, string content, string closeButtonText)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = closeButtonText
            };
            await dialog.ShowAsync();
            Window.Current.Close();
        }
    }
}