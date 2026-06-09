const FileUploadManager = {
    init(onUploaded) {
        const btn = document.getElementById('btn-file');
        const input = document.getElementById('file-input');
        if (!btn || !input) return;

        btn.addEventListener('click', () => input.click());
        input.addEventListener('change', async () => {
            const file = input.files[0];
            if (!file) return;
            input.value = '';

            const size = file.size < 1024 * 1024
                ? `${(file.size / 1024).toFixed(1)} KB`
                : `${(file.size / 1024 / 1024).toFixed(1)} MB`;

            const tempMsg = {
                id: 'temp-' + Date.now(),
                type: 1,
                fileName: file.name,
                fileSize: size,
                fileProgress: 0,
                isMine: true,
                senderAvatarSeed: document.getElementById('im-app').dataset.userSeed
            };

            const container = document.getElementById('message-container');
            const el = MessageRenderer.appendMessage(container, tempMsg, tempMsg.senderAvatarSeed);

            let progress = 0;
            const timer = setInterval(() => {
                progress += 20;
                MessageRenderer.updateFileProgress(el, progress);
                if (progress >= 100) clearInterval(timer);
            }, 200);

            const result = await ApiClient.uploadFile(file);
            if (result?.success && onUploaded) {
                await onUploaded(result.fileName, result.fileSize, result.url);
                el.remove();
            }
        });
    }
};

window.FileUploadManager = FileUploadManager;
