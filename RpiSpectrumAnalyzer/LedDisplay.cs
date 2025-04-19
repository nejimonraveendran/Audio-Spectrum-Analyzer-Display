using System.Device.Spi;
using System.Drawing;
using Iot.Device.Ws28xx;

namespace RpiSpectrumAnalyzer;

class LedDisplay : DisplayBase
{   
    Ws2812b _ledMatrix;
    double[] _curLevels;
    Color[,]? _pixelColors;
    ColPeak[] _colPeaks;
    Color _peakColor;
    int _brightness;
    
    public LedDisplay(int rows, int cols, LedServer ledServer)
    {
        _rows = rows;
        _cols = cols;
        _ledServer = ledServer;
        _curLevels = new double[_rows];
        _pixelColors = new Color[_cols, _rows];
        _colPeaks = new ColPeak[_cols];

        _transitionSpeed = 1.5; //default, configurable via API call
        _peakWait = 500; //default, configurable via API call
        _peakWaitCountDown = 100; //default, configurable via API call
        _brightness = 5; //default, configurable via API call
        _peakColor =  Helpers.HsvToColor(0, 1, _brightness);  //default, configurable via API call
        
        var spiSettings =  new SpiConnectionSettings(0, 0)
        {
            ClockFrequency = 2_400_000,
            Mode = SpiMode.Mode0,
            DataBitLength = 8, 
        };

        using SpiDevice spi = SpiDevice.Create(spiSettings);
        _ledMatrix = new Ws2812b(spi, _cols, _rows);

        _ledServer.OnConfigChanged += (e, config) => 
        {
            if(config?.DisplayType != DisplayType.LED)
                return;
          
            _peakWait = config.PeakWait;
            _peakWaitCountDown = config.PeakWaitCountDown;
            _transitionSpeed = config.TransitionSpeed;
            _peakColor = config.PeakColor;
            _pixelColors = config.PixelColors;
            _brightness = config.Brightness;
            _amplificationFactor = config.AmplificationFactor;
            _showPeaks = config.ShowPeaks;
            _showPeaksWhenSilent = config.ShowPeaksWhenSilent;
  
        };


        SetupDefaultColors();
        Clear();

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
                     _ledMatrix.Image.SetPixel(y, x, _pixelColors[x, y]); //x, y reversed because of the LED strip wiring in my case
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
        for (int x = 0; x < _cols; x++)
        {
            for (int y = 0; y < _rows; y++)
            {
                double hue = Helpers.Map(y, 0, _rows, 120, 1); //map row numbers to the hue range green (120) to red (1)
                _pixelColors[x, y] = Helpers.HsvToColor(hue, 1, _brightness); //1 = full saturation, 
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

            // _colPeaks[col].CurWait = _maxPeakFallingWaitMilliSecs; //reset peak falling interval to max value.
        }

        //set LEDs
        var targetPeakRow = 1;
        if(_showPeaksWhenSilent){
            targetPeakRow = 0;
        }

        if(_colPeaks[col].Row >= targetPeakRow) //if value (x) not at bottom, set peak color of the row
        {
            _ledMatrix.Image.SetPixel(_colPeaks[col].Row, col, _peakColor); //x, y reversed because of the LED strip wiring in my case
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