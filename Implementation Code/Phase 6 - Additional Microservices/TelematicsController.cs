using Microsoft.AspNetCore.Mvc;
using TelematicsService.Models;
using TelematicsService.Services;

namespace TelematicsService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TelematicsController : ControllerBase
    {
        private readonly TelematicsDbContext _context;
        private readonly IDrivingBehaviorAnalyzer _behaviorAnalyzer;
        private readonly ILogger<TelematicsController> _logger; 

        public TelematicsController(
            TelematicsDbContext context,
            IDrivingBehaviorAnalyzer behaviorAnalyzer,
            ILogger<TelematicsController> logger)
        {
            _context = context;
            _behaviorAnalyzer = behaviorAnalyzer;
            _logger = logger;
        }

        [HttpPost("telemetry")]
        public async Task<IActionResult> ReceiveTelemetry([FromBody] VehicleData data)
        {
            data.Timestamp = DateTime.UtcNow;
            _context.VehicleData.Add(data);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Telemetry received", id = data.Id });
        }

        [HttpGet("vehicles/{vehicleId}/latest")]
        public async Task<ActionResult<VehicleData>> GetLatestData(string vehicleId)
        {
            var latestData = await _context.VehicleData
                .Where(v => v.VehicleId == vehicleId)
                .OrderByDescending(v => v.Timestamp)
                .FirstOrDefaultAsync();

            if (latestData == null)
                return NotFound();

            return latestData;
        }

        [HttpGet("vehicles/{vehicleId}/history")]
        public async Task<ActionResult<IEnumerable<VehicleData>>> GetHistory(
            string vehicleId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int limit = 100)
        {
            var query = _context.VehicleData
                .Where(v => v.VehicleId == vehicleId);

            if (startDate.HasValue)
                query = query.Where(v => v.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(v => v.Timestamp <= endDate.Value);

            var data = await query
                .OrderByDescending(v => v.Timestamp)
                .Take(limit)
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("vehicles/{vehicleId}/driving-behavior")]
        public async Task<ActionResult<DrivingBehavior>> GetDrivingBehavior(
            string vehicleId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-7);
            var end = endDate ?? DateTime.UtcNow;

            var behavior = await _behaviorAnalyzer.AnalyzeDrivingBehaviorAsync(
                vehicleId, start, end);

            return Ok(behavior);
        }

        [HttpGet("fleet/summary")]
        public async Task<ActionResult> GetFleetSummary()
        {
            var activeVehicles = await _context.VehicleData
                .Where(v => v.Timestamp > DateTime.UtcNow.AddMinutes(-15))
                .Select(v => v.VehicleId)
                .Distinct()
                .CountAsync();

            var totalVehicles = await _context.VehicleData
                .Select(v => v.VehicleId)
                .Distinct()
                .CountAsync();

            var avgSpeed = await _context.VehicleData
                .Where(v => v.Timestamp > DateTime.UtcNow.AddHours(-1) 
                    && v.Status == VehicleStatus.Moving)
                .AverageAsync(v => v.Speed);

            return Ok(new
            {
                TotalVehicles = totalVehicles,
                ActiveVehicles = activeVehicles,
                InactiveVehicles = totalVehicles - activeVehicles,
                AverageSpeed = Math.Round(avgSpeed, 2)
            });
        }
    }

}
