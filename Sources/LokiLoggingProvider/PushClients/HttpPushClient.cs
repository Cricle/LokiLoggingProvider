namespace LoggingProvider.Loki.PushClients;
using LoggingProvider.Loki.Logger;
using System.Net.Http;
using System.Text.Json;


internal sealed class HttpPushClient : ILokiPushClient
{
    private const string PushEndpointV1 = "/loki/api/v1/push";

    private readonly HttpClient client;

    public HttpPushClient(HttpClient client)
    {
        this.client = client;
    }

    private static void Write(Utf8JsonWriter writer, IReadOnlyList<LokiLogEntry> entries, int start, int length)
    {
        writer.WriteStartObject();
        writer.WriteStartArray("streams");
#if !NETSTANDARD2_0
        Span<char> buffer = stackalloc char[20];
#endif
        for (int i = start; i < length; i++)
        {
            var item = entries[i];

            writer.WriteStartObject();
            writer.WriteStartObject("stream");
            var mem = item.Labels.Labels.Span;
            if (!mem.IsEmpty)
            {
                for (int j = 0; j < mem.Length; j++)
                {
                    var memItem = mem[j];
                    writer.WriteString(memItem.Key, memItem.Value);
                }
            }

            writer.WriteEndObject();

            writer.WriteStartArray("values");
            writer.WriteStartArray();
            var sec = item.Timestamp.ToUnixTimeSeconds();
            var time = sec * 1000000000 + ((int)((item.Timestamp.ToUnixTimeMilliseconds() - (sec * 1000))) * 1000000);
#if NETSTANDARD2_0
            writer.WriteStringValue(time.ToString());
#else
            time.TryFormat(buffer, out var size);
            writer.WriteStringValue(buffer.Slice(0, size));
#endif
            writer.WriteStringValue(item.Message);
            writer.WriteEndArray();
            writer.WriteEndArray();
            writer.WriteEndObject();
            item.Dispose();
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }
    public async Task PushAsync(IReadOnlyList<LokiLogEntry> entries, int start, int length)
    {
        using (var writer = new PooledByteBufferWriter<byte>(1024 * 8))
        using (var utf8Writer = new Utf8JsonWriter(writer, new JsonWriterOptions { SkipValidation = true }))
        {
            Write(utf8Writer, entries,start,length);
            utf8Writer.Flush();
            var array = writer.DangerouGetArray();
            using ByteArrayContent content = new(array, 0, writer.WrittenCount);
            content.Headers.TryAddWithoutValidation("Content-Type", "application/json");
            using HttpRequestMessage request = new(HttpMethod.Post, PushEndpointV1)
            {
                Content = content
            };
            using (var rep = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
            {
                rep.EnsureSuccessStatusCode();
            }
        }
    }
}
