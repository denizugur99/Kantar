
using OllamaSharp;
using System.Text.Json;

namespace Kantarv2.Services
{
    public class LlmService : ILlmService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public LlmService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> GenerateResponseAsync(List<object> chatMessages)
        {

           
            var apiKey = "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";
            var model = "llama-3.1-8b-instant"; 
            var url = "https://api.groq.com/openai/v1/chat/completions";

            var requestBody = new
            {
                model,
                messages = chatMessages
            };


            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey?.Trim());
            request.Content = JsonContent.Create(requestBody);

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return $"Hata Detayı: {response.StatusCode} - {content}";
            }

            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.GetProperty("choices")[0]
                                  .GetProperty("message")
                                  .GetProperty("content")
                                  .GetString() ?? "Cevap boş.";
        }
    }
}