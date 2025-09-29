using IoTSimulator;

var iotHubConnectionString = Environment.GetEnvironmentVariable("IOT_HUB_CONNECTION_STRING");
var deviceCount = int.Parse(Environment.GetEnvironmentVariable("DEVICE_COUNT") ?? "10");

Console.WriteLine($"Starting simulation with {deviceCount} devices...");

var tasks = new List<Task>();
var cancellationTokenSource = new CancellationTokenSource();

for (int i = 0; i < deviceCount; i++)
{
    var deviceId = $"sensor-{i:D4}";
    var deviceConnectionString = $"{iotHubConnectionString};DeviceId={deviceId}";
    
    var simulator = new DeviceSimulator(deviceConnectionString, deviceId);
    tasks.Add(simulator.StartSimulationAsync(cancellationTokenSource.Token));
}

Console.WriteLine("Press Enter to stop simulation...");
Console.ReadLine();

cancellationTokenSource.Cancel();
await Task.WhenAll(tasks);
Console.WriteLine("Simulation stopped.");