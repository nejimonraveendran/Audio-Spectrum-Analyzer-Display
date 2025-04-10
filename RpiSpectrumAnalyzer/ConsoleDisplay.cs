using System.Drawing;

namespace RpiSpectrumAnalyzer;

class ConsoleDisplay : IDisplay
{
    int _rows, _cols;
    double _transitionSpeed = 1;
    int _peakWait;
    int _peakWaitCountDown;
    ConsoleColor _peakColor;
    double[] _curLevels;
    ConsoleColor[,] _pixelColors;
    ConsoleColor[] _consoleHues = 
    {
        ConsoleColor.DarkRed,  
        ConsoleColor.Red, 
        ConsoleColor.DarkYellow, 
        ConsoleColor.Yellow, 
        ConsoleColor.Green, 
        ConsoleColor.DarkGreen,
        ConsoleColor.Cyan,
        ConsoleColor.DarkCyan,
        ConsoleColor.Blue,
        ConsoleColor.DarkBlue,
        ConsoleColor.Magenta,
        ConsoleColor.DarkMagenta
    };

    ColPeak[] _colPeaks;


    public ConsoleDisplay(int rows, int cols)
    {
        _rows = rows;
        _cols = cols;
        _curLevels = new double[_cols];
        _pixelColors = new ConsoleColor[_cols, _rows];
        _colPeaks = new ColPeak[_cols];
        _peakWait = 2000; //config
        _peakWaitCountDown = 20; //config
        _peakColor = ConsoleColor.DarkRed; //config

        Clear();
        SetupDefaultColors();
    }

    public void Clear()
    {
        Console.BackgroundColor = ConsoleColor.Black;
        Console.ForegroundColor = ConsoleColor.Black;
        Console.Clear();
        
    }

    public void DisplayLevels(LevelInfo[] targetLevels)
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
                    Console.ForegroundColor = _pixelColors[x, y];
                }else{
                    Console.ForegroundColor = ConsoleColor.Black;
                }         

                if(!HidePeaks){
                    var peakRow = GetPeakRow(x, (int)_curLevels[x]);

                    var targetPeakRow = 1;
                    if(ShowPeaksWhenSilent){
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


    public bool HidePeaks { get; set; }
    public bool ShowPeaksWhenSilent { get; set; }

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
            for (int y = 0; y < _rows; y++)
            {
                int hueIndex = Helpers.Map(y, 0, _rows-1, 5, 0); //map row numbers to the color range green to red
                _pixelColors[x, y] =  _consoleHues[hueIndex];
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


    //  private int GetPeakRow(int col, int value)
    // {
    //     int peakRow = 0;
    //     int topRowIndex = _rows - 1;

    //     if(value > _colPeaks[col].Row)  //set new peaks if current value is greater than previously stored peak.
    //     {
    //         _colPeaks[col].Row = value; //set peak at one above the actual value.
    //         if(_colPeaks[col].Row > topRowIndex)  //dont let it overflow the top row index
    //         { 
    //             _colPeaks[col].Row = topRowIndex;
    //         }

    //         _colPeaks[col].CurWait = _maxPeakFallingWaitMilliSecs; //reset peak falling interval to max value.
            
    //     }

    //     if(_colPeaks[col].Row > 0){
    //         peakRow = _colPeaks[col].Row;
    //     }else{
    //         peakRow = 0;
    //     }
        
        

    //     //logic for the peaks to fall down
    //     for (int x=0;x<_cols; x++){
            
    //         _colPeaks[x].CurMilSecs = Environment.TickCount64;

    //         if (_colPeaks[x].CurMilSecs - _colPeaks[x].PrevMilSecs >= _colPeaks[x].CurWait)
    //         {
    //             if(_colPeaks[x].Row > 0)
    //             {
    //                 _colPeaks[x].Row = _colPeaks[x].Row - 1; //deduct one row (creates fall down effect)
    //             }

    //             _colPeaks[x].PrevMilSecs = _colPeaks[x].CurMilSecs;
    //         }

    //         _colPeaks[x].CurWait = _colPeaks[x].CurWait - _peakFallingIntervalMilliSecs;

    //         if(_colPeaks[x].CurWait < _peakFallingIntervalMilliSecs || _colPeaks[x].CurWait > _maxPeakFallingWaitMilliSecs)
    //         {
    //             _colPeaks[x].CurWait = _peakFallingIntervalMilliSecs;
    //         }
    //     }

    //     return peakRow;

    // }
    
}