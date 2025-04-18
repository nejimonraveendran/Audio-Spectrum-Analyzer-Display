namespace RpiSpectrumAnalyzer;

interface IDisplay
{
    void Clear();
    void DisplayLevels(LevelInfo[] targetLevels);
    public bool HidePeaks { get; set; }
    public bool ShowPeaksWhenSilent { get; set; }
    public bool IsBrightnessSupported { get; }
    
}