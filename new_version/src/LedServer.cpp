#include "LedServer.h"
#include "WebPage.h"

WebServer* LedServer::_server = nullptr;    
WifiConnection* LedServer::_wifiConn = nullptr;
LedMatrix* LedServer::_ledMatrix = nullptr;
bool LedServer::_clientsPaused = false; 
float LedServer::_speedFilter = 0.08; //default; can be updated via web portal.
float LedServer::_attenuationFactor = 100000.0f; //default value; can be changed from the portal.


//public member definitions
LedServer::LedServer(LedServerArgs args){
  Serial.println("LED Server initializing...");
  
  this->_ledMatrix = args.ledMatrix;
  this->_wifiConn = args.wifiConnection;
  this->_server = args.webServer;
  this->_noOfBands = this->_ledMatrix->getNoOfCols();
  this->_noOfLevels = this->_ledMatrix->getNoOfRows();
  this->_freqBandsOld = new float[this->_noOfBands] {0};
  this->_freqBands = nullptr;

  //start second thread pinned to ESP32 CPU Core 0 for running web server 
  this->_webServerTask = nullptr;
  xTaskCreatePinnedToCore(this->webServerThread, "WebServerTask", 10000, NULL, 4, &_webServerTask, 0); 
}

//update the clients (eg. LED matrix) with the frequency bands
void LedServer::updateClients(float* freqBins){
  if(this->_freqBands == nullptr){
    this->_freqBands = freqBins;
  }

  if(this->_clientsPaused)
    return;
  
  this->attenuateBands();
  this->smoothenSpeed();
  this->sendToLEDMatrix();
}


//PRIVATE MEMBER DEFINITIONS
void LedServer::pauseClients(){
  _clientsPaused = true;
  _ledMatrix->clearMatrix();
}

void LedServer::resumeClients(){
  _ledMatrix->clearMatrix();
  _clientsPaused = false;
}

//frequency levels are usuallly in the 100K range.  We need to attenuate them signficantly to be able to display them on the LED matrix.
void LedServer::attenuateBands(){
  float highestBand = 0.0f;
  
  //find the highest magnitude of all bands
  for (int i = 0; i < this->_noOfBands; i++) {
      if (this->_freqBands[i] > highestBand) {
          highestBand = this->_freqBands[i];
      }
  }

  //if highest band is more than attenuation factor, take that.  Otherwise, take the attentuation factor
  // float attentuationFactor = max(highestBand, this->_attenuationFactor); 
  float attentuationFactor = max(highestBand, this->_attenuationFactor); 

  //for all bands, divide the bands with attenuation factor, to make the frequency values 
  for (int i = 0; i < this->_noOfBands; i++) {
    this->_freqBands[i] /= attentuationFactor;
  }  
}

//smoothen the speed of the transition of levels in the bands
void LedServer::smoothenSpeed(){
  float freqBandsNew[this->_noOfBands] = {0};

  //smooth out the data
  for (unsigned short i = 0; i < this->_noOfBands; i++) {
    freqBandsNew[i] = this->_freqBands[i];
    
    if (freqBandsNew[i] < this->_freqBandsOld[i]) {
      this->_freqBands[i] = max(this->_freqBandsOld[i] - this->_speedFilter, freqBandsNew[i]);
    }else if (freqBandsNew[i] > this->_freqBandsOld[i]) {
      this->_freqBands[i] = freqBandsNew[i];
    }        

    this->_freqBandsOld[i] = this->_freqBands[i];
  } 
}

//send the LED levels to LED matrix
void LedServer::sendToLEDMatrix() {
  //map the frequency values to integers to be compatible with the LED display 
  for (unsigned short col = 0; col < this->_noOfBands; col++) {
    unsigned short level = this->_freqBands[col] * 100;
    unsigned short value = map(level, 0, 100, 0, this->_noOfLevels);
    
    _ledMatrix->setLEDColumn(col, value);   
    _ledMatrix->setLEDColPeak(col, value);            
  }

  _ledMatrix->updateLEDs();
}

