using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace Kantarv2.Services
{
    public class S3Settings
    {
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string BucketName { get; set; } = string.Empty;
        public string Region { get; set; } = "eu-central-1";
    }

    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly S3Settings _settings;
        private readonly ILogger<S3Service> _logger;

        public S3Service(IOptions<S3Settings> settings, ILogger<S3Service> logger)
        {
            _settings = settings.Value;
            _logger = logger;

        

            var config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(_settings.Region)
            };

            _s3Client = new AmazonS3Client(_settings.AccessKey, _settings.SecretKey, config);
        }

        public async Task<string> UploadFileAsync(byte[] fileContent, string fileName, string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        {
            try
            {
                using var stream = new MemoryStream(fileContent);

                var request = new PutObjectRequest
                {
                    BucketName = _settings.BucketName,
                    Key = fileName,
                    InputStream = stream,
                    ContentType = contentType
                };

                await _s3Client.PutObjectAsync(request);

                _logger.LogInformation("File {FileName} uploaded to S3 bucket {BucketName}", fileName, _settings.BucketName);

                return $"https://{_settings.BucketName}.s3.{_settings.Region}.amazonaws.com/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {FileName} to S3", fileName);
                throw;
            }
        }

        public async Task<string> UploadExcelAsync(byte[] excelContent, string fileName)
        {
            if (!fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                fileName += ".xlsx";
            }

            var key = $"excel/{DateTime.UtcNow:yyyy/MM/dd}/{fileName}";

            return await UploadFileAsync(excelContent, key, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }

        public async Task<byte[]?> DownloadFileAsync(string fileName)
        {
            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = _settings.BucketName,
                    Key = fileName
                };

                using var response = await _s3Client.GetObjectAsync(request);
                using var memoryStream = new MemoryStream();

                await response.ResponseStream.CopyToAsync(memoryStream);

                _logger.LogInformation("File {FileName} downloaded from S3 bucket {BucketName}", fileName, _settings.BucketName);

                return memoryStream.ToArray();
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("File {FileName} not found in S3 bucket {BucketName}", fileName, _settings.BucketName);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file {FileName} from S3", fileName);
                throw;
            }
        }

        public async Task<(byte[]? Content, string? FileName)> DownloadFileByPrefixAsync(string prefix)
        {
            try
            {
                // S3'te prefix ile dosya ara
                var listRequest = new ListObjectsV2Request
                {
                    BucketName = _settings.BucketName,
                    Prefix = prefix,
                    MaxKeys = 1
                };

                var listResponse = await _s3Client.ListObjectsV2Async(listRequest);

                if (listResponse.S3Objects.Count == 0)
                {
                    _logger.LogWarning("No file found with prefix {Prefix} in S3 bucket {BucketName}", prefix, _settings.BucketName);
                    return (null, null);
                }

                var s3Key = listResponse.S3Objects[0].Key;
                var content = await DownloadFileAsync(s3Key);
                var fileName = Path.GetFileName(s3Key);

                return (content, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching file with prefix {Prefix} in S3", prefix);
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string fileName)
        {
            try
            {
                var request = new DeleteObjectRequest
                {
                    BucketName = _settings.BucketName,
                    Key = fileName
                };

                await _s3Client.DeleteObjectAsync(request);

                _logger.LogInformation("File {FileName} deleted from S3 bucket {BucketName}", fileName, _settings.BucketName);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {FileName} from S3", fileName);
                return false;
            }
        }

        public async Task<string> GetPreSignedUrlAsync(string fileName, int expirationMinutes = 60)
        {
            try
            {
                var request = new GetPreSignedUrlRequest
                {
                    BucketName = _settings.BucketName,
                    Key = fileName,
                    Expires = DateTime.UtcNow.AddMinutes(expirationMinutes)
                };

                var url = await _s3Client.GetPreSignedURLAsync(request);

                _logger.LogInformation("Pre-signed URL generated for file {FileName}", fileName);

                return url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating pre-signed URL for file {FileName}", fileName);
                throw;
            }
        }
    }
}
