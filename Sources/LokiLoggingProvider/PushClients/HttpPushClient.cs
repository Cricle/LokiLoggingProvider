namespace LoggingProvider.Loki.PushClients;

using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using LoggingProvider.Loki.Logger;


internal sealed class HttpPushClient : ILokiPushClient
{
    private const string PushEndpointV1 = "/loki/api/v1/push";

    private readonly HttpClient client;

    public HttpPushClient(HttpClient client)
    {
        this.client = client;
    }

    private static void Write(Utf8JsonWriter writer, LokiLogEntry value)
    {
        writer.WriteStartObject();
        writer.WriteStartArray("streams");
        writer.WriteStartObject();
        writer.WriteStartObject("stream");
        if (value.Labels.Count != 0)
        {
            foreach (var item in value.Labels)
            {
                writer.WriteString(item.Key, item.Value);
            }
        }

        writer.WriteEndObject();

        writer.WriteStartArray("values");
        writer.WriteStartArray();
        var uxSecond = value.Timestamp.ToUnixTimeSeconds();
        writer.WriteStringValue($"{value.Timestamp.ToUnixTimeSeconds()}{(int)((value.Timestamp.ToUnixTimeMilliseconds() - (uxSecond * 1000)) * 1000000):000000000}");
        writer.WriteStringValue(value.Message);
        writer.WriteEndArray();
        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    public void Push(LokiLogEntry entry)
    {
        string str = string.Empty;
        using (var writer = new PooledByteBufferWriter(1024))
        using (var utf8Writer = new Utf8JsonWriter(writer))
        {
            Write(utf8Writer, entry);
            utf8Writer.Flush();
#if NETSTANDARD2_0
            str = Encoding.UTF8.GetString(writer.WrittenMemory.ToArray());
#else
            str = Encoding.UTF8.GetString(writer.WrittenMemory.Span);
#endif
        }

        StringContent content = new(str, null, "application/json");
        content.Headers.ContentType!.CharSet = null; // Loki does not accept 'charset' in the Content-Type header

        using HttpRequestMessage request = new(HttpMethod.Post, PushEndpointV1)
        {
            Content = content,
        };
#if NETSTANDARD2_0
        client.SendAsync(request).GetAwaiter().GetResult().Dispose();
#else
        var rep = client.Send(request);
        rep.EnsureSuccessStatusCode();
        rep.Dispose();
#endif
    }
}
