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
        this.bindEmojiPicker();
        this.bindGroupMembers();
        this.bindProfileModal();
        this.bindLogout();
        this.bindModalClose();
        FriendManager.init(this);
        HistoryManager.init(this);
        FileUploadManager.init((fileName, fileSizeBytes, fileUrl) => this.sendFileMessage(fileName, fileSizeBytes, fileUrl));

        if (this.state.activeSessionId) {
            this.updateDeleteFriendBtn();
            this.updateGroupMembersBtn();
            SignalRClient.joinConversation(this.state.activeSessionId);
        }

        window.ChatApp.onReceiveMessage = (msg) => this.onReceiveMessage(msg);
    },

    onReceiveMessage(msg) {
        const sid = msg.sessionId || msg.conversationId;
        const preview = msg.type === 1 || msg.type === 'File' ? `[文件] ${msg.fileName}` : msg.content;
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

        // Clear unread badge
        const wrapper = item.querySelector('.avatar-wrapper');
        if (wrapper) {
            const img = wrapper.querySelector('img');
            if (img) {
                const clone = img.cloneNode(true);
                wrapper.replaceWith(clone);
            }
        }

        this.updateDeleteFriendBtn();
        this.updateGroupMembersBtn();
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

    updateGroupMembersBtn() {
        const item = document.querySelector('.session-item.active');
        const btn = document.getElementById('btn-view-members');
        if (item?.dataset.sessionType === 'Group') {
            btn.classList.remove('hidden');
        } else {
            btn.classList.add('hidden');
        }
    },

    bindModalClose() {
        document.querySelectorAll('[data-close]').forEach(btn => {
            btn.addEventListener('click', () => {
                const id = btn.dataset.close;
                document.getElementById(id)?.classList.remove('show');
            });
        });
        document.querySelectorAll('.modal-overlay').forEach(overlay => {
            overlay.addEventListener('click', e => {
                if (e.target === overlay) overlay.classList.remove('show');
            });
        });
    },

    bindEmojiPicker() {
        const emojis = [
            '😀','😂','😍','😎','😊','😭','😅','🤣','😆','😋',
            '👍','👎','🙏','🤝','💪','❤️','🎉','🔥','💯','✅',
            '😤','😡','🥰','🤔','😴','🤗','🤩','😱','🥺','😏',
            '👋','🤦','🤷','💀','🫡','😇','🫶','🙄','😬','🥳',
            '🐶','🐱','🐸','🦄','🌈','⭐','🍕','🎮','🚀','💎'
        ];
        const picker = document.getElementById('emoji-picker');
        if (!picker) return;
        picker.innerHTML = emojis.map(e => `<span class="emoji-item">${e}</span>`).join('');

        const btn = document.getElementById('btn-emoji');
        const input = document.getElementById('chat-input');
        btn?.addEventListener('click', e => {
            e.stopPropagation();
            picker.classList.toggle('hidden');
        });
        picker.addEventListener('click', e => {
            const item = e.target.closest('.emoji-item');
            if (!item || !input) return;
            const start = input.selectionStart;
            const end = input.selectionEnd;
            input.value = input.value.slice(0, start) + item.textContent + input.value.slice(end);
            input.selectionStart = input.selectionEnd = start + item.textContent.length;
            input.focus();
        });
        document.addEventListener('click', e => {
            if (!picker.contains(e.target) && e.target !== btn) {
                picker.classList.add('hidden');
            }
        });
    },

    bindGroupMembers() {
        document.getElementById('btn-view-members')?.addEventListener('click', async () => {
            const item = document.querySelector('.session-item.active');
            const groupId = item?.dataset.groupId;
            if (!groupId) return;
            const list = document.getElementById('members-list');
            list.innerHTML = '<div style="text-align:center;padding:16px;color:var(--text-muted)">加载中...</div>';
            document.getElementById('modal-members').classList.add('show');
            const members = await ApiClient.get(`/api/Group/${groupId}/members`);
            if (!members?.length) {
                list.innerHTML = '<div style="text-align:center;padding:16px;color:var(--text-muted)">暂无成员数据</div>';
                return;
            }
            list.innerHTML = members.map(m => `
                <div class="member-item">
                    <img src="https://api.dicebear.com/7.x/adventurer/svg?seed=${m.avatarSeed}" class="avatar" alt="" />
                    <span class="member-name">${m.nickname}${m.isCreator ? ' <span class="member-badge">群主</span>' : ''}</span>
                </div>`).join('');
        });
    },

    bindLogout() {
        document.getElementById('btn-logout')?.addEventListener('click', () => {
            document.getElementById('modal-logout').classList.add('show');
        });
        document.getElementById('btn-confirm-logout')?.addEventListener('click', () => {
            document.getElementById('logout-form').submit();
        });
    },

    bindProfileModal() {
        document.getElementById('btn-open-profile')?.addEventListener('click', () => {
            document.getElementById('modal-profile').classList.add('show');
        });

        const seedInput = document.getElementById('profile-avatar-seed');
        const preview = document.getElementById('profile-avatar-preview');
        seedInput?.addEventListener('input', () => {
            const seed = seedInput.value.trim() || 'default';
            preview.src = `https://api.dicebear.com/7.x/adventurer/svg?seed=${encodeURIComponent(seed)}`;
        });

        document.getElementById('btn-random-seed')?.addEventListener('click', () => {
            const seed = Math.random().toString(36).slice(2, 10);
            seedInput.value = seed;
            preview.src = `https://api.dicebear.com/7.x/adventurer/svg?seed=${seed}`;
        });

        document.getElementById('btn-save-profile')?.addEventListener('click', async () => {
            const nickname = document.getElementById('profile-nickname').value.trim();
            const avatarSeed = seedInput.value.trim();
            if (!nickname) { alert('昵称不能为空'); return; }
            const result = await ApiClient.put('/api/User/profile', { nickname, avatarSeed });
            if (result) {
                document.getElementById('modal-profile').classList.remove('show');
                const newSeed = result.avatarSeed || this.state.userSeed;
                this.state.userSeed = newSeed;

                // Nav sidebar avatar
                const navAvatar = document.querySelector('.user-avatar-wrapper img');
                if (navAvatar) navAvatar.src = `https://api.dicebear.com/7.x/adventurer/svg?seed=${newSeed}`;

                // Already-rendered own message bubbles
                document.querySelectorAll('.message-right .avatar-sm').forEach(img => {
                    img.src = `https://api.dicebear.com/7.x/adventurer/svg?seed=${newSeed}`;
                });
            }
        });
    },

    async loadMessages(sessionId) {
        const messages = await ApiClient.get(`/api/Chat/messages/${sessionId}`);
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
    
    async sendFileMessage(fileName, fileSizeBytes, fileUrl) {
        if (!this.state.activeSessionId) return;
        const msg = await ApiClient.post('/api/Chat/send-file', {
            sessionId: this.state.activeSessionId,
            fileName: fileName,
            fileSizeBytes: fileSizeBytes,
            content: fileUrl
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
            div.dataset.groupId = s.groupId || '';
            div.dataset.title = s.title;
            div.dataset.status = s.onlineStatus;
            const avatarType = isGroup ? 'identicon' : 'adventurer';
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