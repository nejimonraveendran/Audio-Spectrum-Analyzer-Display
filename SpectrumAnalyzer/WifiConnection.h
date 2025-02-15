#ifndef WifiConnection_h
#define WifiConnection_h

#include <Arduino.h>
#include <WiFi.h>
#include <WiFiManager.h> //v2.0.17

#define MODE_BUTTON_PIN 15

class WifiConnection {
  private:
    WiFiManager _wm;
    bool _wifiConnected = false;
    
  public:
    void setupWifiConnection(){
      //just keep pin D15 and ground shorted during startup to reset WIFI settings.
      if (digitalRead(MODE_BUTTON_PIN) == 0) { 
        Serial.println("button pressed on startup, WIFI settings will be reset");
        _wm.resetSettings();
      }

      //try to connect to previously connected WiFi.  If failed, start an access point named "MySpectrumAnalyzer".
      //connect to the access point using a mobile device or computer. Browse the portal at http://192.168.4.1 to configure wifi settings.
      _wifiConnected = _wm.autoConnect("MySpectrumAnalyzer", "");   
      if(!_wifiConnected){
        Serial.println("Failed to connect to WiFi, so starting WiFi config portal");
        _wm.setConfigPortalBlocking(true);
      }
    }

    void process(){
      if(_wifiConnected){
        _wm.process();
      }
    }
};

#endif

