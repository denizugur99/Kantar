
using OllamaSharp;

namespace Kantarv2.Services
{
    public class LlmService : ILlmService
    {
        private readonly IOllamaApiClient _ollamaApiClient;
        public LlmService()
        {
            _ollamaApiClient = new OllamaApiClient("http://localhost:11434");
            _ollamaApiClient.SelectedModel="llama3";
        }


        public async Task<string> GenerateResponseAsync(string prompt)
        {
            var response = "";
          await foreach( var stream in _ollamaApiClient.GenerateAsync(prompt))
            {
                response += stream.Response;
            }
            return response;
        }
    }
}
