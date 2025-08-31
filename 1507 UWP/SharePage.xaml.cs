using QuickUp.Shared;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace _1507_UWP
{
    public sealed partial class SharePage : Page
    {
        private readonly UploadService _uploadService;
        private readonly UploadRepository _uploadRepository;

        public SharePage()
        {
            this.InitializeComponent();
            _uploadRepository = UploadRepository.Instance;
            var uploadManager = new UploadManager(_uploadRepository);
            _uploadService = new UploadService(uploadManager, _uploadRepository);
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
                PrimaryButtonText = closeButtonText
            };
            await dialog.ShowAsync();
            Window.Current.Close();
        }
    }
}