# Raspberry Pi Audio Spectrum Analyzer Display using .NET
This project implements a spectrum analyzer display on Raspberry Pi using C#.NET.  It captures the audio currently being played on the desktop, via bluetooth, etc., and converts into audio level visualization. 

## Features
### 3 Types of Displays
The spectrum analyzer display can plot the visualization output to 3 types of displays:
- Web Display:  Display shown on a the home page of the application's integrated web portal (default port: 8090).
- Console Display:  Display directly on Raspberry Pi's Terminal or the Terminal of a connected Windows device.
- RGB LED Display Matrix (WS2812B) connected to the GPIO pin 10 of the Raspberry Pi.

### Integrated Web Portal 
- An integrated web portal runs on default port 8090.  This portal can be accessed locally via http://localhost:8090 or via other devices on the network via your Raspberry Pi's host name (for eg. http://raspberrypi.local:8090)
- Ability to configure the colors of the visualization display.
- Ability to configure various properties of the display such as speed, peak delay, amplification, display brightness, etc.   

### Configurable via command-line
- Turn on/off displays: You can run all 3 types of displays at the same time or turn/off one or more via command-line options
- Band selection: You can configure frequency bands (Hz) via command line.
- You can change the default port the web portal listens on to another port. 

## How it Works
The application does the following at a high level:
- Captures the audio currently playing using Alsa.Net C# library 
- Performs Fast Fourier Transform (FFT) on the audio buffer using FftSharp C# library puts the frequencies into specified bands
- Visualizes the frequencies as levels, through RGB LED Display, Web Page Display, and Terminal/Console Display.

## Device Support
This project was developed and tested on Raspberry Pi 5 running Raspberry Pi OS (both full desktop OS and OS Lite - Debian Bookworm).  While this may work on other versions of Raspberry Pi, it was not tested on them.  For the RGB LED Display, any WS2812B RGB LED strip commonly available can be used (refer to the Hardware Setup section below).

## LED Hardware Setup - Optional
If you plan to connect WS2812B LED Strip, follow the instructions provided below for the wiring and the GPIO connections:

## Raspberry Pi SPI Configuration - Optional (Reboot Required)
If you want to use connect WS2812B LED Strip/matrix, you must enable SPI on the Raspberry Pi. This is done by editing the file _/boot/firmware/config.txt_ on your Raspberry Pi. If don't want to enable SPI settings and you don't want to connect LED matrix, start the application with the command line option --disable-led-display.  Otherwise, the application will throw an error upon startup.

**To Enable SPI protocol:**  Open _/boot/firmware/config.txt_ in a text Editor and add the following lines at the end of the file
```
dtparam=spi=on
core_freq=250
core_freq_min=250
```

**Set SPI buffer size:**  Open _/boot/firmware/cmdline.txt_ in a text Editor and add the following at the end of the file
```
spidev.bufsiz=65536
```

After the above steps are complete, you MUST restart the Pi so that the SPI settings take effect upon next boot

```
sudo reboot now
```

## Prerequisites Installation
Tip: You can run the _prereq.sh_ script provided in this project to install the dependencies as a single step by issuing this command:

```
./prereq.sh
```

Explanation:  The above script does the following:
- Installs Git, wget, etc., if not not already installed
- Installs PluseAudio sound server and associated Bluetooth modules
- Installs ALSA (Advanced Linux Sound Architecture) APIs, which is a dependency of Alsa.Net C# library used in the project.
- Installs .NET 8
- Clones this repo to the current path on the local file system
- Builds the project and change the current directory location to the build output path.

## Running the project
To run the project, issue the following from the build output path:

```
./RpiSpectrumAnalyzer
```

If you have not connected LED matrix display, use the following command line option (for other supported command line options, refer to Command Line Reference section at the end of this document):
```
./RpiSpectrumAnalyzer --disable-led-display
```

By default, this will run the application on port 8090.  If you are using Rasperry Pi Os Desktop, you access the application's web interface through the browser at the address:

```
http://localhost:8090
```

If you want to access from other devices on the same network, use your Raspberry Pi's hostname or LAN IP address.  For example:

```
http://raspberrypi.local:8090
```

Now play some audio on your Raspberry Pi, and you should be able to see the audio being visualized as different frequency bands.


## Connecting Bluetooth Audio Devices
If you want to pair a new Bluetooth audio device such as your phone, first you need to pair the device with your Raspberry Pi. You can pair the device using the GUI via desktop. Once the Pi's discoverability is turned on, the Pi's hostname will be shown on your phone under the the available bluetooth devices.  You can directly pair from there. However, if you are using Rasperry Pi OS Lite and connecting to the Pi via Terminal SSH, use the following method to pair a new device.

First of all, turn on Bluetooth scanning:
```
bluetoothctl scan on
```
The above command will start listing the Bluetooth devices around.  Look for the name of the device you want to pair with and note down the MAC address of the device once it is found in the list.  Pair the device using the following commands:

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


## Command Line Reference
The RpiSpectrumAnalyzer application can be started with different command line options to control its behavior.  The supported options are:

``` --port ``` 

Specifies a non-default port for the integrated web portal. Example:
```
./RpiSpectrumAnalyzer --port 8000
```

``` --console-display-levels ``` 

Specifies the number of Console/Terminal display levels (rows).  Default is 16.  Example:
```
./RpiSpectrumAnalyzer --console-display-levels 20
```

``` --led-display-levels ``` 

Specifies the number of LED Matrix display levels (rows).  Default is 10.  Example:
```
./RpiSpectrumAnalyzer --led-display-levels 15
```

``` --web-display-levels ``` 

Specifies the number of Web display levels (rows).  Default is 15.  Example:
```
./RpiSpectrumAnalyzer --web-display-levels 20
```

``` --disable-console-display ``` 

Disables console display.  Default is enabled.  Example:
```
./RpiSpectrumAnalyzer --disable-console-display
```

``` --disable-led-display ``` 

Disables LED display.  Default is enabled. If Pi's SPI settings are not enabled via firmware settings, this option must be used.  Otherwise, the program will throw an error on startup.   Example:
```
./RpiSpectrumAnalyzer --disable-led-display
```


``` --disable-web-display ``` 

Disables web display.  Default is enabled.  Example:
```
./RpiSpectrumAnalyzer --disable-web-display
```

``` --bands ``` 

Specifies custom band frequencies in Hz as comma separated values surrounded by double quotes.  Default is 10 bands. If you use LED display, the number of columns in the LED matrix MUST match the number of bands. Otherwise, unexpected display behavior might happen.  Example:

```
./RpiSpectrumAnalyzer --bands "100, 500, 1000, 2000, 4000, 6000, 8000, 10000, 12000, 14000"  

```

















