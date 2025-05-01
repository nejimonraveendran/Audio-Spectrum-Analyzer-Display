const _host = window.location.hostname + ':' + window.location.port;

const URL = {
    socketServer: `ws://${_host}/ws`,
    apiServer: `http://${_host}/api/config`,
}

const DISPLAY_TYPE = {
    LED: 1,
    CONSOLE: 2,
    WEB: 3
};

const DISPLAY_EVENT = {
    DISPLAY: 2,
    CONFIG_CHANGED: 3,
    CLEAR: 4
};