//web server thread function
void LedServer::webServerThread(void* pvParameters) {    
  if(_wifiConn->setupWifiConnection()){ 
    //if successfully connected to wifi, give visual indication
    pauseClients(); //pause any audio displays
    _ledMatrix->doDemo(CRGB::White);
    resumeClients(); 
  }

  setupWebServerRoutes(); //set up web server handlers
  _server->begin(); //begin web server

  Serial.printf("Web Server started on core %u\n", xPortGetCoreID());

  while(true) {
    _wifiConn->process();  //process wifi requests
    _server->handleClient(); //process web requests
    vTaskDelay(10 / portTICK_PERIOD_MS);  //Give time to other tasks (10 ms) and avoid watchdog trigger errors.
  }
}

//add CORS headers to the web server response
void LedServer::addCorsHeaders(){
  _server->sendHeader("Access-Control-Allow-Origin", "*"); // Allow all origins
  _server->sendHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
  _server->sendHeader("Access-Control-Allow-Headers", "Content-Type");
}

//set up web server route handlers
void LedServer::setupWebServerRoutes(){
  //set up home page route
  _server->on("/", []() {   
    _server->send_P(200, "text/html", g_webPage);
  });

  //config API request preflight
  _server->on("/config", HTTP_OPTIONS, [](){
    addCorsHeaders();
    _server->send(200, "text/plain", "CORS Allowed!");
  });

  //config API request handler 
  _server->on("/config", []() {   
    JsonDocument doc;
    
    doc["noOfCols"] = _ledMatrix->getNoOfCols();
    doc["noOfRows"] = _ledMatrix->getNoOfRows();    
    doc["peakDelay"] = _ledMatrix->getMaxPeakFallingWait();
    doc["peakSpeed"] = _ledMatrix->getPeakFallingIntervalIncrement();      
    doc["speedFilter"] = _speedFilter;  
    doc["atten"] = _attenuationFactor;  
    doc["brightness"] = _ledMatrix->getBrightness();  
    
    //get peak color
    CRGB peakColor = _ledMatrix->getPeakColor();
    doc["peak"]["r"] = peakColor.r;
    doc["peak"]["g"] = peakColor.g;
    doc["peak"]["b"] = peakColor.b;
    
    JsonArray pixels = doc["pixels"].to<JsonArray>();
    unsigned short noOfLeds = _ledMatrix->getNoOfCols() * _ledMatrix->getNoOfRows();
    CRGB* ledColors = _ledMatrix->getLEDColors();
    for (unsigned short i=0; i < noOfLeds; i++) {
      pixels[i]["r"] = ledColors[i].r;
      pixels[i]["g"] = ledColors[i].g;
      pixels[i]["b"] = ledColors[i].b;
    }    

    String response;
    serializeJson(doc, response);
    addCorsHeaders();
    _server->send(200, "application/json", response);
  });

  //In my tests, _server.enableCORS did not work, so adding preflight manually to enable CORS.
  _server->on("/deploy", HTTP_OPTIONS, [](){
    addCorsHeaders();
    _server->send(200, "text/plain", "CORS Allowed!");
  });

  //API request handler to deploy config changes 
  _server->on("/deploy", HTTP_POST, [](){
    JsonDocument doc;
    String payload = _server->arg("data");

    DeserializationError err = deserializeJson(doc, payload);

    if(err){
      Serial.print(F("deserializeJson() returned "));
      Serial.println(err.f_str());
      _server->send(200, "application/json", "{\"result\":\"fail\"}");
      return;
    }

    //set peak delay and speed
    _ledMatrix->setMaxPeakFallingWait(doc["peakDelay"]);
    _ledMatrix->setPeakFallingIntervalIncrement(doc["peakSpeed"]);
    _speedFilter = doc["speedFilter"];
    _attenuationFactor = doc["atten"];
    _ledMatrix->setBrightness(doc["brightness"]);


    //set peak color
    uint8_t r = doc["peak"]["r"];
    uint8_t g = doc["peak"]["g"];
    uint8_t b = doc["peak"]["b"];
    _ledMatrix->setPeakColor(CRGB(r, g, b));

    JsonArray pixels = doc["pixels"].as<JsonArray>();
    unsigned short noOfLeds = _ledMatrix->getNoOfCols() * _ledMatrix->getNoOfRows();
    for (unsigned short i=0; i < noOfLeds; i++) {
      r = pixels[i]["r"];
      g = pixels[i]["g"];
      b = pixels[i]["b"];
      _ledMatrix->setPixelColor(i, CRGB(r, g, b));
    }    

    addCorsHeaders();
    _server->send(200, "application/json", "{\"result\":\"success\"}");

  });  
}





  
  