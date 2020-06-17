using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using SSMVCCoreApp.Models.Abstract;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SSMVCCoreApp.Models.Services
{
    public class PhotoService : IPhotoService
    {
        private CloudStorageAccount _storageAccount;
        private readonly ILogger<PhotoService> _logger;

        public PhotoService(IOptions<StorageUtility> storageUtility, ILogger<PhotoService> logger)
        {
            _storageAccount = storageUtility.Value.StorageAccount;
            _logger = logger;
        }

        public async Task<string> UploadPhotoAsync(string category, IFormFile photoToUpload)
        {
            if (photoToUpload == null || photoToUpload.Length == 0) return null;
            string categoryLowerCase = category.ToLower();
            string fullPath = null;
            try
            {
                CloudBlobClient blobCient = _storageAccount.CreateCloudBlobClient();
                CloudBlobContainer blobContainer = blobCient.GetContainerReference(categoryLowerCase);
                //Create Cotainer if not Exists
                if (await blobContainer.CreateIfNotExistsAsync())
                {
                    await blobContainer.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
                    _logger.LogInformation($"Successfully Create Blob Storage Container '{ blobContainer.Name }' and made it public");
                }

                //Craete ImageName for Photo
                string imageName = $"productPhoto{Guid.NewGuid().ToString()}{Path.GetExtension(photoToUpload.FileName.Substring(photoToUpload.FileName.LastIndexOf("/") + 1))}";

                //Create Block Blob.
                CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference(imageName);
                blockBlob.Properties.ContentType = photoToUpload.ContentType;
                await blockBlob.UploadFromStreamAsync(photoToUpload.OpenReadStream());

                fullPath = blockBlob.Uri.ToString();
                _logger.LogInformation($"Blob Service, Photo Service.UploadPhoto, imagePath='{fullPath}'");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error Uploading Photo Blob to Storage");
                throw;
            }
            return fullPath;
        }

        public async Task<bool> DeletePhotoAsync(string category, string photoUrl)
        {
            if (string.IsNullOrWhiteSpace(photoUrl)) return true;
            string categoryLowerCase = category.ToLower();
            bool deleted = false;
            try
            {
                CloudBlobClient blobCient = _storageAccount.CreateCloudBlobClient();
                CloudBlobContainer blobContainer = blobCient.GetContainerReference(categoryLowerCase);
                //Create Cotainer if not Exists
                if (blobContainer.Name == category.ToLower())
                {
                    string blobName = photoUrl.Substring(photoUrl.LastIndexOf("/") + 1);

                    //Create Block Blob.
                    CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference(blobName);
                    deleted = await blockBlob.DeleteIfExistsAsync();
                }

                _logger.LogInformation($"Blob Service, PhotoService.DeletePhoto, deletedImage='{ photoUrl }'");

                BlobContinuationToken blobContinuationToken = null;
                var blobList = blobContainer.ListBlobsSegmentedAsync(blobContinuationToken);
                var cloudBlobList = blobList.Result.Results.Select(blb => blb as CloudBlob);

                if (cloudBlobList.Count() <= 0)
                {
                    await blobContainer.DeleteIfExistsAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could Not Delete Photo");
                return false;
            }
            return deleted;
        }
    }
}
