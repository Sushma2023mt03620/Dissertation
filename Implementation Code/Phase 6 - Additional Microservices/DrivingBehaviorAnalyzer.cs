using TelematicsService.Models;

namespace TelematicsService.Services
{
    public interface IDrivingBehaviorAnalyzer
    {
        Task<DrivingBehavior> AnalyzeDrivingBehaviorAsync(
            string vehicleId, 
            DateTime startDate, 
            DateTime endDate);
    }

    public class DrivingBehaviorAnalyzer : IDrivingBehaviorAnalyzer
    {
        private readonly TelematicsDbContext _context;
        private readonly ILogger<DrivingBehaviorAnalyzer> _logger;

        private const double HARSH_BRAKING_THRESHOLD = -0.4; // g-force
        private const double HARSH_ACCELERATION_THRESHOLD = 0.35; // g-force
        private const double SPEED_LIMIT = 120; // km/h

        public DrivingBehaviorAnalyzer(
            TelematicsDbContext context,
            ILogger<DrivingBehaviorAnalyzer> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DrivingBehavior> AnalyzeDrivingBehaviorAsync(
            string vehicleId, 
            DateTime startDate, 
            DateTime endDate)
        {
            var telemetryData = await _context.VehicleData
                .Where(v => v.VehicleId == vehicleId 
                    && v.Timestamp >= startDate 
                    && v.Timestamp <= endDate)
                .OrderBy(v => v.Timestamp)
                .ToListAsync();

            if (!telemetryData.Any())
            {
                return new DrivingBehavior 
                { 
                    VehicleId = vehicleId,
                    AnalysisDate = DateTime.UtcNow 
                };
            }

            var harshBraking = 0;
            var harshAcceleration = 0;
            var speedingIncidents = 0;

            for (int i = 1; i < telemetryData.Count; i++)
            {
                var current = telemetryData[i];
                var previous = telemetryData[i - 1];
                var timeDiff = (current.Timestamp - previous.Timestamp).TotalSeconds;

                if (timeDiff > 0 && timeDiff < 10) // Valid consecutive readings
                {
                    // Calculate acceleration
                    var acceleration = (current.Speed - previous.Speed) / timeDiff;

                    if (acceleration < HARSH_BRAKING_THRESHOLD)
                        harshBraking++;
                    
                    if (acceleration > HARSH_ACCELERATION_THRESHOLD)
                        harshAcceleration++;
                }

                // Check speeding
                if (current.Speed > SPEED_LIMIT)
                    speedingIncidents++;
            }

            var avgFuelEfficiency = telemetryData
                .Where(v => v.Speed > 0)
                .Average(v => CalculateFuelEfficiency(v));

            var safetyScore = CalculateSafetyScore(
                harshBraking, 
                harshAcceleration, 
                speedingIncidents, 
                telemetryData.Count);

            var behavior = new DrivingBehavior
            {
                VehicleId = vehicleId,
                AnalysisDate = DateTime.UtcNow,
                HarshBrakingCount = harshBraking,
                HarshAccelerationCount = harshAcceleration,
                SpeedingIncidents = speedingIncidents,
                AverageFuelEfficiency = avgFuelEfficiency,
                SafetyScore = safetyScore
            };

            _logger.LogInformation(
                "Analyzed driving behavior for vehicle {VehicleId}. Safety Score: {Score}",
                vehicleId, safetyScore);

            return behavior;
        }

        private double CalculateFuelEfficiency(VehicleData data)
        {
            // Simplified fuel efficiency calculation
            // In reality, this would be based on actual fuel consumption data
            return data.Speed > 0 ? 100 / (data.Speed * 0.5 + data.EngineRPM * 0.001) : 0;
        }

        private int CalculateSafetyScore(
            int harshBraking, 
            int harshAcceleration, 
            int speedingIncidents, 
            int totalDataPoints)
        {
            const int BASE_SCORE = 100;
            const double HARSH_BRAKING_PENALTY = 0.5;
            const double HARSH_ACCEL_PENALTY = 0.3;
            const double SPEEDING_PENALTY = 0.2;

            var penalties = 
                (harshBraking * HARSH_BRAKING_PENALTY) +
                (harshAcceleration * HARSH_ACCEL_PENALTY) +
                (speedingIncidents * SPEEDING_PENALTY);

            var score = BASE_SCORE - (penalties / totalDataPoints * 100);
            return Math.Max(0, Math.Min(100, (int)score));
        }
    }
}