using System.Device.Spi;
using System.Drawing;
using Iot.Device.Ws28xx;

namespace RpiSpectrumAnalyzer;

//Author: Nejimon Raveendran
//PROJECT DEPENDENCIES:
//sudo apt-get install libasound2-dev
//dotnet add package Alsa.Net --version 1.0.8
//dotnet add package FftSharp --version 2.2.0
//dotnet add package Iot.Device.Bindings --version 3.2.0

//add the following to /boot/firmware/cmdline.txt
//spidev.bufsiz=65536

//add the following to /boot/firmware/config.txt
//dtparam=spi=on
//core_freq=250
//core_freq_min=250

class Program
{
    static int[] _bands = {100, 500, 1000, 2000, 4000, 6000, 8000, 10000, 12000, 14000}; //audio frequencies (Hz) to analyze
    const int _consoleDisplayLevels = 16; //number of levels
    const int _ledDisplayLevels = 10; //number of levels
    const int _webDisplayLevels = 30; //number of levels
    
    
    const int _amplificationFactor = 5000; //variable to amplify the display levels   

    //other configuration (not recommended to change) 
    const int _sampleRate = 44100; //sampling frequency in Hz
    const int _bitsPerSample = 16; //bits per each sample (2 bytes)
    const int _channels = 1; //we just need 1 channel recording for our use case
    const string _webServerUrl = "http://0.0.0.0:8080";
    
    static void Main(string[] args)
    {
        var webServer = new WebServer(_webServerUrl);
        var captureParams = new CaptureParams{ BitsPerSample = _bitsPerSample, SampleRate = _sampleRate, Channels = _channels };
        var analyzer = new Analyzer(new AnalyzerParams{ Bands = _bands, SampleRate = _sampleRate });
        IDisplay ledDisplay = new LedDisplay(_ledDisplayLevels, _bands.Length);
        IDisplay consoleDisplay = new ConsoleDisplay(_consoleDisplayLevels, _bands.Length);
        IDisplay webDisplay = new WebDisplay(_webDisplayLevels, _bands.Length, webServer);

        ledDisplay.HidePeaks = false;
        consoleDisplay.HidePeaks = false;
        webDisplay.HidePeaks = false;

        ledDisplay.ShowPeaksWhenSilent = true;
        consoleDisplay.ShowPeaksWhenSilent = true;
        webDisplay.ShowPeaksWhenSilent = true;


        var cts = new CancellationTokenSource();
        //start capturing system audio (executed on a different threat)
        AudioCapture.StartCapture((buffer) =>{
            var bands = analyzer.ConvertToFrequencyBands(buffer)
                                .Amplify(_amplificationFactor)
                                .Normalize();
            
            // consoleDisplay.DisplayLevels(bands.ToLevels(_consoleDisplayLevels));
            ledDisplay.DisplayLevels(bands.ToLevels(_ledDisplayLevels));
            webDisplay.DisplayLevels(bands.ToLevels(_webDisplayLevels));

        }, captureParams, cts);


        webServer.Start();

        //press any key to terminate:
        Console.Read();
        cts.Cancel();
        Thread.Sleep(100);
        ledDisplay.Clear();
        consoleDisplay.Clear();
           
    }

    
}

