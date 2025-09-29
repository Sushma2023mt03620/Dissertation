namespace ProductionLineService.Models
{
    public class ProductionJob
    {
        public int Id { get; set; }
        public string JobNumber { get; set; }
        public string ProductionLineId { get; set; }
        public DateTime ScheduledStart { get; set; }
        public DateTime? ActualStart { get; set; }
        public DateTime? ActualEnd { get; set; }
        public int PlannedQuantity { get; set; }
        public int ProducedQuantity { get; set; }
        public int DefectQuantity { get; set; }
        public JobStatus Status { get; set; }
    }

    public enum JobStatus
    {
        Scheduled,
        InProgress,
        Completed,
        Paused,
        Cancelled
    }

    public class OEEMetrics
    {
        public double Availability { get; set; }
        public double Performance { get; set; }
        public double Quality { get; set; }
        public double OEE => Availability * Performance * Quality;
        public DateTime CalculatedAt { get; set; }
    }
}