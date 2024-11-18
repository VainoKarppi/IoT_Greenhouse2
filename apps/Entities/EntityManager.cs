using System.Globalization;
using System.Threading.Tasks;
using NetDaemon.Extensions.MqttEntityManager;
using NetDaemon.Extensions.MqttEntityManager.Models;

[NetDaemonApp]
public class EntityManagerApp
{
    public readonly IHaContext _haContext;

    // Injecting the IHaContext into the constructor
    public EntityManagerApp(IHaContext haContext)
    {
        _haContext = haContext;
        Console.WriteLine($"Entity Manager initialized: {_haContext}");

        SensorMonitor.HaContext = _haContext;
    }
}