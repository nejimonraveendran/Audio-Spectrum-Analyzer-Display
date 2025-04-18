namespace RpiSpectrumAnalyzer;

using System.Net.WebSockets;

class LedServer
{
    private List<IDisplay> _displays { get; }
    private WebServer _webServer { get; }

    public LedServer(List<IDisplay> displays, WebServer webServer)
    {
        _displays = displays;
        _webServer = webServer;
    }    

    
    
}