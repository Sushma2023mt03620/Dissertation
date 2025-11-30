using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using ProductionLineService.Models;
using Xunit;

namespace IntegrationTests
{
    public class ProductionApiTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public ProductionApiTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CreateJob_WithValidData_ReturnsCreated()
        {
            // Arrange
            var newJob = new ProductionJob
            {
                JobNumber = $"JOB-{Guid.NewGuid():N}",
                ProductionLineId = "line-001",
                ScheduledStart = DateTime.UtcNow.AddHours(1),
                PlannedQuantity = 100
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/production/jobs", newJob);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
            
            var createdJob = await response.Content.ReadFromJsonAsync<ProductionJob>();
            Assert.NotNull(createdJob);
            Assert.Equal(newJob.JobNumber, createdJob.JobNumber);
        }

        [Fact]
        public async Task GetOEE_ForValidLine_ReturnsMetrics()
        {
            // Act
            var response = await _client.GetAsync("/api/production/oee/line-001");

            // Assert
            response.EnsureSuccessStatusCode();
            var oee = await response.Content.ReadFromJsonAsync<OEEMetrics>();
            Assert.NotNull(oee);
            Assert.InRange(oee.OEE, 0, 1);
        }

        [Fact]
        public async Task HealthCheck_ReturnsHealthy()
        {
            // Act
            var response = await _client.GetAsync("/health");

            // Assert
            response.EnsureSuccessStatusCode();
        }
    }
}
