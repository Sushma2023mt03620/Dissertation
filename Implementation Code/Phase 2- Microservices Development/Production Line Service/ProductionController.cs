using Microsoft.AspNetCore.Mvc;
using ProductionLineService.Models;
using ProductionLineService.Services;

namespace ProductionLineService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductionController : ControllerBase
    {
        private readonly ProductionDbContext _context;
        private readonly IOEECalculationService _oeeService;
        private readonly ILogger<ProductionController> _logger;

        public ProductionController(
            ProductionDbContext context,
            IOEECalculationService oeeService,
            ILogger<ProductionController> logger)
        {
            _context = context;
            _oeeService = oeeService;
            _logger = logger;
        }

        [HttpGet("jobs")]
        public async Task<ActionResult<IEnumerable<ProductionJob>>> GetJobs(
            [FromQuery] string? lineId = null,
            [FromQuery] JobStatus? status = null)
        {
            var query = _context.ProductionJobs.AsQueryable();

            if (!string.IsNullOrEmpty(lineId))
                query = query.Where(j => j.ProductionLineId == lineId);

            if (status.HasValue)
                query = query.Where(j => j.Status == status.Value);

            return await query.ToListAsync();
        }

        [HttpPost("jobs")]
        public async Task<ActionResult<ProductionJob>> CreateJob(ProductionJob job)
        {
            job.Status = JobStatus.Scheduled;
            _context.ProductionJobs.Add(job);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created production job {JobNumber}", job.JobNumber);
            return CreatedAtAction(nameof(GetJob), new { id = job.Id }, job);
        }

        [HttpGet("jobs/{id}")]
        public async Task<ActionResult<ProductionJob>> GetJob(int id)
        {
            var job = await _context.ProductionJobs.FindAsync(id);
            if (job == null)
                return NotFound();

            return job;
        }

        [HttpPut("jobs/{id}/start")]
        public async Task<IActionResult> StartJob(int id)
        {
            var job = await _context.ProductionJobs.FindAsync(id);
            if (job == null)
                return NotFound();

            job.Status = JobStatus.InProgress;
            job.ActualStart = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("jobs/{id}/complete")]
        public async Task<IActionResult> CompleteJob(int id)
        {
            var job = await _context.ProductionJobs.FindAsync(id);
            if (job == null)
                return NotFound();

            job.Status = JobStatus.Completed;
            job.ActualEnd = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("oee/{lineId}")]
        public async Task<ActionResult<OEEMetrics>> GetOEE(
            string lineId,
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null)
        {
            var start = startTime ?? DateTime.UtcNow.Date;
            var end = endTime ?? DateTime.UtcNow;

            var metrics = await _oeeService.CalculateOEEAsync(lineId, start, end);
            return Ok(metrics);
        }
    }
}
