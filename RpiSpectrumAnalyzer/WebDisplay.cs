namespace RpiSpectrumAnalyzer;

using System.Drawing;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

class WebDisplay : DisplayBase
{
    private PixelColor[][]? _pixelColors; //private Color[,]? _pixelColors;
    private PixelColor _peakColor;

    public WebDisplay(int rows, int cols)
    {
        _rows = rows;
        _cols = cols;
        _pixelColors = new PixelColor[_cols][]; //_pixelColors = new Color[_cols, _rows];
        _peakColor = new PixelColor{R = 255, G = 0, B = 0}; // Color.FromArgb(255, 0, 0);//  Helpers.HsvToColor(0, 1, 1);  //default, configurable via API call

        _transitionSpeedMin = 1;
        _transitionSpeed = 2; //default, configurable via API call
        _transitionSpeedMax = _rows/2;

        _peakWaitMin = 1;
        _peakWait = 500; //default, configurable via API call
        _peakWaitMax = 5000;

        _peakWaitCountDownMin = 1;
        _peakWaitCountDown = 20; //default, configurable via API call
        _peakWaitCountDownMax = 1000;

        SocketClients = [];

        SetupDefaultColors();
    }

    public List<SocketClient> SocketClients { get; set; }

    // public override int Rows => _rows;
    // public override int Cols => _cols;

    public override DisplayConfiguration GetConfiguration()
    {
        return new DisplayConfiguration
        {
            DisplayType = DisplayType.WEB,
            Rows = _rows,
            Cols = _cols,
            PeakWaitMin = _peakWaitMin,
            PeakWait = _peakWait,
            PeakWaitMax = _peakWaitMax,
            PeakWaitCountDownMin = _peakWaitCountDownMin,
            PeakWaitCountDown = _peakWaitCountDown,
            PeakWaitCountDownMax = _peakWaitCountDownMax,
            TransitionSpeedMin = _transitionSpeedMin,
            TransitionSpeed = _transitionSpeed,
            TransitionSpeedMax = _transitionSpeedMax,
            AmplificationFactorMin = _amplificationFactorMin,
            AmplificationFactor = _amplificationFactor,
            AmplificationFactorMax = _amplificationFactorMax,
            ShowPeaks = _showPeaks,
            ShowPeaksWhenSilent = _showPeaksWhenSilent,
            IsBrightnessSupported = IsBrightnessSupported,
            PeakColor = _peakColor,
            PixelColors = _pixelColors,
        };

    }

    public override void UpdateConfiguration(DisplayConfiguration? config)
    {
        _peakWait = config?.PeakWait > 0 ? config.PeakWait : _peakWait;
        _peakWaitCountDown = config?.PeakWaitCountDown > 0 ? config.PeakWaitCountDown : _peakWaitCountDown;
        _transitionSpeed = config?.TransitionSpeed > 0 ? config.TransitionSpeed : _transitionSpeed;
        _amplificationFactor = config?.AmplificationFactor > 0 ? config.AmplificationFactor : _amplificationFactor;
        _showPeaks = config?.ShowPeaks == null ? false : config.ShowPeaks;
        _showPeaksWhenSilent = config?.ShowPeaksWhenSilent == null ? false : config.ShowPeaksWhenSilent;
        _peakColor = config?.PeakColor != null ? config.PeakColor : _peakColor;
        _pixelColors = config?.PixelColors != null ? config.PixelColors : _pixelColors;
     }


    public override void Clear()
    {
        var payload = JsonSerializer.Serialize(new WebDisplayData{ Event = WebDisplayEvent.COMMAND, Data = new {Command = "clear"}});
        SendToClients(payload);
    }

    public override void DisplayAsLevels(BandInfo[] bands)
    {
        var levels = bands.Amplify(_amplificationFactor)
                        .Normalize()
                        .ToLevels(_rows);

        DisplayLevels(levels);
    }


    private void DisplayLevels(LevelInfo[] targetLevels)
    {
        //Update web displays via web socket
        var payload = JsonSerializer.Serialize(new WebDisplayData{ Event = WebDisplayEvent.DISPLAY, Data = targetLevels });
        SendToClients(payload);
    }


    private void SendToClients(string content){
        if(SocketClients == null || SocketClients.Count == 0)
            return;

        var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(content));

        //send to all connected clients        
        foreach (var client in SocketClients)
        {
            if(client == null || client.Socket == null || client.Socket.State != WebSocketState.Open)
                continue;

            client.Socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        
    }

    // private void SetupDefaultColors()
    // {
    //     for (int x = 0; x < _cols; x++)
    //     {
    //         _pixelColors[x] = new Color[_rows]; //_pixelColors[x] = new Color[_rows, _cols];
    //         for (int y = 0; y < _rows; y++)
    //         {
    //             double hue = Helpers.Map(y, 0, _rows, 120, 1); //map row numbers to the hue range green (120) to red (1)
    //             _pixelColors[x][y] = Helpers.HsvToColor(hue, 1, 1); //1 = full saturation, 
    //             //_pixelColors[x, y] = Helpers.HsvToColor(hue, 1, _brightness); //1 = full saturation, 
    //         }            
    //     }
    // }

    private void SetupDefaultColors()
    {
        var fromColor = new PixelColor{R = 100, G = 255, B = 0};
        var toColor = new PixelColor{R = 255, G = 100, B = 0};   
        var gradient = ColorHelper.GenerateGradient(fromColor, toColor, _rows); 
        
        for (int x = 0; x < _cols; x++)
        {
            _pixelColors[x] = new PixelColor[_rows]; //_pixelColors[x] = new Color[_rows, _cols];
            for (int y = 0; y < _rows; y++)
            {
                _pixelColors[x][y] = gradient[y]; //1 = full saturation, 
                //_pixelColors[x, y] = Helpers.HsvToColor(hue, 1, _brightness); //1 = full saturation, 
            }            
        }
    }

}
