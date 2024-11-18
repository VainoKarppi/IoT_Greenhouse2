using System;
using System.Device.Gpio;
using Iot.Device.DHTxx;
using System.Threading;
using NetDaemon.Extensions.MqttEntityManager;
using System.Globalization;
using NetDaemon.HassModel.Integration;
using System.Threading.Tasks;



public static class SensorMonitor {
    private const int dhtPin = 4; // Adjust this to the GPIO pin you are using
    private const int pumpTogglePin = 18;

    private static double PumpTreshold = 9999; // Start initially with this value before the data is read

    private static double? lastTemperature = null;
    private static double? lastHumidity = null;

    public static GpioController? Controller { get; set; }
    public static Dht11? Dht11Controller { get; set; }

    public static IHaContext? HaContext { get; set; }

    private static IMqttEntityManager? _mqttEntityManager;

    private static bool Running { get; set; } = false;



    public static void StartMonitor(IMqttEntityManager entityManager) {
        if (Running || Controller != null || Dht11Controller != null) throw new Exception("Monitor already started!");

        _mqttEntityManager = entityManager;

        Controller = new GpioController();
        Dht11Controller = new Dht11(dhtPin);

        Running = true;

        Controller.OpenPin(dhtPin, PinMode.Output);
        Controller.OpenPin(pumpTogglePin, PinMode.Output);
        Controller.Write(pumpTogglePin, PinValue.Low);

        Console.WriteLine("Waiting for DHT11 to return data...");

        // Start external thread that updates the data
        StartMonitorThread();

        // wait until the first data arrives
        while (lastTemperature is null || lastHumidity is null) Thread.Sleep(50);


        // Start main internal thread to automatically pump the water if treshold is succeed
        while (Running) {
            
            if (lastTemperature.HasValue && lastHumidity.HasValue) {
                Console.WriteLine($"Temperature: {lastTemperature} °C");
                Console.WriteLine($"Humidity: {lastHumidity} %");

                SendSensorDataToHomeAssistant(entityManager, lastTemperature.Value, lastHumidity.Value);
            }

            UpdatePumpThreshold(); // Read Threshold data from HAS

            // toggle pump for 3 seconds
            if (lastHumidity != null && lastHumidity > PumpTreshold) {
                PumpWater();
            }

            Thread.Sleep(10000);
        }
    }

    public static void StopMonitor() {
        Running = false;

        Controller = null;
        Dht11Controller = null;

        _mqttEntityManager = null;

        lastHumidity = null;
        lastTemperature = null;
    }


    private static void StartMonitorThread() {
        new Thread(() => {
            while (true) {
                if (!Running || Dht11Controller is null) return;

                if (Dht11Controller.TryReadTemperature(out var temperature))
                    lastTemperature = temperature.DegreesCelsius;

                if (Dht11Controller.TryReadHumidity(out var humidity))
                    lastHumidity = humidity.Value;
                
                Thread.Sleep(500);
            }
        }).Start();
    }


    public static bool PumpWater(int time = 2000) {
        if (Controller is null) return false;

        Controller.Write(pumpTogglePin, PinValue.High);
        Thread.Sleep(time);
        Controller.Write(pumpTogglePin, PinValue.Low);

        return true;
    }

    private static void UpdatePumpThreshold() {
        if (HaContext is null) throw new Exception("Unable to get value. HaContext = null");

        const string entityId = "input_number.pump_threshold";

        try {
            // Get the current state of the input_number entity
            var state = HaContext.GetState(entityId);

            if (state != null) {
                Console.WriteLine($"Current value of {entityId}: {state?.State}");
                PumpTreshold = double.Parse(state?.State!, CultureInfo.InvariantCulture);
            } else {
                Console.WriteLine($"Entity {entityId} not found or state is null.");
            }
        } catch (Exception ex) {
            Console.WriteLine($"Error reading state of {entityId}: {ex.Message}");
        }
    }

    public static async void SendSensorDataToHomeAssistant(IMqttEntityManager entityManager, double temperature, double humidity) {

        try {
            Console.WriteLine("Sending temperature and humidty data...");

            await entityManager.SetStateAsync("sensor.greenhouse_temperature", temperature.ToString());
            await entityManager.SetStateAsync("sensor.greenhouse_humidity", humidity.ToString());

            Console.WriteLine($"Sent Temperature: {temperature} to Home Assistant");
            Console.WriteLine($"Sent Humidity: {humidity} to Home Assistant");
        } catch (Exception ex) {
            Console.WriteLine($"Failed to send data to Home Assistant: {ex.Message}");
        }
    }
}












/*

while (true) {

    // Check if values were assigned and display them
    if (lastTemperature.HasValue && lastHumidity.HasValue) {
        Console.WriteLine($"Temperature: {lastTemperature} °C");
        Console.WriteLine($"Humidity: {lastHumidity} %");
    }

    
    // toggle pump for 3 seconds
    if (lastHumidity != null && lastHumidity > pumpTreshold) {
        PumpWater();
    }

    Thread.Sleep(10000);
    
}
*/
