using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Exceptions;

namespace CompactorSimulator;

internal class Program
{
    private static async Task Main(string[] args)
    {
        // 1.Build a Host using Host.CreateDefaultBuilder to load appsettings.json
        var host = Host.CreateDefaultBuilder(args).Build();

        // 2. Read MqttConfig and SimulationConfig from configuration
        var config = host.Services.GetRequiredService<IConfiguration>();
        var mqttHost = config["Mqtt:Host"]; // reads from appsettings.json or environment variables
        var mqttPort = config.GetValue<int>("Mqtt:Port");

        var simulationConfig = new SimulationConfig();
        config.GetSection("Simulation").Bind(simulationConfig);

        Console.WriteLine($"MQTT Broker: {mqttHost}:{mqttPort}");
        Console.WriteLine($"Site ID: {simulationConfig.SiteId}");

        // 3. Create and connect a single MqttClient using MqttFactory
        var factory = new MqttFactory();
        var mqttClient = factory.CreateMqttClient();
        
        var mqttOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(mqttHost, mqttPort)
            .WithClientId("compactor-simulator")
            .Build();

        try
        {             
            Console.WriteLine("Connecting to MQTT broker...");
            await mqttClient.ConnectAsync(mqttOptions, CancellationToken.None);
            Console.WriteLine("Connected to MQTT broker.");

            // 4. Spin up one VehicleSimulator task per vehicle in the config
            var token = host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;

            var simulatorTasks = new List<Task>();
            foreach (var vehicleConfig in simulationConfig.Vehicles)
            {
                var simulator = new VehicleSimulator(vehicleConfig, simulationConfig.SiteId, mqttClient);
                simulatorTasks.Add(simulator.RunAsync(token));
            }

            //  5. Wait for Ctrl+C (using the host's CancellationToken) to gracefully shut down all tasks
            await host.WaitForShutdownAsync();

            await Task.WhenAll(simulatorTasks);
        }
        catch (MqttCommunicationException ex)
        {
            Console.Error.WriteLine($"Failed to connect to MQTT broker: {ex.Message}");
            return; // Exit the application - no point running the simulation
        }
        finally
        {
            if (mqttClient.IsConnected)
            {
                await mqttClient.DisconnectAsync();
            }
        }
    }
}