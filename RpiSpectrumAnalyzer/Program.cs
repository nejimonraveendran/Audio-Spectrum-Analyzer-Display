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
    private static string _ledServerUrl = "http://0.0.0.0:8080";
    private static int[] _bands = {100, 500, 1000, 2000, 4000, 6000, 8000, 10000, 12000, 14000}; //audio frequencies (Hz) to analyze
    private static int _consoleDisplayLevels = 16; //number of levels
    private static int _ledDisplayLevels = 10; //number of levels
    private static int _webDisplayLevels = 30; //number of levels
    
    
    static void Main(string[] args)
    {
        const int sampleRate = 44100; //sampling frequency in Hz

        var ledServer = new LedServer(_ledServerUrl);
        var analyzer = new Analyzer(new AnalyzerParams{ Bands = _bands, SampleRate = sampleRate });

        var displays = new List<DisplayBase>
        {
            new ConsoleDisplay(_consoleDisplayLevels, _bands.Length, ledServer),
            new LedDisplay(_ledDisplayLevels, _bands.Length, ledServer),
            new WebDisplay(_webDisplayLevels, _bands.Length, ledServer)
        };


        var cts = new CancellationTokenSource();

        //start capturing system audio (executed on a different threat)
        AudioCapture.StartCapture((buffer) =>{            
            var bands = analyzer.ConvertToFrequencyBands(buffer);
            displays.ForEach(display => display.DisplayAsLevels(bands));

        }, sampleRate, cts);


        ledServer.Start();

        //press any key to terminate:
        Console.Read();
        cts.Cancel();
        Thread.Sleep(100);
        displays.ForEach(display => display.Clear());
           
    }

    
}

