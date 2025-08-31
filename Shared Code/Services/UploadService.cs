using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace QuickUp.Shared
{
    public class UploadService
    {
        private readonly UploadManager _uploadManager;
        private readonly UploadRepository _uploadRepository;

        public UploadService(UploadManager uploadManager, UploadRepository uploadRepository)
        {
            _uploadManager = uploadManager ?? throw new ArgumentNullException(nameof(uploadManager));
            _uploadRepository = uploadRepository ?? throw new ArgumentNullException(nameof(uploadRepository));
        }

        public async Task<UploadResult> ProcessFileAsync(StorageFile file, IProgress<double> progress)
        {
            string url = null;
            var uploadedFileInfo = new UploadedFile
            {
                FileName = file.Name,
                Status = "Pending Upload"
            };

            _uploadRepository.AddUploadedFile(uploadedFileInfo);

            try
            {
                url = await _uploadManager.UploadFile(file, uploadedFileInfo, progress);

                if (!string.IsNullOrEmpty(url))
                {
                    uploadedFileInfo.Status = "Successful";
                    _uploadRepository.EditUploadedFile(uploadedFileInfo);
                    return new UploadResult
                    {
                        Url = url,
                        UploadedFile = uploadedFileInfo,
                        IsSuccessful = true,
                        ErrorMessage = null,
                        OriginalException = null
                    };
                }
                else
                {
                    uploadedFileInfo.Status = "Unsuccessful - No URL Returned";
                    uploadedFileInfo.URL = "N/A";
                    _uploadRepository.EditUploadedFile(uploadedFileInfo);
                    return new UploadResult
                    {
                        Url = null,
                        UploadedFile = uploadedFileInfo,
                        IsSuccessful = false,
                        ErrorMessage = "Upload service did not return a URL.",
                        OriginalException = null
                    };
                }
            }
            catch (Exception ex)
            {
                uploadedFileInfo.Status = "Unsuccessful - Failed";
                uploadedFileInfo.URL = "N/A";
                _uploadRepository.EditUploadedFile(uploadedFileInfo);
                return new UploadResult
                {
                    Url = null,
                    UploadedFile = uploadedFileInfo,
                    IsSuccessful = false,
                    ErrorMessage = "File upload failed.",
                    OriginalException = ex
                };
            }
        }

        public async Task<UploadResult> ProcessSharedFileAsync(StorageFile file, IProgress<double> progress)
        {
            string url = null;
            var uploadedFileInfo = new UploadedFile
            {
                FileName = file.Name,
                Status = "Pending Upload"
            };

            _uploadRepository.AddUploadedFileToDatabaseOnly(uploadedFileInfo);

            try
            {
                url = await _uploadManager.UploadFile(file, uploadedFileInfo, progress);

                if (!string.IsNullOrEmpty(url))
                {
                    uploadedFileInfo.Status = "Successful";
                    _uploadRepository.EditUploadedFile(uploadedFileInfo);
                    return new UploadResult
                    {
                        Url = url,
                        UploadedFile = uploadedFileInfo,
                        IsSuccessful = true,
                        ErrorMessage = null,
                        OriginalException = null
                    };
                }
                else
                {
                    uploadedFileInfo.Status = "Unsuccessful - No URL Returned";
                    uploadedFileInfo.URL = "N/A";
                    _uploadRepository.EditUploadedFile(uploadedFileInfo);
                    return new UploadResult
                    {
                        Url = null,
                        UploadedFile = uploadedFileInfo,
                        IsSuccessful = false,
                        ErrorMessage = "Upload service did not return a URL.",
                        OriginalException = null
                    };
                }
            }
            catch (Exception ex)
            {
                uploadedFileInfo.Status = "Unsuccessful - Failed";
                uploadedFileInfo.URL = "N/A";
                _uploadRepository.EditUploadedFile(uploadedFileInfo);
                return new UploadResult
                {
                    Url = null,
                    UploadedFile = uploadedFileInfo,
                    IsSuccessful = false,
                    ErrorMessage = "File upload failed.",
                    OriginalException = ex
                };
            }
        }

        public async Task<bool> DeleteRemoteFileAsync(UploadedFile file)
        {
            if (file == null) return false;

            return await _uploadManager.DeleteFileAsync(file);
        }
    }
}