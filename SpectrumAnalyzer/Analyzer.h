#ifndef Analyzer_h
#define Analyzer_h

//includes
#include <driver/i2s.h>
#include <driver/adc.h>
#include <arduinoFFT.h>  //v1.5.6
#include "Common.h"

//defines
#define ADC_INPUT ADC1_CHANNEL_0
#define ARRAYSIZE(a) (sizeof(a)/sizeof(a[0]))
#define GAIN_DAMPEN 2
#define SAMPLE_BLOCK 1024 

class Analyzer{
  private:
    //private member variables
    static arduinoFFT _fft;
    int _samplingFrequency = 44100; //audio sampling frequency  
    int _noiseThreshold = 1000; //noise cutoff (mostly towards upper bands).

    uint16_t _offset = (int)ADC_INPUT * 0x1000 + 0xFFF; 
    double _vReal[SAMPLE_BLOCK]; 
    double _vImag[SAMPLE_BLOCK]; 
    int16_t _samples[SAMPLE_BLOCK]; 

    //private methods
    int bucketFrequency(int iBucket){
      if (iBucket <= 1)
        return 0;
  
      int iOffset = iBucket - 2;
      return iOffset * (_samplingFrequency / 2) / (SAMPLE_BLOCK / 2);
    }

  public:
    //public methods
    void setupAdc(){
      esp_err_t err;

      // The I2S config as per the example
      const i2s_config_t i2s_config = {
        .mode = (i2s_mode_t)(I2S_MODE_MASTER | I2S_MODE_RX | I2S_MODE_ADC_BUILT_IN),
        .sample_rate = _samplingFrequency,
        .bits_per_sample = I2S_BITS_PER_SAMPLE_16BIT, // could only get it to work with 32bits
        .channel_format = I2S_CHANNEL_FMT_ONLY_LEFT, // although the SEL config should be left, it seems to transmit on right
        .communication_format = I2S_COMM_FORMAT_I2S_MSB,
        .intr_alloc_flags = ESP_INTR_FLAG_LEVEL1,     // Interrupt level 1
        .dma_buf_count = 2,                           // number of buffers
        .dma_buf_len = SAMPLE_BLOCK,                     // samples per buffer
        .use_apll = false,
        .tx_desc_auto_clear = false,
        .fixed_mclk = 0
      };

      // Configuring the I2S driver and pins.
      // This function must be called before any I2S driver read/write operations.

      err = adc1_config_channel_atten(ADC1_CHANNEL_0, ADC_ATTEN_DB_0); // adc_gpio_init(ADC_UNIT_1, ADC_CHANNEL_0); //step 1
      if (err != ESP_OK) {
        Serial.printf("Failed setting up adc channel: %d\n", err);
        while (true);
      }

      err = i2s_driver_install(I2S_NUM_0, &i2s_config,  0, NULL);  //step 2
  
      if (err != ESP_OK) {
        Serial.printf("Failed installing driver: %d\n", err);
        while (true);
      }

      err = i2s_set_adc_mode(ADC_UNIT_1, ADC1_CHANNEL_0);
      if (err != ESP_OK) {
        Serial.printf("Failed setting up adc mode: %d\n", err);
        while (true);
      }
      
      Serial.println("I2S driver installed.");

      delay(100);

      i2s_adc_enable(I2S_NUM_0);
      Serial.println("Audio input setup completed");
   
    }


    void readAudioFromInput(){
      size_t bytesRead = 0;
      int TempADC = 0;
      //ModeBut.read();

      //############ Step 1: read samples from the I2S Buffer ##################
      i2s_read(I2S_NUM_0,
              (void*)_samples,
              sizeof(_samples),
              &bytesRead,   // workaround This is the actual buffer size last half will be empty but why?
              portMAX_DELAY); // no timeout

      if (bytesRead != sizeof(_samples)) {
        Serial.printf("Could only read %u bytes of %u in FillBufferI2S()\n", bytesRead, sizeof(_samples));
      }

      //############ Step 2: compensate for Channel number and offset, safe all to vReal Array   ############
      for (uint16_t i = 0; i < ARRAYSIZE(_samples); i++) {
        _vReal[i] = _offset - _samples[i];
        _vImag[i] = 0.0; //Imaginary part must be zeroed in case of looping to avoid wrong calculations and overflows
      }      
    }


    void computeFFT(){
      _fft.DCRemoval();
      _fft.Windowing(_vReal, SAMPLE_BLOCK, FFT_WIN_TYP_HAMMING, FFT_FORWARD);
      _fft.Compute(_vReal, _vImag, SAMPLE_BLOCK, FFT_FORWARD);
      _fft.ComplexToMagnitude(_vReal, _vImag, SAMPLE_BLOCK);
      _fft.MajorPeak(_vReal, SAMPLE_BLOCK, _samplingFrequency);
      for (int i = 0; i < G_NUM_BANDS; i++) {
        g_freqBins[i] = 0;
      }
      
      //############ Step 4: Fill the frequency bins with the FFT Samples ############
      for (int i = 2; i < SAMPLE_BLOCK / 2; i++) {
        if (_vReal[i] > _noiseThreshold) {
          int freq = bucketFrequency(i);
          int iBand = 0;
          while (iBand < G_NUM_BANDS) {
            if (freq < g_BandTable[iBand]){
              break;
            } 
            iBand++;
          }
          if (iBand > G_NUM_BANDS)iBand = G_NUM_BANDS;
          g_freqBins[iBand] += _vReal[i];
        }
      }


      //############ Step 5: Averaging and making it all fit on screen
      static float lastAllBandsPeak = 0.0f;
      float allBandsPeak = 0;
      
      for (int i = 0; i < G_NUM_BANDS; i++) {
        if (g_freqBins[i] > allBandsPeak) {
          allBandsPeak = g_freqBins[i];
        }
      }

      if (allBandsPeak < 1){
        allBandsPeak = 1;
      } 
      allBandsPeak = max(allBandsPeak, ((lastAllBandsPeak * (GAIN_DAMPEN - 1)) + allBandsPeak) / GAIN_DAMPEN); // Dampen rate of change a little bit on way down
      
      lastAllBandsPeak = allBandsPeak;

      if (allBandsPeak < 80000){
        allBandsPeak = 80000;
      } 

      for (int i = 0; i < G_NUM_BANDS; i++) {
        g_freqBins[i] /= (allBandsPeak * 1.0f);
      }

    }
};

arduinoFFT Analyzer::_fft = arduinoFFT();

#endif
