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
                app.refreshSessions();
            } else if (e.target.classList.contains('btn-reject')) {
                await ApiClient.post('/api/Friend/reject', { requestId: id });
                card.remove();
            }
        });

        document.getElementById('contacts-detail')?.addEventListener('click', async e => {
            const btn = e.target.closest('.btn-remove-friend');
            if (!btn) return;
            if (!confirm('确定删除该好友？')) return;
            const res = await ApiClient.del(`/api/Friend/${btn.dataset.friendId}`);
            if (res?.success) {
                btn.closest('.contact-item').remove();
                app.refreshSessions();
            }
        });

        document.getElementById('btn-delete-friend')?.addEventListener('click', async () => {
            const sessionEl = document.querySelector('.session-item.active');
            const friendId = sessionEl?.dataset.targetUserId;
            if (!friendId || !confirm('确定删除该好友？')) return;
            const res = await ApiClient.del(`/api/Friend/${friendId}`);
            if (res?.success) app.refreshSessions();
        });
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
                const res = await ApiClient.post('/api/Friend/request', { targetUserId: u.id });
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
