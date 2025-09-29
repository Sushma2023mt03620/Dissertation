using Prometheus;

namespace Common.Metrics
{
    public static class PrometheusMetrics
    {
        public static readonly Counter RequestsTotal = Metrics
            .CreateCounter("automotive_requests_total", "Total HTTP requests",
                new CounterConfiguration
                {
                    LabelNames = new[] { "service", "endpoint", "method", "status" }
                });

        public static readonly Histogram RequestDuration = Metrics
            .CreateHistogram("automotive_request_duration_seconds", "HTTP request duration",
                new HistogramConfiguration
                {
                    LabelNames = new[] { "service", "endpoint" },
                    Buckets = Histogram.ExponentialBuckets(0.001, 2, 10)
                });

        public static readonly Gauge ActiveJobs = Metrics
            .CreateGauge("automotive_active_jobs", "Number of active production jobs",
                new GaugeConfiguration
                {
                    LabelNames = new[] { "line_id", "status" }
                });

        public static readonly Histogram OEEValue = Metrics
            .CreateHistogram("automotive_oee_value", "OEE calculation values",
                new HistogramConfiguration
                {
                    LabelNames = new[] { "line_id" },
                    Buckets = new[] { 0.5, 0.6, 0.7, 0.75, 0.8, 0.85, 0.9, 0.95, 1.0 }
                });

        public static readonly Counter IoTMessagesProcessed = Metrics
            .CreateCounter("automotive_iot_messages_total", "Total IoT messages processed",
                new CounterConfiguration
                {
                    LabelNames = new[] { "device_type", "status" }
                });

        public static readonly Gauge VehicleSpeed = Metrics
            .CreateGauge("automotive_vehicle_speed", "Current vehicle speed",
                new GaugeConfiguration
                {
                    LabelNames = new[] { "vehicle_id" }
                });

        public static readonly Counter MaintenancePredictions = Metrics
            .CreateCounter("automotive_maintenance_predictions_total", "Maintenance predictions made",
                new CounterConfiguration
                {
                    LabelNames = new[] { "urgency", "prediction" }
                });
    }
}