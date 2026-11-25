using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Ducks.IntegrationTests;

internal static class HttpExtensions
{
    public static async Task<JsonElement> JsonAsync(this HttpResponseMessage response, HttpStatusCode esperado)
    {
        var corpo = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == esperado, $"Esperado {(int)esperado}, veio {(int)response.StatusCode}: {corpo}");
        if (string.IsNullOrWhiteSpace(corpo)) return default;
        // Algumas rotas antigas respondem texto simples (ex.: BadRequest("mensagem")).
        return corpo.TrimStart() is ['{' or '[', ..] ? JsonDocument.Parse(corpo).RootElement.Clone() : JsonSerializer.SerializeToElement(corpo);
    }

    public static async Task<JsonElement> PostJsonAsync(this HttpClient client, string url, object? body, HttpStatusCode esperado = HttpStatusCode.OK) =>
        await (await client.PostAsJsonAsync(url, body ?? new { })).JsonAsync(esperado);

    public static async Task<JsonElement> PutJsonAsync(this HttpClient client, string url, object body, HttpStatusCode esperado = HttpStatusCode.OK) =>
        await (await client.PutAsJsonAsync(url, body)).JsonAsync(esperado);

    public static async Task<JsonElement> GetJsonAsync(this HttpClient client, string url, HttpStatusCode esperado = HttpStatusCode.OK) =>
        await (await client.GetAsync(url)).JsonAsync(esperado);

    public static int Int(this JsonElement e, string prop) => e.GetProperty(prop).GetInt32();
    public static string Str(this JsonElement e, string prop) => e.GetProperty(prop).GetString() ?? string.Empty;
}
