using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Distribuidora_La_Central.Shared.Helpers
{
    public class HttpService
    {
        private readonly HttpClient _http;

        public HttpService(HttpClient http)
        {
            _http = http;
        }

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            var url = ApiHelper.GetApiUrl(endpoint);
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return default;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<T?> PostAsync<T>(string endpoint, T data)
        {
            var url = ApiHelper.GetApiUrl(endpoint);
            var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(url, content);
            return response.IsSuccessStatusCode ? data : default;
        }

        public async Task<T?> PutAsync<T>(string endpoint, T data)
        {
            var url = ApiHelper.GetApiUrl(endpoint);
            var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
            var response = await _http.PutAsync(url, content);
            return response.IsSuccessStatusCode ? data : default;
        }

        public async Task<bool> DeleteAsync(string endpoint)
        {
            var url = ApiHelper.GetApiUrl(endpoint);
            var response = await _http.DeleteAsync(url);
            return response.IsSuccessStatusCode;
        }
    }
}
