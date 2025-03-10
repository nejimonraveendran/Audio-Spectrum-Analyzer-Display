#ifndef Analyzer_h
#define Analyzer_h

#include "Common.h"

// #include <Arduino.h>
// #include <driver/i2s.h>
// #include <driver/adc.h>
// #include <arduinoFFT.h>

// #define twoPi 6.28318531

class Analyzer{
    private:
        uint8_t _noOfBands; //number of bands to divide the frequency spectrum into
        uint32_t _samplingFrequency; //audio sampling frequency  
        int _sampleSize; //number of samples to take
        int _noiseThreshold; //noise cutoff (mostly towards upper bands).
        uint16_t _offset; //offset for the ADC
        double* _vReal; //array to hold real part of the FFT complex numbers
        double* _vImag; //array to hold imaginary part of the FFT complex numbers
        float* _freqBands; //array to hold the frequency band levels
        int16_t* _samples; //array to hold the audio samples from I2S ADC
        unsigned short* _bandTable; //array to hold band frequencies in Hz
        arduinoFFT* _fft; //Arduino FFT library object
        unsigned short getCurrentFreqBin(unsigned short binIndex); //returns the frequency for a given bin index.
        void putIntoFrequencyBands(); //puts the FFT results into frequency bands

    public:
        Analyzer(uint8_t numberOfBands, unsigned short* bandTable); //constructor
        bool setupAdc(); //setup the ADC and I2S for audio sampling
        void readAudioSamples(); //read audio samples from the ADC through I2S 
        void convertToBands(float* freqBins);  //convert the audio samples to frequency bands

};

#endif