using System;
using System.Device.Gpio;
using System.Threading;

public class LightSensor
{
    private const int PinNumber = 18; // GPIO pin number
    private GpioController gpioController;

    public LightSensor()
    {
        gpioController = new GpioController();
        gpioController.OpenPin(PinNumber, PinMode.Input);
    }

    public void ReadLight()
    {
        while (true)
        {
            PinValue value = gpioController.Read(PinNumber);
            if (value == PinValue.High)
            {
                Console.WriteLine("Light is ON");
            }
            else
            {
                Console.WriteLine("Light is OFF");
            }
            Thread.Sleep(1000); // Read every second
        }
    }

    public void Cleanup()
    {
        gpioController.ClosePin(PinNumber);
    }
}
