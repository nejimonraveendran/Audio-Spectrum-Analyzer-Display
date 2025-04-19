namespace RpiSpectrumAnalyzer;

using System.Drawing;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

class WebDisplay : DisplayBase
{
    private Color[][]? _pixelColors; //private Color[,]? _pixelColors;
    private Color _peakColor;

    public WebDisplay(int rows, int cols)
    {
        _rows = rows;
        _cols = cols;
        _pixelColors = new Color[_cols][]; //_pixelColors = new Color[_cols, _rows];
        _peakColor =  Helpers.HsvToColor(0, 1, 1);  //default, configurable via API call
        _transitionSpeed = 2; //default, configurable via API call
        _peakWait = 500; //default, configurable via API call
        _peakWaitCountDown = 20; //default, configurable via API call

        SocketClients = [];

        SetupDefaultColors();
    }

    public List<SocketClient> SocketClients { get; set; }

    public override int Rows => _rows;
    public override int Cols => _cols;

    public override DisplayConfiguration GetConfiguration()
    {
        return new WebDisplayConfiguration
        {
            DisplayType = DisplayType.WEB,
            PeakWait = _peakWait,
            PeakWaitCountDown = _peakWaitCountDown,
            TransitionSpeed = _transitionSpeed,
            AmplificationFactor = _amplificationFactor,
            ShowPeaks = _showPeaks,
            ShowPeaksWhenSilent = _showPeaksWhenSilent,
            IsBrightnessSupported = IsBrightnessSupported,
            PeakColor = _peakColor,
            PixelColors = _pixelColors,
            // Brightness = _brightness, //not supported
        };

    }

    public override void UpdateConfiguration(DisplayConfiguration? config)
    {
        if(config?.DisplayType != DisplayType.WEB)
            return;

        var webDisplayConfig = config as WebDisplayConfiguration;
        if (webDisplayConfig == null)
            return;
        
        _peakWait = webDisplayConfig.PeakWait > 0 ? webDisplayConfig.PeakWait : _peakWait;
        _peakWaitCountDown = webDisplayConfig.PeakWaitCountDown > 0 ? webDisplayConfig.PeakWaitCountDown : _peakWaitCountDown;
        _transitionSpeed = webDisplayConfig.TransitionSpeed > 0 ? webDisplayConfig.TransitionSpeed : _transitionSpeed;
        _amplificationFactor = webDisplayConfig.AmplificationFactor > 0 ? webDisplayConfig.AmplificationFactor : _amplificationFactor;
        _showPeaks = webDisplayConfig.ShowPeaks;
        _showPeaksWhenSilent = webDisplayConfig.ShowPeaksWhenSilent;
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

    private void SetupDefaultColors()
    {
        for (int x = 0; x < _cols; x++)
        {
            _pixelColors[x] = new Color[_rows]; //_pixelColors[x] = new Color[_rows, _cols];
            for (int y = 0; y < _rows; y++)
            {
                double hue = Helpers.Map(y, 0, _rows, 120, 1); //map row numbers to the hue range green (120) to red (1)
                _pixelColors[x][y] = Helpers.HsvToColor(hue, 1, 1); //1 = full saturation, 
                //_pixelColors[x, y] = Helpers.HsvToColor(hue, 1, _brightness); //1 = full saturation, 
            }            
        }
    }


}
