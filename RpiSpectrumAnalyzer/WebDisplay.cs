namespace RpiSpectrumAnalyzer;

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

class WebDisplay : DisplayBase
{
    public WebDisplay(int rows, int cols)
    {
        _rows = rows;
        _cols = cols;
        _pixelColors = new PixelColor[_cols][]; 
        _peakColor = new PixelColor{R = 255, G = 0, B = 0};  //default, configurable via API call

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

        _gradientStartColor = new PixelColor{R = 100, G = 255, B = 0};
        _gradientEndColor = new PixelColor{R = 255, G = 100, B = 0};   

        SetupDefaultColors();
    }

    public List<SocketClient> SocketClients { get; set; }

    public void AddSocketClient(SocketClient socketClient)
    {
        SocketClients.RemoveAll(sc => sc.Id == socketClient.Id); //remove old client
        SocketClients.RemoveAll(sc => sc.Socket == null || sc.Socket.State != WebSocketState.Open); //remove closed sockets
        SocketClients.Add(socketClient);

        //send startup config message
        var payload = JsonSerializer.Serialize(new WebDisplayData{ Event = WebDisplayEvent.CONFIG_CHANGED, Data = new {Config = this.GetConfiguration()}});
        SendToClients(payload);
    }

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
            GradientStartColor = _gradientStartColor,
            GradientEndColor = _gradientEndColor
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
        _gradientStartColor = config?.GradientStartColor != null ? config.GradientStartColor : _gradientStartColor;
        _gradientEndColor = config?.GradientEndColor != null ? config.GradientEndColor : _gradientEndColor;

        var payload = JsonSerializer.Serialize(new WebDisplayData{ Event = WebDisplayEvent.CONFIG_CHANGED, Data = new {Config = this.GetConfiguration()}});
        SendToClients(payload);
     }


    public override void Clear()
    {
        var payload = JsonSerializer.Serialize(new WebDisplayData{ Event = WebDisplayEvent.CLEAR });
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

    private void SetupDefaultColors()
    {
        var gradient = ColorConversion.GenerateGradient(_gradientStartColor, _gradientEndColor, _rows); 
        
        for (int x = 0; x < _cols; x++)
        {
            _pixelColors[x] = new PixelColor[_rows]; 
            for (int y = 0; y < _rows; y++)
            {
                _pixelColors[x][y] = gradient[y]; //1 = full saturation, 
            }            
        }
    }

}
