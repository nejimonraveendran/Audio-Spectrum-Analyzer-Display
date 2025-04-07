#include "WifiConnection.h"

WifiConnection::WifiConnection(){
    this->_resetButtonPin = 15; //GPIO pin reset button is connected to
}

bool WifiConnection::setupWifiConnection(){
    Serial.println("Setting up WiFi connection...");

    //just keep pin D15 and ground shorted during startup to reset WIFI settings.
    if (digitalRead(_resetButtonPin) == 0) { 
      Serial.println("Reset button pressed, WiFi settings will be reset!");
      _wm.resetSettings();
    }
  
    //try to connect to previously connected WiFi.  If failed, start an access point named "SpectrumAnalyzer".
    //connect to the access point using a mobile device or computer. Browse the portal at "http://192.168.4.1" or "http://sadconfig.local" to configure wifi settings.
    _wifiConnected = _wm.autoConnect("SpectrumAnalyzer", "");   
    if(!_wifiConnected){
      Serial.println("Failed to connect to WiFi, so starting WiFi config portal");
      setupDNS("sadconfig");  //"http://sadconfig.local"
      _wm.setConfigPortalBlocking(true);
    }else{
      setupDNS("sadbig"); //"http://sad.local"
      return true; //successfully connected and dns setup
    }
  
    return false;
}

//process wifi requests
void WifiConnection::process(){
    if(_wifiConnected){
        _wm.process();
    }
}

//set up DNS for the web server
void WifiConnection::setupDNS(String name){
    if (!MDNS.begin(name)) {   // Set hostname 
      Serial.println("Error while attempting to set DNS!");
      delay(1000); 
    }
  }
    


  