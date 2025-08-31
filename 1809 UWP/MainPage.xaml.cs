using _1809_UWP.Services;
using Microsoft.UI.Xaml.Controls;
using QuickUp.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Data.Xml.Dom;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Notifications;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace _1809_UWP
{
    public sealed partial class MainPage : Page
    {
        private readonly Queue<DialogQueueItem> _dialogQueue = new Queue<DialogQueueItem>();
        private bool _isDialogShowing = false;
        private UploadedFile _fileToShare;

        FontIcon uploadIcon = new FontIcon
        {
            Glyph = "\uE898",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        public ObservableCollection<UploadedFile> UploadedFiles { get; } = UploadRepository.Instance.UploadedFiles;
        private readonly UploadService _uploadService;
        private string _filter;
        private List<UploadedFile> _originalFiles;

        public MainPage()
        {
            this.InitializeComponent();
            ExtendViewIntoTitleBar();
            SetTitleBarBackground();
            ApplyBackdropOrAcrylic();
            LoadHistory();
            var uploadManager = new UploadManager(UploadRepository.Instance);
            _uploadService = new UploadService(uploadManager, UploadRepository.Instance);
            DataTransferManager.GetForCurrentView().DataRequested += OnDataRequested;
        }

        private class DialogQueueItem
        {
            public ContentDialog Dialog { get; }
            public TaskCompletionSource<ContentDialogResult> CompletionSource { get; }

            public DialogQueueItem(ContentDialog dialog)
            {
                Dialog = dialog;
                CompletionSource = new TaskCompletionSource<ContentDialogResult>();
            }
        }

        private async Task<ContentDialogResult> ShowQueuedDialogAsync(ContentDialog dialog)
        {
            var queueItem = new DialogQueueItem(dialog);

            _dialogQueue.Enqueue(queueItem);

            ProcessDialogQueueAsync();

            return await queueItem.CompletionSource.Task;
        }

        private async void ProcessDialogQueueAsync()
        {
            if (_isDialogShowing || _dialogQueue.Count == 0)
            {
                return;
            }

            _isDialogShowing = true;
            DialogQueueItem itemToShow = null;

            try
            {
                itemToShow = _dialogQueue.Dequeue();

                ContentDialogResult result = await itemToShow.Dialog.ShowAsync();

                itemToShow.CompletionSource.SetResult(result);
            }
            catch (Exception ex)
            {
                itemToShow?.CompletionSource.TrySetException(ex);
            }
            finally
            {
                _isDialogShowing = false;
                ProcessDialogQueueAsync();
            }
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

        private async void UrlButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is UploadedFile file && !string.IsNullOrEmpty(file.URL))
            {
                var dataPackage = new DataPackage();
                dataPackage.SetText(file.URL);
                Clipboard.SetContent(dataPackage);

                var dialog = new ContentDialog
                {
                    Title = "URL Copied",
                    Content = "The URL has been copied to your clipboard.",
                    PrimaryButtonText = "Close",
                    SecondaryButtonText = "Open in Browser"
                };

                dialog.SecondaryButtonClick += async (s, args) =>
                {
                    if (Uri.TryCreate(file.URL, UriKind.Absolute, out var uri))
                    {
                        await Windows.System.Launcher.LaunchUriAsync(uri);
                    }
                };

                await ShowQueuedDialogAsync(dialog);
            }
        }

        private async void FilesListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (filesListView.SelectedItem is UploadedFile file)
            {
                var detailsPanel = new StackPanel { Spacing = 8 };
                detailsPanel.Children.Add(new TextBlock { Text = $"File Name: {file.FileName ?? "N/A"}", TextWrapping = TextWrapping.Wrap });
                detailsPanel.Children.Add(new TextBlock { Text = $"Status: {file.Status ?? "N/A"}" });
                detailsPanel.Children.Add(new TextBlock { Text = $"URL: {file.URL ?? "N/A"}", TextWrapping = TextWrapping.Wrap });
                detailsPanel.Children.Add(new TextBlock { Text = $"Expires: {file.DaysUntilExpiry ?? "N/A"}" });
                detailsPanel.Children.Add(new TextBlock { Text = $"File Size: {file.FileSizeReadable ?? "N/A"}" });

                var dialog = new ContentDialog
                {
                    Title = "File Details",
                    Content = detailsPanel,
                    CloseButtonText = "Close"
                };

                await ShowQueuedDialogAsync(dialog);
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is UploadedFile file)
            {
                var confirmationDialog = new ContentDialog
                {
                    Title = "Delete File?",
                    Content = $"Are you sure you want to delete '{file.FileName}'?\nThis will remove it from your history and online.",
                    PrimaryButtonText = "Cancel",
                    SecondaryButtonText = "Delete"
                };

                var result = await ShowQueuedDialogAsync(confirmationDialog);

                if (result == ContentDialogResult.Secondary)
                {
                    bool deletedSuccessfully = false;

                    if (file.IsExpired)
                    {
                        deletedSuccessfully = true;
                    }
                    else
                    {
                        deletedSuccessfully = await _uploadService.DeleteRemoteFileAsync(file);
                    }

                    if (deletedSuccessfully)
                    {
                        UploadRepository.Instance.DeleteUploadedFile(file);

                        if (!UploadedFiles.Any())
                        {
                            this.noHistory.Visibility = Visibility.Visible;
                            this.filesListView.Visibility = Visibility.Collapsed;
                        }
                    }
                    else
                    {
                        var errorDialog = new ContentDialog
                        {
                            Title = "Deletion Failed",
                            Content = "Could not delete the file from the server. Please check your internet connection and try again.",
                            PrimaryButtonText = "OK"
                        };
                        await ShowQueuedDialogAsync(errorDialog);
                    }
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
            UploadResult uploadResult = null;

            var progressIndicator = new Progress<double>(ReportProgress);

            try
            {
                uploadResult = await _uploadService.ProcessFileAsync(file, progressIndicator);
                LoadHistory();
                filesListView.ScrollIntoView(file);
                if (uploadResult.IsSuccessful && !string.IsNullOrEmpty(uploadResult.Url))
                {
                    ToastTemplateType toastTemplate = ToastTemplateType.ToastText02;
                    XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);
                    XmlNodeList toastTextElements = toastXml.GetElementsByTagName("text");
                    toastTextElements[0].AppendChild(toastXml.CreateTextNode("Upload Successful!"));
                    toastTextElements[1].AppendChild(toastXml.CreateTextNode($"{file.Name} has been uploaded."));
                    ToastNotification toast = new ToastNotification(toastXml);
                    ToastNotificationManager.CreateToastNotifier().Show(toast);

                    ReviewRequestService.IncrementSuccessfulUploadCount();
                    ReviewRequestService.TryRequestReview();

                    string url = uploadResult.Url;
                    var dataPackage = new DataPackage();
                    dataPackage.SetText(url);
                    Clipboard.SetContent(dataPackage);

                    var dialog = new ContentDialog
                    {
                        Title = "Upload Successful",
                        Content = "The URL has been copied to your clipboard.",
                        PrimaryButtonText = "Close",
                        SecondaryButtonText = "Open in Browser",
                    };

                    dialog.SecondaryButtonClick += async (s, args) =>
                    {
                        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                        {
                            await Windows.System.Launcher.LaunchUriAsync(uri);
                        }
                    };

                    await ShowQueuedDialogAsync(dialog);
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
                this.progressBar.IsIndeterminate = false;
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

            await ShowQueuedDialogAsync(errorDialog);
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
                    buttonText.Text = "Processing...";
                }
            });
        }

        private void ProgressRingButton_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }

        private async void DeleteAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (!UploadedFiles.Any())
            {
                return;
            }

            var initialDialog = new ContentDialog
            {
                Title = "Delete All History?",
                Content = "This will attempt to delete all non-expired files from the server and clear your entire local history.",
                PrimaryButtonText = "Cancel",
                SecondaryButtonText = "Proceed"
            };

            var initialResult = await ShowQueuedDialogAsync(initialDialog);

            if (initialResult != ContentDialogResult.Secondary)
            {
                return;
            }

            var filesToDelete = UploadedFiles.ToList();
            var failedDeletions = new List<UploadedFile>();

            foreach (var file in filesToDelete)
            {
                if (!file.IsExpired)
                {
                    bool success = await _uploadService.DeleteRemoteFileAsync(file);
                    if (!success)
                    {
                        failedDeletions.Add(file);
                    }
                }
            }

            if (failedDeletions.Count == 0)
            {
                ClearAllLocalHistory();
            }
            else
            {
                string errorMessage = "The following files could not be deleted from the server, possibly due to a network issue:\n\n";
                errorMessage += string.Join("\n", failedDeletions.Select(f => f.FileName));

                if (failedDeletions.Count > 5)
                {
                    errorMessage += $"\n... and {failedDeletions.Count - 5} more.";
                }

                errorMessage += "\n\nDo you want to clear your local history anyway?";

                var forceClearDialog = new ContentDialog
                {
                    Title = "Deletion Failed for Some Files",
                    Content = errorMessage,
                    PrimaryButtonText = "Cancel",
                    SecondaryButtonText = "Clear History Anyway"
                };

                var forceClearResult = await ShowQueuedDialogAsync(forceClearDialog);

                if (forceClearResult == ContentDialogResult.Secondary)
                {
                    ClearAllLocalHistory();
                }
            }
        }

        private void ClearAllLocalHistory()
        {
            var filesToClear = UploadedFiles.ToList();
            foreach (var file in filesToClear)
            {
                UploadRepository.Instance.DeleteUploadedFile(file);
            }

            if (!UploadedFiles.Any())
            {
                this.noHistory.Visibility = Visibility.Visible;
                this.filesListView.Visibility = Visibility.Collapsed;
            }
        }

        private async void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "About QuickUp Tool",
                Content = new ScrollViewer()
                {
                    Content = new TextBlock()
                    {
                        Inlines =
                        {
                            new Run() { Text = "QuickUp Tool" },
                            new LineBreak(),
                            new Run() { Text = "Version 2.0.3.0 (1809_UWP)" },
                            new LineBreak(),
                            new Run() { Text = "Copyright © 2025 MegaBytesMe" },
                            new LineBreak(),
                            new Run() { Text = " "},
                            new LineBreak(),
                            new Run() { Text = "Source code available on " },
                            new Hyperlink()
                            {
                                NavigateUri = new Uri("https://github.com/megabytesme/QuickUp-Tool"),
                                Inlines = { new Run() { Text = "GitHub" } }
                            },
                            new LineBreak(),
                            new Run() { Text = "Anything wrong? Let us know: " },
                            new Hyperlink()
                            {
                                NavigateUri = new Uri("https://github.com/megabytesme/QuickUp-Tool/issues"),
                                Inlines = { new Run() { Text = "Support" } }
                            },
                            new LineBreak(),
                            new Run() { Text = "Privacy Policy: " },
                            new Hyperlink()
                            {
                                NavigateUri = new Uri("https://github.com/megabytesme/QuickUp-Tool/blob/master/PRIVACYPOLICY.md"),
                                Inlines = { new Run() { Text = "Privacy Policy" } }
                            },
                            new LineBreak(),
                            new LineBreak(),
                            new Run() { Text = "Like what you see? View my " },
                            new Hyperlink()
                            {
                                NavigateUri = new Uri("https://github.com/megabytesme"),
                                Inlines = { new Run() { Text = "GitHub" } },
                            },
                            new Run() { Text = " and maybe my " },
                            new Hyperlink()
                            {
                                NavigateUri = new Uri(
                                    "https://apps.microsoft.com/search/publisher?name=MegaBytesMe"
                                ),
                                Inlines = { new Run() { Text = "Other Apps," } },
                            },
                            new Run()
                            {
                                Text = " or consider buying me a coffee (supporting me) on ",
                            },
                            new Hyperlink()
                            {
                                NavigateUri = new Uri("https://ko-fi.com/megabytesme"),
                                Inlines = { new Run() { Text = "Ko-fi! :-)" } },
                            },
                            new LineBreak(),
                            new LineBreak(),
                            new Run() { Text = "QuickUp Tool is a Windows utility which allows you to quickly upload files without needing an account." }
                        },
                        TextWrapping = TextWrapping.Wrap
                    }
                },
                CloseButtonText = "OK"
            };

            await ShowQueuedDialogAsync(dialog);
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                _filter = sender.Text.ToLowerInvariant();
                if (_originalFiles == null)
                {
                    _originalFiles = UploadedFiles.ToList();
                }
                RefreshFileList();
            }
        }

        private void RefreshFileList()
        {
            UploadedFiles.Clear();
            if (string.IsNullOrEmpty(_filter))
            {
                if (_originalFiles != null)
                {
                    foreach (var file in _originalFiles)
                    {
                        UploadedFiles.Add(file);
                    }
                }
                else
                {
                    LoadHistory();
                }

            }
            else
            {
                if (_originalFiles != null)
                {
                    foreach (var file in _originalFiles)
                    {
                        if (file.FileName.ToLowerInvariant().Contains(_filter))
                        {
                            UploadedFiles.Add(file);
                        }
                    }
                }
            }

            if (!UploadedFiles.Any())
            {
                this.noHistory.Visibility = Visibility.Visible;
                this.filesListView.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.noHistory.Visibility = Visibility.Collapsed;
                this.filesListView.Visibility = Visibility.Visible;
            }
        }

        private void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is UploadedFile file)
            {
                _fileToShare = file;
                DataTransferManager.ShowShareUI();
            }
        }

        private void OnDataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            if (_fileToShare != null)
            {
                DataRequest request = args.Request;
                request.Data.Properties.Title = $"Link for {_fileToShare.FileName}";
                request.Data.SetWebLink(new Uri(_fileToShare.URL));
                request.Data.SetText($"Here is the link to my uploaded file: {_fileToShare.URL}");
            }
        }
    }
}