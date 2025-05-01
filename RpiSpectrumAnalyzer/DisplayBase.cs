namespace RpiSpectrumAnalyzer;

abstract class DisplayBase
{
    protected int _rows, _cols;

    protected double _transitionSpeedMin; 
    protected double _transitionSpeed; //default, configurable via API call
    protected double _transitionSpeedMax; 
    
    protected int _peakWaitMin; 
    protected int _peakWait; //default, configurable via API call
    protected int _peakWaitMax; 
    
    protected int _peakWaitCountDownMin; 
    protected int _peakWaitCountDown; //default, configurable via API call
    protected int _peakWaitCountDownMax; 
    
    protected int _amplificationFactorMin = 1000; 
    protected int _amplificationFactor = 5000; //default, configurable via API call
    protected int _amplificationFactorMax = 10000; 
    
    protected bool _showPeaks = true; //default, configurable via API call
    protected bool _showPeaksWhenSilent = true; //default, configurable via API call

    protected PixelColor[][]? _pixelColors; 
    protected PixelColor? _peakColor;

    protected PixelColor? _gradientStartColor;
    protected PixelColor? _gradientEndColor;

    public abstract void Clear();
    public abstract DisplayConfiguration? GetConfiguration();
    public abstract void UpdateConfiguration(DisplayConfiguration? config);
    public abstract void DisplayAsLevels(BandInfo[] bands);
    public virtual bool IsBrightnessSupported => false;

}