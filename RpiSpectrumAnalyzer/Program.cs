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

    //default values for the params - can be changed through arguments
    private static string _ledServerUrl = "http://0.0.0.0:8090";
    private static int[] _bands = {100, 500, 1000, 2000, 4000, 6000, 8000, 10000, 12500, 14000}; //audio frequencies (Hz) to analyze
    private static int _consoleDisplayLevels = 16; //default number of levels
    private static int _ledDisplayLevels = 10; //number of levels
    private static int _webDisplayLevels = 15; //number of levels
    private static bool _consoleDisplayEnabled = true;
    private static bool _ledDisplayEnabled = true;
    private static bool _webDisplayEnabled = true;
    
    
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
                displays.Add(new LedDisplay(_ledDisplayLevels, _bands.Length));
        }
        catch (Exception ex) 
        {
            ShowError("Error during setting up LED Didplay. Please make sure SPI settings are correctly configured. To disable LED Display, use --disable-led-display command line option.", ex);      
            PrepareForExit(displays, cts);
            return;              
        }


        if(_webDisplayEnabled)
            displays.Add(new WebDisplay(_webDisplayLevels, _bands.Length));

        if(_consoleDisplayEnabled)
            displays.Add(new ConsoleDisplay(_consoleDisplayLevels, _bands.Length));                    

        ledServer.DisplayClients.AddRange(displays);

        //start capturing system audio (executed on a different threat)
        AudioCapture.StartCapture(result =>{
            if(result.Exception != null){
                ShowError("Error during audio capture. Please make sure PulseAudio is installed and running.", result.Exception);
                return;
            }

            var bands = analyzer.ConvertToFrequencyBands(result.Buffer);
            displays.ForEach(display => display.DisplayAsLevels(bands));

        }, sampleRate, cts);

        ledServer.Start(cts);

        var availableConsoleDisplay = ledServer.DisplayClients.FirstOrDefault(d => d.GetType().Equals(typeof(ConsoleDisplay))) as ConsoleDisplay;
        
        if(availableConsoleDisplay != null)
            availableConsoleDisplay.Info = $"Server running at {_ledServerUrl}. Press any key to exit.";
        

        PrepareForExit(displays, cts);


        
        // try
        // {        

            // //press any key to terminate:
            // var keyPressListenerTask = Task.Run(() => 
            // {
            //     var availableConsoleDisplay = ledServer.DisplayClients.FirstOrDefault(d => d.GetType().Equals(typeof(ConsoleDisplay))) as ConsoleDisplay;
                
            //     if(availableConsoleDisplay != null)
            //         availableConsoleDisplay.Info = "Press any key to exit.";
                
            //     Console.Read();
            //     cts.Cancel();

            //     availableConsoleDisplay.Info = "Cancelled.";

            //     // Thread.Sleep(100);
            //     // displays.ForEach(display => display.Clear());

            // }, cts.Token);

            // var availableConsoleDisplay = ledServer.DisplayClients.FirstOrDefault(d => d.GetType().Equals(typeof(ConsoleDisplay))) as ConsoleDisplay;
            
            // if(availableConsoleDisplay != null)
            //     availableConsoleDisplay.Info = "Press any key to exit.";
        
            
            // Task.WhenAny(ledServerTask, keyPressListenerTask).Wait();

            // Console.ForegroundColor = ConsoleColor.White;
            // Console.WriteLine("exited");
            // // Console.WriteLine(exception?.ToString());
            
            // Thread.Sleep(100);
            // displays.ForEach(display => display.Clear());

         
            // await captureTask.ContinueWith((r)=> 
            // {
            //     Console.ForegroundColor = ConsoleColor.Red;
            //     if(r.Exception != null)
            //     {
            //         Console.WriteLine(r.Exception.ToString());
            //     }
                
            // });


            // await ledServerTask.ContinueWith((r)=> 
            // {
            //     Console.ForegroundColor = ConsoleColor.Red;
            //     if(r.Exception != null)
            //     {
            //         Console.WriteLine(r.Exception.ToString());
            //     }
                
            // });
        // }
        // catch (Exception ex)
        // {
        //     Console.ForegroundColor = ConsoleColor.Red;
        //     Console.WriteLine("Unexpected error: " + Environment.NewLine + ex.Message); 
        // }

    

    }

    private static void PrepareForExit(List<DisplayBase> displays, CancellationTokenSource cts)
    {
        Console.Read();
        cts.Cancel();
        Thread.Sleep(100);
        displays.ForEach(display => display.Clear());   
    }

    private static void ShowError(string message, Exception exception)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message);
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(exception?.ToString());
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Press any key to exit.");
        
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

