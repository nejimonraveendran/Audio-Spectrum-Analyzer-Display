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
    private static string _ledServerUrl = "http://0.0.0.0:8090";
    private static int[] _bands = {100, 500, 1000, 2000, 4000, 6000, 8000, 10000, 12000, 14000}; //audio frequencies (Hz) to analyze
    private static int _consoleDisplayLevels = 16; //default number of levels
    private static int _ledDisplayLevels = 10; //number of levels
    private static int _webDisplayLevels = 15; //number of levels
    private static bool _consoleDisplayEnabled = true;
    private static bool _ledDisplayEnabled = true;
    private static bool _webDisplayEnabled = true;
    
    
    static void Main(string[] args)
    {
        if (!SetConfigFromArgs(args)) return;

        const int sampleRate = 44100; //sampling frequency in Hz

        var ledServer = new LedServer(_ledServerUrl);
        var analyzer = new Analyzer(new AnalyzerParams{ Bands = _bands, SampleRate = sampleRate });

        var displays = new List<DisplayBase>();

        if(_consoleDisplayEnabled)
            displays.Add(new ConsoleDisplay(_consoleDisplayLevels, _bands.Length));
        
        if(_ledDisplayEnabled)
            displays.Add(new LedDisplay(_ledDisplayLevels, _bands.Length));

        if(_webDisplayEnabled)
            displays.Add(new WebDisplay(_webDisplayLevels, _bands.Length));

        ledServer.DisplayClients.AddRange(displays);

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


    // --port http://0.0.0.0:9090 
    // --bands "100, 500, 1000, 2000, 4000, 6000, 8000, 10000, 12000, 14000" 
    // --console-display-levels 20 
    // --led-display-levels 10 
    // --web-display-levels 20
    // --disable-console-display
    // --disable-led-display
    // --disable-web-display
    private static bool SetConfigFromArgs(string[] args)
    {
        if(args.Length == 0) return true;

        try
        {
            var optionsDict = new Dictionary<string, string>();

            for (int i = 0; i < args.Length; i++)
            {
                if(!args[i].StartsWith("--")) continue;
                if(!args[i].StartsWith("--disable-")){
                    optionsDict.Add(args[i], args[i+1]);
                }
                else
                {
                    optionsDict.Add(args[i], string.Empty);
                }

                
            }

            string portOption = "--port";
            if(optionsDict.ContainsKey("--port"))
            {
                var port = optionsDict.FirstOrDefault(kvp => kvp.Key == portOption).Value.Trim();
                _ledServerUrl = string.IsNullOrEmpty(port) ? _ledServerUrl : $"http://0.0.0.0:{port}";
            }

            string consoleDisplayLevelsOption = "--console-display-levels";
            if(optionsDict.ContainsKey(consoleDisplayLevelsOption))
            {
                var consoleLevels = optionsDict.FirstOrDefault(kvp => kvp.Key == consoleDisplayLevelsOption).Value.Trim();
                _consoleDisplayLevels = string.IsNullOrEmpty(consoleLevels) ? _consoleDisplayLevels : Convert.ToInt32(consoleLevels);
            }

            string ledDisplayLevelsOption = "--led-display-levels";
            if(optionsDict.ContainsKey(ledDisplayLevelsOption))
            {
                var ledLevels = optionsDict.FirstOrDefault(kvp => kvp.Key == ledDisplayLevelsOption).Value.Trim();
                _ledDisplayLevels = string.IsNullOrEmpty(ledLevels) ? _ledDisplayLevels : Convert.ToInt32(ledLevels);
            }

            string webDisplayLevelOption = "--web-display-levels";
            if(optionsDict.ContainsKey(webDisplayLevelOption))
            {
                var webLevels = optionsDict.FirstOrDefault(kvp => kvp.Key == webDisplayLevelOption).Value.Trim();
                _webDisplayLevels = string.IsNullOrEmpty(webLevels) ? _webDisplayLevels : Convert.ToInt32(webLevels);
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

            if(optionsDict.ContainsKey("--disable-led-display"))
            {
                _ledDisplayEnabled = false;
            }

            if(optionsDict.ContainsKey("--disable-web-display"))
            {
                _webDisplayEnabled = false;
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
}

