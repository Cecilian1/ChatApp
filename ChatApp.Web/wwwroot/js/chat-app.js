const ChatApp = {
    state: {
        activeSessionId: null,
        userId: null,
        userSeed: null,
        mockReplyTimer: null,
        currentTab: 'chat'
    },

    async init() {
        const app = document.getElementById('im-app');
        if (!app) return;

        this.state.userId = app.dataset.userId;
        this.state.userSeed = app.dataset.userSeed;
        this.state.activeSessionId = app.dataset.activeSession || null;

        const apiBase = app.dataset.apiBase || '';
        if (apiBase && typeof AvatarUtil !== 'undefined') {
            AvatarUtil.init(apiBase);
        }

        ChatApp.onReceiveMessage = (msg) => this.onReceiveMessage(msg);
        ChatApp.onReceiveFriendRequest = (req) => this.onReceiveFriendRequest(req);

        if (typeof SignalRClient !== 'undefined') {
            try {
                await SignalRClient.init();
            } catch {
                // Realtime unavailable; HTTP chat still works
            }
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
        }

        this.initSessionUnreadCounts();
        this.syncRequestsBadge();
        this.syncChatBadge();
    },

    isViewingSession(sessionId) {
        return this.state.currentTab === 'chat'
            && String(sessionId) === String(this.state.activeSessionId);
    },

    onReceiveFriendRequest(req) {
        if (req.toUserId && req.toUserId !== this.state.userId) return;
        this.appendRequestCard(req);
        this.syncRequestsBadge();
    },

    appendRequestCard(req) {
        const list = document.getElementById('request-list');
        if (!list) return;
        if (list.querySelector(`[data-request-id="${req.id}"]`)) return;

        list.querySelector('.empty-state')?.remove();
        const card = document.createElement('div');
        card.className = 'request-card';
        card.dataset.requestId = req.id;
        card.innerHTML = `
            <span><strong>${req.fromNickname}</strong> (${req.fromUsername}) 请求添加您为好友</span>
            <div class="request-actions">
                <button class="btn-sm btn-sm-primary btn-accept">同意</button>
                <button class="btn-sm btn-sm-danger btn-reject">拒绝</button>
            </div>`;
        list.appendChild(card);
    },

    getPendingRequestCount() {
        return document.querySelectorAll('#request-list .request-card').length;
    },

    syncRequestsBadge() {
        const count = this.state.currentTab === 'requests' ? 0 : this.getPendingRequestCount();
        this.updateRequestsBadge(count);
    },

    updateRequestsBadge(count) {
        const wrapper = document.querySelector('.nav-item-wrapper[data-nav="requests"]');
        if (!wrapper) return;

        let badge = document.getElementById('requests-badge');
        if (count <= 0) {
            badge?.remove();
            return;
        }

        if (!badge) {
            badge = document.createElement('span');
            badge.id = 'requests-badge';
            badge.className = 'nav-badge';
            wrapper.appendChild(badge);
        }
        badge.textContent = count > 99 ? '99+' : count;
    },

    getTotalUnreadCount() {
        return [...document.querySelectorAll('.session-item')]
            .reduce((sum, item) => sum + (parseInt(item.dataset.unreadCount || '0', 10) || 0), 0);
    },

    syncChatBadge() {
        const count = this.getTotalUnreadCount();
        this.updateChatBadge(count);
    },

    updateChatBadge(count) {
        const wrapper = document.querySelector('.nav-item-wrapper[data-nav="chat"]');
        if (!wrapper) return;

        let badge = document.getElementById('chat-badge');
        if (count <= 0) {
            badge?.remove();
            return;
        }

        if (!badge) {
            badge = document.createElement('span');
            badge.id = 'chat-badge';
            badge.className = 'nav-badge';
            wrapper.appendChild(badge);
        }
        badge.textContent = count > 99 ? '99+' : count;
    },

    async loadPendingRequests() {
        const requests = await ApiClient.get('/api/Friend/requests/pending');
        const list = document.getElementById('request-list');
        if (!list) return;

        if (!requests?.length) {
            list.innerHTML = '<div class="empty-state"><span>暂无待处理的好友申请</span></div>';
            return;
        }

        list.innerHTML = requests.map(r => `
            <div class="request-card" data-request-id="${r.id}">
                <span><strong>${r.fromNickname}</strong> (${r.fromUsername}) 请求添加您为好友</span>
                <div class="request-actions">
                    <button class="btn-sm btn-sm-primary btn-accept">同意</button>
                    <button class="btn-sm btn-sm-danger btn-reject">拒绝</button>
                </div>
            </div>`).join('');
    },

    joinAllConversations() {
        // Web hub relays API events via user group; no per-conversation join needed.
    },

    initSessionUnreadCounts() {
        document.querySelectorAll('.session-item').forEach(item => {
            const badge = item.querySelector('.badge');
            item.dataset.unreadCount = badge ? (parseInt(badge.textContent, 10) || 0) : '0';
        });
    },

    incrementSessionUnread(sessionId) {
        const item = document.querySelector(`.session-item[data-session-id="${sessionId}"]`);
        if (!item) {
            this.refreshSessions().then(() => this.syncChatBadge());
            return;
        }

        const count = (parseInt(item.dataset.unreadCount || '0', 10) || 0) + 1;
        item.dataset.unreadCount = count;

        let avatar = item.querySelector('.avatar-wrapper img, img.avatar');
        if (!avatar) return;

        let wrapper = item.querySelector('.avatar-wrapper');
        if (!wrapper) {
            wrapper = document.createElement('div');
            wrapper.className = 'avatar-wrapper';
            avatar.replaceWith(wrapper);
            wrapper.appendChild(avatar);
        }

        let badge = wrapper.querySelector('.badge');
        if (!badge) {
            badge = document.createElement('span');
            badge.className = 'badge';
            wrapper.appendChild(badge);
        }
        badge.textContent = count > 99 ? '99+' : count;
        this.syncChatBadge();
    },

    clearSessionUnread(item) {
        item.dataset.unreadCount = '0';
        const wrapper = item.querySelector('.avatar-wrapper');
        if (!wrapper) return;
        const img = wrapper.querySelector('img');
        if (img) {
            const clone = img.cloneNode(true);
            wrapper.replaceWith(clone);
        }
        this.syncChatBadge();
    },

    onReceiveMessage(msg) {
        const sid = msg.sessionId || msg.conversationId;
        const preview = msg.type === 1 || msg.type === 'File' ? `[文件] ${msg.fileName}` : msg.content;
        const sentAt = msg.sentAt || new Date().toISOString();

        if (String(msg.senderId) === String(this.state.userId)) {
            this.updateSessionPreview(sid, preview, sentAt);
            return;
        }

        if (this.isViewingSession(sid)) {
            MessageRenderer.appendMessage(
                document.getElementById('message-container'), msg, this.state.userSeed);
        } else {
            this.incrementSessionUnread(sid);
        }

        this.updateSessionPreview(sid, preview, sentAt);
    },

    bindNavTabs() {
        document.querySelectorAll('.nav-item[data-tab]').forEach(btn => {
            btn.addEventListener('click', async () => {
                const tab = btn.dataset.tab;
                this.state.currentTab = tab;
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
                if (tab === 'requests') await this.loadPendingRequests();
                this.syncRequestsBadge();
                this.syncChatBadge();
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

        this.clearSessionUnread(item);

        this.updateDeleteFriendBtn();
        this.updateGroupMembersBtn();
        await this.loadMessages(sessionId);
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
                    <img src="${AvatarUtil.url(m.avatarSeed)}" class="avatar" alt="" />
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
            preview.src = AvatarUtil.url(seed);
        });

        document.getElementById('btn-random-seed')?.addEventListener('click', () => {
            const seed = Math.random().toString(36).slice(2, 10);
            seedInput.value = seed;
            preview.src = AvatarUtil.url(seed);
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
                if (navAvatar) navAvatar.src = AvatarUtil.url(newSeed);

                document.querySelectorAll('.message-right .avatar-sm').forEach(img => {
                    img.src = AvatarUtil.url(newSeed);
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
            this.updateSessionPreview(this.state.activeSessionId, content, msg.sentAt);
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
            this.updateSessionPreview(this.state.activeSessionId, `[文件] ${fileName}`, msg.sentAt);
        }
    },

    updateSessionPreview(sessionId, text, sentAt) {
        const item = document.querySelector(`.session-item[data-session-id="${sessionId}"]`);
        if (item) {
            item.querySelector('.last-msg').textContent = text;
            item.querySelector('.time').textContent = MessageRenderer.formatTime(sentAt || new Date().toISOString());
        }
    },

    async refreshContacts() {
        const friends = await ApiClient.get('/api/Friend');
        const list = document.getElementById('contact-list');
        if (!list || !friends) return;
        list.innerHTML = '';
        friends.forEach(f => {
            const div = document.createElement('div');
            div.className = 'contact-item';
            div.dataset.friendId = f.userId;
            div.dataset.friendName = f.nickname;
            div.innerHTML = `
                <img src="${AvatarUtil.url(f.avatarSeed)}" alt="" class="avatar" />
                <div class="contact-info">
                    <div class="contact-top">
                        <span class="nickname">${f.nickname}</span>
                        <span class="time">${f.onlineStatus || ''}</span>
                    </div>
                </div>
                <button class="btn-delete-contact" title="删除好友">🗑️</button>`;
            list.appendChild(div);
        });
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
            div.dataset.unreadCount = s.id === this.state.activeSessionId ? '0' : (s.unreadCount || 0);
            const unread = s.id === this.state.activeSessionId ? 0 : (s.unreadCount || 0);
            div.innerHTML = `
                ${unread > 0
                ? `<div class="avatar-wrapper"><img src="${AvatarUtil.url(s.avatarSeed, isGroup)}" class="avatar" /><span class="badge">${unread > 99 ? '99+' : unread}</span></div>`
                : `<img src="${AvatarUtil.url(s.avatarSeed, isGroup)}" class="avatar" />`}
                <div class="session-info">
                    <div class="session-top">
                        <span class="nickname">${s.title}</span>
                        <span class="time">${s.lastMessageTime ? MessageRenderer.formatTime(s.lastMessageTime) : ''}</span>
                    </div>
                    <div class="session-bottom"><span class="last-msg">${s.lastMessage || ''}</span></div>
                </div>`;
            list.appendChild(div);
        });
        this.initSessionUnreadCounts();
        this.syncChatBadge();
    }
};

document.addEventListener('DOMContentLoaded', () => ChatApp.init());
window.ChatApp = ChatApp;