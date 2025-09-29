using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace Common.Telemetry
{
    public interface ICustomTelemetry
    {
        void TrackOEECalculation(string lineId, double oeeValue, TimeSpan duration);
        void TrackVehicleTelemetry(string vehicleId, Dictionary<string, double> metrics);
        void TrackMaintenancePrediction(string vehicleId, double probability, string urgency);
    }

    public class CustomTelemetry : ICustomTelemetry
    {
        private readonly TelemetryClient _telemetryClient;

        public CustomTelemetry(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }

        public void TrackOEECalculation(string lineId, double oeeValue, TimeSpan duration)
        {
            var telemetry = new MetricTelemetry
            {
                Name = "OEE_Calculation",
                Sum = oeeValue
            };
            telemetry.Properties.Add("ProductionLineId", lineId);
            telemetry.Properties.Add("CalculationDurationMs", duration.TotalMilliseconds.ToString());

            _telemetryClient.TrackMetric(telemetry);

            // Track custom event
            _telemetryClient.TrackEvent("OEE_Calculated",
                new Dictionary<string, string>
                {
                    { "LineId", lineId },
                    { "OEE", oeeValue.ToString("P2") },
                    { "Status", oeeValue > 0.85 ? "Excellent" : oeeValue > 0.75 ? "Good" : "NeedsImprovement" }
                });
        }

        public void TrackVehicleTelemetry(string vehicleId, Dictionary<string, double> metrics)
        {
            foreach (var metric in metrics)
            {
                var telemetry = new MetricTelemetry
                {
                    Name = $"Vehicle_{metric.Key}",
                    Sum = metric.Value
                };
                telemetry.Properties.Add("VehicleId", vehicleId);

                _telemetryClient.TrackMetric(telemetry);
            }
        }

        public void TrackMaintenancePrediction(string vehicleId, double probability, string urgency)
        {
            _telemetryClient.TrackEvent("MaintenancePrediction",
                new Dictionary<string, string>
                {
                    { "VehicleId", vehicleId },
                    { "Probability", probability.ToString("P2") },
                    { "Urgency", urgency }
                },
                new Dictionary<string, double>
                {
                    { "ProbabilityValue", probability }
                });
        }
    }
}