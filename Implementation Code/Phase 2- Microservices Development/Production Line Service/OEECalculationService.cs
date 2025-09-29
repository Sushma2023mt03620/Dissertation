using ProductionLineService.Models;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace ProductionLineService.Services
{
    public interface IOEECalculationService
    {
        Task<OEEMetrics> CalculateOEEAsync(string productionLineId, DateTime startTime, DateTime endTime);
    }

    public class OEECalculationService : IOEECalculationService
    {
        private readonly ProductionDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly ILogger<OEECalculationService> _logger;

        public OEECalculationService(
            ProductionDbContext context,
            IDistributedCache cache,
            ILogger<OEECalculationService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<OEEMetrics> CalculateOEEAsync(
            string productionLineId, 
            DateTime startTime, 
            DateTime endTime)
        {
            var cacheKey = $"oee:{productionLineId}:{startTime:yyyyMMdd}:{endTime:yyyyMMdd}";
            
            // Check cache first
            var cachedResult = await _cache.GetStringAsync(cacheKey);
            if (cachedResult != null)
            {
                return JsonSerializer.Deserialize<OEEMetrics>(cachedResult);
            }

            var jobs = await _context.ProductionJobs
                .Where(j => j.ProductionLineId == productionLineId 
                    && j.ActualStart >= startTime 
                    && j.ActualStart <= endTime)
                .ToListAsync();

            if (!jobs.Any())
            {
                return new OEEMetrics { CalculatedAt = DateTime.UtcNow };
            }

            // Calculate Availability
            var totalPlannedTime = (endTime - startTime).TotalMinutes;
            var totalDowntime = jobs
                .Where(j => j.Status == JobStatus.Paused)
                .Sum(j => j.ActualEnd.HasValue && j.ActualStart.HasValue 
                    ? (j.ActualEnd.Value - j.ActualStart.Value).TotalMinutes 
                    : 0);
            var availability = (totalPlannedTime - totalDowntime) / totalPlannedTime;

            // Calculate Performance
            var totalProduced = jobs.Sum(j => j.ProducedQuantity);
            var totalPlanned = jobs.Sum(j => j.PlannedQuantity);
            var performance = totalPlanned > 0 ? (double)totalProduced / totalPlanned : 0;

            // Calculate Quality
            var totalDefects = jobs.Sum(j => j.DefectQuantity);
            var quality = totalProduced > 0 
                ? (double)(totalProduced - totalDefects) / totalProduced 
                : 0;

            var oeeMetrics = new OEEMetrics
            {
                Availability = availability,
                Performance = performance,
                Quality = quality,
                CalculatedAt = DateTime.UtcNow
            };

            // Cache the result for 5 minutes
            await _cache.SetStringAsync(
                cacheKey, 
                JsonSerializer.Serialize(oeeMetrics),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });

            _logger.LogInformation(
                "OEE calculated for line {LineId}: {OEE:P2}", 
                productionLineId, 
                oeeMetrics.OEE);

            return oeeMetrics;
        }
    }
}