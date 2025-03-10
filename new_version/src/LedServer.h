#ifndef LedServer_h
#define LedServer_h

#include "LedMatrix.h"
#include "WifiConnection.h"

//structure for passing arguments to the LedServer constructor
struct LedServerArgs{
  WifiConnection* wifiConnection;
  WebServer* webServer;
  LedMatrix* ledMatrix;
};

class LedServer {
  private:
    TaskHandle_t _webServerTask; //task handler for webserver 
    static WifiConnection* _wifiConn; 
    static WebServer* _server;
    static LedMatrix* _ledMatrix;    
    float* _freqBandsOld; //array to hold the previous frequency band levels
    float* _freqBands; //array to hold the frequency band levels
    static float _speedFilter; //factor used to smoothen the speed of the bands.  Can be changed via web portal.
    static float _attenuationFactor; //factor used to attenuate/amplify the bands signal.  Can be changed via web portal.
    unsigned short _noOfBands; //number of bands
    unsigned short _noOfLevels; //number of levels 
    static bool _clientsPaused; //flag to pause/resume the clients (eg. LED matrix)
    void attenuateBands(); //attenuate the bands
    void smoothenSpeed(); //smoothen the speed of the transition of levels in the bands
    void sendToLEDMatrix(); //send the LED levels to LED matrix
    static void webServerThread(void* pvParameters); //web server thread function
    static void addCorsHeaders(); //add CORS headers to the web server response
    static void setupWebServerRoutes(); //set up web server routes
    static void pauseClients(); //pause the clients (eg. LED matrix)
    static void resumeClients(); //resume the clients (eg. LED matrix)

  public:
    LedServer(LedServerArgs args);
    void updateClients(float* freqBins); //update the clients (eg. LED matrix) with the frequency bands
};


#endif
