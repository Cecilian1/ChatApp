const ApiClient = {
    async get(url) {
        const res = await fetch(url);
        if (res.status === 401) { window.location.href = '/Account/Login'; return null; }
        return res.json();
    },
    async post(url, body) {
        const res = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        });
        if (res.status === 401) { window.location.href = '/Account/Login'; return null; }
        return res.json();
    },
    async del(url) {
        const res = await fetch(url, { method: 'DELETE' });
        if (res.status === 401) { window.location.href = '/Account/Login'; return null; }
        return res.json();
    },
    async uploadFile(file) {
        const form = new FormData();
        form.append('file', file);
        const res = await fetch('/api/File/upload', { method: 'POST', body: form });
        if (res.status === 401) { window.location.href = '/Account/Login'; return null; }
        return res.json();
    }
};

window.ApiClient = ApiClient;
