using System.Reflection;
using Microsoft.Extensions.Hosting;
using NetDaemon.Extensions.Logging;
using NetDaemon.Extensions.MqttEntityManager;
using NetDaemon.Extensions.Scheduler;
using NetDaemon.Extensions.Tts;
using NetDaemon.Runtime;
using HomeAssistantGenerated;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;
using NetDaemon.Client.HomeAssistant.Model;
using NetDaemon.AppModel;
using System.Threading;

#pragma warning disable CA1812

try {
    var host = StartHost(args);

    // Resolve the IMqttEntityManager from the service provider
    var entityManager = host.Services.GetRequiredService<IMqttEntityManager>();

    Console.WriteLine($"MANAGER 1: {entityManager}");


    // Run the host in a separate task
    var hostTask = host.RunAsync();
    
    // Start the monitor after the server has been started finally
    new Thread(() => {
        Thread.Sleep(2000);

        // Start the sensor monitor with the entity manager on raspberry pi (disabled for windows)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            SensorMonitor.StartMonitor(entityManager);
        }
        
        // DEBUG DATA
        while (true) {
            SensorMonitor.SendSensorDataToHomeAssistant(entityManager, 20 + new Random().Next(10), 20 + new Random().Next(10));
            Thread.Sleep(10000);
        }
        
    }).Start();

    await hostTask.ConfigureAwait(false);
} catch (Exception e) {
    Console.WriteLine($"Failed to start host... {e}");
    throw;
}

static IHost StartHost(string[] args) {
    var host = Host.CreateDefaultBuilder(args)
        .UseNetDaemonAppSettings()
        .UseNetDaemonDefaultLogging()
        .UseNetDaemonRuntime()
        .UseNetDaemonTextToSpeech()
        .UseNetDaemonMqttEntityManagement()
        .ConfigureServices((_, services) =>
            services
                .AddAppsFromAssembly(Assembly.GetExecutingAssembly())
                .AddNetDaemonStateManager()
                .AddNetDaemonScheduler()
                .AddHomeAssistantGenerated()
        )
        .Build();

    return host;
}

