using Serilog;
using Serilog.Events;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;

namespace Common.Logging
{
    public static class StructuredLogging
    {
        public static ILogger CreateLogger(string serviceName, string appInsightsKey)
        {
            return new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.WithProperty("Service", serviceName)
                .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"))
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Service} {Message:lj}{NewLine}{Exception}")
                .WriteTo.ApplicationInsights(
                    appInsightsKey,
                    TelemetryConverter.Traces)
                .CreateLogger();
        }
    }
}
