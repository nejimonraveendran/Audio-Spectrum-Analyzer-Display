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
            var payload = JsonSerializer.Serialize(new WebDisplayData{ Event = "startup", Data = new {Rows = _rows, Cols = _cols}});
            SendToClient(payload);
        };

    }

    public bool HidePeaks { get; set; }
    public bool ShowPeaksWhenSilent { get; set; }

    public void Clear()
    {
        var payload = JsonSerializer.Serialize(new WebDisplayData{ Event = "command", Data = new {Command = "clear"}});
        SendToClient(payload);
    }

    public void DisplayLevels(LevelInfo[] targetLevels)
    {
        //Update web displays via web socket
        var payload = JsonSerializer.Serialize(new WebDisplayData{ Event = "display", Data = targetLevels });
        SendToClient(payload);
    }


    private void SendToClient(string content){
        if(_webServer == null || _webServer.SocketConnection == null || _webServer.SocketConnection.State != WebSocketState.Open)
            return;

        var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(content));
        _webServer.SocketConnection.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);

    }

}
