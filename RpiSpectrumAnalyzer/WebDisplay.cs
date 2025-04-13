using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace RpiSpectrumAnalyzer;

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
        if(_webServer == null || _webServer.SocketConnections.Count == 0)
            return;

        
        var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(content));

        foreach (var ws in _webServer.SocketConnections)
        {
            if(ws == null || ws.State != WebSocketState.Open)
                continue;

            ws.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        
    }

}
