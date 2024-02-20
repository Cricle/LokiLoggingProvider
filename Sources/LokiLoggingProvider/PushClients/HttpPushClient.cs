namespace LoggingProvider.Loki.PushClients;

using LoggingProvider.Loki.Logger;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;


internal sealed class HttpPushClient : ILokiPushClient
{
    internal class LokiSendModeJsonSerialze : JsonConverter<LokiLogEntry>
    {
        public override LokiLogEntry Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, LokiLogEntry value, JsonSerializerOptions options)
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
    }

    private static readonly JsonSerializerOptions options = new JsonSerializerOptions { Converters = { new LokiSendModeJsonSerialze() } };

    private const string PushEndpointV1 = "/loki/api/v1/push";

    private readonly HttpClient client;

    public HttpPushClient(HttpClient client)
    {
        this.client = client;
    }
    public void Push(LokiLogEntry entry)
    {
        var str = JsonSerializer.Serialize(entry, options);

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
        rep.Dispose();
#endif
    }
}
