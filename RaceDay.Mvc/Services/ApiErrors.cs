using System.Text.Json;

namespace RaceDay.Mvc.Services;

public static class ApiErrors
{
    public static async Task<string> ReadMessageAsync(HttpResponseMessage response, string fallback)
    {
        string body;
        try
        {
            body = await response.Content.ReadAsStringAsync();
        }
        catch
        {
            return fallback;
        }

        if (string.IsNullOrWhiteSpace(body)) return fallback;

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.String)
                return root.GetString() ?? fallback;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    var messages = errors.EnumerateObject()
                        .SelectMany(e => e.Value.EnumerateArray().Select(m => m.GetString()))
                        .Where(m => !string.IsNullOrWhiteSpace(m));
                    string joined = string.Join(" ", messages);
                    if (!string.IsNullOrWhiteSpace(joined)) return joined;
                }

                if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                    return title.GetString() ?? fallback;
            }
        }
        catch (JsonException)
        {
            if (body.Length <= 200) return body.Trim('"');
        }

        return fallback;
    }
}
