namespace RpiSpectrumAnalyzer;

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

class WebDisplay : IDisplay
{
    int _rows, _cols;
    private WebServer _webServer;
    public WebDisplay(int rows, int cols, WebServer webServer)
    {
        _rows = rows;
        _cols = cols;
        _webServer = webServer;

        _webServer.OnSocketClientConnected += (e, ws) => 
        {
            var payload = JsonSerializer.Serialize(new WebDisplayData{ Event = WebDisplayEvent.STARTUP, Data = new {Rows = _rows, Cols = _cols}});
            SendToClient(payload);
        };

    }

    public bool HidePeaks { get; set; }
    public bool ShowPeaksWhenSilent { get; set; }
    public bool IsBrightnessSupported => false;

    public void Clear()
    {
        var payload = JsonSerializer.Serialize(new WebDisplayData{ Event = WebDisplayEvent.COMMAND, Data = new {Command = "clear"}});
        SendToClient(payload);
    }

    public void DisplayLevels(LevelInfo[] targetLevels)
    {
        //Update web displays via web socket
        var payload = JsonSerializer.Serialize(new WebDisplayData{ Event = WebDisplayEvent.DISPLAY, Data = targetLevels });
        SendToClient(payload);
    }


    private void SendToClient(string content){
        if(_webServer == null || _webServer.SocketClients.Count == 0)
            return;

        
        var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(content));

        foreach (var client in _webServer.SocketClients)
        {
            if(client == null || client.Socket == null || client.Socket.State != WebSocketState.Open)
                continue;

            client.Socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        
    }

}
