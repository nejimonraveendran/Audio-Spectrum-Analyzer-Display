namespace RpiSpectrumAnalyzer;

internal static class ColorConversion
{   
    private static (ConsoleColor ConsoleColor, PixelColor PixelColor) [] _consoleHueMap = 
    {
        (ConsoleColor.DarkRed, new PixelColor{ R = 128, G = 0, B = 0 }),
        (ConsoleColor.Red, new PixelColor{ R = 255, G = 0, B = 0 }),
        (ConsoleColor.DarkYellow, new PixelColor{ R = 128, G = 128, B = 0 }),
        (ConsoleColor.Yellow, new PixelColor{ R = 255, G = 255, B = 0 }),
        (ConsoleColor.Green, new PixelColor{ R = 0, G = 255, B = 0 }),
        (ConsoleColor.DarkGreen, new PixelColor{ R = 0, G = 128, B = 0 }),
        (ConsoleColor.Cyan, new PixelColor{ R = 0, G = 255, B = 255 }),
        (ConsoleColor.DarkCyan, new PixelColor{ R = 0, G = 128, B = 128 }),
        (ConsoleColor.Blue, new PixelColor{ R = 0, G = 0, B = 255 }),
        (ConsoleColor.DarkBlue, new PixelColor{ R = 0, G = 0, B = 128 }),
        (ConsoleColor.Magenta, new PixelColor{ R = 255, G = 0, B = 255 }),
        (ConsoleColor.DarkMagenta, new PixelColor{ R = 128, G = 0, B = 128 }),

    };

    public static PixelColor ConsoleColorToPixelColor(ConsoleColor consoleColor)
    {
        return _consoleHueMap.First(c => c.ConsoleColor == consoleColor).PixelColor;
    }

    public static ConsoleColor PixelColorToConsoleColor (PixelColor color)
    {
        var hsl = PixelColorToHsl(color);

        if(hsl.L > 0.75)
            return ConsoleColor.White;

        var max = _consoleHueMap.Length - 1;
        var mappedValue = hsl.H.Map(0, 300, 0, max);
        mappedValue = mappedValue > max ? max : mappedValue;
        return _consoleHueMap[mappedValue].ConsoleColor;
    }

    public static PixelColor PixelColorWithBrightness(PixelColor color, double brightness)
    {
        var r = (int) (color.R  *  brightness/100); 
        var g = (int) (color.G *  brightness/100); 
        var b = (int) (color.B *  brightness/100); 
        return new PixelColor{ R = r, G = g, B = b}; 
    }

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

    private static (int H, double S, double L) PixelColorToHsl(PixelColor pixelColor)
    {
        double rNorm = pixelColor.R / 255.0;
        double gNorm = pixelColor.G / 255.0;
        double bNorm = pixelColor.B / 255.0;

        double max = Math.Max(rNorm, Math.Max(gNorm, bNorm));
        double min = Math.Min(rNorm, Math.Min(gNorm, bNorm));
        double delta = max - min;

        double h = 0;
        double s = 0;
        double l = (max + min) / 2.0;

        if (delta != 0)
        {
            s = l < 0.5 ? delta / (max + min) : delta / (2.0 - max - min);

            if (max == rNorm)
                h = ((gNorm - bNorm) / delta) + (gNorm < bNorm ? 6 : 0);
            else if (max == gNorm)
                h = ((bNorm - rNorm) / delta) + 2;
            else
                h = ((rNorm - gNorm) / delta) + 4;

            h /= 6.0;
        }

        return ((int) (h * 360.0), s, l);
    }

} 