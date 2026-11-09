
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.API.Services
{
    public class AzureBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly IConfiguration _configuration;

        public AzureBlobStorageService(
            BlobServiceClient blobServiceClient,
            IConfiguration configuration)
        {
            _blobServiceClient = blobServiceClient;
            _configuration = configuration;
        }

        public async Task<string> UploadImageAsync(
            IFormFile file,
            string containerName)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("No image was provided.");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException("Only JPG, PNG and WebP images are allowed.");
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                throw new ArgumentException("Image size cannot exceed 5 MB.");
            }

            var containerClient =
                _blobServiceClient.GetBlobContainerClient(containerName);

            await containerClient.CreateIfNotExistsAsync(
                PublicAccessType.None);

            var fileName = $"{Guid.NewGuid()}{extension}";

            var blobClient = containerClient.GetBlobClient(fileName);

            await using var stream = file.OpenReadStream();

            await blobClient.UploadAsync(
                stream,
                new BlobHttpHeaders
                {
                    ContentType = file.ContentType
                });

            return blobClient.Uri.ToString();
        }
    }
}
