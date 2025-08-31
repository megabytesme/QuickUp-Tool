using QuickUp.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;

namespace _1703_UWP
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
        private string _filter;
        private List<UploadedFile> _originalFiles;

        public MainPage()
        {
            this.InitializeComponent();
            LoadHistory();
            var uploadManager = new UploadManager(UploadRepository.Instance);
            _uploadService = new UploadService(uploadManager, UploadRepository.Instance);
        }

        private void LoadHistory()
        {
            this.noHistory.Visibility = Visibility.Collapsed;
            manualFilesGrid.Children.Clear();
            manualFilesGrid.RowDefinitions.Clear();

            if (!UploadedFiles.Any())
            {
                this.noHistory.Visibility = Visibility.Visible;
                return;
            }

            this.noHistory.Visibility = Visibility.Collapsed;

            Grid headerRow = new Grid();
            headerRow.Padding = new Thickness(12);
            headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star), MinWidth = 150 });
            headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 80 });
            headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star), MinWidth = 200 });
            headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star), MinWidth = 120 });
            headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star), MinWidth = 100 });
            headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 80 });

            TextBlock fileNameHeader = new TextBlock { Text = "File Name", FontWeight = FontWeights.SemiBold, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(fileNameHeader, 0);
            headerRow.Children.Add(fileNameHeader);

            TextBlock statusHeader = new TextBlock { Text = "Status", FontWeight = FontWeights.SemiBold, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(statusHeader, 1);
            headerRow.Children.Add(statusHeader);

            TextBlock urlHeader = new TextBlock { Text = "URL", FontWeight = FontWeights.SemiBold, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(urlHeader, 2);
            headerRow.Children.Add(urlHeader);

            TextBlock expiryDateHeader = new TextBlock { Text = "Expiry Date", FontWeight = FontWeights.SemiBold, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(expiryDateHeader, 3);
            headerRow.Children.Add(expiryDateHeader);

            TextBlock fileSizeHeader = new TextBlock { Text = "File Size", FontWeight = FontWeights.SemiBold, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(fileSizeHeader, 4);
            headerRow.Children.Add(fileSizeHeader);

            TextBlock deleteButtonHeader = new TextBlock { Text = "", FontWeight = FontWeights.SemiBold, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }; // **Empty header for Delete Button**
            Grid.SetColumn(deleteButtonHeader, 5);
            headerRow.Children.Add(deleteButtonHeader);

            manualFilesGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            manualFilesGrid.Children.Add(headerRow);
            Grid.SetRow(headerRow, 0);

            int rowIndex = 1;

            foreach (var file in UploadedFiles)
            {
                Grid dataRow = new Grid();
                dataRow.Padding = new Thickness(12);
                dataRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star), MinWidth = 150 });
                dataRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 80 });
                dataRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star), MinWidth = 200 });
                dataRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star), MinWidth = 120 });
                dataRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star), MinWidth = 100 });
                dataRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 80 });

                string fileNameToDisplay = file.FileName ?? "File name not available";
                TextBlock fileNameText = new TextBlock { Text = fileNameToDisplay, TextWrapping = TextWrapping.Wrap, TextTrimming = TextTrimming.CharacterEllipsis, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(fileNameText, 0);
                dataRow.Children.Add(fileNameText);

                string statusToDisplay = file.Status ?? "Status not available";
                TextBlock statusText = new TextBlock { Text = statusToDisplay, TextWrapping = TextWrapping.Wrap, TextTrimming = TextTrimming.CharacterEllipsis, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(statusText, 1);
                dataRow.Children.Add(statusText);

                HyperlinkButton urlButton = new HyperlinkButton { HorizontalAlignment = HorizontalAlignment.Center, MaxWidth = 300, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(urlButton, 2);
                string urlToDisplay = file.URL ?? "URL not available";
                TextBlock urlTextBlock = new TextBlock
                {
                    Text = urlToDisplay,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                urlButton.Content = urlTextBlock;
                dataRow.Children.Add(urlButton);

                TextBlock expiryDateText = new TextBlock { Text = file.DaysUntilExpiry, TextWrapping = TextWrapping.Wrap, TextTrimming = TextTrimming.CharacterEllipsis, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(expiryDateText, 3);
                dataRow.Children.Add(expiryDateText);

                string fileSizeToDisplay = file.FileSizeReadable ?? "Size not available";
                TextBlock fileSizeText = new TextBlock { Text = fileSizeToDisplay, TextWrapping = TextWrapping.Wrap, TextTrimming = TextTrimming.CharacterEllipsis, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                Grid.SetColumn(fileSizeText, 4);
                dataRow.Children.Add(fileSizeText);

                Button deleteButton = new Button { Width = 36, Height = 36, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
                deleteButton.Click += DeleteButton_Click;
                deleteButton.DataContext = file;
                FontIcon deleteIcon = new FontIcon { FontSize = 12, Glyph = "\uE74D" };
                deleteButton.Content = deleteIcon;
                Grid.SetColumn(deleteButton, 5);
                dataRow.Children.Add(deleteButton);

                manualFilesGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                manualFilesGrid.Children.Add(dataRow);
                Grid.SetRow(dataRow, rowIndex);
                rowIndex++;
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is UploadedFile file)
            {
                UploadRepository.Instance.DeleteUploadedFile(file);
                LoadHistory();
            }
            if (!UploadedFiles.Any())
            {
                this.noHistory.Visibility = Visibility.Visible;
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
                LoadHistory();
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
                PrimaryButtonText = "OK"
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

        private async void DeleteAllButton_Click(object sender, RoutedEventArgs e)
        {
            var filesToDelete = UploadedFiles.ToList();
            foreach (var file in filesToDelete)
            {
                UploadRepository.Instance.DeleteUploadedFile(file);
            }
            UploadedFiles.Clear();
            LoadHistory();
            this.noHistory.Visibility = Visibility.Visible;
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
                            new Run() { Text = "Version 1.0.3.0 (1507_UWP)" },
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
                            new Run() { Text = "Like what you see? View my " },
                            new Hyperlink()
                            {
                                NavigateUri = new Uri("https://github.com/megabytesme"),
                                Inlines = { new Run() { Text = "GitHub" } }
                            },
                            new Run() { Text = " and maybe my " },
                            new Hyperlink()
                            {
                                NavigateUri = new Uri("https://apps.microsoft.com/search?query=megabytesme"),
                                Inlines = { new Run() { Text = "Other Apps" } }
                            },
                            new LineBreak(),
                            new Run() { Text = " "},
                            new LineBreak(),
                            new Run() { Text = "QuickUp Tool is a Windows utility which allows you to quickly upload files without needing an account." }
                        },
                        TextWrapping = TextWrapping.Wrap
                    }
                },
                PrimaryButtonText = "OK"
            };

            await dialog.ShowAsync();
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
            }
            else
            {
                this.noHistory.Visibility = Visibility.Collapsed;
            }
            LoadHistory();
        }
    }
}