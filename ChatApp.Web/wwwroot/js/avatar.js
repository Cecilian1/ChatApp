const AvatarUtil = {
    apiBase: '',

    init(apiBase) {
        this.apiBase = (apiBase || '').replace(/\/$/, '');
    },

    url(seed, isGroup = false) {
        const normalized = (seed || 'default').trim();
        const group = isGroup ? 'true' : 'false';
        return `${this.apiBase}/api/avatar/svg?seed=${encodeURIComponent(normalized)}&group=${group}`;
    }
};

window.AvatarUtil = AvatarUtil;
