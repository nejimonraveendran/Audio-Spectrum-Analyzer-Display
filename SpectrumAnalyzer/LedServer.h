#ifndef LedServer_h
#define LedServer_h

#include "stdint.h"
#include "http_parser.h"
#include "HTTP_Method.h"
#include <Arduino.h>
#include <WebServer.h>
#include <Ticker.h>
#include "LedMatrix.h"
#include "Common.h"
#include <ArduinoJson.h>

class LedServer {
  private:
    static TaskHandle_t _webserverTask; // setting up the task handler for webserver 
    static bool _canUpdateClients;
    //static String _bandLabels[G_NUM_BANDS];
    static WebServer _server;
    static LedMatrix _ledMatrix;    

    static void uiTask(void* pvParameters ) {
      Serial.print("Webserver task is  running on core ");
      Serial.println(xPortGetCoreID());
      
      while(true) {
        g_wifiConn.process();
        // _webSocket.loop();
        _server.handleClient();

        if (_canUpdateClients) {
          float _freqBinsOld[G_NUM_BANDS];                                  //*
          float _freqBinsNew[G_NUM_BANDS];                                  //*
          
          // lets smooth out the data we send
          for (int i = 0; i < G_NUM_BANDS; i++) {
            _freqBinsNew[i] = g_freqBins[i];
            
            if (_freqBinsNew[i] < _freqBinsOld[i]) {
              g_freqBins[i] = max(_freqBinsOld[i] - g_speedfilter, _freqBinsNew[i]);
              // if (g_freqBins[i] > 1.0) {
              //   g_freqBins[i] = 1; //to prevent glitch when changing number of channels during runtime
              // } 
            }else if (_freqBinsNew[i] > _freqBinsOld[i]) {
              g_freqBins[i] = _freqBinsNew[i];
            }
            
            _freqBinsOld[i] = g_freqBins[i];

          } 

          // done smoothing now send the data
          sendDataToClients(); // webbrowser

          _canUpdateClients = false;
        }
      }
    }

    static void sendDataToClients() {
      for (int col = 0; col < G_NUM_BANDS; col++) {
        uint8_t level = g_freqBins[col] * 100;
        uint8_t value = map(level, 0, 100, 0, G_NUM_LEVELS);
        _ledMatrix.setLEDColumn(col, value);   
        _ledMatrix.setLEDColPeak(col, value);    
      }

      //update LED matrix  
      _ledMatrix.updateLEDs();
    }

    static void addCorsHeaders(){
      _server.sendHeader("Access-Control-Allow-Origin", "*"); // Allow all origins
      _server.sendHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
      _server.sendHeader("Access-Control-Allow-Headers", "Content-Type");
    }


  public:
    LedServer(){
      delay(3000);
      xTaskCreatePinnedToCore(uiTask, "UITask", 10000, NULL, 4, &_webserverTask, 0); 
    }

    void setupServer(){
      //set up home page route
      _server.on("/", []() {   
        _server.send_P(200, "text/html", g_homePage);
      });

      _server.on("/config", HTTP_OPTIONS, [](){
        addCorsHeaders();
        _server.send(200, "text/plain", "CORS Allowed!");
      });

      //retrieve config from server
      _server.on("/config", []() {   
        StaticJsonDocument<15000> doc;
        doc["noOfCols"] = G_NUM_BANDS;
        doc["noOfRows"] = G_NUM_LEVELS;    
        doc["peakDelay"] = g_maxPeakFallingWait;
        doc["peakSpeed"] = g_peakFallingIntervalIncrement;      
        doc["speedFilter"] = g_speedfilter;  

        //set peak color
        doc["peak"]["r"] = g_peakColor.r;
        doc["peak"]["g"] = g_peakColor.g;
        doc["peak"]["b"] = g_peakColor.b;
        
        JsonArray pixels = doc.createNestedArray("pixels");

        for (uint16_t i=0; i<G_NUM_LEDS; i++) {
          pixels[i]["r"] = g_ledColors[i].r;
          pixels[i]["g"] = g_ledColors[i].g;
          pixels[i]["b"] = g_ledColors[i].b;
        }    

        String response;
        serializeJson(doc, response);
  
        addCorsHeaders();
        _server.send(200, "application/json", response);
      });

      //In my tests, _server.enableCORS did not work, so adding preflight manually to enable CORS.
      _server.on("/deploy", HTTP_OPTIONS, [](){
        addCorsHeaders();
        _server.send(200, "text/plain", "CORS Allowed!");
      });

      //deploy config changes 
      _server.on("/deploy", HTTP_POST, [](){
        StaticJsonDocument<15000> doc;    
        String payload = _server.arg("data");

        //Serial.println(payload);

        DeserializationError err = deserializeJson(doc, payload);

        if(err){
          Serial.print(F("deserializeJson() returned "));
          Serial.println(err.f_str());
          _server.send(200, "application/json", "{\"result\":\"fail\"}");
          return;
        }

        //set peak delay and speed
        g_maxPeakFallingWait = doc["peakDelay"];
        g_peakFallingIntervalIncrement = doc["peakSpeed"];      
        g_speedfilter = doc["speedFilter"];  

        //set peak color
        uint8_t r = doc["peak"]["r"];
        uint8_t g = doc["peak"]["g"];
        uint8_t b = doc["peak"]["b"];
        g_peakColor = CRGB(r, g, b);

        JsonArray pixels = doc["pixels"].as<JsonArray>();

        for (uint16_t i=0; i<G_NUM_LEDS; i++) {
          r = pixels[i]["r"];
          g = pixels[i]["g"];
          b = pixels[i]["b"];
          g_ledColors[i] = CRGB(r, g, b);
        }    

        addCorsHeaders();
        _server.send(200, "application/json", "{\"result\":\"success\"}");

      });


      //start web server and socket server
      //_server.enableCORS();  //this did not work
      _server.begin();             

      //info
      Serial.println("HTTP server started");
    }

    void updateClients(){
      _ledMatrix.updateLEDs();  //this update is requied to avoid random pixel color flicker  
      _canUpdateClients = true;
    }

};

WebServer LedServer::_server(80);
bool LedServer::_canUpdateClients = false;
TaskHandle_t LedServer::_webserverTask = NULL;    
LedMatrix LedServer::_ledMatrix;

#endif

