namespace Kantarv2.Services
{
    public interface ILlmService
    {
        Task<string> GenerateResponseAsync(string prompt);
    }
}
