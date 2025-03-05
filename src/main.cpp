#include <Arduino.h>
#include "Analyzer.h"
#include "LedServer.h"
#include "Common.h"

LedMatrix _ledMatrix;
Analyzer _analyzer;
LedServer _ledServer;


void setup() {  
  Serial.begin(115200);
  
  _ledServer.setupServer();
  _analyzer.setupAdc();
  
}

void loop() {
  _analyzer.readAudioFromInput();
  _analyzer.computeFFT();
  _ledServer.updateClients();
}


