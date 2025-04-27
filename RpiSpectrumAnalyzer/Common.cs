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

// class CaptureParams
// {
//     public uint SampleRate {get; set;}
//     public ushort BitsPerSample {get; set;}

// }

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
    public PixelColor PeakColor { get; set; }
    public PixelColor[][]? PixelColors { get; set; } 
    public PixelColor GradientBottomColor { get; set; }
    public PixelColor GradientTopColor { get; set; }
    
}


// class ConsoleDisplayConfiguration : DisplayConfiguration
// {
//     public ConsoleColor PeakColor { get; set; }
//     public ConsoleColor[][]? PixelColors { get; set; } // public ConsoleColor[,]? PixelColors { get; set; }
    
// }

// class LedDisplayConfiguration : DisplayConfiguration
// {
//     public Color PeakColor { get; set; }
//     public Color[][]? PixelColors { get; set; } //public Color[,]? PixelColors { get; set; }
    
// }


// class WebDisplayConfiguration : DisplayConfiguration
// {
//     public Color PeakColor { get; set; }
//     public Color[][]? PixelColors { get; set; } 
// }

class SocketClient
{
    public string? Id { get; set; }
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

    public static int Map(this int value, int fromMin, int fromMax, int toMin, int toMax)
    {
        return toMin + (value - fromMin) * (toMax - toMin) / (fromMax - fromMin);
    }

}

class PixelColor
{
    public int R { get; set; }
    public int G { get; set; }
    public int B { get; set; }
    
}

// class Dummy
// {
//     public Color PeakColor { get; set; }
// }

class ColorHelper
{

    /// <summary>
    /// 
    /// </summary>
    /// <param name="hue">0 to 360</param>
    /// <param name="saturation">0 to 1</param>
    /// <param name="value"></param>
    /// <returns></returns>
    // public static Color HsvToColor(double hue, double saturation, double value)
    // {
    //     int hi = (int)(hue / 60) % 6;
    //     double f = (hue / 60) - Math.Floor(hue / 60);

    //     double v = value;
    //     double p = v * (1 - saturation);
    //     double q = v * (1 - f * saturation);
    //     double t = v * (1 - (1 - f) * saturation);

    //     (double r, double g, double b) = hi switch
    //     {
    //         0 => (v, t, p),
    //         1 => (q, v, p),
    //         2 => (p, v, t),
    //         3 => (p, q, v),
    //         4 => (t, p, v),
    //         _ => (v, p, q),
    //     };

    //     return Color.FromArgb((int)r, (int)g, (int)b);
    // }

    public Color RgbColor { get; set; }
    public ConsoleColor ConsoleColor { get; set; }
    public string? HtmlColor { get; set; }
    


    private static ColorHelper[] _consoleColors = 
    {
        new ColorHelper{ ConsoleColor = ConsoleColor.DarkRed, RgbColor = Color.DarkRed, HtmlColor = "#8B0000"},
        new ColorHelper{ ConsoleColor = ConsoleColor.Red, RgbColor = Color.Red, HtmlColor = "#FF0000"},
        new ColorHelper{ ConsoleColor = ConsoleColor.DarkYellow, RgbColor = Color.Orange, HtmlColor = "#FFA500"},
        new ColorHelper{ ConsoleColor = ConsoleColor.Yellow, RgbColor = Color.Yellow, HtmlColor = "#FFFF00"},
        new ColorHelper{ ConsoleColor = ConsoleColor.Green, RgbColor = Color.Green, HtmlColor = "#00FF00"},
        new ColorHelper{ ConsoleColor = ConsoleColor.DarkGreen, RgbColor = Color.DarkGreen, HtmlColor = "#006400"},
        new ColorHelper{ ConsoleColor = ConsoleColor.Cyan, RgbColor = Color.Cyan, HtmlColor = "#00FFFF"},
        new ColorHelper{ ConsoleColor = ConsoleColor.DarkCyan, RgbColor = Color.DarkCyan, HtmlColor = "#008B8B"},
        new ColorHelper{ ConsoleColor = ConsoleColor.Blue, RgbColor = Color.Blue, HtmlColor = "#0000FF"},
        new ColorHelper{ ConsoleColor = ConsoleColor.DarkBlue, RgbColor = Color.DarkBlue, HtmlColor = "#00008B"},
        new ColorHelper{ ConsoleColor = ConsoleColor.Magenta, RgbColor = Color.Magenta, HtmlColor = "#FF00FF"},
        new ColorHelper{ ConsoleColor = ConsoleColor.DarkMagenta, RgbColor = Color.DarkMagenta, HtmlColor = "#8B008B"},
    };


