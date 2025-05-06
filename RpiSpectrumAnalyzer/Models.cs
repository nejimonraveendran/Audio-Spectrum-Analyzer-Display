
namespace RpiSpectrumAnalyzer;
using System.Net.WebSockets;

struct BandInfo
{    
    public int Band { get; set; }
    public double Magnitude { get; set; }
}

struct LevelInfo
{    
    public int Band { get; set; }
    public int Level { get; set; }
}

struct WebDisplayData
{
    public WebDisplayEvent Event { get; set; }
    public object Data { get; set; }
}

struct ColPeak
{
    public int Col { get; set; }
    public int Row { get; set; }
    public long CurWait { get; set; }
    public long CurMilSecs { get; set; }   
    public long PrevMilSecs { get; set; }    
}

class CaptureResult
{
    public byte[]? Buffer { get; set; }
    public Exception? Exception { get; set; }
}

class AnalyzerParams
{
    public int[]? Bands { get; set; }
    public uint SampleRate {get; set;}
}

enum DisplayType
{
    LED = 1,
    CONSOLE = 2,
    WEB = 3
}

enum WebDisplayEvent
{
    DISPLAY = 2,
    CONFIG_CHANGED = 3,
    CLEAR = 4
}

enum LedDisplayWiring
{
    ZigZag = 0,
    Serpentine = 1
}


class DisplayConfiguration
{
    public DisplayType DisplayType { get; set; }
    public int Rows { get; set; }
    public int Cols { get; set; }
    public int BrightnessMin { get; set; }
    public int Brightness { get; set; }
    public int BrightnessMax { get; set; }
    public double TransitionSpeedMin { get; set; }
    public double TransitionSpeed { get; set; }
    public double TransitionSpeedMax { get; set; }
    public int PeakWaitMin { get; set; }
    public int PeakWait { get; set; }
    public int PeakWaitMax { get; set; }
    public int PeakWaitCountDownMin { get; set; }
    public int PeakWaitCountDown { get; set; }
    public int PeakWaitCountDownMax { get; set; }
    public int AmplificationFactorMin { get; set; }
    public int AmplificationFactor { get; set; }
    public int AmplificationFactorMax { get; set; }
    public bool ShowPeaks { get; set; }
    public bool ShowPeaksWhenSilent { get; set; }
    public bool IsBrightnessSupported { get; set; }
    public PixelColor? PeakColor { get; set; }
    public PixelColor[][]? PixelColors { get; set; } 
    public PixelColor? GradientStartColor { get; set; }
    public PixelColor? GradientEndColor { get; set; }
    
}

class SocketClient
{
    public string? Id { get; set; }
    public WebSocket? Socket { get; set; }
}

class PixelColor
{
    public int R { get; set; }
    public int G { get; set; }
    public int B { get; set; }
    
}
