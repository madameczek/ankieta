using Serilog.Sinks.Graylog.Core.Transport;

namespace Polls.Api.Common.Logging;

/// <summary>Graylog GELF input. The host is set per environment (user secrets / k8s secret); an empty host turns the sink off.</summary>
internal sealed class GraylogSettings
{
    public const string Section = "Graylog";

    public string? Host { get; init; }

    public int Port { get; init; } = 12201;

    public TransportType Transport { get; init; } = TransportType.Udp;
}
