using System.Drawing;

namespace RpiSpectrumAnalyzer;

class ConsoleDisplay : DisplayBase
{

    // private ConsoleColor[] _consoleHues = 
    // {
    //     ConsoleColor.DarkRed,  
    //     ConsoleColor.Red, 
    //     ConsoleColor.DarkYellow, 
    //     ConsoleColor.Yellow, 
    //     ConsoleColor.Green, 
    //     ConsoleColor.DarkGreen,
    //     ConsoleColor.Cyan,
    //     ConsoleColor.DarkCyan,
    //     ConsoleColor.Blue,
    //     ConsoleColor.DarkBlue,
    //     ConsoleColor.Magenta,
    //     ConsoleColor.DarkMagenta
    // };

    private ConsoleColor _peakColor;
    private ConsoleColor[][]? _pixelColors; //private ConsoleColor[,]? _pixelColors;
    private ColPeak[] _colPeaks;
    private double[] _curLevels;
    
    public ConsoleDisplay(int rows, int cols)
    {
        _rows = rows;
        _cols = cols;
        _curLevels = new double[_cols];
        _pixelColors = new ConsoleColor[_cols][]; //new ConsoleColor[_cols, _rows];
        _colPeaks = new ColPeak[_cols];
        
        _peakWaitMin = 1;
        _peakWait = 2000; //default, configurable via API call
        _peakWaitMax = 5000;

        _peakWaitCountDownMin = 1;
        _peakWaitCountDown = 20; //default, configurable via API call
        _peakWaitCountDownMax = 1000;

        _transitionSpeedMin = 1;
        _transitionSpeed = 1; //default, configurable via API call
        _transitionSpeedMax = _rows/2;

        _peakColor = ConsoleColor.DarkRed; //default, configurable via API call
        
        Clear();
        SetupDefaultColors();

    }

    public override int Rows => _rows;
    public override int Cols => _cols;

    public override DisplayConfiguration GetConfiguration()
    {
        return new DisplayConfiguration
        {
            DisplayType = DisplayType.LED,
            Rows = _rows,
            Cols = _cols,
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

            
            PeakColor = ColorHelper.ConsoleColorToPixelColor(_peakColor),
            PixelColors = _pixelColors?.Select(c => c.Select(p => ColorHelper.ConsoleColorToPixelColor(p)).ToArray()).ToArray(), //convert to array of arrays
            
        };
    }

    public override void UpdateConfiguration(DisplayConfiguration? config)
    {
        if(config?.DisplayType != DisplayType.CONSOLE)
            return;
        
        // var consoleDisplayConfig = config as ConsoleDisplayConfiguration;
        // if (consoleDisplayConfig == null)
        //     return;

        // _peakWait = consoleDisplayConfig.PeakWait > 0 ? consoleDisplayConfig.PeakWait : _peakWait;
        // _peakWaitCountDown = consoleDisplayConfig.PeakWaitCountDown > 0 ? consoleDisplayConfig.PeakWaitCountDown : _peakWaitCountDown;
        // _transitionSpeed = consoleDisplayConfig.TransitionSpeed > 0 ? consoleDisplayConfig.TransitionSpeed : _transitionSpeed;
        // _amplificationFactor = consoleDisplayConfig.AmplificationFactor > 0 ? consoleDisplayConfig.AmplificationFactor : _amplificationFactor;
        // _showPeaks = consoleDisplayConfig.ShowPeaks;
        // _showPeaksWhenSilent = consoleDisplayConfig.ShowPeaksWhenSilent;

    }


    public override void Clear()
    {
        Console.BackgroundColor = ConsoleColor.Black;
        Console.ForegroundColor = ConsoleColor.Black;
        Console.Clear();
        
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
        string levelChars = "===";

        for (int x = 0; x < targetLevels.Length; x++)
        {
            int xPos = x * levelChars.Length * 2; //space columns
            DisplayLabels(xPos, _rows+1, targetLevels[x].Band);

            if(targetLevels[x].Level > _curLevels[x])
            {
                _curLevels[x] = targetLevels[x].Level; //bring up to target level gradually
            }else if (targetLevels[x].Level < _curLevels[x])
            {
                _curLevels[x] = Math.Max(_curLevels[x] - _transitionSpeed, targetLevels[x].Level);  //bring down gradually

            }


            for (int y = 0; y < _rows; y++)
            {
                if(y < _curLevels[x] ){
                    Console.ForegroundColor = _pixelColors[x][y]; //_pixelColors[x, y];
                }else{
                    Console.ForegroundColor = ConsoleColor.Black;
                }         

                if(_showPeaks){
                    var peakRow = GetPeakRow(x, (int)_curLevels[x]);

                    var targetPeakRow = 1;
                    if(_showPeaksWhenSilent){
                        targetPeakRow = 0;
                    }

                    if(y == peakRow && peakRow >= targetPeakRow){ //to turn off peaks at the bottom when there is silence, change this to peakRow > 0
                        Console.ForegroundColor = _peakColor;
                    }
                }
   

                Console.SetCursorPosition(xPos,  _rows - y); 
                Console.Write(levelChars);
            }            

        }
    }


    private void DisplayLabels(int x, int y, int value){
        Console.ForegroundColor = ConsoleColor.White;
        Console.SetCursorPosition(x,  y); 

        string label = value.ToString();
        if(value >= 1000){
            label = $"{Convert.ToInt32(value / 1000)}K"; 
        }

        Console.Write(label);
    }


    private void SetupDefaultColors()
    {
        for (int x = 0; x < _cols; x++)
        {
            _pixelColors[x] = new ConsoleColor[_rows]; //_pixelColors[x, y] = new ConsoleColor[_rows];
            for (int y = 0; y < _rows; y++)
            {
                int hueIndex = y.Map(0, _rows-1, 5, 0); //map row numbers to the color range green to red
                _pixelColors[x][y] = ColorHelper.ConsoleHues[hueIndex]; //_pixelColors[x, y] =  _consoleHues[hueIndex];
            }            
        }
    }

    private int GetPeakRow(int col, int value)
    {
        int peakRow = 0;
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
        
        if(_colPeaks[col].Row > 0){
            peakRow = _colPeaks[col].Row;
        }else{
            peakRow = 0;
        }
        
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

        return peakRow;

    }

}