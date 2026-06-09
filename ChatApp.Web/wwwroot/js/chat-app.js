const ChatApp = {
    state: {
        activeSessionId: null,
        userId: null,
        userSeed: null,
        mockReplyTimer: null
    },

    init() {
        const app = document.getElementById('im-app');
        if (!app) return;

        this.state.userId = app.dataset.userId;
        this.state.userSeed = app.dataset.userSeed;
        this.state.activeSessionId = app.dataset.activeSession || null;

        const apiBase = app.dataset.apiBase || '';
        const jwt = app.dataset.jwt || '';
        if (apiBase && typeof SignalRClient !== 'undefined') {
            SignalRClient.init(apiBase, () => jwt);
        }

        this.bindNavTabs();
        this.bindSessionList();
        this.bindSendMessage();
        FriendManager.init(this);
        HistoryManager.init(this);
        FileUploadManager.init((fileName, fileSize, fileUrl) => this.sendFileMessage(fileName, fileSize, fileUrl));

        if (this.state.activeSessionId) {
            this.updateDeleteFriendBtn();
            SignalRClient.joinConversation(this.state.activeSessionId);
        }

        window.ChatApp.onReceiveMessage = (msg) => this.onReceiveMessage(msg);
    },

    onReceiveMessage(msg) {
        const sid = msg.sessionId || msg.conversationId;
        const preview = msg.type === 1 || msg.type === 'File' ? `[文件] ${msg.fileName}` : msg.content;
        // Skip messages we sent ourselves (already appended from HTTP response)
        if (msg.senderId === this.state.userId) {
            this.updateSessionPreview(sid, preview);
            return;
        }
        if (sid !== this.state.activeSessionId) {
            this.updateSessionPreview(sid, preview);
            return;
        }
        MessageRenderer.appendMessage(
            document.getElementById('message-container'), msg, this.state.userSeed);
        this.updateSessionPreview(sid, preview);
    },

    bindNavTabs() {
        document.querySelectorAll('.nav-item[data-tab]').forEach(btn => {
            btn.addEventListener('click', () => {
                const tab = btn.dataset.tab;
                document.querySelectorAll('.nav-item[data-tab]').forEach(b => b.classList.remove('active'));
                btn.classList.add('active');

                document.querySelectorAll('.list-panel').forEach(p => p.classList.remove('active'));
                document.getElementById(`panel-${tab}`)?.classList.add('active');

                document.querySelectorAll('.main-view').forEach(v => {
                    v.classList.remove('active');
                    v.hidden = true;
                });
                const activeView = document.getElementById(`view-${tab}`);
                if (activeView) {
                    activeView.classList.add('active');
                    activeView.hidden = false;
                }

                if (tab === 'history') HistoryManager.search();
            });
        });
    },

    bindSessionList() {
        const list = document.getElementById('session-list');
        list?.addEventListener('click', e => {
            const item = e.target.closest('.session-item');
            if (!item) return;
            this.selectSession(item);
        });

        document.getElementById('session-search')?.addEventListener('input', e => {
            const q = e.target.value.toLowerCase();
            list.querySelectorAll('.session-item').forEach(item => {
                const title = item.dataset.title.toLowerCase();
                item.style.display = title.includes(q) ? '' : 'none';
            });
        });
    },

    async selectSession(item) {
        document.querySelectorAll('.session-item').forEach(i => i.classList.remove('active'));
        item.classList.add('active');

        const sessionId = item.dataset.sessionId;
        this.state.activeSessionId = sessionId;
        document.getElementById('chat-header').style.display = '';
        document.getElementById('chat-footer').style.display = '';
        document.getElementById('chat-title').textContent = item.dataset.title;
        document.getElementById('chat-status').textContent = item.dataset.status;

        this.updateDeleteFriendBtn();
        await this.loadMessages(sessionId);
        if (this.state.activeSessionId === sessionId)
            SignalRClient.joinConversation(sessionId);
    },

    updateDeleteFriendBtn() {
        const item = document.querySelector('.session-item.active');
        const btn = document.getElementById('btn-delete-friend');
        if (item?.dataset.sessionType === 'Private' && item.dataset.targetUserId) {
            btn.classList.remove('hidden');
        } else {
            btn.classList.add('hidden');
        }
    },

    async loadMessages(sessionId) {
        const messages = await ApiClient.get(`/api/Chat/messages/${sessionId}`);
        // Discard result if the user switched to a different session while loading
        if (this.state.activeSessionId !== sessionId) return;
        const container = document.getElementById('message-container');
        container.innerHTML = '';
        if (!messages?.length) {
            container.innerHTML = '<div class="empty-state"><span>暂无消息，发送第一条吧</span></div>';
            return;
        }
        messages.forEach(m => MessageRenderer.appendMessage(container, m, this.state.userSeed));
    },

    bindSendMessage() {
        const input = document.getElementById('chat-input');
        const btn = document.getElementById('btn-send');

        const send = () => this.sendTextMessage();
        btn?.addEventListener('click', send);
        input?.addEventListener('keydown', e => {
            if (e.key === 'Enter' && e.ctrlKey) {
                e.preventDefault();
                send();
            }
        });
    },

    async sendTextMessage() {
        const input = document.getElementById('chat-input');
        const content = input.value.trim();
        if (!content || !this.state.activeSessionId) return;

        const msg = await ApiClient.post('/api/Chat/send', {
            sessionId: this.state.activeSessionId,
            content
        });
        if (msg) {
            input.value = '';
            MessageRenderer.appendMessage(
                document.getElementById('message-container'), msg, this.state.userSeed);
            this.updateSessionPreview(this.state.activeSessionId, content);
        }
    },

    async sendFileMessage(fileName, fileSize, fileUrl) {
        if (!this.state.activeSessionId) return;
        const msg = await ApiClient.post('/api/Chat/send-file', {
            sessionId: this.state.activeSessionId,
            fileName,
            fileSize,
            content: fileUrl || fileName,
            progress: 100
        });
        if (msg) {
            MessageRenderer.appendMessage(
                document.getElementById('message-container'), msg, this.state.userSeed);
            this.updateSessionPreview(this.state.activeSessionId, `[文件] ${fileName}`);
        }
    },

    updateSessionPreview(sessionId, text) {
        const item = document.querySelector(`.session-item[data-session-id="${sessionId}"]`);
        if (item) {
            item.querySelector('.last-msg').textContent = text;
            item.querySelector('.time').textContent = MessageRenderer.formatTime(new Date());
        }
    },

    async refreshSessions() {
        const sessions = await ApiClient.get('/api/Chat/sessions');
        const list = document.getElementById('session-list');
        list.innerHTML = '';
        sessions?.forEach(s => {
            const isGroup = s.type === 1 || s.type === 'Group';
            const div = document.createElement('div');
            div.className = `session-item${s.id === this.state.activeSessionId ? ' active' : ''}`;
            div.dataset.sessionId = s.id;
            div.dataset.sessionType = isGroup ? 'Group' : 'Private';
            div.dataset.targetUserId = s.targetUserId || '';
            div.dataset.title = s.title;
            div.dataset.status = s.onlineStatus;
            const avatarType = isGroup ? 'identicon' : 'bottts';
            div.innerHTML = `
                ${s.unreadCount > 0
                    ? `<div class="avatar-wrapper"><img src="https://api.dicebear.com/7.x/${avatarType}/svg?seed=${s.avatarSeed}" class="avatar" /><span class="badge">${s.unreadCount}</span></div>`
                    : `<img src="https://api.dicebear.com/7.x/${avatarType}/svg?seed=${s.avatarSeed}" class="avatar" />`}
                <div class="session-info">
                    <div class="session-top">
                        <span class="nickname">${s.title}</span>
                        <span class="time">${s.lastMessageTime ? MessageRenderer.formatTime(s.lastMessageTime) : ''}</span>
                    </div>
                    <div class="session-bottom"><span class="last-msg">${s.lastMessage || ''}</span></div>
                </div>`;
            list.appendChild(div);
        });
    }
};

document.addEventListener('DOMContentLoaded', () => ChatApp.init());
window.ChatApp = ChatApp;
