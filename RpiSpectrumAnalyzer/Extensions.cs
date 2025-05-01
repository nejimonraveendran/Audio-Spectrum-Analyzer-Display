
namespace RpiSpectrumAnalyzer;

internal static class Extensions
{
    public static BandInfo[] Amplify(this BandInfo[] bands, int amplificationFactor)
    {
        return bands.Select(b => new BandInfo{ Band = b.Band, Magnitude = b.Magnitude * amplificationFactor }).ToArray();
    }

    public static BandInfo[] Normalize(this BandInfo[] bands)
    {
        return bands.Select(b => new BandInfo{ Band = b.Band, Magnitude = Math.Clamp(b.Magnitude, 0, 1)}).ToArray();
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
