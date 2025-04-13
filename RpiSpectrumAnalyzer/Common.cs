using System.Drawing;
using System.Net.WebSockets;

namespace RpiSpectrumAnalyzer;

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
    public string Event { get; set; }
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

static class WebDisplayEvent
{
    public static string STARTUP => "startup";
    public static string COMMAND => "command";
    public static string DISPLAY => "display";
}

class CaptureParams
{
    public uint SampleRate {get; set;}
    public ushort BitsPerSample {get; set;}
    public ushort Channels {get; set;}
}

class AnalyzerParams
{
    public int[] Bands { get; set; }
    public uint SampleRate {get; set;}
}


class ConfigDto
{
    public int Brightness { get; set; }
}


class SocketClient
{
    public string Id { get; set; }
    public WebSocket? Socket { get; set; }
}

//extension helpers
static class Extensions
{
    public static BandInfo[] Amplify(this BandInfo[] bands, int amplificationFactor)
    {
        return bands.Select(b => new BandInfo{ Band = b.Band, Magnitude = b.Magnitude * amplificationFactor }).ToArray();
    }

    public static BandInfo[] Normalize(this BandInfo[] bands)
    {
        return bands.Select(b => new BandInfo{ Band = b.Band, Magnitude = Math.Clamp(b.Magnitude, 0, 1)}).ToArray();
    }

    public static LevelInfo[] ToLevels(this BandInfo[] bands, double fromMin, double fromMax, int toMin, int toMax)
    {
        return bands.Select(b => new LevelInfo{ Band = b.Band, Level = Convert.ToInt32(toMin + (b.Magnitude - fromMin) * (toMax - toMin) / (fromMax - fromMin))}).ToArray();
    }


    public static LevelInfo[] ToLevels(this BandInfo[] bands, int maxLevel)
    {
        return bands.Select(b => new LevelInfo{ Band = b.Band, Level = Convert.ToInt32(b.Magnitude * maxLevel / 1)}).ToArray();
    }
}

static class Helpers
{
    public static Color HsvToColor(double hue, double saturation, double value)
    {
        int hi = (int)(hue / 60) % 6;
        double f = (hue / 60) - Math.Floor(hue / 60);

        double v = value;
        double p = v * (1 - saturation);
        double q = v * (1 - f * saturation);
        double t = v * (1 - (1 - f) * saturation);

        (double r, double g, double b) = hi switch
        {
            0 => (v, t, p),
            1 => (q, v, p),
            2 => (p, v, t),
            3 => (p, q, v),
            4 => (t, p, v),
            _ => (v, p, q),
        };

        return Color.FromArgb((int)r, (int)g, (int)b);
    }


    public static double Map(double value, double fromMin, double fromMax, double toMin, double toMax)
    {
        return toMin + (value - fromMin) * (toMax - toMin) / (fromMax - fromMin);
    }   

    public static int Map(int value, int fromMin, int fromMax, int toMin, int toMax)
    {
        return toMin + (value - fromMin) * (toMax - toMin) / (fromMax - fromMin);
    }

} 