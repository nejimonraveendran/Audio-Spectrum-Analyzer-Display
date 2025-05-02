# Raspberry Pi Audio Spectrum Analyzer Display using .NET
This project implements a spectrum analyzer display on Raspberry Pi using C#.NET. The application does the following at a high level:
- Captures the audio currently playing using Alsa.Net C# library
- Performs Fast Fourier Transform (FFT) on the audio buffer using FftSharp C# library puts the frequencies into specified bands
- Visualizes the frequencies as levels.   

## Preparing Raspberry Pi for sound capabilities (if not already done)

I usually use Raspberry Pi OS Lite on my Pis. By default, Pi does not have have a sound server running, so we need to install one.  We will use PulseAudio as our sound server and Bluetooth as the sound device. If PulseAudio is not already installed, it can be installed with the following command:

```
sudo apt update
sudo apt install bluez blueman pulseaudio pulseaudio-module-bluetooth -y 
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

Note (if you are using Raspberry Pi Desktop OS):  You can pair the device using the GUI via desktop.  If you are connecting your phone, your Raspberry Pi's hostname will be showing up on your phone under the the available bluetooth devices.  You can directly pair from there as well. 

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
### Install ALSA APIs
This project uses under ALSA (Advanced Linux Sound Architecture) APIs via the C# library [Alsa.Net](https://www.nuget.org/packages/Alsa.Net).  Alsa.Net depends on libasound2-dev package, so we need to make sure that we have installed it via the command:
```
sudo apt install libasound2-dev
```

Also note that this project depends on the Nuget package [FftSharp](https://www.nuget.org/packages/FftSharp) for FFT calculations.

### Install .NET 8
This project was built on .NET 8, so we need to install .NET for ARM 64 bit
```
sudo apt update
wget https://dotnetcli.azureedge.net/dotnet/Sdk/8.0.100/dotnet-sdk-8.0.100-linux-arm64.tar.gz
sudo mkdir -p /usr/share/dotnet
sudo tar -xvf dotnet-sdk-8.0.100-linux-*.tar.gz -C /usr/share/dotnet
echo 'export DOTNET_ROOT=/usr/share/dotnet' >> ~/.bashrc
echo 'export PATH=$PATH:/usr/share/dotnet' >> ~/.bashrc
rm dotnet-sdk-8.0.100-linux-arm64.tar.gz
source ~/.bashrc
```
Verify the installation by using the command:
```
dotnet --version
```

### Configure Raspberry Pi SPI interface 
**Enable SPI protocol (for RGB LED panel):**  Open /boot/firmware/config.txt in a text Editor, add the following lines at the end of the file
```
dtparam=spi=on
core_freq=250
core_freq_min=250
```

**Set SPI buffer size:**  Open /boot/firmware/cmdline.txt in a text Editor, insert a space and add the following at the end of the file
```
spidev.bufsiz=65536
```

## Restart
After the above steps are complete, you MUST restart the Pi so that the SPI settings take effect upon next boot

```
sudo reboot now
```

## Downloading and running the project
First of all, clone this repo to a local path on Raspberry Pi and build the project

```
clone https://github.com/nejimonraveendran/Audio-Spectrum-Analyzer-Display.git
cd Audio-Spectrum-Analyzer-Display/RpiSpectrumAnalyzer
dotnet build
```

To run the application, first go to the output directory:
```
cd /bin/Debug/net8.0 
```

Then run the application:

```
./RpiSpectrumAnalyzer
```

By default, this will run the application on port 8090.  If you are using Rasperry Pi Os Desktop, you access the application's web interface through the browser at the address:

```
http://localhost:8090
```

If you want to access from other devices on the same network, use your Raspberry Pi's hostname or LAN IP address.  For example:

```
http://raspberrypi.local:8090
```










