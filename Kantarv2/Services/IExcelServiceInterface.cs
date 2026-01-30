namespace Kantarv2.Services
{
    public interface IExcelServiceInterface
    {
        byte[] GenerateExcel<T>(IEnumerable<T> data,string sheetName);
    }
}
