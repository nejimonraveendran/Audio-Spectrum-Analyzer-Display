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
using System.Text.Json.Serialization;

namespace RpiSpectrumAnalyzer;

//to use Host class, add the folowing to the csproj file:
 //<ItemGroup><FrameworkReference Include="Microsoft.AspNetCore.App" /></ItemGroup>

class LedServer
{
    private string _url;
    private string _contentRoot;

    public List<DisplayBase> DisplayClients { get; set; }

    public LedServer(string url)
    {
        _url = url;
        _contentRoot = Directory.GetCurrentDirectory();
        DisplayClients = [];
        
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

    async Task HandleNotFound(HttpContext context){
        context.Response.StatusCode = context.Response.StatusCode = StatusCodes.Status404NotFound;;
        await context.Response.WriteAsync("<html>Nothing at that location!</html>");
    }

    private async Task HandleWebSocketConnection(HttpContext context){
        if(context.WebSockets.IsWebSocketRequest)
        {
            string? clientId = context.Request.Query["clientId"];
            if(string.IsNullOrEmpty(clientId))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            var webDisplay = DisplayClients.FirstOrDefault(d => d is WebDisplay) as WebDisplay;
            if(webDisplay != null)
            {
                webDisplay.SocketClients.RemoveAll(sc => sc.Id == clientId); //remove old client
                webDisplay.SocketClients.RemoveAll(sc => sc.Socket == null || sc.Socket.State != WebSocketState.Open); //remove closed sockets
                webDisplay.SocketClients.Add(new SocketClient{ Id = clientId, Socket = webSocket });

                //send startup message to new client
                var payload = JsonSerializer.Serialize(new WebDisplayData{ Event = WebDisplayEvent.STARTUP, Data = new {Rows = webDisplay.Rows, Cols = webDisplay.Cols}});
                var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(payload));
                await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            }

            await new TaskCompletionSource<object>().Task; //required to keep the middleware pipeline up and running.  Otherwise, WS will immediately disconnect.

        }else
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }


    private async Task HandleGetConfig(HttpContext context)
    {
        string? displayType = context.Request.Query["displayType"];

        if(string.IsNullOrEmpty(displayType))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("\"error\":\"displayType required\"");
            return;
        }

        var displayTypeEnum = (DisplayType)Enum.Parse(typeof(DisplayType), displayType);
        
        switch(displayTypeEnum)
        {
            case DisplayType.LED:
                if (DisplayClients.FirstOrDefault(d => d is LedDisplay) is LedDisplay ledDisplay) 
                {
                    var ledDisplayConfig = ledDisplay.GetConfiguration() as LedDisplayConfiguration;
                    var json = JsonSerializer.Serialize(ledDisplayConfig);
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(json);
                }
                break;
            case DisplayType.CONSOLE:
                if (DisplayClients.FirstOrDefault(d => d is ConsoleDisplay) is ConsoleDisplay consoleDisplay) 
                {
                    var consoleDisplayConfig = consoleDisplay.GetConfiguration() as ConsoleDisplayConfiguration;
                    var json = JsonSerializer.Serialize(consoleDisplayConfig);
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(json);
                }
                break;
            case DisplayType.WEB:
                if (DisplayClients.FirstOrDefault(d => d is WebDisplay) is WebDisplay webDisplay) 
                {
                    var webDisplayConfig = webDisplay.GetConfiguration() as WebDisplayConfiguration;
                    var json = JsonSerializer.Serialize(webDisplayConfig);
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(json);
                }
                break;
            default:
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("\"error\":\"displayType not found\"");
                break;
        }

    }

    private async Task HandleUpdateConfig(HttpContext context)
    {
        var json = await JsonDocument.ParseAsync(context.Request.Body);
        
        var config = JsonSerializer.Deserialize<DisplayConfiguration>(json);
        if(config == null)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("\"error\":\"invalid config\"");
            return;
        }

        switch(config.DisplayType)
        {
            case DisplayType.LED:
                var ledDisplay = DisplayClients.FirstOrDefault(d => d is LedDisplay) as LedDisplay;
                var ledDisplayConfig = JsonSerializer.Deserialize<LedDisplayConfiguration>(json);
                ledDisplay?.UpdateConfiguration(ledDisplayConfig);
                break;
            case DisplayType.CONSOLE:
                var consoleDisplay = DisplayClients.FirstOrDefault(d => d is ConsoleDisplay) as ConsoleDisplay;
                var consoleDisplayConfig = JsonSerializer.Deserialize<ConsoleDisplayConfiguration>(json);
                consoleDisplay?.UpdateConfiguration(consoleDisplayConfig);
                break;
            case DisplayType.WEB:
                var webDisplay = DisplayClients.FirstOrDefault(d => d is WebDisplay) as WebDisplay;
                var webDisplayConfig = JsonSerializer.Deserialize<WebDisplayConfiguration>(json);
                webDisplay?.UpdateConfiguration(webDisplayConfig);
                break;
            default:
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("\"error\":\"displayType not found\"");
                return;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.WriteAsync("\"result\":0");
    }

}