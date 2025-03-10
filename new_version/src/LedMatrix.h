#ifndef LedMatrix_h
#define LedMatrix_h

#include "Common.h"

//structure for storing the state of the peak pixels
struct ColPeak{
    unsigned short col;
    unsigned short row;
    unsigned short curWait;
    unsigned long curMillis;
    unsigned long prevMillis;
};

class LedMatrix {
    private:
      unsigned short  _brightness; //LED brightness.  Can be changed via web portal.
      unsigned short  _maxCurrentDraw; //maximum LD current draw
      unsigned short _noOfRows; //number of rows in the matrix
      unsigned short _noOfCols; //number of columns in the matrix
      unsigned short _noOfLEDs; //number of LEDs in the matrix
      CRGB* _ledColors; //array for storing the color of the LEDs.  Can be changed via web portal.
      static CRGB* _LEDs; //FastLED array (should be static)
      ColPeak* _colPeaks; //array for storing the current position of peak pixels for each band
      CRGB _peakColor; //color of the peak pixels.  Can be changed via web portal.
      unsigned short _maxPeakFallingWait; //determines the max peak fall down interval.  Can be changed via web portal.
      unsigned short _peakFallingIntervalIncrement; //determines peak fall down acceleration.  Can be changed via web portal.
      void setupLedDefaultColors(); //sets up the default colors for the LEDs
      unsigned short xyToIndex(unsigned short x, unsigned short y); //converts x,y coordinates to LED index
  
    public:
      LedMatrix(unsigned short numberOfRows, unsigned short numberOfCols); //constructor
      void clearMatrix(); //clears the LED matrix
      void updateLEDs(); //updates the LED matrix
      void doDemo(CRGB color); //runs a demo on the LED matrix
      void setLEDColPeak(unsigned short col, unsigned short value); //sets the peak pixels for the column
      void setLEDColumn(unsigned short col, unsigned short value); //sets the LED column 
      unsigned short getNoOfRows(); //returns the number of rows in the matrix
      unsigned short getNoOfCols(); //returns the number of columns in the matrix
      unsigned short getBrightness(); //returns the FastLED brightness
      CRGB* getLEDColors(); //returns the LED colors
      CRGB getPeakColor(); //returns the peak color
      unsigned short getMaxPeakFallingWait(); //returns the max peak fall down interval
      unsigned short getPeakFallingIntervalIncrement(); //returns the peak fall down acceleration
      void setLEDColors(CRGB* value); //sets the LED colors
      void setPixelColor(unsigned short index, CRGB value); //sets the color of a pixel
      void setBrightness(unsigned short value); //sets the FastLED brightness
      void setPeakColor(CRGB value); //sets the peak color
      void setMaxPeakFallingWait(unsigned short value); //sets the max peak fall down interval
      void setPeakFallingIntervalIncrement(unsigned short value); //sets the peak fall down acceleration
  
};
    

#endif