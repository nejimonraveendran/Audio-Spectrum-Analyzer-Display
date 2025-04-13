using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Routing;
using System.Text;
using System.Text.Json;
using System.Net.WebSockets;
using Microsoft.Extensions.FileProviders;

namespace RpiSpectrumAnalyzer;

//to use Host class, add the folowing to the csproj file:
 //<ItemGroup><FrameworkReference Include="Microsoft.AspNetCore.App" /></ItemGroup>

class WebServer
{
    string _url;
    string _contentRoot;

    // List<WebSocket?> _webSockets;
    List<SocketClient> _socketClients;

    public WebServer(string url)
    {
        _url = url;
        _contentRoot = Directory.GetCurrentDirectory();
        _socketClients = new List<SocketClient>();
    }

    public void Start()
    {
        Task.Factory.StartNew(() => 
        {
            var host = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder => 
            {
                webBuilder.UseUrls(_url);
                
                webBuilder.Configure(app => 
                {
                    app.UseDefaultFiles();
                    app.UseStaticFiles(new StaticFileOptions 
                    {
                        DefaultContentType = "text/html",
                        FileProvider  = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"))
                    });

                    //websocket with 1 min keepalive ping-pong
                    app.UseWebSockets(new WebSocketOptions{ KeepAliveInterval = TimeSpan.FromMinutes(1)});

                    app.UseRouter(routes => 
                    {
                        routes.MapGet("/api/config", async context => 
                        {
                            await HandleGetConfig(context);
                        });

                        routes.MapPost("/api/config", async context => 
                        {
                            await HandleUpdateConfig(context);
                        });

                        routes.MapRoute("/ws", async context => 
                        {
                            await HandleWebSocketConnection(context);
                        });

                    });

                    //terminal middleware for unhandled routes
                    app.Run(async context => 
                    {
                        await HandleNotFound(context);
                    });

                });

                //clear the output logging to console so that our spectrum analyzer bar display does not get spoiled by ASP.NET's log messages 
                webBuilder.SuppressStatusMessages(true);
                webBuilder.ConfigureLogging((context, logging) => 
                {
                    logging.ClearProviders();
                });

            }).Build();

            host.Run();
            
        }, TaskCreationOptions.LongRunning);

    }

    public IList<SocketClient> SocketClients  => _socketClients;

    public event EventHandler<WebSocket?> OnSocketClientConnected;

    async Task HandleNotFound(HttpContext context){
        context.Response.StatusCode = context.Response.StatusCode = StatusCodes.Status404NotFound;;
        await context.Response.WriteAsync("<html>Nothing at that location!</html>");
    }

    private async Task HandleWebSocketConnection(HttpContext context){
        if(context.WebSockets.IsWebSocketRequest)
        {
            var clientId = context.Request.Query["clientId"];
            if(string.IsNullOrEmpty(clientId))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            //check if we already have a socket with the same id
            _socketClients.RemoveAll(ws => ws.Id == clientId); //remove old socket with same id
            _socketClients.RemoveAll(ws => ws.Socket == null || ws.Socket.State != WebSocketState.Open); //remove closed sockets
            _socketClients.Add(new SocketClient{ Id = clientId, Socket = webSocket });
            
            this.OnSocketClientConnected?.Invoke(this, webSocket); //fire connected event

            await new TaskCompletionSource<object>().Task; //required to keep the middleware pipeline up and running.  Otherwise, WS will immediately disconnect.

        }else
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }


    private async Task HandleGetConfig(HttpContext context)
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("\"get\":0");
    }

    private async Task HandleUpdateConfig(HttpContext context)
    {
        
        var json = await JsonDocument.ParseAsync(context.Request.Body);
        json.Deserialize<ConfigDto>();
        var config = JsonSerializer.Deserialize<ConfigDto>(json);

        string res = JsonSerializer.Serialize<ConfigDto>(config);

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(res);
    }

}