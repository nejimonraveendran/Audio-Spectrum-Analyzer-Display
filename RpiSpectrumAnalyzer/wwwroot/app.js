const socket = new WebSocket("ws://mypi5.local:8080/ws");

socket.onopen = () => {
    console.log("Connected to WebSocket");
    socket.send("Hello Server!");
};

socket.onmessage = (event) => {
    // console.log("Message from server:", event.data);
};

socket.onclose = () => {
    console.log("Disconnected from WebSocket");
};
