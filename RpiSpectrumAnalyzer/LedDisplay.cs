namespace RpiSpectrumAnalyzer;

using System.Device.Spi;
using System.Drawing;
using Iot.Device.Ws28xx;

class LedDisplay : DisplayBase
{   
    private Ws2812b _ledMatrix;
    private double[] _curLevels;
    private ColPeak[] _colPeaks;
    private int _brightnessMin;
    private int _brightness;
    private int _brightnessMax;
    
    public LedDisplay(int rows, int cols)
    {
        _rows = rows;
        _cols = cols;
        _curLevels = new double[_rows];
        _pixelColors = new PixelColor[_cols][]; 
        _colPeaks = new ColPeak[_cols];

        _transitionSpeedMin = 1;
        _transitionSpeed = 1.5; //default, configurable via API call
        _transitionSpeedMax = _rows/2;

        _peakWaitMin = 1;
        _peakWait = 500; //default, configurable via API call
        _peakWaitMax = 5000;

        _peakWaitCountDownMin = 1;
        _peakWaitCountDown = 100; //default, configurable via API call
        _peakWaitCountDownMax = 1000;

        _brightnessMin = 1;
        _brightness = 2; //default, configurable via API call
        _brightnessMax = 50;

        _peakColor = new PixelColor{R = 255, G = 0, B = 0};  //default, configurable via API call

        _gradientStartColor = new PixelColor{R = 100, G = 255, B = 0};
        _gradientEndColor = new PixelColor{R = 255, G = 100, B = 0};        
        
        var spiSettings =  new SpiConnectionSettings(0, 0)
        {
            ClockFrequency = 2_400_000,
            Mode = SpiMode.Mode0,
            DataBitLength = 8, 
        };

        using SpiDevice spi = SpiDevice.Create(spiSettings);
        _ledMatrix = new Ws2812b(spi, _cols, _rows);

        SetupDefaultColors();
        Clear();

    }

    public override bool IsBrightnessSupported => true;

    public override DisplayConfiguration GetConfiguration()
    {
        return new DisplayConfiguration
        {
            DisplayType = DisplayType.LED,
            Rows = _rows,
            Cols = _cols,
            BrightnessMin = _brightnessMin,
            Brightness = _brightness,
            BrightnessMax = _brightnessMax,
            PeakWaitMin = _peakWaitMin,
            PeakWait = _peakWait,
            PeakWaitMax = _peakWaitMax,
            PeakWaitCountDownMin = _peakWaitCountDownMin,
            PeakWaitCountDown = _peakWaitCountDown,
            PeakWaitCountDownMax = _peakWaitCountDownMax,
            TransitionSpeedMin = _transitionSpeedMin,
            TransitionSpeed = _transitionSpeed,
            TransitionSpeedMax = _transitionSpeedMax,
            AmplificationFactorMin = _amplificationFactorMin,
            AmplificationFactor = _amplificationFactor,
            AmplificationFactorMax = _amplificationFactorMax,
            ShowPeaks = _showPeaks,
            ShowPeaksWhenSilent = _showPeaksWhenSilent,
            IsBrightnessSupported = IsBrightnessSupported,
            PeakColor = _peakColor,
            PixelColors = _pixelColors,
            GradientStartColor = _gradientStartColor,
            GradientEndColor = _gradientEndColor

        };
    }

    public override void UpdateConfiguration(DisplayConfiguration? config)
    {
        _peakWait = config?.PeakWait > 0 ? config.PeakWait : _peakWait;
        _peakWaitCountDown = config?.PeakWaitCountDown > 0 ? config.PeakWaitCountDown : _peakWaitCountDown;
        _transitionSpeed = config?.TransitionSpeed > 0 ? config.TransitionSpeed : _transitionSpeed;
        _brightness = config?.Brightness > 0 ? config.Brightness : _brightness;
        _amplificationFactor = config?.AmplificationFactor > 0 ? config.AmplificationFactor : _amplificationFactor;
        _showPeaks = config?.ShowPeaks == null ? false : config.ShowPeaks;
        _showPeaksWhenSilent = config?.ShowPeaksWhenSilent == null ? false : config.ShowPeaksWhenSilent;
        _peakColor = config?.PeakColor != null ? config.PeakColor : _peakColor;
        _pixelColors = config?.PixelColors != null ? config.PixelColors : _pixelColors;
        _gradientStartColor = config?.GradientStartColor != null ? config.GradientStartColor : _gradientStartColor;
        _gradientEndColor = config?.GradientEndColor != null ? config.GradientEndColor : _gradientEndColor;
        
    }

