using System;
using System.Net.Http;
using System.Text.Json;
using System.Windows;

namespace Fallout4AcfPatcher.Services
{
    public class DepotDataService
    {
        private readonly HttpClient _httpClient = new();

        public async Task<Dictionary<int, string>> FetchDepotDataAsync(string url)
        {
            try
            {
                var json = await _httpClient.GetStringAsync(url);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                
                if (dict == null) return [];

                var result = new Dictionary<int, string>();

                foreach (var kvp in dict)
                {
                    if (int.TryParse(kvp.Key, out int key))
                    {
                        result.Add(key, kvp.Value);
                    }
                }

                return result;
            }
            catch (Exception)
            {
                return [];
            }
        }
    }
}
