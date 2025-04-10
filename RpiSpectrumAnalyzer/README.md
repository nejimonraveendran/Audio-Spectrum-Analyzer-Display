# Raspberry Pi Audio Spectrum Analyzer Display using .NET
This project implements a spectrum analyzer display on Raspberry Pi using C#.NET. The application does the following at a high level:
- Captures the audio currently playing using Alsa.Net C# library
- Performs Fast Fourier Transform (FFT) on the audio buffer using FftSharp C# library puts the frequencies into specified bands
- Visualizes the frequencies as levels.   

## Preparing Raspberry Pi for sound capabilities (if not already done)

I usually use Raspberry Pi OS Lite on my Pis. By default, Pi does not have have a sound server running, so we need to install one.  We will use PulseAudio as our sound server and Bluetooth as the sound device. If PulseAudio is not already installed, it can be installed with the following command:

```
sudo apt update
sudo apt install bluez blueman pulseaudio pulseaudio-module-bluetooth 
sudo systemctl restart bluetooth
bluetoothctl power on
bluetoothctl discoverable on
```

Start PulseAudio server using the command:
```
pulseaudio --start
```

Check if PulseAudio is already running:
```
pulseaudio --check; echo $?
```
If the above command returns 0, PulseAudio is running.

**Important:**  PulseAudio server should be running in order to be able to connect devices via Bluetooth.  Devices will disconnet as soon as you exit Pulse Audio Server


If you want to pair a new device such as your phone, first turn on Bluetooth scanning:
```
bluetoothctl scan on
```
The above command will start listing the Bluetooth devices around.  Look for the name of the device you want to pair with and note down the MAC address of the device once it is found in the list.  Pair it using the following commands (one-time activity):
```
bluetoothctl pair <mac_address>
bluetoothctl trust <mac_address>
```
To view already paired/connected devices, you can use:
```
bluetoothctl devices Paired
bluetoothctl devices Connected
``` 

If not already connected, you can connect an already paired device using the command:
```
bluetoothctl connect <mac_address>
```
If you are connecting your phone, the above command will list Pi as a paired/connected device on the Phone. 

**Note:** From this point on, you can also use any regular method you use to connect other devices, e.g., by tapping the device name (your pi's hostname) on the phone. 

## Project Dependencies
This project uses under ALSA (Advanced Linux Sound Architecture) APIs via the C# library [Alsa.Net](https://www.nuget.org/packages/Alsa.Net).  Alsa.Net depends on libasound2-dev package, so we need to make sure that we have installed it via the command:
```
sudo apt-get install libasound2-dev
```

Also note that this project depends on the Nuget package [FftSharp](https://www.nuget.org/packages/FftSharp) for FFT calculations.


