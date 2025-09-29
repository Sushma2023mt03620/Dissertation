namespace TelematicsService.Models
{
    public class VehicleData
    {
        public int Id { get; set; }
        public string VehicleId { get; set; }
        public DateTime Timestamp { get; set; }
        public GpsLocation Location { get; set; }
        public double Speed { get; set; }
        public double FuelLevel { get; set; }
        public double EngineRPM { get; set; }
        public double EngineTemperature { get; set; }
        public VehicleStatus Status { get; set; }
    }

    public class GpsLocation
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }
    }

    public enum VehicleStatus
    {
        Idle,
        Moving,
        Stopped,
        Maintenance
    }

    public class DrivingBehavior
    {
        public string VehicleId { get; set; }
        public DateTime AnalysisDate { get; set; }
        public int HarshBrakingCount { get; set; }
        public int HarshAccelerationCount { get; set; }
        public int SpeedingIncidents { get; set; }
        public double AverageFuelEfficiency { get; set; }
        public int SafetyScore { get; set; } // 0-100
    }
}