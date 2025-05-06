let _webDisplay;
let _socketClient;

$(document).ready(() => {
    _socketClient = new SocketClient(URL.socketServer, onSocketConnect, onSocketDisConnect, onSocketMessage);            
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
    if(data.Event == DISPLAY_EVENT.CONFIG_CHANGED){
        _webDisplay = new WebDisplay(data.Data.Config, '.content', new Helpers());
    }else if (data.Event == DISPLAY_EVENT.DISPLAY){
        _webDisplay.displayLevels(data.Data);
    }
};

let showMessage = (msg, color) => {
    $('#msg').html(msg);
    $('#msg').css('color', color);    
};


