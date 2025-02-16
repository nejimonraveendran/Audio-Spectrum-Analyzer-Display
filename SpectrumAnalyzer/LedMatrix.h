#include "crgb.h"
#include "Framebuffer_GFX.h"
#ifndef LedMatrix_h
#define LedMatrix_h

//includes
#include "esp32-hal.h"
#include <stdint.h>
#include "Common.h"

//defines and constants
#define LED_PIN  18 //
#define COLOR_ORDER GRB
#define CHIPSET WS2812B

CRGB _ledsPlusSafetyPixel[ G_NUM_LEDS + 1];
CRGB* const LEDs( _ledsPlusSafetyPixel + 1);


struct ColPeak{
  uint8_t col;
  uint8_t row;
  uint16_t curWait;
  uint64_t curMillis;
  uint64_t prevMillis;
};


class LedMatrix {
  private:
    uint8_t _brightness = 20; //config
    uint16_t _maxCurrentDraw = 5000; //config
    ColPeak _colPeaks[G_NUM_BANDS]; //for storing the current position of peak pixels
    uint16_t _curPeakFallingInterval = g_maxPeakFallingWait; //determines peak fall down interval
    uint64_t _curMillis = 0; //for column animation speed tracking
    uint64_t _prevMillis = 0; //for column animation speed tracking
    

    void setupLedDefaultColors(){    
      for (uint16_t x=0; x < G_NUM_BANDS; x++) {
        for (uint16_t y=0; y < G_NUM_LEVELS; y++) {
              uint16_t hue = map(y, 0, G_NUM_LEVELS, 100, 1);
              CRGB clr = CHSV(hue, 255, 255);
              g_ledColors[xyToIndex(x, y)]  = clr; 
        }
      }
    }

    uint16_t xyToIndex( uint8_t x, uint8_t y){
      uint16_t i;  
      i = (x * G_NUM_LEVELS) + y;
      return i;
    }

  public:
    LedMatrix(){
      setupLedDefaultColors();

      FastLED.addLeds<CHIPSET, LED_PIN, COLOR_ORDER>(LEDs, G_NUM_LEDS).setCorrection(TypicalSMD5050);
      FastLED.setMaxPowerInMilliWatts(_maxCurrentDraw);
      // FastLED.setMaxRefreshRate(G_NUM_LEDS * 4);
      FastLED.setBrightness( _brightness );
      FastLED.clear();  
    }

    void updateLEDs(){
      FastLED.show();
    }

    void setText(uint16_t cursorX, String text){
      //CRGB ledColors[xyToIndex(3, 10)];
      
      uint16_t x = 1;
      
      //2
      LEDs[xyToIndex(x+2, 0)] = CRGB::Red;
      LEDs[xyToIndex(x+1, 0)] = CRGB::Red;
      LEDs[xyToIndex(x+0, 0)] = CRGB::Red;
      LEDs[xyToIndex(x+0, 1)] = CRGB::Red;
      LEDs[xyToIndex(x+0, 2)] = CRGB::Red;
      LEDs[xyToIndex(x+0, 3)] = CRGB::Red;
      LEDs[xyToIndex(x+0, 4)] = CRGB::Red;
      LEDs[xyToIndex(x+0, 5)] = CRGB::Red;
      LEDs[xyToIndex(x+1, 5)] = CRGB::Red;
      LEDs[xyToIndex(x+2, 5)] = CRGB::Red;
      LEDs[xyToIndex(x+2, 6)] = CRGB::Red;
      LEDs[xyToIndex(x+2, 7)] = CRGB::Red;
      LEDs[xyToIndex(x+2, 8)] = CRGB::Red;
      LEDs[xyToIndex(x+2, 9)] = CRGB::Red;
      LEDs[xyToIndex(x+1, 9)] = CRGB::Red;
      LEDs[xyToIndex(x+0, 9)] = CRGB::Red;
      

      // for(uint16_t x=cursorX; x<cursorX+2; x++){
      //   for(uint16_t y=0; y<G_NUM_LEVELS; y++){

      //   }
      // }
      
    }

    void setLEDColPeak(uint8_t col, uint8_t value){ //col = x, value = y
      uint8_t topRowIndex = G_NUM_LEVELS - 1; //17

      if(value > _colPeaks[col].row){//set new peaks if current value is greater than previously stored peak.
        _colPeaks[col].row = value; //set peak at one above the actual value.

        if(_colPeaks[col].row > topRowIndex){ //dont let it overflow the top row index
          _colPeaks[col].row = topRowIndex;
        }

        _colPeaks[col].curWait = g_maxPeakFallingWait; //reset peak falling interval to max value.

      }

      // //set LEDs
      if(_colPeaks[col].row > 0) //if value (x) not at bottom, set peak color of the row
      {
        LEDs[ xyToIndex(col, _colPeaks[col].row)]  = g_peakColor;
      }
      else {//for all other rows, set to black. 
        LEDs[ xyToIndex(col, _colPeaks[col].row)]  = CRGB::Black;
      }
      
      //logic for the peaks to fall down
      for (uint8_t x=0; x < G_NUM_BANDS; x++){
        _colPeaks[x].curMillis = millis();
        if (_colPeaks[x].curMillis - _colPeaks[x].prevMillis >= _colPeaks[x].curWait){
          if(_colPeaks[x].row > 0){
            _colPeaks[x].row = _colPeaks[x].row - 1; //deduct one row (creates fall down effect)
          }

          _colPeaks[x].prevMillis = _colPeaks[x].curMillis;
        }

        _colPeaks[x].curWait = _colPeaks[x].curWait - g_peakFallingIntervalIncrement;

        if(_colPeaks[x].curWait < g_peakFallingIntervalIncrement || _colPeaks[x].curWait > g_maxPeakFallingWait){
          _colPeaks[x].curWait = g_peakFallingIntervalIncrement;
        }
      }
    }


    void setLEDColumn(uint8_t col, uint8_t value) {
      uint16_t topRowIndex = G_NUM_LEVELS - 1;

      for (uint8_t y=0; y<=topRowIndex; y++) //iterate all rows in the column (vertically from bottom to top)
      {    
        if(y > value-1) 
        {
          LEDs[ xyToIndex(col, y)] = CRGB::Black;  
        }else
        {
          LEDs[ xyToIndex(col, y)]  = g_ledColors[xyToIndex(col, y)]; 
        }
      }

    }

};

#endif

