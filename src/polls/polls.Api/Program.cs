using System.Globalization;
using Polls.Api.Common;
using Polls.Api.Common.Logging;
using Polls.Api.Infrastructure;
using Serilog;
using Serilog.Events;

// Bootstrap logger (console) until the host is built, so startup errors are logged too; then the full logger
// (console + Graylog, D16) replaces it. Log.CloseAndFlush sends what is left before the process ends.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Request lines only, never bodies – answer contents are not logged; console always, Graylog when Graylog:Address is set (D16)
    builder.Services.AddSerilog(
        (services, log) => log
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
            .WriteToGraylog(builder.Configuration, builder.Environment),
        // tests start several app instances in one process; the static bootstrap logger can be frozen only once
        preserveStaticLogger: builder.Environment.IsEnvironment("Testing"));

    builder.Services.AddPollsCommon();
    builder.Services.AddPollsDatabase();

    var app = builder.Build();

    app.UsePollsCommon();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application failed to start or stopped unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
