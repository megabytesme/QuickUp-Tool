using System;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

namespace _1507_UWP.Services
{
    public static class ReviewRequestService
    {
        private static readonly ApplicationDataContainer _localSettings = ApplicationData
            .Current
            .LocalSettings;
        private const string LaunchCountKey = "AppLaunchCount";
        private const string UploadCountKey = "SuccessfulUploadCount";
        private const string ReviewRequestShownKey = "ReviewRequestShown";

        public static void Initialize() { }

        public static void IncrementLaunchCount()
        {
            int launchCount = _localSettings.Values[LaunchCountKey] as int? ?? 0;
            _localSettings.Values[LaunchCountKey] = launchCount + 1;
        }

        public static void IncrementSuccessfulUploadCount()
        {
            int uploadCount = _localSettings.Values[UploadCountKey] as int? ?? 0;
            _localSettings.Values[UploadCountKey] = uploadCount + 1;
        }

        public static bool AlreadyShown => _localSettings.Values[ReviewRequestShownKey] as bool? ?? false;

        public static async void TryRequestReview()
        {
            bool alreadyShown = _localSettings.Values[ReviewRequestShownKey] as bool? ?? false;
            int launchCount = _localSettings.Values[LaunchCountKey] as int? ?? 0;
            int uploadCount = _localSettings.Values[UploadCountKey] as int? ?? 0;

            if (!alreadyShown && launchCount >= 2 && uploadCount >= 3)
            {
                _localSettings.Values[ReviewRequestShownKey] = true;

                string storeId = "9npfvnqdx777";

                var reviewUri = new Uri($"ms-windows-store://review/?ProductId={storeId}");

                await Windows.System.Launcher.LaunchUriAsync(reviewUri);
            }
        }
    }
}
