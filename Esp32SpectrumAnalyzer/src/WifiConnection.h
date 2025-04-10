#ifndef WifiConnection_h
#define WifiConnection_h

#include "Common.h"

class WifiConnection {
  private:
    WiFiManager _wm; //wifi manager
    bool _wifiConnected = false; //flag to indicate if wifi is connected
    unsigned short _resetButtonPin; //GPIO pin reset button is connected to (for resetting wifi settings)
    void setupDNS(String name); //set up DNS for the web server
    
  public:
    WifiConnection(); //constructor
    bool setupWifiConnection(); //setup wifi connection
    void process(); //process wifi requests
};

#endif
