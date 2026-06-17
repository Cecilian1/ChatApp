const SignalRClient = {
    connection: null,
    joinedConversations: new Set(),

    init() {
        if (typeof signalR === 'undefined') return Promise.resolve();

        const app = document.getElementById('im-app');
        const apiBase = app?.dataset.apiBase || 'http://localhost:5200';
        const token = app?.dataset.jwt || '';

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(apiBase + '/hubs/chat', {
                accessTokenFactory: () => token
            })
            .withAutomaticReconnect()
            .build();

        this.connection.on('ReceiveMessage', msg => {
            console.log('SignalR 收到消息:', msg);

            if (typeof ChatApp.onReceiveMessage === 'function') {
                try {
                    ChatApp.onReceiveMessage(msg);
                    SignalRClient.updateSessionList(msg);
                } catch (e) {
                    console.warn('ChatApp.onReceiveMessage 异常，直接渲染:', e);
                    SignalRClient.renderMessageDirectly(msg);
                    SignalRClient.updateSessionList(msg);
                }
            } else {
                SignalRClient.renderMessageDirectly(msg);
                SignalRClient.updateSessionList(msg);
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
    },

    renderMessageDirectly(msg) {
        try {
            const activeItem = document.querySelector('.session-item.active');
            const activeSessionId = activeItem?.dataset.sessionId;

            if (msg.sessionId !== activeSessionId) {
                console.log('消息不属于当前会话，不显示');
                return;
            }

            const container = document.getElementById('message-container');
            if (!container) return;

            const emptyState = container.querySelector('#chat-empty, .empty-state');
            if (emptyState) {
                emptyState.remove();
            }

            const app = document.getElementById('im-app');
            const userSeed = app?.dataset.userSeed || 'Felix';

            if (typeof MessageRenderer !== 'undefined' && MessageRenderer.appendMessage) {
                MessageRenderer.appendMessage(container, msg, userSeed);
                console.log('消息已直接渲染到界面');
                setTimeout(() => {
                    container.scrollTop = container.scrollHeight;
                }, 50);
            }
        } catch (e) {
            console.error('直接渲染消息失败:', e);
        }
    },

    updateSessionList(msg) {
        try {
            const sessionId = msg.sessionId;
            const preview = msg.type === 1 || msg.type === 'File' ? `[文件] ${msg.fileName}` : msg.content;

            const sessionItem = document.querySelector(`.session-item[data-session-id="${sessionId}"]`);
            if (sessionItem) {
                const lastMsgEl = sessionItem.querySelector('.last-msg');
                if (lastMsgEl) lastMsgEl.textContent = preview;

                const timeEl = sessionItem.querySelector('.time');
                if (timeEl) {
                    const now = new Date();
                    timeEl.textContent = now.toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' });
                }
            }

            const list = document.getElementById('session-list');
            if (!list) return;

            const items = Array.from(list.querySelectorAll('.session-item'));
            if (items.length === 0) return;

            const targetItem = items.find(el => el.dataset.sessionId === sessionId);
            if (!targetItem) return;

            list.removeChild(targetItem);
            list.insertBefore(targetItem, list.firstChild);

            console.log('会话列表已更新并重新排序');
        } catch (e) {
            console.error('更新会话列表失败:', e);
        }
    }
};

window.SignalRClient = SignalRClient;