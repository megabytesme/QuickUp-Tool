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

namespace _1507_UWP
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
            manualFilesGrid.Children.Clear();
            manualFilesGrid.RowDefinitions.Clear();
            manualFilesGrid.ColumnDefinitions.Clear();

            if (!UploadedFiles.Any())
            {
                manualFilesGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                manualFilesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                Grid.SetRow(noHistory, 0);
                Grid.SetColumn(noHistory, 0);
                manualFilesGrid.Children.Add(noHistory);
                this.noHistory.Visibility = Visibility.Visible;
                return;
            }

            this.noHistory.Visibility = Visibility.Collapsed;

            manualFilesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star), MinWidth = 150 });
            manualFilesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 80 });
            manualFilesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star), MinWidth = 200 });
            manualFilesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star), MinWidth = 120 });
            manualFilesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star), MinWidth = 100 });
            manualFilesGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 80 });

            manualFilesGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            TextBlock fileNameHeader = new TextBlock { Text = "File Name", FontWeight = FontWeights.SemiBold, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(12) };
            Grid.SetRow(fileNameHeader, 0);
            Grid.SetColumn(fileNameHeader, 0);
            manualFilesGrid.Children.Add(fileNameHeader);

            TextBlock statusHeader = new TextBlock { Text = "Status", FontWeight = FontWeights.SemiBold, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(12) };
            Grid.SetRow(statusHeader, 0);
            Grid.SetColumn(statusHeader, 1);
            manualFilesGrid.Children.Add(statusHeader);

            TextBlock urlHeader = new TextBlock { Text = "URL", FontWeight = FontWeights.SemiBold, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(12) };
            Grid.SetRow(urlHeader, 0);
            Grid.SetColumn(urlHeader, 2);
            manualFilesGrid.Children.Add(urlHeader);

            TextBlock expiryDateHeader = new TextBlock { Text = "Expiry Date", FontWeight = FontWeights.SemiBold, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(12) };
            Grid.SetRow(expiryDateHeader, 0);
            Grid.SetColumn(expiryDateHeader, 3);
            manualFilesGrid.Children.Add(expiryDateHeader);

            TextBlock fileSizeHeader = new TextBlock { Text = "File Size", FontWeight = FontWeights.SemiBold, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(12) };
            Grid.SetRow(fileSizeHeader, 0);
            Grid.SetColumn(fileSizeHeader, 4);
            manualFilesGrid.Children.Add(fileSizeHeader);

            int rowIndex = 1;

            var hoverBrush = new SolidColorBrush(Windows.UI.Colors.Gray) { Opacity = 0.2 };
            var transparentBrush = new SolidColorBrush(Windows.UI.Colors.Transparent);

            foreach (var file in UploadedFiles)
            {
                manualFilesGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var rowBorders = new List<Border>();

                for (int i = 0; i < manualFilesGrid.ColumnDefinitions.Count; i++)
                {
                    var cellBorder = new Border
                    {
                        BorderThickness = new Thickness(1, 1, i == (manualFilesGrid.ColumnDefinitions.Count - 1) ? 1 : 0, 1),
                        BorderBrush = transparentBrush,
                        Background = transparentBrush
                    };

                    Grid.SetRow(cellBorder, rowIndex);
                    Grid.SetColumn(cellBorder, i);
                    rowBorders.Add(cellBorder);
                    manualFilesGrid.Children.Add(cellBorder);

                    UIElement content = null;
                    switch (i)
                    {
                        case 0:
                            content = new TextBlock { Text = file.FileName ?? "N/A", TextWrapping = TextWrapping.Wrap, Padding = new Thickness(12), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                            break;
                        case 1:
                            content = new TextBlock { Text = file.Status ?? "N/A", Padding = new Thickness(12), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                            break;
                        case 2:
                            var urlButton = new HyperlinkButton { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(12) };
                            urlButton.Content = new TextBlock { Text = file.URL ?? "N/A", TextWrapping = TextWrapping.Wrap };
                            if (Uri.TryCreate(file.URL, UriKind.Absolute, out var uri)) urlButton.NavigateUri = uri;
                            content = urlButton;
                            break;
                        case 3:
                            content = new TextBlock { Text = file.DaysUntilExpiry, Padding = new Thickness(12), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                            break;
                        case 4:
                            content = new TextBlock { Text = file.FileSizeReadable ?? "N/A", Padding = new Thickness(12), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                            break;
                        case 5:
                            var deleteButton = new Button { Width = 36, Height = 36, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
                            deleteButton.Click += DeleteButton_Click;
                            deleteButton.DataContext = file;
                            deleteButton.Content = new FontIcon { FontSize = 12, Glyph = "\uE74D" };
                            content = deleteButton;
                            break;
                    }

                    if (content != null)
                    {
                        Grid.SetRow((FrameworkElement)content, rowIndex);
                        Grid.SetColumn((FrameworkElement)content, i);
                        manualFilesGrid.Children.Add(content);
                    }
                }

                var topBorder = new Border { BorderThickness = new Thickness(0, 1, 0, 0), BorderBrush = transparentBrush };
                Grid.SetRow(topBorder, rowIndex);
                Grid.SetColumn(topBorder, 0);
                Grid.SetColumnSpan(topBorder, manualFilesGrid.ColumnDefinitions.Count);
                rowBorders.Add(topBorder);
                manualFilesGrid.Children.Add(topBorder);

                foreach (var element in manualFilesGrid.Children.Where(c => Grid.GetRow(c as FrameworkElement) == rowIndex))
                {
                    element.PointerEntered += (s, e) =>
                    {
                        foreach (var b in rowBorders)
                        {
                            b.Background = hoverBrush;
                        }
                    };

                    element.PointerExited += (s, e) =>
                    {
                        foreach (var b in rowBorders)
                        {
                            b.Background = transparentBrush;
                            b.BorderBrush = transparentBrush;
                        }
                    };
                }
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

                    var dialog = new ContentDialog
                    {
                        Title = "Upload Successful",
                        Content = "The URL has been copied to your clipboard.",
                        PrimaryButtonText = "Close",
                        SecondaryButtonText = "Open in Browser",
                    };

                    ContentDialogResult result = await dialog.ShowAsync();

                    if (result == ContentDialogResult.Secondary)
                    {
                        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                        {
                            await Windows.System.Launcher.LaunchUriAsync(uri);
                        }
                    }
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
                                NavigateUri = new Uri("https://apps.microsoft.com/search/publisher?name=MegaBytesMe"),
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