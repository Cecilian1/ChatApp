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

        this.bindNavTabs();
        this.bindSessionList();
        this.bindSendMessage();
        FriendManager.init(this);
        HistoryManager.init(this);
        FileUploadManager.init((fileName, fileSize) => this.sendFileMessage(fileName, fileSize));

        if (this.state.activeSessionId) {
            this.updateDeleteFriendBtn();
            this.startMockReply();
        }
    },

    bindNavTabs() {
        document.querySelectorAll('.nav-item[data-tab]').forEach(btn => {
            btn.addEventListener('click', () => {
                const tab = btn.dataset.tab;
                document.querySelectorAll('.nav-item[data-tab]').forEach(b => b.classList.remove('active'));
                btn.classList.add('active');

                document.querySelectorAll('.list-panel').forEach(p => p.classList.remove('active'));
                document.getElementById(`panel-${tab}`)?.classList.add('active');

                document.querySelectorAll('.main-view').forEach(v => v.classList.remove('active'));
                document.getElementById(`view-${tab}`)?.classList.add('active');

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

        this.state.activeSessionId = item.dataset.sessionId;
        document.getElementById('chat-header').style.display = '';
        document.getElementById('chat-footer').style.display = '';
        document.getElementById('chat-title').textContent = item.dataset.title;
        document.getElementById('chat-status').textContent = item.dataset.status;

        this.updateDeleteFriendBtn();
        await this.loadMessages(this.state.activeSessionId);
        this.startMockReply();
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

    async sendFileMessage(fileName, fileSize) {
        if (!this.state.activeSessionId) return;
        const msg = await ApiClient.post('/api/Chat/send-file', {
            sessionId: this.state.activeSessionId,
            fileName,
            fileSize,
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
    },

    startMockReply() {
        if (this.state.mockReplyTimer) clearInterval(this.state.mockReplyTimer);
        this.state.mockReplyTimer = setInterval(async () => {
            if (!this.state.activeSessionId) return;
            const item = document.querySelector('.session-item.active');
            if (!item || item.dataset.sessionType !== 'Private') return;

            const replies = ['收到，我继续完善前端。', '好的，没问题。', '稍后发你。'];
            const content = replies[Math.floor(Math.random() * replies.length)];
            const sessionId = this.state.activeSessionId;

            const messages = await ApiClient.get(`/api/Chat/messages/${sessionId}`);
            const last = messages?.[messages.length - 1];
            if (last?.isMine) {
                const fakeReply = {
                    id: 'mock-' + Date.now(),
                    sessionId,
                    senderName: item.dataset.title,
                    senderAvatarSeed: 'Peter',
                    type: 0,
                    content,
                    isMine: false,
                    sentAt: new Date().toISOString()
                };
                MessageRenderer.appendMessage(
                    document.getElementById('message-container'), fakeReply, this.state.userSeed);
                this.updateSessionPreview(sessionId, content);
            }
        }, 15000);
    }
};

document.addEventListener('DOMContentLoaded', () => ChatApp.init());
window.ChatApp = ChatApp;
