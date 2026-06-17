// SignalR client — connects to same-origin Web hub (relayed to API)
const SignalRClient = {
    connection: null,
    joinedConversations: new Set(),

    init() {
        if (typeof signalR === 'undefined') return Promise.resolve();

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/chat')
            .withAutomaticReconnect()
            .build();

        this.connection.on('ReceiveMessage', msg => {
            if (typeof ChatApp.onReceiveMessage === 'function') {
                ChatApp.onReceiveMessage(msg);
            }
        });

        this.connection.on('ReceiveFriendRequest', req => {
            if (typeof ChatApp.onReceiveFriendRequest === 'function') {
                ChatApp.onReceiveFriendRequest(req);
            }
        });

        return this.connection.start().catch(err => {
            console.warn('SignalR connect failed:', err);
            throw err;
        });
    }
};

window.SignalRClient = SignalRClient;
