using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;


public class MLService
{
    private readonly HttpClient _httpClient;

    public MLService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }


    public async Task<(string category, string sentiment)> Predict(string message)
    {
        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(new { message }),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync("http://127.0.0.1:5001/predict", content);

            if (!response.IsSuccessStatusCode)
                return ("General", "Neutral");

            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine("ML RESPONSE: " + json); // debug

            var result = JsonSerializer.Deserialize<MLResponse>(json);

            return (result?.Category ?? "General", result?.Sentiment ?? "Neutral");
        }
        catch
        {
            return ("General", "Neutral");
        }
    }

}




public class MLResponse
{
    [JsonPropertyName("category")]
    public string Category { get; set; }

    [JsonPropertyName("sentiment")]
    public string Sentiment { get; set; }
}
