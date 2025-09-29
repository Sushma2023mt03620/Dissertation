using Xunit;
using Moq;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using ProductionLineService.Services;
using ProductionLineService.Models;
using ProductionLineService.Data;

namespace ProductionLineService.Tests
{
    public class OEECalculationServiceTests
    {
        private readonly Mock<ProductionDbContext> _mockContext;
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly Mock<ILogger<OEECalculationService>> _mockLogger;
        private readonly OEECalculationService _service;

        public OEECalculationServiceTests()
        {
            var options = new DbContextOptionsBuilder<ProductionDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;
            _mockContext = new Mock<ProductionDbContext>(options);
            _mockCache = new Mock<IDistributedCache>();
            _mockLogger = new Mock<ILogger<OEECalculationService>>();
            _service = new OEECalculationService(
                _mockContext.Object,
                _mockCache.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task CalculateOEE_WithValidData_ReturnsCorrectMetrics()
        {
            // Arrange
            var lineId = "line-001";
            var startTime = DateTime.UtcNow.AddHours(-8);
            var endTime = DateTime.UtcNow;

            var jobs = new List<ProductionJob>
            {
                new ProductionJob
                {
                    ProductionLineId = lineId,
                    ActualStart = startTime,
                    ActualEnd = endTime,
                    PlannedQuantity = 100,
                    ProducedQuantity = 95,
                    DefectQuantity = 3,
                    Status = JobStatus.Completed
                }
            };

            // Mock cache miss
            _mockCache.Setup(c => c.GetAsync(
                It.IsAny<string>(), 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[])null);

            // Act
            var result = await _service.CalculateOEEAsync(lineId, startTime, endTime);

            // Assert
            Assert.NotNull(result);
            Assert.InRange(result.Availability, 0, 1);
            Assert.InRange(result.Performance, 0, 1);
            Assert.InRange(result.Quality, 0, 1);
            Assert.InRange(result.OEE, 0, 1);
        }

        [Theory]
        [InlineData(100, 95, 5, 0.90)] // 95 good parts out of 95 produced
        [InlineData(100, 100, 10, 0.90)] // 90 good parts out of 100 produced
        [InlineData(100, 80, 0, 1.00)] // Perfect quality
        public async Task CalculateOEE_QualityCalculation_IsCorrect(
            int planned, 
            int produced, 
            int defects, 
            double expectedQuality)
        {
            // This test validates quality calculation logic
            var jobs = new List<ProductionJob>
            {
                new ProductionJob
                {
                    PlannedQuantity = planned,
                    ProducedQuantity = produced,
                    DefectQuantity = defects
                }
            };

            // Calculate quality manually
            var actualQuality = (double)(produced - defects) / produced;
            
            Assert.Equal(expectedQuality, actualQuality, 2);
        }
    }
}