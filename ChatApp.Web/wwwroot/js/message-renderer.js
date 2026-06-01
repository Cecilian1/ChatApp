const MessageRenderer = {
    formatTime(dateStr) {
        const d = new Date(dateStr);
        return d.toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' });
    },

    createMessageElement(msg, userSeed) {
        const wrapper = document.createElement('div');
        wrapper.className = `message-wrapper ${msg.isMine ? 'message-right' : 'message-left'}`;
        wrapper.dataset.messageId = msg.id;

        const avatar = document.createElement('img');
        avatar.className = 'avatar-sm';
        avatar.alt = '头像';
        avatar.src = `https://api.dicebear.com/7.x/adventurer/svg?seed=${msg.senderAvatarSeed || userSeed}`;

        const content = document.createElement('div');
        content.className = 'message-content';

        if (!msg.isMine) {
            const name = document.createElement('span');
            name.className = 'sender-name';
            name.textContent = msg.senderName;
            content.appendChild(name);
        }

        if (msg.type === 1 || msg.type === 'File') {
            content.appendChild(this.createFileCard(msg));
        } else {
            const bubble = document.createElement('div');
            bubble.className = 'msg-bubble';
            bubble.textContent = msg.content;
            content.appendChild(bubble);
        }

        wrapper.appendChild(avatar);
        wrapper.appendChild(content);
        return wrapper;
    },

    createFileCard(msg) {
        const card = document.createElement('div');
        card.className = 'file-card';
        card.innerHTML = `
            <div class="file-icon">📄</div>
            <div class="file-info">
                <span class="file-name">${msg.fileName || msg.content}</span>
                <span class="file-size">${msg.fileSize || ''}</span>
                ${msg.fileProgress != null && msg.fileProgress < 100
                    ? `<div class="progress-bar"><div class="progress" style="width:${msg.fileProgress}%"></div></div>`
                    : ''}
            </div>`;
        return card;
    },

    appendMessage(container, msg, userSeed) {
        const empty = container.querySelector('#chat-empty');
        if (empty) empty.remove();
        const el = this.createMessageElement(msg, userSeed);
        container.appendChild(el);
        container.scrollTop = container.scrollHeight;
        return el;
    },

    updateFileProgress(element, progress) {
        const bar = element.querySelector('.progress');
        if (bar) bar.style.width = `${progress}%`;
    }
};

window.MessageRenderer = MessageRenderer;
