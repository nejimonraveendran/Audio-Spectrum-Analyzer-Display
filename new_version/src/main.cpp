#include "Analyzer.h"
#include "LedServer.h"
#include "LedMatrix.h"
#include "WifiConnection.h"

//CUSTOM CONFIGURATION SECTION
#define NUM_LEVELS 10  //change this to the number of levels you want to display on the LED matrix
unsigned short _bandTable[] = { //frequency bands in Hz
  100, 250, 500, 750, 1000, 2000, 4000, 6000, 8000, 10000 
};


//do not touch from here
#define ARRAYSIZE(a) (sizeof(a)/sizeof(a[0]))
Analyzer* _analyzer;
LedServer* _ledServer;
float* _freqBands;

void setup() {
  Serial.begin(115200);

  unsigned short noOfBands = ARRAYSIZE(_bandTable);
  _analyzer = new Analyzer(noOfBands, _bandTable);

  //set up ADC. If it fails, no point in moving forward.
  if(!_analyzer->setupAdc())
    return;

  //array to hold frequency band levels
  _freqBands = new float[noOfBands];

  //prepare arguments for the LED Server
  LedServerArgs args = {
    .wifiConnection = new WifiConnection(), 
    .webServer = new WebServer(80), 
    .ledMatrix = new LedMatrix(NUM_LEVELS, noOfBands) 
  };

  //create new LED server with arguments
  _ledServer = new LedServer(args);

  //before entering the loop, give some time for the thread in the LED server to connect to WiFi etc.
  delay(2000); 

  //main loop to process audio input and display of output
  while(true){
    _analyzer->readAudioSamples();
    _analyzer->convertToBands(_freqBands);
    _ledServer->updateClients(_freqBands);
  }
}

//we do not use Arduino loop in this sketch.
void loop() {
  Serial.println("This will never be executed!");
}

