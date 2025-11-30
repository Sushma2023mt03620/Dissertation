using Microsoft.Azure.Devices.Client;
using System.Text;
using System.Text.Json;

namespace IoTSimulator
{
    public class SensorData
    {
        public string DeviceId { get; set; }
        public DateTime Timestamp { get; set; }
        public double Temperature { get; set; }
        public double Vibration { get; set; }
        public double Pressure { get; set; }
        public int ProductionCount { get; set; }
        public string Status { get; set; }
    }

    public class DeviceSimulator
    {
        private readonly DeviceClient _deviceClient;
        private readonly string _deviceId;
        private readonly Random _random = new Random();

        public DeviceSimulator(string connectionString, string deviceId)
        {
            _deviceId = deviceId;
            _deviceClient = DeviceClient.CreateFromConnectionString(
                connectionString, 
                TransportType.Mqtt);
        }

        public async Task StartSimulationAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine($"Device {_deviceId} starting simulation...");

            while (!cancellationToken.IsCancellationRequested)
            {
                var sensorData = GenerateSensorData();
                var messageString = JsonSerializer.Serialize(sensorData);
                var message = new Message(Encoding.UTF8.GetBytes(messageString))
                {
                    ContentType = "application/json",
                    ContentEncoding = "utf-8"
                };

                // Add custom properties
                message.Properties.Add("deviceType", "ProductionSensor");
                message.Properties.Add("criticalAlert", 
                    sensorData.Temperature > 80 ? "true" : "false");

                await _deviceClient.SendEventAsync(message);
                Console.WriteLine($"Sent: {messageString}");

                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }

        private SensorData GenerateSensorData()
        {
            return new SensorData
            {
                DeviceId = _deviceId,
                Timestamp = DateTime.UtcNow,
                Temperature = 60 + _random.NextDouble() * 30, // 60-90°C
                Vibration = _random.NextDouble() * 10, // 0-10 mm/s
                Pressure = 100 + _random.NextDouble() * 50, // 100-150 PSI
                ProductionCount = _random.Next(1, 10),
                Status = _random.Next(100) > 5 ? "Running" : "Maintenance"
            };
        }

        public async Task DisposeAsync()
        {
            await _deviceClient.CloseAsync();
            _deviceClient.Dispose();
        }
    }
}
