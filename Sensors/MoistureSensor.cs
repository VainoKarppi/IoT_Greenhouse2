using System;
using System.Device.Gpio;
using System.Threading;

public class MoistureSensor
{
    private const int PinNumber = 17; // GPIO pin to read the sensor data
    private GpioController gpioController;

    public MoistureSensor()
    {
        gpioController = new GpioController();
        gpioController.OpenPin(PinNumber, PinMode.Output);
    }

    public void ReadMoisture()
    {
        // Step 1: Discharge the capacitor by setting the GPIO to low and waiting briefly
        gpioController.Write(PinNumber, PinValue.Low);
        Thread.Sleep(10);  // Briefly discharge any remaining charge

        // Step 2: Start charging the capacitor by setting GPIO high
        gpioController.Write(PinNumber, PinValue.High);
        Thread.Sleep(10);  // Allow some time to begin charging the capacitor

        // Step 3: Switch the GPIO pin to input mode to start timing the charge
        gpioController.SetPinMode(PinNumber, PinMode.Input);
        DateTime startTime = DateTime.Now;

        // Step 4: Wait until the capacitor charges to the threshold (GPIO reads high)
        while (gpioController.Read(PinNumber) == PinValue.Low)
        {
            // This loop will exit when the voltage across the capacitor reaches the threshold
        }

        DateTime endTime = DateTime.Now;

        // Step 5: Calculate the time it took for the capacitor to reach the threshold
        TimeSpan chargeTime = endTime - startTime;

        Console.WriteLine($"Charge time: {chargeTime.TotalMilliseconds} ms");

        // Step 6: Interpret the result
        // Shorter times generally indicate drier soil, longer times indicate wetter soil
        // Optionally, you can map these times to a moisture percentage based on calibration
    }

    public void Cleanup()
    {
        gpioController.ClosePin(PinNumber);
    }
}