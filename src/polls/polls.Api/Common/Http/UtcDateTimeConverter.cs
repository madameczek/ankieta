using System.Text.Json;
using System.Text.Json.Serialization;

namespace Polls.Api.Common.Http;

/// <summary>
/// All timestamps are stored in UTC, but SQL Server returns them without a kind; this writes every <see cref="DateTime"/>
/// with a trailing <c>Z</c>, so clients convert to local time correctly.
/// </summary>
internal sealed class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetDateTime().ToUniversalTime();

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
        writer.WriteStringValue(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
