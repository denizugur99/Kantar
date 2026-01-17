namespace Kantarv2.Services
{
    public interface IS3Service
    {
        Task<string> UploadFileAsync(byte[] fileContent, string fileName, string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        Task<string> UploadExcelAsync(byte[] excelContent, string fileName);
        Task<byte[]?> DownloadFileAsync(string fileName);
        Task<bool> DeleteFileAsync(string fileName);
        Task<string> GetPreSignedUrlAsync(string fileName, int expirationMinutes = 60);
    }
}
