﻿using System.Device.Spi;
using System.Drawing;
using Iot.Device.Ws28xx;

namespace RpiSpectrumAnalyzer;

//Author: Nejimon Raveendran
//PROJECT DEPENDENCIES:
//sudo apt install bluez blueman pulseaudio pulseaudio-module-bluetooth -y 
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

    //default values for the params - can be changed through arguments
    private static string _ledServerUrl = "http://0.0.0.0:8090";
    private static int[] _bands = {100, 500, 1000, 2000, 4000, 6000, 8000, 10000, 12500, 14000}; //audio frequencies (Hz) to analyze
    private static int _consoleDisplayLevels = 16; //default number of levels
    private static int _ledDisplayLevels = 10; //number of levels
    private static int _webDisplayLevels = 15; //number of levels
    private static int _ledDisplayCols = 0; //number of LED display cols (for validation against number of bands)
    private static bool _consoleDisplayEnabled = true;
    private static bool _ledDisplayEnabled = false;
    private static bool _webDisplayEnabled = true;
    private static LedDisplayWiring _ledDisplayWiring = LedDisplayWiring.ZigZagStrip;
    
    
    
    static async Task Main(string[] args)
    {

        if (!SetConfigFromArgs(args)) return;

        const int sampleRate = 44100; //sampling frequency in Hz
        var cts = new CancellationTokenSource();
        var displays = new List<DisplayBase>();

        var ledServer = new LedServer(_ledServerUrl);
        var analyzer = new Analyzer(new AnalyzerParams{ Bands = _bands, SampleRate = sampleRate });

        try
        {
            if(_ledDisplayEnabled)
            {
                if(_bands.Length > _ledDisplayCols){
                    ShowError($"Number of bands ({_bands.Length}) cannot be greater than number of LED columns ({_ledDisplayCols}).  Please use --bands and --led-display-cols options.");
                    PrepareForExit(displays, cts);
                    return;
                }

                displays.Add(new LedDisplay(_ledDisplayLevels, _bands.Length, _ledDisplayWiring));
            }
        }
        catch (Exception ex) 
        {
            ShowError("Error during setting up LED Didplay. Please make sure SPI settings are correctly configured.", ex);      
            PrepareForExit(displays, cts);
            return;              
        }

        string consoleMessage = $"Server running at {_ledServerUrl}. Press any key to exit.";
        if(_consoleDisplayEnabled)
        {
            var consoleDisplay = new ConsoleDisplay(_consoleDisplayLevels, _bands.Length);
            consoleDisplay.Message = consoleMessage;
            displays.Add(consoleDisplay);
        }else
        {
            Console.WriteLine(consoleMessage);
        }

        
        if(_webDisplayEnabled)
            displays.Add(new WebDisplay(_webDisplayLevels, _bands.Length));                                

        ledServer.DisplayClients.AddRange(displays);        
        
        var ledServerTask = ledServer.Start(cts);
        if(ledServerTask.Exception != null)
        {
            ShowError("Error during web server startup.", ledServerTask.Exception); 
            PrepareForExit(displays, cts);
            return; 
        }

        //start capturing system audio (executed on a different threat)
        AudioCapture.StartCapture(result =>{
            if(result.Exception != null){
                ShowError("Error during audio capture.", result.Exception);
                return;
            }

            var bands = analyzer.ConvertToFrequencyBands(result.Buffer);
            displays.ForEach(display => display.DisplayAsLevels(bands));

        }, sampleRate, cts);
        
        PrepareForExit(displays, cts);    

    }

    private static void PrepareForExit(List<DisplayBase> displays, CancellationTokenSource cts)
    {
        Console.Read();
        cts.Cancel();
        Thread.Sleep(100);
        displays.ForEach(display => display.Clear());   
    }

    private static void ShowError(string message, Exception? exception = null)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message);

        if(exception != null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(exception?.ToString());
        }

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Press any key to exit.");
        
    }

    private static bool SetConfigFromArgs(string[] args)
    {
        if(args.Length == 0) return true;

        try
        {
            var optionsDict = new Dictionary<string, string>();

            for (int i = 0; i < args.Length; i++)
            {
                if(!args[i].StartsWith("--")) continue;
                if(!args[i].StartsWith("--disable-") && !args[i].StartsWith("--enable-")){
                    optionsDict.Add(args[i], args[i+1]);
                }
                else
                {
                    optionsDict.Add(args[i], string.Empty);
                }
            }

            string portOption = "--port";
            if(optionsDict.ContainsKey(portOption))
            {
                int port = ConvertOptionToInt(optionsDict, portOption);
                _ledServerUrl = $"http://0.0.0.0:{port}";
            }

            string consoleDisplayLevelsOption = "--console-display-levels";
            if(optionsDict.ContainsKey(consoleDisplayLevelsOption))
            {
                _consoleDisplayLevels = ConvertOptionToInt(optionsDict, consoleDisplayLevelsOption); 
            }
            
            string ledDisplayLevelsOption = "--led-display-levels";
            if(optionsDict.ContainsKey(ledDisplayLevelsOption))
            {
                _ledDisplayLevels = ConvertOptionToInt(optionsDict, ledDisplayLevelsOption);
            }

            string ledDisplayColsOption = "--led-display-cols";
            if(optionsDict.ContainsKey(ledDisplayColsOption))
            {
                _ledDisplayCols = ConvertOptionToInt(optionsDict, ledDisplayColsOption);
            }

            string webDisplayLevelOption = "--web-display-levels";
            if(optionsDict.ContainsKey(webDisplayLevelOption))
            {
                _webDisplayLevels = ConvertOptionToInt(optionsDict, webDisplayLevelOption);
            }


            string bandsOption = "--bands";
            if(optionsDict.ContainsKey(bandsOption))
            {
                var bandsString = optionsDict.FirstOrDefault(kvp => kvp.Key == bandsOption).Value;      
                var bands = bandsString.Split(',').ToList().Select(band => Convert.ToInt32(band.Trim())).ToArray();
                if(bands.Length > 0) _bands = bands;
            }

            if(optionsDict.ContainsKey("--disable-console-display"))
            {
                _consoleDisplayEnabled = false;
            }

            if(optionsDict.ContainsKey("--enable-led-display"))
            {
                _ledDisplayEnabled = true;
            }

            if(optionsDict.ContainsKey("--disable-web-display"))
            {
                _webDisplayEnabled = false;
            }

            string ledDisplayWiring = "--led-display-wiring";
            if(optionsDict.ContainsKey(ledDisplayWiring))
            {
                var wiring = optionsDict.FirstOrDefault(kvp => kvp.Key == ledDisplayWiring).Value.Trim().ToLower();

                _ledDisplayWiring = wiring switch
                {
                    "serpentine" => LedDisplayWiring.SerpentineMatrix,
                    "zigzag" => LedDisplayWiring.ZigZagStrip,
                    "s" => LedDisplayWiring.SerpentineMatrix,
                    "z" => LedDisplayWiring.ZigZagStrip,
                    _ => LedDisplayWiring.ZigZagStrip
                };

            }
            else
            {
                _ledDisplayWiring = LedDisplayWiring.ZigZagStrip;
            }

            return true;

        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error parsing the input options: " + Environment.NewLine + ex.Message);
            return false;
        }
    }

    private static int ConvertOptionToInt(Dictionary<string, string> optionsDict, string optionKey)
    {
        string optionValue = optionsDict.FirstOrDefault(kvp => kvp.Key == optionKey).Value;

        if(string.IsNullOrWhiteSpace(optionValue))
            throw new ArgumentException($"Command command line option cannot be empty: {optionKey}");

        if (!int.TryParse(optionValue.Trim(), out int result)) 
            throw new ArgumentException($"Command command line option must be an integer: {optionKey}");

        return result;
        
    }

}

