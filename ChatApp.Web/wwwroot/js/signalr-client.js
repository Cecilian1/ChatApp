// SignalR client for real-time chat
const SignalRClient = {
    connection: null,
    apiBase: '',

    init(apiBase, getToken) {
        this.apiBase = apiBase.replace(/\/$/, '');
        if (typeof signalR === 'undefined') return;

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(`${this.apiBase}/hubs/chat`, {
                accessTokenFactory: () => getToken() || ''
            })
            .withAutomaticReconnect()
            .build();

        this.connection.on('ReceiveMessage', msg => {
            if (window.ChatApp?.onReceiveMessage) {
                window.ChatApp.onReceiveMessage(msg);
            }
        });

        this.connection.start().catch(err => console.warn('SignalR connect failed:', err));
    },

    async joinConversation(conversationId) {
        if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) return;
        try {
            await this.connection.invoke('JoinConversation', conversationId);
        } catch (e) { console.warn(e); }
    },

    async leaveConversation(conversationId) {
        if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) return;
        try {
            await this.connection.invoke('LeaveConversation', conversationId);
        } catch (e) { console.warn(e); }
    }
};

window.SignalRClient = SignalRClient;
