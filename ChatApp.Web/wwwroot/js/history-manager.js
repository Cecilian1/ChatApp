const HistoryManager = {
    init(app) {
        document.getElementById('btn-history-search')?.addEventListener('click', () => this.search());
        document.getElementById('history-keyword')?.addEventListener('keydown', e => {
            if (e.key === 'Enter') this.search();
        });
    },

    async search() {
        const keyword = document.getElementById('history-keyword').value.trim();
        const timeRange = document.getElementById('history-range').value;
        const params = new URLSearchParams();
        if (keyword) params.set('keyword', keyword);
        if (timeRange) params.set('timeRange', timeRange);

        const messages = await ApiClient.get(`/api/Chat/history?${params}`);
        this.renderList(messages || []);
        this.renderDetail(messages || []);
    },

    renderList(messages) {
        const list = document.getElementById('history-list');
        list.innerHTML = '';
        messages.forEach(m => {
            const item = document.createElement('div');
            item.className = 'history-item';
            item.dataset.messageId = m.id;
            item.innerHTML = `
                <div class="history-info">
                    <div class="history-top">
                        <span class="nickname">${m.senderName}</span>
                        <span class="time">${MessageRenderer.formatTime(m.sentAt)}</span>
                    </div>
                    <span class="history-preview">${m.type === 1 || m.type === 'File' ? '[文件] ' + (m.fileName || m.content) : m.content}</span>
                </div>`;
            item.addEventListener('click', () => this.highlightMessage(m.id));
            list.appendChild(item);
        });
    },

    renderDetail(messages) {
        const detail = document.getElementById('history-detail');
        detail.innerHTML = '';
        if (!messages.length) {
            detail.innerHTML = '<div class="empty-state"><span>暂无历史消息</span></div>';
            return;
        }
        messages.forEach(m => {
            const row = document.createElement('div');
            row.className = 'history-row';
            row.dataset.messageId = m.id;
            row.innerHTML = `
                <div>
                    <strong>${m.senderName}</strong>
                    <span style="color:#999;font-size:12px;margin-left:8px;">${new Date(m.sentAt).toLocaleString('zh-CN')}</span>
                    <p style="margin-top:4px;font-size:13px;">${m.type === 1 || m.type === 'File' ? '[文件] ' + (m.fileName || m.content) : m.content}</p>
                </div>
                <button class="btn-sm btn-sm-danger btn-del-history" data-id="${m.id}">删除</button>`;
            detail.appendChild(row);
        });

        detail.querySelectorAll('.btn-del-history').forEach(btn => {
            btn.addEventListener('click', async () => {
                if (!confirm('确定删除该消息？')) return;
                await ApiClient.del(`/api/Chat/message/${btn.dataset.id}`);
                btn.closest('.history-row').remove();
            });
        });
    },

    highlightMessage(id) {
        document.querySelectorAll('.history-row').forEach(r => {
            r.style.background = r.dataset.messageId === id ? '#f0f7ff' : '';
        });
    }
};

window.HistoryManager = HistoryManager;
