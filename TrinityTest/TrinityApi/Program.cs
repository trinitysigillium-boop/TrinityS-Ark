using System.Security.Cryptography;
using System.Text;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Endpoint: consultar balance en Bitso
app.MapGet("/balance", async () =>
{
    string apiKey = Environment.GetEnvironmentVariable("BITSO_API_KEY") ?? "";
    string apiSecret = Environment.GetEnvironmentVariable("BITSO_API_SECRET") ?? "";

    if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
        return Results.BadRequest("API Key/Secret no configurados");

    string nonce = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
    string message = nonce + "GET" + "/api/v3/balance/";

    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(apiSecret));
    string signature = BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(message))).Replace("-", "").ToLower();

    using var client = new HttpClient();
    client.DefaultRequestHeaders.Add("Authorization", $"Bitso {apiKey}:{nonce}:{signature}");

    var response = await client.GetAsync("https://api.bitso.com/api/v3/balance/");
    string result = await response.Content.ReadAsStringAsync();

    return Results.Ok(result);
});

// Endpoint: transferir criptomonedas
app.MapPost("/transfer", async (string amount, string currency, string address) =>
{
    string apiKey = Environment.GetEnvironmentVariable("BITSO_API_KEY") ?? "";
    string apiSecret = Environment.GetEnvironmentVariable("BITSO_API_SECRET") ?? "";

    if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
        return Results.BadRequest("API Key/Secret no configurados");

    var payload = new {
        amount = amount,
        currency = currency,
        address = address
    };

    string jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);

    string nonce = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
    string message = nonce + "POST" + "/api/v3/withdrawals/" + jsonPayload;

    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(apiSecret));
    string signature = BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(message))).Replace("-", "").ToLower();

    using var client = new HttpClient();
    client.DefaultRequestHeaders.Add("Authorization", $"Bitso {apiKey}:{nonce}:{signature}");
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
    var response = await client.PostAsync("https://api.bitso.com/api/v3/withdrawals/", content);

    string result = await response.Content.ReadAsStringAsync();
    return Results.Ok(result);
});

app.Run();

