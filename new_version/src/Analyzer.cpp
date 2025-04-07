#include "Analyzer.h"
#include "Common.h"

Analyzer::Analyzer(uint8_t numberOfBands, unsigned short* bandTable){
    this->_noOfBands =  numberOfBands;
    this->_samplingFrequency = 44100; //44.1kHz
    this->_sampleSize = 1024; //number of audio samples to read (must be power of 2)  
    this->_noiseThreshold = 1000;
    this->_offset = (uint16_t)ADC1_CHANNEL_0 * 0x1000 + 0xFFF;;
    this->_vReal = new double[this->_sampleSize] {0};
    this->_vImag = new double[this->_sampleSize] {0};
    this->_samples = new int16_t[this->_sampleSize] {0};
    this->_freqBands = nullptr;
    this->_bandTable = new unsigned short[this->_noOfBands] {0};
    
    for (unsigned short i = 0; i < this->_noOfBands; i++)
    {
        _bandTable[i] = bandTable[i];
    }

    this->_fft = new arduinoFFT(this->_vReal, this->_vImag, this->_sampleSize, this->_samplingFrequency);
    
}

bool Analyzer::setupAdc(){
    esp_err_t err;
    
    //I2S config structure
    const i2s_config_t i2s_config = {
      .mode = (i2s_mode_t)(I2S_MODE_MASTER | I2S_MODE_RX | I2S_MODE_ADC_BUILT_IN),
      .sample_rate = _samplingFrequency,
      .bits_per_sample = I2S_BITS_PER_SAMPLE_16BIT, // could only get it to work with 32bits
      .channel_format = I2S_CHANNEL_FMT_ONLY_LEFT, // although the SEL config should be left, it seems to transmit on right
      .communication_format = I2S_COMM_FORMAT_STAND_I2S,
      .intr_alloc_flags = ESP_INTR_FLAG_LEVEL1,     // Interrupt level 1
      .dma_buf_count = 2,                           // number of buffers
      .dma_buf_len = _sampleSize,                     // samples per buffer
      .use_apll = false,
      .tx_desc_auto_clear = false,
      .fixed_mclk = 0
    };
  
    // Configuring the I2S driver.  
    err = adc1_config_channel_atten(ADC1_CHANNEL_0, ADC_ATTEN_DB_0); 
    if (err != ESP_OK) {
      Serial.printf("adc1_config_channel_atten failed with error code: %d\n", err);
      return false;
    }

    //install I2S driver
    err = i2s_driver_install(I2S_NUM_0, &i2s_config,  0, NULL);  
    if (err != ESP_OK) {
      Serial.printf("i2s_driver_install failed with error code: %d\n", err);
      return false;
    }

    //set up the I2S ADC mode
    err = i2s_set_adc_mode(ADC_UNIT_1, ADC1_CHANNEL_0);
    if (err != ESP_OK) {
        Serial.printf("i2s_set_adc_mode failed with error code: %d\n", err);
        return false;
    }
    
    delay(100);
    //enable the ADC
    err = i2s_adc_enable(I2S_NUM_0);
    if (err != ESP_OK) {
      Serial.printf("i2s_adc_enable failed with error code: %d\n", err);
      return false;
    }
    
    Serial.println("I2S driver installed and audio input setup completed");

    return true;
  
}
  

void Analyzer::readAudioSamples(){
    size_t bytesToRead = 1024 * sizeof(int16_t);
    size_t bytesRead = 0;

    i2s_read(I2S_NUM_0, _samples, bytesToRead, &bytesRead, portMAX_DELAY); 

    //calculate the offset and save bytes to vReal Array
    for (uint16_t i = 0; i < _sampleSize; i++) {
      _vReal[i] = _offset - _samples[i]; //real part of the complex numbers returned
      _vImag[i] = 0.0; //We do not need imaginary part

    //   double normalizedValue = ((_samples[i] / 4095.0f) * 2.0f) - 1.0f;
    //   _vReal[i] = normalizedValue; //real part of the complex numbers returned
    //   _vImag[i] = 0.0; //We do not need imaginary part

        // Serial.print(normalizedValue);
        // Serial.print(", ");

    }   

    // Serial.println();
}


void Analyzer::convertToBands(float* freqBands){
    if(this->_freqBands == nullptr){
        this->_freqBands = freqBands;
    }

    //Compute FFT using ArduinoFFT library and put into frequency bands
    _fft->Windowing(FFT_WIN_TYP_HAMMING, FFT_FORWARD);
    _fft->Compute(FFT_FORWARD);
    _fft->ComplexToMagnitude();
    this->putIntoFrequencyBands();

}
  

//PRIVATE MEMBERS DEFINITION:
void Analyzer::putIntoFrequencyBands(){
    for (unsigned short i = 0; i < this->_noOfBands; i++) {
        this->_freqBands[i] = 0;
    }
    
    //in FFT, based on the sampling frequency and the number of samples, there will be a fixed number of frequency components (aka bins resolution)
    //for 1024 audio samples at a sampling frequency of 44100 Hz, there will be 513 samples from 0 Hz to 220500 Hz (formula: no.of bins = (no.of samples/2) + 1)
    //loop over half of samples (only first half is usable), find the frequency components in the samples, match to the band table, and add the matching frequencies into frequency bands array.
    for (unsigned short i = 2; i < this->_sampleSize / 2; i++) {
        if (this->_vReal[i] > _noiseThreshold) { //try to ignore any static noise component in the audio.
            int freq =  i * (this->_samplingFrequency / this->_sampleSize); //find the frequency bin at this index position 
     
            //loop over the bands. If the current bin frequency is in the range of the value provided in the band table, add the corresponding real number to the frequency band. 
            for (unsigned short b = 0; b < this->_noOfBands; b++)
            {
                int startFreq = b == 0 ? 0 : _bandTable[b-1];
                int endFreq = _bandTable[b];

                if(freq > startFreq && freq <= endFreq){
                    this->_freqBands[b] += this->_vReal[i];
                }

            }            
        }
    }
}
