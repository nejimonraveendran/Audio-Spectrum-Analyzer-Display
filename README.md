# Audio Spectrum Analyzer Display Using Raspberry Pi and ESP32 with .NET and C++ 

This repository contains two main projects, both implementing an audio spectrum analyzer display using 2 different hardware devices, platforms, and programming languages.

![Spectrum Analyzer](/Assets/Rpi-Spectrum-Analyzer-thumb.jpg)


![](/Assets/youtube.jpg) [Watch Video Demo](https://www.youtube.com/watch?v=yKQn0UK1OT8)

## Raspberry Pi Spectrum Analyzer 
- Written in C# on .NET 8
- 3 Types of Displays (Web, Terminal, LED)
- Integrated Web Portal for display and configuration 
- Configurable via command line
- Available as [Docker image](https://hub.docker.com/r/nejimonraveendran/rpispectrumanalyzer)

[Detailed How-To Documentation](/RpiSpectrumAnalyzer/README.md)

## ESP32 Spectrum Analyzer
- Written in C++
- Leverages commonly available WS2812B RGB LED strip for the display.
- Integrated Web Portal for configuration 

[Detailed How-To Documentation](/Esp32SpectrumAnalyzer/README.md)

## Concepts Covered
This project touches upon different aspects of computer science, physics, hardware and software platforms, etc.  Some of the key ones are:
-  **Fast Fourier Transform and Sound Analysis:** [FFT](https://en.wikipedia.org/wiki/Fast_Fourier_transform) is an algorithm that analyzes signals (audio signals in our case) and converts them into frequencies.  In this project, we capture audio signals and apply FFT on them to convert them into predefined frequency bands. For this, on ESP32, we feed external analog audio into ESP32's [analog-to-digital converter](https://docs.espressif.com/projects/esp-idf/en/v4.4/esp32/api-reference/peripherals/adc.html) (ADC) input, capture it through ESP32's [I2S](https://docs.espressif.com/projects/esp-idf/en/stable/esp32/api-reference/peripherals/i2s.html) bus via our application's code, and performs FFT analysis using arduinoFFT library.  On Raspberry Pi, we capture the system audio through the Advanced Linux System Audio (ALSA) APIs using [ALSA.NET](https://www.nuget.org/packages/Alsa.Net) .NET library and perform FFT using [FFTSharp](https://www.nuget.org/packages/FftSharp) .NET library.
- **IoT Programming**:  Raspberry Pi 5 is a single-board computer and ESP32-WROOM-32 is an IoT microcontroller development board. Both offer many general input-output pins (GPIO), which can be used to connect physical devices.  In this project, we take advantage of those GPIO pins to connect physical RGB LED displays. The ESP32 solution is programmed using C++ in Visual Studio Code with PlatformIO plugin.  For Raspberry Pi programming, C#.NET (.NET 8) in Visual Studio Code was used.  
- **Docker Containerization**:  The Raspberr Pi version of this project is available as a [Docker container image](https://hub.docker.com/r/nejimonraveendran/rpispectrumanalyzer).  This demonstrates how a container image can access the kernel devices of the underlying host.
- **Web Socket programming**:  The Web Display in the Raspberry Pi solution was implemented using Web Sockets for real-time communication from the web server to the browser. 