    public static PixelColor ConsoleColorToPixelColor(ConsoleColor consoleColor)
    {
        var color = _consoleColors.FirstOrDefault(c => c.ConsoleColor == consoleColor);
        return color == null ? new PixelColor{ R = 0, G = 0, B = 0} : new PixelColor{ R = color.RgbColor.R, G = color.RgbColor.G, B = color.RgbColor.B}; 
    }

    public static PixelColor FromLedColor(PixelColor color, double brightness)
    {
        var r = (int) (color.R  *  100/brightness); 
        var g = (int) (color.G *  100/brightness); 
        var b = (int) (color.B *  100/brightness); 
        return new PixelColor{ R = r, G = g, B = b}; 
    }

    public static PixelColor ToLedColor(PixelColor color, double brightness)
    {
        var r = (int) (color.R  *  brightness/100); 
        var g = (int) (color.G *  brightness/100); 
        var b = (int) (color.B *  brightness/100); 
        return new PixelColor{ R = r, G = g, B = b}; 
    }


    public static ColorHelper ToConsoleColor(Color color)
    {
        var hue = ColorToHue(color);
        var mappedValue = hue.Map(0, 360, 0, _consoleColors.Length);
        Console.WriteLine(hue.ToString() + " = " + mappedValue.ToString());
        return _consoleColors[mappedValue];
    }


    public static ConsoleColor[] ConsoleHues => _consoleColors.Select(c => c.ConsoleColor).ToArray(); 
        
    
    // public static Color[] GenerateGradient(Color startColor, Color endColor, int gradientCount)
    // {
    //     Color[] gradient = new Color[gradientCount];
    //     for (int i = 0; i < gradientCount; i++)
    //     {
    //         double ratio = (double)i / (gradientCount - 1);
    //         int r = (int)(startColor.R + (endColor.R - startColor.R) * ratio);
    //         int g = (int)(startColor.G + (endColor.G - startColor.G) * ratio);
    //         int b = (int)(startColor.B + (endColor.B - startColor.B) * ratio);
    //         gradient[i] = Color.FromArgb(r, g, b);
    //     }

    //     return gradient;
    // }

    public static PixelColor[] GenerateGradient(PixelColor startColor, PixelColor endColor, int gradientCount)
    {
        PixelColor[] gradient = new PixelColor[gradientCount];
        for (int i = 0; i < gradientCount; i++)
        {
            double ratio = (double)i / (gradientCount - 1);
            int r = (int)(startColor.R + (endColor.R - startColor.R) * ratio);
            int g = (int)(startColor.G + (endColor.G - startColor.G) * ratio);
            int b = (int)(startColor.B + (endColor.B - startColor.B) * ratio);
            gradient[i] = new PixelColor{ R = r, G = g, B = b}; 
        }

        return gradient;
    }


    private static int ColorToHue(Color color)
    {
        double rf = color.R / 255.0;
        double gf = color.G / 255.0;
        double bf = color.B / 255.0;

        double max = Math.Max(rf, Math.Max(gf, bf));
        double min = Math.Min(rf, Math.Min(gf, bf));
        double delta = max - min;

        double hue = 0;

        if (delta == 0)
        {
            hue = 0; // When max equals min
        }
        else if (max == rf)
        {
            hue = ((gf - bf) / delta) % 6;
        }
        else if (max == gf)
        {
            hue = ((bf - rf) / delta) + 2;
        }
        else if (max == bf)
        {
            hue = ((rf - gf) / delta) + 4;
        }

        hue *= 60;

        if (hue < 0)
        {
            hue += 360;
        }

        return (int) hue;
    }



    // public static double Map(double value, double fromMin, double fromMax, double toMin, double toMax)
    // {
    //     return toMin + (value - fromMin) * (toMax - toMin) / (fromMax - fromMin);
    // }   

    // public static int Map(int value, int fromMin, int fromMax, int toMin, int toMax)
    // {
    //     return toMin + (value - fromMin) * (toMax - toMin) / (fromMax - fromMin);
    // }

} 