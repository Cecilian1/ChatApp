const FriendManager = {
    init(app) {
        document.getElementById('btn-open-add')?.addEventListener('click', () => {
            document.getElementById('modal-add').classList.add('show');
        });

        document.querySelectorAll('[data-close]').forEach(btn => {
            btn.addEventListener('click', () => {
                document.getElementById(btn.dataset.close).classList.remove('show');
            });
        });

        document.getElementById('tab-add-friend')?.addEventListener('click', () => {
            document.getElementById('add-friend-panel').classList.remove('hidden');
            document.getElementById('create-group-panel').classList.add('hidden');
            document.getElementById('tab-add-friend').className = 'btn-sm btn-sm-primary';
            document.getElementById('tab-create-group').className = 'btn-sm btn-sm-outline';
        });

        document.getElementById('tab-create-group')?.addEventListener('click', () => {
            document.getElementById('create-group-panel').classList.remove('hidden');
            document.getElementById('add-friend-panel').classList.add('hidden');
            document.getElementById('tab-create-group').className = 'btn-sm btn-sm-primary';
            document.getElementById('tab-add-friend').className = 'btn-sm btn-sm-outline';
        });

        document.getElementById('btn-search-user')?.addEventListener('click', () => this.searchUsers());
        document.getElementById('search-user-input')?.addEventListener('keydown', e => {
            if (e.key === 'Enter') this.searchUsers();
        });

        document.getElementById('btn-create-group')?.addEventListener('click', () => this.createGroup(app));

        document.getElementById('request-list')?.addEventListener('click', async e => {
            const card = e.target.closest('.request-card');
            if (!card) return;
            const id = card.dataset.requestId;
            if (e.target.classList.contains('btn-accept')) {
                await ApiClient.post('/api/Friend/accept', { requestId: id });
                card.remove();
                this.ensureRequestEmptyState();
                app.syncRequestsBadge();
                app.refreshSessions();
            } else if (e.target.classList.contains('btn-reject')) {
                await ApiClient.post('/api/Friend/reject', { requestId: id });
                card.remove();
                this.ensureRequestEmptyState();
                app.syncRequestsBadge();
            }
        });

        // Delete from chat header button
        document.getElementById('btn-delete-friend')?.addEventListener('click', () => {
            const sessionEl = document.querySelector('.session-item.active');
            const friendId = sessionEl?.dataset.targetUserId;
            const friendName = sessionEl?.dataset.title || '该好友';
            if (!friendId) return;
            this.openDeleteConfirm(friendId, friendName, app);
        });

        // Delete from contact list button
        document.getElementById('contact-list')?.addEventListener('click', e => {
            const btn = e.target.closest('.btn-delete-contact');
            if (!btn) return;
            const item = btn.closest('.contact-item');
            const friendId = item?.dataset.friendId;
            const friendName = item?.dataset.friendName || '该好友';
            if (!friendId) return;
            this.openDeleteConfirm(friendId, friendName, app);
        });

        document.getElementById('btn-confirm-delete-friend')?.addEventListener('click', async () => {
            const friendId = document.getElementById('btn-confirm-delete-friend').dataset.friendId;
            if (!friendId) return;
            const res = await ApiClient.del(`/api/Friend/${friendId}`);
            document.getElementById('modal-delete-friend').classList.remove('show');
            if (res?.success !== false) {
                // Remove from contact list
                document.querySelector(`#contact-list .contact-item[data-friend-id="${friendId}"]`)?.remove();
                // If this friend's session is active, clear the chat area
                const activeSession = document.querySelector('.session-item.active');
                if (activeSession?.dataset.targetUserId === friendId) {
                    document.getElementById('chat-header').style.display = 'none';
                    document.getElementById('chat-footer').style.display = 'none';
                    document.getElementById('message-container').innerHTML =
                        '<div class="empty-state" id="chat-empty"><span class="empty-state-icon">💬</span><span>选择一个会话开始聊天</span></div>';
                    app.state.activeSessionId = null;
                }
                await Promise.all([app.refreshSessions(), app.refreshContacts()]);
            }
        });

        document.getElementById('contact-search')?.addEventListener('input', e => {
            const q = e.target.value.toLowerCase();
            document.querySelectorAll('#contact-list .contact-item').forEach(item => {
                const name = item.querySelector('.nickname')?.textContent.toLowerCase() || '';
                item.style.display = name.includes(q) ? '' : 'none';
            });
        });
    },

    openDeleteConfirm(friendId, friendName, app) {
        document.getElementById('delete-friend-name').textContent = friendName;
        document.getElementById('btn-confirm-delete-friend').dataset.friendId = friendId;
        document.getElementById('modal-delete-friend').classList.add('show');
    },

    ensureRequestEmptyState() {
        const list = document.getElementById('request-list');
        if (!list || list.querySelector('.request-card')) return;
        list.innerHTML = '<div class="empty-state"><span>暂无待处理的好友申请</span></div>';
    },

    async searchUsers() {
        const keyword = document.getElementById('search-user-input').value.trim();
        const users = await ApiClient.get(`/api/Friend/search?keyword=${encodeURIComponent(keyword)}`);
        const container = document.getElementById('search-results');
        container.innerHTML = '';
        if (!users?.length) {
            container.innerHTML = '<p style="color:#999;font-size:13px;">未找到用户</p>';
            return;
        }
        users.forEach(u => {
            const div = document.createElement('div');
            div.className = 'search-result-item';
            div.innerHTML = `<span>${u.nickname} (${u.username})</span>`;
            const btn = document.createElement('button');
            btn.className = 'btn-sm btn-sm-primary';
            btn.textContent = '添加';
            btn.onclick = async () => {
            const res = await ApiClient.post('/api/Friend/send-friend-request', { targetUserId: u.id });
                btn.textContent = res?.success ? '已发送' : (res?.error || '失败');
                btn.disabled = true;
            };
            div.appendChild(btn);
            container.appendChild(div);
        });
    },

    async createGroup(app) {
        const name = document.getElementById('group-name-input').value.trim();
        const memberIds = [...document.querySelectorAll('#group-members input:checked')].map(c => c.value);
        const res = await ApiClient.post('/api/Group', { name, memberIds });
        if (res?.id) {
            document.getElementById('modal-add').classList.remove('show');
            await app.refreshSessions();
        } else {
            alert(res?.error || '创建失败');
        }
    }
};

window.FriendManager = FriendManager;