    public override void Clear()
    {
        _ledMatrix.Image.Clear();
        _ledMatrix.Update();
    }

    public override void DisplayAsLevels(BandInfo[] bands)
    {
        var levels = bands.Amplify(_amplificationFactor)
                        .Normalize()
                        .ToLevels(_rows);

        DisplayLevels(levels);
    }

    private void DisplayLevels(LevelInfo[] targetLevels)
    {
        for (int x = 0; x < _cols; x++)
        {
            if(targetLevels[x].Level > _curLevels[x])
            {
                _curLevels[x] = targetLevels[x].Level;

            }else if (targetLevels[x].Level < _curLevels[x])
            {
                _curLevels[x] = Math.Max(_curLevels[x] - _transitionSpeed, targetLevels[x].Level);  //bring down gradually
            }

            for (int y = 0; y < _rows; y++)
            {
                if(y < _curLevels[x] ){
                    var pixelColor = ColorConversion.PixelColorWithBrightness(_pixelColors[x][y], _brightness);
                    _ledMatrix.Image.SetPixel(y, x, Color.FromArgb(pixelColor.R, pixelColor.G, pixelColor.B)); //x, y reversed because of the bottom-to-top LED strip wiring in my case
                }else{
                      _ledMatrix.Image.SetPixel(y, x, Color.FromArgb(0, 0, 0));
                }         
            }

            if(_showPeaks)
            {
                SetColumnPeaks(x, (int)_curLevels[x]);
            }

            _ledMatrix.Update();
        }
                
    }

    private void SetupDefaultColors()
    {
        var gradient = ColorConversion.GenerateGradient(_gradientStartColor, _gradientEndColor, _rows); 

        for (int x = 0; x < _cols; x++)
        {
            _pixelColors[x] = new PixelColor[_rows]; 
            for (int y = 0; y < _rows; y++)
            {
                _pixelColors[x][y] = gradient[y]; //1 = full saturation, 
            }            
        }
    }

    private void SetColumnPeaks(int col, int value)
    {
        int topRowIndex = _rows - 1;

        if(value > _colPeaks[col].Row)  //set new peaks if current value is greater than previously stored peak.
        {
            _colPeaks[col].Row = value; //set peak at one above the actual value.
            if(_colPeaks[col].Row > topRowIndex)  //dont let it overflow the top row index
            { 
                _colPeaks[col].Row = topRowIndex;
            }

            _colPeaks[col].CurMilSecs = _colPeaks[col].PrevMilSecs = Environment.TickCount64;
            _colPeaks[col].CurWait = _peakWait;

        }

        //set LEDs
        var targetPeakRow = 1;
        if(_showPeaksWhenSilent){
            targetPeakRow = 0;
        }

        if(_colPeaks[col].Row >= targetPeakRow) //if value (x) not at bottom, set peak color of the row
        {
            var peakColor = ColorConversion.PixelColorWithBrightness(_peakColor, _brightness);
            _ledMatrix.Image.SetPixel(_colPeaks[col].Row, col, Color.FromArgb(peakColor.R, peakColor.G, peakColor.B)); //x, y reversed because of the LED strip wiring in my case
        }
        else //otherwise set to black. 
        { 
            _ledMatrix.Image.SetPixel(_colPeaks[col].Row, col, Color.FromArgb(0, 0, 0));
        }

        //logic for the peaks to fall down
        _colPeaks[col].CurMilSecs = Environment.TickCount64;

        if (_colPeaks[col].CurMilSecs - _colPeaks[col].PrevMilSecs >= _colPeaks[col].CurWait)
        {
            if(_colPeaks[col].Row > 0)
            {
                _colPeaks[col].Row -= 1; //deduct one row (creates fall down effect)
                _colPeaks[col].PrevMilSecs = _colPeaks[col].CurMilSecs;

            }
        
        }

        _colPeaks[col].CurWait -=  _peakWaitCountDown;

        if(_colPeaks[col].CurWait < _peakWaitCountDown)
        {
            _colPeaks[col].CurWait = _peakWaitCountDown;
            
        }

    }

}