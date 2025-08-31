using System;
using Windows.Storage;
using Windows.Services.Store;

namespace _1809_UWP.Services
{
    public static class ReviewRequestService
    {
        private static StoreContext _storeContext;

        private static readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;
        private const string LaunchCountKey = "AppLaunchCount";
        private const string UploadCountKey = "SuccessfulUploadCount";
        private const string ReviewRequestShownKey = "ReviewRequestShown";

        public static void Initialize()
        {
            _storeContext = StoreContext.GetDefault();
        }

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

        public static async void TryRequestReview()
        {
            if (_storeContext == null)
                return;

            bool alreadyShown = _localSettings.Values[ReviewRequestShownKey] as bool? ?? false;
            int launchCount = _localSettings.Values[LaunchCountKey] as int? ?? 0;
            int uploadCount = _localSettings.Values[UploadCountKey] as int? ?? 0;

            if (!alreadyShown && launchCount >= 2 && uploadCount >= 3)
            {
                _localSettings.Values[ReviewRequestShownKey] = true;

                StoreRateAndReviewResult result = await _storeContext.RequestRateAndReviewAppAsync();
            }
        }
    }
}