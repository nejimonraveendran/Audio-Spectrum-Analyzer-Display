namespace RpiSpectrumAnalyzer;

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

class WebDisplay : DisplayBase
{
    public WebDisplay(int rows, int cols, LedServer ledServer)
    {
        _rows = rows;
        _cols = cols;
        _ledServer = ledServer;

        _ledServer.OnConfigChanged += (e, config) => 
        {
            if(config?.DisplayType != DisplayType.WEB)
                return;
          
            _peakWait = config.PeakWait;
            _peakWaitCountDown = config.PeakWaitCountDown;
            _transitionSpeed = config.TransitionSpeed;
            _amplificationFactor = config.AmplificationFactor;
            _showPeaks = config.ShowPeaks;
            _showPeaksWhenSilent = config.ShowPeaksWhenSilent;
            
        };

        _ledServer.OnSocketClientConnected += (e, ws) => 
        {
            var payload = JsonSerializer.Serialize(new WebDisplayData{ Event = WebDisplayEvent.STARTUP, Data = new {Rows = _rows, Cols = _cols}});
            SendToClient(payload);
        };

    }

    public override bool IsBrightnessSupported() => true;

    public override void Clear()
    {
        var payload = JsonSerializer.Serialize(new WebDisplayData{ Event = WebDisplayEvent.COMMAND, Data = new {Command = "clear"}});
        SendToClient(payload);
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
        SendToClient(payload);
    }


    private void SendToClient(string content){
        if(_ledServer == null || _ledServer.SocketClients.Count == 0)
            return;

        
        var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(content));

        foreach (var client in _ledServer.SocketClients)
        {
            if(client == null || client.Socket == null || client.Socket.State != WebSocketState.Open)
                continue;

            client.Socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        
    }

}
