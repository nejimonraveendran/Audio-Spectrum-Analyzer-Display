class SocketClient{
    constructor(url, onConnect, onDisconnect, onMessage){
        this._socket = null;
        this._url = url;
        this._onConnect = onConnect;
        this._onDisconnect = onDisconnect;
        this._onMessage = onMessage;
    }

    connect(){
        this._socket = new WebSocket(this._url);
        this._socket.onopen = this.onSocketConnect.bind(this);
        this._socket.onclose = this.onSocketDisConnect.bind(this);
        this._socket.onmessage = this.onSocketMessage.bind(this);
    }

    onSocketConnect(){
        this._onConnect();
    }

    onSocketDisConnect(callback){
        this._onDisconnect();
    }

    onSocketMessage(event){
        if(event.data === null || event.data === undefined || event.data == '') return;
        let data = JSON.parse(event.data);
        this._onMessage(data);

    }

    getStatus(){
        if(this._socket === null || this._socket === undefined) return -1; //-1 = unknown
        return this._socket.readyState; //0 = connecting, 1 = connected, 2 = closing, 3 = closed,
    }

}
