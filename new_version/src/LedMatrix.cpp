#include "LedMatrix.h"
#include "Common.h"

#define LED_PIN 18 //GPIO pin the LED strip is connected to (no way in FastLED to make it a variable).
CRGB* LedMatrix::_LEDs = nullptr;

LedMatrix::LedMatrix(unsigned short numberOfRows, unsigned short numberOfCols){
    Serial.println("LED Matrix initializing...");
    
    this->_noOfRows = numberOfRows;
    this->_noOfCols = numberOfCols;
    this->_noOfLEDs = this->_noOfRows * this->_noOfCols;
    this->_maxCurrentDraw = 5000; 
    this->_brightness = 20; //default brightness value.  Can be changed via web portal. 
    
    this->_colPeaks = new ColPeak[this->_noOfCols] {}; 
    this->_peakColor = CRGB(255, 255, 255); //default peak LED color.  Can be changed via web portal.
    this->_maxPeakFallingWait = 1500; //default value.  Can be changed via web portal.
    this->_peakFallingIntervalIncrement = 25; //dfault value. Can be changed via web portal.

    this->_ledColors = new CRGB[this->_noOfLEDs]; //array for storing the color of the LEDs.  Can be changed via web portal.
    this->_LEDs = new CRGB[this->_noOfLEDs]; //FastLED array (should be static)
    this->setupLedDefaultColors(); //sets up the default colors for the LEDs
    
    //initialize FastLED
    FastLED.addLeds<WS2812B, LED_PIN, GRB>(_LEDs, this->_noOfLEDs).setCorrection(TypicalSMD5050);
    FastLED.setMaxPowerInMilliWatts(this->_maxCurrentDraw);
    FastLED.setBrightness(this->_brightness);
    delay(50);
    this->clearMatrix();
      
}

void LedMatrix::clearMatrix(){
  FastLED.clear();
  FastLED.show();  
}


void LedMatrix::updateLEDs(){
    FastLED.setBrightness(this->_brightness);
    FastLED.show();  
}

void LedMatrix::doDemo(CRGB color){
    for(unsigned short i=0; i< this->_noOfLEDs; i++){
        this->_LEDs[i] = color;
        this->updateLEDs();
        delay(10);
    }        
    
    delay(50);
    this->clearMatrix();
}

unsigned short LedMatrix::getNoOfRows(){
    return this->_noOfRows;
}

unsigned short LedMatrix::getNoOfCols(){
    return this->_noOfCols;
}

unsigned short LedMatrix::getMaxPeakFallingWait(){
  return this->_maxPeakFallingWait;
}

unsigned short LedMatrix::getPeakFallingIntervalIncrement(){
  return this->_peakFallingIntervalIncrement;
}

CRGB LedMatrix::getPeakColor(){
  return this->_peakColor;
}

unsigned short LedMatrix::getBrightness(){
  return this->_brightness;
}

CRGB* LedMatrix::getLEDColors(){
  return this->_ledColors;
}

void LedMatrix::setLEDColors(CRGB* value){
  unsigned short noOfLeds = this->getNoOfCols() * this->getNoOfRows();

  for (unsigned short i = 0; i < noOfLeds; i++)
  {
    this->_ledColors[i].r = value[i].r;
    this->_ledColors[i].g = value[i].g;
    this->_ledColors[i].b = value[i].b;
  }
    
}

void LedMatrix::setPixelColor(unsigned short index, CRGB value){
  this->_ledColors[index].r = value.r;
  this->_ledColors[index].g = value.g;
  this->_ledColors[index].b = value.b;
}


void LedMatrix::setBrightness(unsigned short value){
  this->_brightness = value;
}

void LedMatrix::setPeakColor(CRGB value){
  this->_peakColor = value;
}

void LedMatrix::setMaxPeakFallingWait(unsigned short value){
  this->_maxPeakFallingWait = value;
}

void LedMatrix::setPeakFallingIntervalIncrement(unsigned short value){
  this->_peakFallingIntervalIncrement = value;
}

void LedMatrix::setLEDColPeak(unsigned short col, unsigned short value){
    unsigned short topRowIndex = this->_noOfRows - 1; 

    if(value > this->_colPeaks[col].row){//set new peaks if current value is greater than previously stored peak.
      this->_colPeaks[col].row = value; //set peak at one above the actual value.

        if(this->_colPeaks[col].row > topRowIndex){ //dont let it overflow the top row index
          this->_colPeaks[col].row = topRowIndex;
        }

        this->_colPeaks[col].curMillis = this->_colPeaks[col].prevMillis = millis(); //reset the time interval for the peak falling.
        this->_colPeaks[col].curWait = this->_maxPeakFallingWait; //reset peak falling interval to max value.
    }

    //set LEDs
    if(this->_colPeaks[col].row > 0) //if value (x) not at bottom, set peak color of the row
    {
        _LEDs[ xyToIndex(col, this->_colPeaks[col].row)]  = this->_peakColor;
    }
    else {//for all other rows, set to black. 
        _LEDs[ xyToIndex(col, this->_colPeaks[col].row)]  = CRGB::Black;
    }
    
    //logic for the peaks to fall down
    this->_colPeaks[col].curMillis = millis(); //update current time
    
    if (this->_colPeaks[col].curMillis - this->_colPeaks[col].prevMillis >= this->_colPeaks[col].curWait){
      if(this->_colPeaks[col].row > 0){
        this->_colPeaks[col].row -= 1; //deduct one row (creates fall down effect)
        this->_colPeaks[col].prevMillis = this->_colPeaks[col].curMillis;
      }
    }

    this->_colPeaks[col].curWait -= this->_peakFallingIntervalIncrement;

    if(this->_colPeaks[col].curWait < this->_peakFallingIntervalIncrement){
      this->_colPeaks[col].curWait = this->_peakFallingIntervalIncrement;
    }

  }
  

  void LedMatrix::setLEDColumn(unsigned short col, unsigned short value){
    unsigned short topRowIndex = this->_noOfRows - 1;
  
    for (unsigned short y=0; y<=topRowIndex; y++) //iterate all rows in the column (vertically from bottom to top)
    {    
      if(y > value-1) 
      {
        _LEDs[ xyToIndex(col, y)] = CRGB::Black;  
      }else
      {
        _LEDs[ xyToIndex(col, y)]  = this->_ledColors[xyToIndex(col, y)]; 
      }
    }
  }



//PRIVATE MEMBER DEFINITIONS
void LedMatrix::setupLedDefaultColors(){
    for (unsigned short x=0; x < this->_noOfCols; x++) {
      for (unsigned short y=0; y < this->_noOfRows; y++) {
            uint16_t hue = map(y, 0, this->_noOfRows, 100, 1);
            CRGB clr = CHSV(hue, 255, 255);
            this->_ledColors[xyToIndex(x, y)]  = clr; 
      }
    }
  }
  
  //converts x,y coordinates to LED index
  unsigned short LedMatrix::xyToIndex(unsigned short x, unsigned short y){
      return (x * this->_noOfRows) + y;
  }
