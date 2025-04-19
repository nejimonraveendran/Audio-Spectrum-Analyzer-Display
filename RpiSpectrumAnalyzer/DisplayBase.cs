namespace RpiSpectrumAnalyzer;

abstract class DisplayBase
{
    protected int _rows, _cols;
    protected double _transitionSpeed; //default, configurable via API call
    protected int _peakWait; //default, configurable via API call
    protected int _peakWaitCountDown; //default, configurable via API call
    protected bool _showPeaks = true; //default, configurable via API call
    protected bool _showPeaksWhenSilent = true; //default, configurable via API call
    protected int _amplificationFactor = 5000; //default, configurable via API call
    public abstract int Rows { get; }
    public abstract int Cols { get; }
    public abstract void Clear();
    public abstract DisplayConfiguration? GetConfiguration();
    public abstract void UpdateConfiguration(DisplayConfiguration? config);
    public abstract void DisplayAsLevels(BandInfo[] bands);
    public virtual bool IsBrightnessSupported => false;

}