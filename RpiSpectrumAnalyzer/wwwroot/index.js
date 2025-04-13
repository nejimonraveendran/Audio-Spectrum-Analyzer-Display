let _socketServer = "ws://mypi5.local:8080/ws";
let _webDisplay;
let _socketClient;
const  _events = {
    STARTUP: 'startup',
    DISPLAY: 'display',
};


$(document).ready(() => {
    _socketClient = new SocketClient(_socketServer, onSocketConnect, onSocketDisConnect, onSocketMessage);            
    _socketClient.connect();

    window.setInterval(() => {
        let readyState = _socketClient.getStatus();
        switch (readyState) {
            case 0: //connecting
                showMessage("Connecting to Spectrum Server...", 'yellow');
                break;
            case 3: //closed
                _socketClient.connect();
                break;
            default:
                break;
        }
        
    }, 3000); 

});

let onSocketConnect = () =>{
    showMessage("Connected to Spectrum Server", 'greenyellow');
};

let onSocketDisConnect = () =>{
    showMessage("Disconnected from Spectrum Server", 'red');

    if(_webDisplay === null || _webDisplay === undefined) return;
    _webDisplay.clear();    
};


let onSocketMessage = (data) => {
    if(data.Event == _events.STARTUP){
        _webDisplay = new WebDisplay(data.Data.Rows, data.Data.Cols, '.content');
    }else if (data.Event == _events.DISPLAY){
        _webDisplay.displayLevels(data.Data);
    }
};

let showMessage = (msg, color) => {
    $('#msg').html(msg);
    $('#msg').css('color', color);    
};


