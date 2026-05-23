using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace QldtSdh.Wpf.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            // Send default user header for audits/rules
            _httpClient.DefaultRequestHeaders.Add("X-User-Name", "Admin");

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            try
            {
                var response = await _httpClient.GetAsync(endpoint);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"API Error ({response.StatusCode}): {error}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Không thể kết nối đến Backend API: {ex.Message}", ex);
            }
        }

        public async Task<TResult?> PostAsync<TRequest, TResult>(string endpoint, TRequest data)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, data, _jsonOptions);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<TResult>(_jsonOptions);
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"API Error ({response.StatusCode}): {error}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Không thể kết nối đến Backend API: {ex.Message}", ex);
            }
        }

        public async Task<bool> PutAsync<TRequest>(string endpoint, TRequest data)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync(endpoint, data, _jsonOptions);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"API Error ({response.StatusCode}): {error}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Không thể kết nối đến Backend API: {ex.Message}", ex);
            }
        }
    }
}
