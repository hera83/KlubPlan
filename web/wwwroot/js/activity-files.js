// ─── Aktivitet "Filer"-fane ────────────────────────────────────────────────────
// Dokumenthotel med mapper, versioner og drag & drop — opfører sig så tæt på en
// mappe på computeren som muligt:
//   • Klik markerer (Ctrl/⌘ = tilføj, Shift = interval), dobbeltklik åbner.
//   • Højreklik / "⋮" giver handlingsmenu. Tastatur: Enter, F2, Delete, Ctrl+A,
//     Esc og Backspace (op til overmappen).
//   • Træk filer/mapper fra computeren ind for at uploade (mappestruktur bevares),
//     eller træk elementer ind på en mappe/brødkrumme for at flytte dem.
// Markup: Views/Activities/_FilesTab.cshtml, _FilesPanel.cshtml, _FilesModals.cshtml.
// Scriptet kan køre flere gange (async navigation i site.js) — hver rod initialiseres én gang.
(() => {
    const INTERNAL_TYPE = 'application/x-activity-files';

    // Samme format som ByteSizeFormatter.FormatDanish (C#): "35,2 MB".
    window.FvFileSize = window.FvFileSize || {
        format(bytes) {
            const units = ['B', 'KB', 'MB', 'GB', 'TB'];
            let size = Number(bytes) || 0;
            let unit = 0;
            while (size >= 1024 && unit < units.length - 1) {
                size /= 1024;
                unit++;
            }
            if (unit === 0) return `${size} B`;
            const rounded = Math.round(size * 10) / 10;
            return `${rounded.toLocaleString('da-DK', { maximumFractionDigits: 1 })} ${units[unit]}`;
        }
    };

    const escapeHtml = (text) => String(text ?? '').replace(/[&<>"']/g, (c) => ({
        '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
    })[c]);

    const plural = (count, one, many) => `${count} ${count === 1 ? one : many}`;

    const isTyping = (el) => !!el && (el.isContentEditable || ['INPUT', 'TEXTAREA', 'SELECT'].includes(el.tagName));

    const triggerDownload = (url) => {
        const a = document.createElement('a');
        a.href = url;
        a.download = '';
        document.body.appendChild(a);
        a.click();
        a.remove();
    };

    const initRoot = (root) => {
        if (root.dataset.filesInit) return;
        root.dataset.filesInit = '1';

        const ds = root.dataset;
        const activityId = ds.activityId;
        const maxBytes = Number(ds.maxBytes);
        const blocked = new Set((ds.blockedExtensions || '').split(',').filter(Boolean).map((e) => e.toLowerCase()));

        const region = root.querySelector('[data-files-region]');
        const dropzone = root.querySelector('[data-files-dropzone]');
        const dropTargetName = root.querySelector('[data-files-drop-target-name]');
        const searchInput = root.querySelector('[data-files-search]');
        const selectionBar = root.querySelector('[data-files-selection-bar]');
        const selectionCount = root.querySelector('[data-files-selection-count]');
        const fileInput = root.querySelector('[data-files-input]');
        const folderInput = root.querySelector('[data-files-folder-input]');
        const versionInput = root.querySelector('[data-files-version-input]');
        const menu = root.querySelector('[data-files-context-menu]');
        const tabCount = document.querySelector('[data-files-tab-count]');

        const nameModalEl = document.getElementById('filesNameModal');
        const moveModalEl = document.getElementById('filesMoveModal');
        const deleteModalEl = document.getElementById('filesDeleteModal');
        const versionsModalEl = document.getElementById('filesVersionsModal');

        const state = {
            folderId: ds.folderId || '',
            folderName: 'Filer',
            search: '',
            sort: { key: 'name', dir: 1 },
            selected: new Set(),
            anchorIndex: null
        };

        const token = () => root.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

        // ─── Server calls ──────────────────────────────────────────────────────
        const postForm = async (url, fields, { toast = true } = {}) => {
            const fd = new FormData();
            fd.append('__RequestVerificationToken', token());
            fd.append('ActivityId', activityId);
            Object.entries(fields).forEach(([key, value]) => {
                if (Array.isArray(value)) value.forEach((v) => fd.append(key, v));
                else if (value !== null && value !== undefined && value !== '') fd.append(key, value);
            });
            try {
                const res = await fetch(url, { method: 'POST', body: fd, headers: { 'X-Requested-With': 'fetch' } });
                const data = await res.json();
                if (toast && data.message) window.FvToast?.show(data.type || (data.success ? 'success' : 'error'), data.message);
                return data;
            } catch {
                window.FvToast?.show('error', 'Netværksfejl. Prøv igen.');
                return { success: false };
            }
        };

        const selectionFields = (keys) => {
            const fileIds = [];
            const folderIds = [];
            keys.forEach((key) => {
                const [kind, id] = key.split(':');
                (kind === 'folder' ? folderIds : fileIds).push(id);
            });
            return { FileIds: fileIds, FolderIds: folderIds };
        };

        // ─── Rows & selection ──────────────────────────────────────────────────
        const rows = () => Array.from(region.querySelectorAll('[data-files-item]'));
        const keyOf = (row) => `${row.dataset.kind}:${row.dataset.id}`;
        const rowByKey = (key) => rows().find((r) => keyOf(r) === key);

        const renderSelection = () => {
            const all = rows();
            all.forEach((row) => {
                const selected = state.selected.has(keyOf(row));
                row.classList.toggle('is-selected', selected);
                const cb = row.querySelector('[data-files-select]');
                if (cb) cb.checked = selected;
            });
            const count = state.selected.size;
            selectionBar.hidden = count === 0;
            selectionCount.textContent = count;
            const selectAll = region.querySelector('[data-files-select-all]');
            if (selectAll) {
                selectAll.checked = count > 0 && count === all.length;
                selectAll.indeterminate = count > 0 && count < all.length;
            }
        };

        const selectOnly = (row) => {
            state.selected = new Set(row ? [keyOf(row)] : []);
            state.anchorIndex = row ? rows().indexOf(row) : null;
            renderSelection();
        };

        const clearSelection = () => selectOnly(null);

        const handleRowClick = (row, e) => {
            const all = rows();
            const index = all.indexOf(row);
            if (e.shiftKey && state.anchorIndex !== null) {
                const [from, to] = [Math.min(state.anchorIndex, index), Math.max(state.anchorIndex, index)];
                if (!(e.ctrlKey || e.metaKey)) state.selected.clear();
                all.slice(from, to + 1).forEach((r) => state.selected.add(keyOf(r)));
            } else if (e.ctrlKey || e.metaKey) {
                const key = keyOf(row);
                state.selected.has(key) ? state.selected.delete(key) : state.selected.add(key);
                state.anchorIndex = index;
            } else {
                selectOnly(row);
                return;
            }
            renderSelection();
        };

        // ─── Sorting (client side; folders always first, like Explorer) ────────
        const collator = new Intl.Collator('da-DK', { numeric: true, sensitivity: 'base' });

        const applySort = () => {
            const tbody = region.querySelector('[data-files-table] tbody');
            if (!tbody) return;
            const { key, dir } = state.sort;
            const sorted = rows().sort((a, b) => {
                if (a.dataset.kind !== b.dataset.kind) return a.dataset.kind === 'folder' ? -1 : 1;
                let result;
                if (key === 'name') result = collator.compare(a.dataset.name, b.dataset.name);
                else if (key === 'modified') result = Number(a.dataset.sortModified) - Number(b.dataset.sortModified);
                else result = Number(a.dataset.sortSize) - Number(b.dataset.sortSize);
                return (result || collator.compare(a.dataset.name, b.dataset.name)) * dir;
            });
            sorted.forEach((row) => tbody.appendChild(row));
            region.querySelectorAll('[data-files-sort]').forEach((btn) => {
                const icon = btn.querySelector('i');
                const active = btn.dataset.filesSort === key;
                btn.classList.toggle('is-active', active);
                icon.className = `bi ms-1 ${active ? (dir === 1 ? 'bi-arrow-up' : 'bi-arrow-down') : ''}`;
            });
        };

        // ─── Loading a folder ──────────────────────────────────────────────────
        const updateUrl = () => {
            try {
                const url = new URL(ds.urlDetails, location.origin);
                url.searchParams.set('tab', 'filer');
                if (state.folderId) url.searchParams.set('folder', state.folderId);
                history.replaceState({ ajaxNav: true, url: url.toString() }, '', url.toString());
            } catch { /* ignore */ }
        };

        const afterPanelRender = () => {
            const panel = region.querySelector('[data-files-panel]');
            if (panel) {
                state.folderId = panel.dataset.folderId || '';
                state.folderName = panel.dataset.folderName || 'Filer';
                if (tabCount) {
                    const total = Number(panel.dataset.totalFiles || 0);
                    tabCount.textContent = total;
                    tabCount.hidden = total === 0;
                }
            }
            dropTargetName.textContent = state.folderName;
            // Keep only selections that still exist (e.g. after a rename).
            const existing = new Set(rows().map(keyOf));
            state.selected = new Set([...state.selected].filter((k) => existing.has(k)));
            state.anchorIndex = null;
            applySort();
            renderSelection();
        };

        let loadController = null;
        const loadPanel = async (folderId = state.folderId, { keepSelection = false } = {}) => {
            if (loadController) loadController.abort();
            loadController = new AbortController();
            const params = new URLSearchParams({ activityId });
            if (folderId) params.set('folderId', folderId);
            if (state.search) params.set('search', state.search);
            region.classList.add('is-loading');
            try {
                const res = await fetch(`${ds.urlPanel}?${params}`, { headers: { 'X-Requested-With': 'fetch' }, signal: loadController.signal });
                if (!res.ok) throw new Error('load failed');
                const html = await res.text();
                if (!keepSelection) state.selected.clear();
                region.innerHTML = html;
                afterPanelRender();
                updateUrl();
            } catch (err) {
                if (err.name !== 'AbortError') window.FvToast?.show('error', 'Filerne kunne ikke hentes. Prøv igen.');
            } finally {
                region.classList.remove('is-loading');
            }
        };

        const openFolder = (folderId) => {
            if (state.search) {
                state.search = '';
                searchInput.value = '';
            }
            loadPanel(folderId);
        };

        const goUp = () => {
            const crumbs = region.querySelectorAll('.files-breadcrumb [data-files-open-folder]');
            if (state.search || crumbs.length === 0) return;
            openFolder(crumbs[crumbs.length - 1].dataset.filesOpenFolder);
        };

        const openItem = (row) => {
            if (!row) return;
            if (row.dataset.kind === 'folder') {
                openFolder(row.dataset.id);
            } else if (row.dataset.preview === 'true') {
                window.open(row.dataset.openUrl, '_blank', 'noopener');
            } else {
                triggerDownload(row.dataset.downloadUrl);
            }
        };

        // ─── Actions ───────────────────────────────────────────────────────────
        const selectedKeys = () => [...state.selected];

        const describe = (keys) => {
            if (keys.length === 1) {
                const row = rowByKey(keys[0]);
                return row ? `"${row.dataset.name}"` : '1 element';
            }
            return plural(keys.length, 'element', 'elementer');
        };

        const downloadItems = (keys) => {
            if (keys.length === 0) return;
            if (keys.length === 1 && keys[0].startsWith('file:')) {
                const row = rowByKey(keys[0]);
                if (row) triggerDownload(row.dataset.downloadUrl);
                return;
            }
            const params = new URLSearchParams({ activityId });
            const fields = selectionFields(keys);
            fields.FileIds.forEach((id) => params.append('FileIds', id));
            fields.FolderIds.forEach((id) => params.append('FolderIds', id));
            window.FvToast?.show('info', 'Zip-filen gøres klar — download starter om et øjeblik.');
            triggerDownload(`${ds.urlZip}?${params}`);
        };

        // Name modal (ny mappe / omdøb mappe / omdøb fil)
        const nameForm = nameModalEl?.querySelector('[data-files-name-form]');
        const nameInput = nameModalEl?.querySelector('[data-files-name-input]');
        const nameError = nameModalEl?.querySelector('[data-files-name-error]');
        let nameMode = null;

        const uniqueFolderName = () => {
            const taken = new Set(rows().map((r) => r.dataset.name.toLowerCase()));
            let name = 'Ny mappe';
            for (let n = 2; taken.has(name.toLowerCase()); n++) name = `Ny mappe (${n})`;
            return name;
        };

        const openNameModal = (mode) => {
            if (!nameModalEl) return;
            nameMode = mode;
            const titles = {
                'create-folder': ['bi-folder-plus', 'Ny mappe'],
                'rename-folder': ['bi-pencil', 'Omdøb mappe'],
                'rename-file': ['bi-pencil', 'Omdøb fil']
            };
            const [icon, title] = titles[mode.type];
            nameModalEl.querySelector('[data-files-name-icon]').className = `bi ${icon} me-2`;
            nameModalEl.querySelector('[data-files-name-title]').textContent = title;
            nameInput.value = mode.type === 'create-folder' ? uniqueFolderName() : mode.name;
            nameInput.classList.remove('is-invalid');
            bootstrap.Modal.getOrCreateInstance(nameModalEl).show();
        };

        const onNameModalShown = () => {
            if (!nameMode) return;
            nameInput.focus();
            // Like Explorer: select the name without its extension when renaming a file.
            const dot = nameMode.type === 'rename-file' ? nameInput.value.lastIndexOf('.') : -1;
            nameInput.setSelectionRange(0, dot > 0 ? dot : nameInput.value.length);
        };

        const onNameSubmit = async (e) => {
            e.preventDefault();
            if (!nameMode) return;
            const name = nameInput.value.trim();
            if (!name) {
                nameError.textContent = 'Angiv et navn.';
                nameInput.classList.add('is-invalid');
                return;
            }
            if (/[\\/:*?"<>|]/.test(name)) {
                nameError.textContent = 'Navnet må ikke indeholde \\ / : * ? " < > |';
                nameInput.classList.add('is-invalid');
                return;
            }
            const submitBtn = nameForm.querySelector('button[type="submit"]');
            submitBtn.disabled = true;
            let data;
            if (nameMode.type === 'rename-file') {
                data = await postForm(ds.urlRenameFile, { FileId: nameMode.id, Name: name });
            } else {
                data = await postForm(ds.urlSaveFolder, {
                    FolderId: nameMode.type === 'rename-folder' ? nameMode.id : '',
                    ParentFolderId: nameMode.type === 'create-folder' ? state.folderId : '',
                    Name: name
                });
            }
            submitBtn.disabled = false;
            if (data.success) {
                const mode = nameMode;
                nameMode = null;
                bootstrap.Modal.getInstance(nameModalEl)?.hide();
                if (mode.type === 'create-folder' && data.id) state.selected = new Set([`folder:${data.id}`]);
                await loadPanel(state.folderId, { keepSelection: true });
            }
        };

        const renameItem = (row) => {
            if (!row) return;
            openNameModal({ type: row.dataset.kind === 'folder' ? 'rename-folder' : 'rename-file', id: row.dataset.id, name: row.dataset.name });
        };

        // Move modal
        let moveKeys = [];
        let moveTarget = null;

        const moveItems = async (keys, targetFolderId) => {
            const data = await postForm(ds.urlMove, { ...selectionFields(keys), TargetFolderId: targetFolderId || '' });
            if (data.success || data.type === 'warning') {
                state.selected.clear();
                await loadPanel();
            }
            return data;
        };

        const openMoveModal = async (keys) => {
            if (!moveModalEl || keys.length === 0) return;
            moveKeys = keys;
            moveTarget = null;
            const tree = moveModalEl.querySelector('[data-files-move-tree]');
            const confirmBtn = moveModalEl.querySelector('[data-files-move-confirm]');
            moveModalEl.querySelector('[data-files-move-what]').textContent = describe(keys);
            confirmBtn.disabled = true;
            tree.innerHTML = '<div class="text-center text-muted py-3"><span class="spinner-border spinner-border-sm me-2"></span>Henter mapper…</div>';
            bootstrap.Modal.getOrCreateInstance(moveModalEl).show();

            try {
                const res = await fetch(`${ds.urlFolderTree}?activityId=${activityId}`, { headers: { 'X-Requested-With': 'fetch' } });
                const folders = await res.json();
                const movingFolders = new Set(keys.filter((k) => k.startsWith('folder:')).map((k) => Number(k.split(':')[1])));

                // A folder can't be moved into itself or below itself — disable that sub tree.
                let blockedDepth = null;
                tree.innerHTML = folders.map((f) => {
                    if (blockedDepth !== null && f.depth <= blockedDepth) blockedDepth = null;
                    if (blockedDepth === null && f.id !== null && movingFolders.has(f.id)) blockedDepth = f.depth;
                    const disabled = blockedDepth !== null;
                    const isCurrent = String(f.id ?? '') === String(state.folderId);
                    const icon = f.id === null ? 'bi-folder2-open' : 'bi-folder-fill files-icon-folder';
                    return `<button type="button" class="list-group-item list-group-item-action files-folder-tree-item" role="option"
                                data-target="${f.id ?? ''}" style="--depth:${f.depth}" ${disabled ? 'disabled' : ''}>
                                <i class="bi ${icon} me-2"></i>${escapeHtml(f.name)}
                                ${isCurrent ? '<span class="badge-info ms-2">Nuværende mappe</span>' : ''}
                            </button>`;
                }).join('');
            } catch {
                tree.innerHTML = '<p class="text-danger mb-0">Mapperne kunne ikke hentes.</p>';
            }
        };

        const onMoveTreeClick = (e) => {
            const item = e.target.closest('.files-folder-tree-item');
            if (!item || item.disabled) return;
            moveModalEl.querySelectorAll('.files-folder-tree-item').forEach((i) => i.classList.toggle('active', i === item));
            moveTarget = item.dataset.target;
            moveModalEl.querySelector('[data-files-move-confirm]').disabled = false;
        };

        const onMoveConfirm = async (e) => {
            const btn = e.currentTarget;
            if (moveTarget === null) return;
            btn.disabled = true;
            const data = await moveItems(moveKeys, moveTarget);
            btn.disabled = false;
            if (data.success || data.type === 'warning') bootstrap.Modal.getInstance(moveModalEl)?.hide();
        };

        // Delete modal
        let deleteKeys = [];

        const openDeleteModal = async (keys) => {
            if (!deleteModalEl || keys.length === 0) return;
            deleteKeys = keys;
            const loading = deleteModalEl.querySelector('[data-files-delete-loading]');
            const summary = deleteModalEl.querySelector('[data-files-delete-summary]');
            const confirmBtn = deleteModalEl.querySelector('[data-files-delete-confirm]');
            deleteModalEl.querySelector('[data-files-delete-what]').textContent = describe(keys);
            loading.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Beregner hvad der slettes…';
            loading.hidden = false;
            summary.hidden = true;
            confirmBtn.disabled = true;
            bootstrap.Modal.getOrCreateInstance(deleteModalEl).show();

            const data = await postForm(ds.urlDeleteSummary, selectionFields(keys), { toast: false });
            if (data.fileCount === undefined) {
                loading.innerHTML = '<span class="text-danger">Kunne ikke beregne hvad der slettes.</span>';
                return;
            }
            deleteModalEl.querySelector('[data-files-delete-folders-wrap]').hidden = data.folderCount === 0;
            deleteModalEl.querySelector('[data-files-delete-folders]').textContent = data.folderCount;
            deleteModalEl.querySelector('[data-files-delete-files]').textContent = data.fileCount;
            deleteModalEl.querySelector('[data-files-delete-size]').textContent = data.totalSizeText;
            const older = data.versionCount - data.fileCount;
            deleteModalEl.querySelector('[data-files-delete-versions]').textContent = older > 0
                ? `Størrelsen inkluderer ${plural(older, 'tidligere version', 'tidligere versioner')}, som også slettes.`
                : '';
            loading.hidden = true;
            summary.hidden = false;
            confirmBtn.disabled = false;
            confirmBtn.focus();
        };

        const onDeleteConfirm = async (e) => {
            const btn = e.currentTarget;
            btn.disabled = true;
            const data = await postForm(ds.urlDelete, selectionFields(deleteKeys));
            btn.disabled = false;
            if (data.success) {
                bootstrap.Modal.getInstance(deleteModalEl)?.hide();
                state.selected.clear();
                await loadPanel();
            }
        };

        // Versions modal
        let versionsFileId = null;

        const loadVersions = async () => {
            const body = versionsModalEl.querySelector('[data-files-versions-body]');
            try {
                const res = await fetch(`${ds.urlVersions}?activityId=${activityId}&fileId=${versionsFileId}`, { headers: { 'X-Requested-With': 'fetch' } });
                if (!res.ok) throw new Error();
                body.innerHTML = await res.text();
            } catch {
                body.innerHTML = '<p class="text-danger mb-0">Versionerne kunne ikke hentes.</p>';
            }
        };

        const openVersionsModal = (row) => {
            if (!versionsModalEl || !row || row.dataset.kind !== 'file') return;
            versionsFileId = row.dataset.id;
            versionsModalEl.querySelector('[data-files-versions-body]').innerHTML =
                '<div class="text-center text-muted py-3"><span class="spinner-border spinner-border-sm me-2"></span>Henter versioner…</div>';
            bootstrap.Modal.getOrCreateInstance(versionsModalEl).show();
            loadVersions();
        };

        const onVersionsClick = async (e) => {
            const restoreBtn = e.target.closest('[data-files-restore]');
            if (!restoreBtn) return;
            restoreBtn.disabled = true;
            const data = await postForm(ds.urlRestore, { VersionId: restoreBtn.dataset.filesRestore });
            if (data.success) {
                await loadVersions();
                loadPanel(state.folderId, { keepSelection: true });
            } else {
                restoreBtn.disabled = false;
            }
        };

        // ─── Upload queue ──────────────────────────────────────────────────────
        const uploadPanel = root.querySelector('[data-files-upload-panel]');
        const uploadList = root.querySelector('[data-files-upload-list]');
        const uploadTitle = root.querySelector('[data-files-upload-title]');
        const uploadTotal = root.querySelector('[data-files-upload-total]');
        const queue = [];
        const batch = { total: 0, done: 0, ok: 0, versions: 0, failed: 0, bytesTotal: 0, bytesDone: 0 };
        let uploading = false;

        const updateUploadHeader = () => {
            const finished = batch.done === batch.total;
            uploadTitle.textContent = finished
                ? (batch.failed > 0 ? `Upload færdig — ${plural(batch.failed, 'fejl', 'fejl')}` : 'Upload færdig')
                : `Uploader ${Math.min(batch.done + 1, batch.total)} af ${batch.total}…`;
            const pct = batch.bytesTotal > 0 ? Math.round((batch.bytesDone / batch.bytesTotal) * 100) : (finished ? 100 : 0);
            uploadTotal.style.width = `${finished ? 100 : pct}%`;
            uploadTotal.classList.toggle('bg-danger', finished && batch.failed > 0 && batch.ok === 0);
        };

        const addUploadRow = (name, sizeBytes) => {
            const li = document.createElement('li');
            li.className = 'files-upload-item';
            li.innerHTML = `
                <i class="bi files-upload-status bi-hourglass-split text-muted"></i>
                <div class="files-upload-body">
                    <div class="files-upload-name" title="${escapeHtml(name)}">${escapeHtml(name)}</div>
                    <div class="files-upload-meta">${window.FvFileSize.format(sizeBytes)} · Venter…</div>
                    <div class="progress files-upload-progress"><div class="progress-bar" style="width:0%"></div></div>
                </div>`;
            uploadList.prepend(li);
            return li;
        };

        const setRowResult = (li, ok, text) => {
            const icon = li.querySelector('.files-upload-status');
            icon.className = `bi files-upload-status ${ok ? 'bi-check-circle-fill text-success' : 'bi-exclamation-circle-fill text-danger'}`;
            li.querySelector('.files-upload-meta').textContent = text;
            li.querySelector('.files-upload-progress')?.remove();
            li.classList.toggle('is-error', !ok);
        };

        const showUploadPanel = () => {
            uploadPanel.hidden = false;
            uploadPanel.classList.remove('is-collapsed');
        };

        const validateFile = (file) => {
            const dot = file.name.lastIndexOf('.');
            const ext = dot >= 0 ? file.name.slice(dot).toLowerCase() : '';
            if (blocked.has(ext)) return 'Programfiler og scripts kan ikke uploades';
            if (file.size > maxBytes) return `Filen er større end ${ds.maxSizeText}`;
            return null;
        };

        const uploadOne = (job) => new Promise((resolve) => {
            const { file, li } = job;
            const fd = new FormData();
            fd.append('__RequestVerificationToken', token());
            fd.append('ActivityId', activityId);
            if (job.folderId) fd.append('FolderId', job.folderId);
            if (job.relativePath) fd.append('RelativePath', job.relativePath);
            if (job.targetFileId) fd.append('TargetFileId', job.targetFileId);
            fd.append('File', file, file.name);

            const bar = li.querySelector('.progress-bar');
            li.querySelector('.files-upload-status').className = 'bi files-upload-status bi-arrow-up-circle text-primary';
            li.querySelector('.files-upload-meta').textContent = `${window.FvFileSize.format(file.size)} · Uploader…`;

            const xhr = new XMLHttpRequest();
            xhr.open('POST', ds.urlUpload);
            xhr.setRequestHeader('X-Requested-With', 'fetch');
            xhr.upload.onprogress = (e) => {
                if (!e.lengthComputable) return;
                bar.style.width = `${Math.round((e.loaded / e.total) * 100)}%`;
                batch.bytesDone = job.bytesBefore + Math.min(file.size, e.loaded);
                updateUploadHeader();
            };
            xhr.onload = () => {
                let data = null;
                try { data = JSON.parse(xhr.responseText); } catch { /* not json */ }
                if (xhr.status === 413) data = { success: false, message: `Filen er større end ${ds.maxSizeText}` };
                resolve(data || { success: false, message: 'Uventet svar fra serveren' });
            };
            xhr.onerror = () => resolve({ success: false, message: 'Netværksfejl' });
            xhr.send(fd);
        });

        const processQueue = async () => {
            if (uploading) return;
            uploading = true;
            // Sequential on purpose: files from one dropped folder must not race to create the same sub folder.
            while (queue.length > 0) {
                const job = queue.shift();
                job.bytesBefore = batch.bytesDone;
                updateUploadHeader();
                const data = await uploadOne(job);
                batch.done++;
                batch.bytesDone = job.bytesBefore + job.file.size;
                if (data.success) {
                    batch.ok++;
                    if (data.isNewVersion) batch.versions++;
                    setRowResult(job.li, true, data.isNewVersion ? `Gemt som ny version (v${data.versionNumber})` : 'Uploadet');
                } else {
                    batch.failed++;
                    setRowResult(job.li, false, (data.message || 'Fejl').replace(/^.*?: /, ''));
                }
                updateUploadHeader();
            }
            uploading = false;

            if (batch.ok > 0) {
                const versionsText = batch.versions > 0 ? ` (${batch.versions} som ny version)` : '';
                window.FvToast?.show('success', `${plural(batch.ok, 'fil', 'filer')} uploadet${versionsText}.`);
                await loadPanel(state.folderId, { keepSelection: true });
                if (versionsModalEl?.classList.contains('show') && versionsFileId) loadVersions();
            }
            if (batch.failed > 0) {
                window.FvToast?.show('error', `${plural(batch.failed, 'fil', 'filer')} kunne ikke uploades — se listen nederst til højre.`);
            }
        };

        // entries: [{ file, relativePath }]
        const enqueueUploads = (entries, { folderId = state.folderId, targetFileId = null } = {}) => {
            if (!entries.length) return;
            if (!uploading && batch.done === batch.total) {
                Object.assign(batch, { total: 0, done: 0, ok: 0, versions: 0, failed: 0, bytesTotal: 0, bytesDone: 0 });
                uploadList.innerHTML = '';
            }
            showUploadPanel();
            entries.forEach(({ file, relativePath }) => {
                const li = addUploadRow(relativePath || file.name, file.size);
                batch.total++;
                const error = validateFile(file);
                if (error) {
                    batch.done++;
                    batch.failed++;
                    setRowResult(li, false, error);
                    return;
                }
                batch.bytesTotal += file.size;
                queue.push({ file, relativePath, folderId, targetFileId, li });
            });
            updateUploadHeader();
            if (queue.length > 0) {
                processQueue();
            } else if (!uploading && batch.failed > 0) {
                window.FvToast?.show('error', `${plural(batch.failed, 'fil', 'filer')} kunne ikke uploades — se listen nederst til højre.`);
            }
        };

        // Reads dropped items incl. whole folders (keeps the folder structure via relativePath).
        const collectDropped = async (dataTransfer) => {
            const entries = Array.from(dataTransfer.items || [])
                .filter((item) => item.kind === 'file')
                .map((item) => item.webkitGetAsEntry?.())
                .filter(Boolean);
            const plainFiles = Array.from(dataTransfer.files || []);
            if (entries.length === 0) return plainFiles.map((file) => ({ file, relativePath: '' }));

            const result = [];
            const readAll = (reader) => new Promise((resolve, reject) => reader.readEntries(resolve, reject));
            const walk = async (entry, path) => {
                if (entry.isFile) {
                    const file = await new Promise((resolve, reject) => entry.file(resolve, reject));
                    result.push({ file, relativePath: path ? `${path}/${file.name}` : '' });
                } else if (entry.isDirectory) {
                    const reader = entry.createReader();
                    const dirPath = path ? `${path}/${entry.name}` : entry.name;
                    let batchEntries;
                    do {
                        batchEntries = await readAll(reader);
                        for (const child of batchEntries) await walk(child, dirPath);
                    } while (batchEntries.length > 0);
                }
            };
            try {
                for (const entry of entries) await walk(entry, '');
            } catch {
                return plainFiles.map((file) => ({ file, relativePath: '' }));
            }
            return result;
        };

        // ─── Context menu ──────────────────────────────────────────────────────
        const closeMenu = () => {
            menu.classList.remove('show');
            menu.innerHTML = '';
        };

        const buildMenuItems = (keys) => {
            if (keys.length === 0) {
                return [
                    { icon: 'bi-folder-plus', label: 'Ny mappe', run: () => openNameModal({ type: 'create-folder' }) },
                    { icon: 'bi-file-earmark-arrow-up', label: 'Upload filer', run: () => fileInput.click() },
                    { icon: 'bi-folder-symlink', label: 'Upload mappe', run: () => folderInput.click() }
                ];
            }
            if (keys.length > 1) {
                return [
                    { icon: 'bi-download', label: `Download ${keys.length} elementer (zip)`, run: () => downloadItems(keys) },
                    { icon: 'bi-folder-symlink', label: 'Flyt til…', run: () => openMoveModal(keys) },
                    { divider: true },
                    { icon: 'bi-trash', label: 'Slet', danger: true, run: () => openDeleteModal(keys) }
                ];
            }
            const row = rowByKey(keys[0]);
            if (!row) return [];
            if (row.dataset.kind === 'folder') {
                return [
                    { icon: 'bi-folder2-open', label: 'Åbn', run: () => openItem(row) },
                    { icon: 'bi-download', label: 'Download som zip', run: () => downloadItems(keys) },
                    { divider: true },
                    { icon: 'bi-pencil', label: 'Omdøb', hint: 'F2', run: () => renameItem(row) },
                    { icon: 'bi-folder-symlink', label: 'Flyt til…', run: () => openMoveModal(keys) },
                    { divider: true },
                    { icon: 'bi-trash', label: 'Slet', hint: 'Del', danger: true, run: () => openDeleteModal(keys) }
                ];
            }
            return [
                row.dataset.preview === 'true'
                    ? { icon: 'bi-box-arrow-up-right', label: 'Åbn i ny fane', run: () => openItem(row) }
                    : null,
                { icon: 'bi-download', label: 'Download', run: () => downloadItems(keys) },
                { icon: 'bi-clock-history', label: `Versioner (${row.dataset.versionCount})`, run: () => openVersionsModal(row) },
                { icon: 'bi-upload', label: 'Upload ny version…', run: () => { versionsFileId = row.dataset.id; versionInput.click(); } },
                { divider: true },
                { icon: 'bi-pencil', label: 'Omdøb', hint: 'F2', run: () => renameItem(row) },
                { icon: 'bi-folder-symlink', label: 'Flyt til…', run: () => openMoveModal(keys) },
                { divider: true },
                { icon: 'bi-trash', label: 'Slet', hint: 'Del', danger: true, run: () => openDeleteModal(keys) }
            ].filter(Boolean);
        };

        let menuActions = [];
        const openMenu = (keys, x, y) => {
            menuActions = buildMenuItems(keys);
            if (menuActions.length === 0) return;
            menu.innerHTML = menuActions.map((item, i) => item.divider
                ? '<li><hr class="dropdown-divider"></li>'
                : `<li><button type="button" class="dropdown-item${item.danger ? ' text-danger' : ''}" data-menu-index="${i}" role="menuitem">
                       <i class="bi ${item.icon} me-2"></i>${escapeHtml(item.label)}${item.hint ? `<span class="files-menu-hint">${item.hint}</span>` : ''}
                   </button></li>`).join('');
            menu.classList.add('show');
            const rect = menu.getBoundingClientRect();
            menu.style.left = `${Math.max(8, Math.min(x, window.innerWidth - rect.width - 8))}px`;
            menu.style.top = `${Math.max(8, Math.min(y, window.innerHeight - rect.height - 8))}px`;
            menu.querySelector('button')?.focus({ preventScroll: true });
        };

        menu.addEventListener('click', (e) => {
            const btn = e.target.closest('[data-menu-index]');
            if (!btn) return;
            const item = menuActions[Number(btn.dataset.menuIndex)];
            closeMenu();
            item?.run();
        });

        // ─── Event wiring ──────────────────────────────────────────────────────
        root.addEventListener('click', (e) => {
            const action = e.target.closest('[data-files-action]')?.dataset.filesAction;
            if (!action) return;
            const keys = selectedKeys();
            switch (action) {
                case 'new-folder': openNameModal({ type: 'create-folder' }); break;
                case 'upload-files': fileInput.click(); break;
                case 'upload-folder': folderInput.click(); break;
                case 'download-selection': downloadItems(keys); break;
                case 'move-selection': openMoveModal(keys); break;
                case 'delete-selection': openDeleteModal(keys); break;
                case 'clear-selection': clearSelection(); break;
            }
        });

        region.addEventListener('click', (e) => {
            const opener = e.target.closest('[data-files-open-folder]');
            if (opener) {
                e.preventDefault();
                openFolder(opener.dataset.filesOpenFolder);
                return;
            }

            const sortBtn = e.target.closest('[data-files-sort]');
            if (sortBtn) {
                const key = sortBtn.dataset.filesSort;
                state.sort = { key, dir: state.sort.key === key ? -state.sort.dir : 1 };
                applySort();
                return;
            }

            const row = e.target.closest('[data-files-item]');
            const rowAction = e.target.closest('[data-files-row-action]');
            if (row && rowAction) {
                const action = rowAction.dataset.filesRowAction;
                if (action === 'menu') {
                    if (!state.selected.has(keyOf(row))) selectOnly(row);
                    const rect = rowAction.getBoundingClientRect();
                    openMenu(selectedKeys(), rect.right - 220, rect.bottom + 4);
                } else if (action === 'versions') {
                    openVersionsModal(row);
                } else if (action === 'download') {
                    downloadItems([keyOf(row)]);
                }
                return;
            }

            if (e.target.closest('[data-files-select], [data-files-select-all], a, button')) return;

            if (row) handleRowClick(row, e);
            else if (!e.target.closest('.files-breadcrumb')) clearSelection();
        });

        region.addEventListener('change', (e) => {
            if (e.target.matches('[data-files-select-all]')) {
                state.selected = e.target.checked ? new Set(rows().map(keyOf)) : new Set();
                renderSelection();
            } else if (e.target.matches('[data-files-select]')) {
                const row = e.target.closest('[data-files-item]');
                const key = keyOf(row);
                e.target.checked ? state.selected.add(key) : state.selected.delete(key);
                state.anchorIndex = rows().indexOf(row);
                renderSelection();
            }
        });

        region.addEventListener('dblclick', (e) => {
            if (e.target.closest('a, button, input')) return;
            const row = e.target.closest('[data-files-item]');
            if (row) openItem(row);
        });

        dropzone.addEventListener('contextmenu', (e) => {
            if (e.target.closest('input, .files-breadcrumb')) return;
            e.preventDefault();
            const row = e.target.closest('[data-files-item]');
            if (row && !state.selected.has(keyOf(row))) selectOnly(row);
            if (!row) clearSelection();
            openMenu(row ? selectedKeys() : [], e.clientX, e.clientY);
        });

        let searchTimer = null;
        searchInput.addEventListener('input', () => {
            clearTimeout(searchTimer);
            searchTimer = setTimeout(() => {
                state.search = searchInput.value.trim();
                loadPanel(state.folderId);
            }, 300);
        });

        fileInput.addEventListener('change', () => {
            enqueueUploads(Array.from(fileInput.files).map((file) => ({ file, relativePath: '' })));
            fileInput.value = '';
        });
        folderInput.addEventListener('change', () => {
            enqueueUploads(Array.from(folderInput.files).map((file) => ({ file, relativePath: file.webkitRelativePath || '' })));
            folderInput.value = '';
        });
        versionInput.addEventListener('change', () => {
            const file = versionInput.files[0];
            if (file && versionsFileId) enqueueUploads([{ file, relativePath: '' }], { targetFileId: versionsFileId });
            versionInput.value = '';
        });

        uploadPanel.querySelector('[data-files-upload-toggle]').addEventListener('click', () => uploadPanel.classList.toggle('is-collapsed'));
        uploadPanel.querySelector('[data-files-upload-close]').addEventListener('click', () => {
            if (uploading) {
                uploadPanel.classList.add('is-collapsed');
                return;
            }
            uploadPanel.hidden = true;
        });

        nameForm?.addEventListener('submit', onNameSubmit);
        nameModalEl?.addEventListener('shown.bs.modal', onNameModalShown);
        nameInput?.addEventListener('input', () => nameInput.classList.remove('is-invalid'));
        moveModalEl?.querySelector('[data-files-move-tree]').addEventListener('click', onMoveTreeClick);
        moveModalEl?.querySelector('[data-files-move-tree]').addEventListener('dblclick', (e) => {
            if (e.target.closest('.files-folder-tree-item:not([disabled])')) moveModalEl.querySelector('[data-files-move-confirm]').click();
        });
        moveModalEl?.querySelector('[data-files-move-confirm]').addEventListener('click', onMoveConfirm);
        deleteModalEl?.querySelector('[data-files-delete-confirm]').addEventListener('click', onDeleteConfirm);
        versionsModalEl?.querySelector('[data-files-versions-body]').addEventListener('click', onVersionsClick);
        versionsModalEl?.querySelector('[data-files-upload-version]').addEventListener('click', () => versionInput.click());

        // ─── Drag & drop ───────────────────────────────────────────────────────
        const isExternal = (e) => Array.from(e.dataTransfer?.types || []).includes('Files');
        const isInternal = (e) => Array.from(e.dataTransfer?.types || []).includes(INTERNAL_TYPE);
        let dragKeys = [];
        let dragDepth = 0;

        const clearDropTargets = () => root.querySelectorAll('.is-drop-target').forEach((el) => el.classList.remove('is-drop-target'));

        // Valid folder drop target under the pointer, or null (e.g. a folder that is itself being dragged).
        const folderTargetOf = (e) => {
            const target = e.target.closest('[data-files-drop-folder]');
            if (!target || !region.contains(target)) return null;
            if (isInternal(e)) {
                const row = target.closest('[data-files-item]');
                if (row && dragKeys.includes(keyOf(row))) return null;
            }
            return target;
        };

        region.addEventListener('dragstart', (e) => {
            const row = e.target.closest?.('[data-files-item]');
            if (!row) return;
            if (!state.selected.has(keyOf(row))) selectOnly(row);
            dragKeys = selectedKeys();
            e.dataTransfer.effectAllowed = 'move';
            e.dataTransfer.setData(INTERNAL_TYPE, JSON.stringify(dragKeys));
            e.dataTransfer.setData('text/plain', dragKeys.length === 1 ? row.dataset.name : `${dragKeys.length} elementer`);
            dragKeys.forEach((k) => rowByKey(k)?.classList.add('is-dragging'));
        });

        region.addEventListener('dragend', () => {
            rows().forEach((r) => r.classList.remove('is-dragging'));
            clearDropTargets();
            dragKeys = [];
        });

        dropzone.addEventListener('dragenter', (e) => {
            if (!isExternal(e)) return;
            e.preventDefault();
            dragDepth++;
            dropzone.classList.add('is-dragover');
        });

        dropzone.addEventListener('dragleave', (e) => {
            if (!isExternal(e)) {
                if (!e.relatedTarget || !region.contains(e.relatedTarget)) clearDropTargets();
                return;
            }
            dragDepth = Math.max(0, dragDepth - 1);
            if (dragDepth === 0) {
                dropzone.classList.remove('is-dragover');
                clearDropTargets();
            }
        });

        dropzone.addEventListener('dragover', (e) => {
            const external = isExternal(e);
            const internal = isInternal(e);
            if (!external && !internal) return;

            const target = folderTargetOf(e);
            clearDropTargets();
            if (target) target.classList.add('is-drop-target');

            if (internal && !target) {
                e.dataTransfer.dropEffect = 'none';
                e.preventDefault();
                return;
            }
            e.preventDefault();
            e.dataTransfer.dropEffect = external ? 'copy' : 'move';
            if (external) {
                const row = target?.closest('[data-files-item]');
                dropTargetName.textContent = row ? row.dataset.name : (target ? target.textContent.trim() : state.folderName);
            }
        });

        dropzone.addEventListener('drop', async (e) => {
            const external = isExternal(e);
            const internal = isInternal(e);
            if (!external && !internal) return;
            e.preventDefault();

            const target = folderTargetOf(e);
            const targetFolderId = target ? target.dataset.filesDropFolder : state.folderId;
            dragDepth = 0;
            dropzone.classList.remove('is-dragover');
            clearDropTargets();
            dropTargetName.textContent = state.folderName;

            if (internal) {
                if (!target) return;
                let keys = dragKeys;
                try { keys = JSON.parse(e.dataTransfer.getData(INTERNAL_TYPE)); } catch { /* use dragKeys */ }
                if (String(targetFolderId) === String(state.folderId)) return;
                await moveItems(keys, targetFolderId);
                return;
            }

            // collectDropped reads the DataTransfer items before its first await — they expire after the event.
            const entries = await collectDropped(e.dataTransfer);
            enqueueUploads(entries, { folderId: targetFolderId });
        });

        // ─── Keyboard + document level (removed again when the page is navigated away) ──
        const isActive = () => root.isConnected && root.offsetParent !== null && !document.querySelector('.modal.show');

        const onKeyDown = (e) => {
            if (!root.isConnected) return cleanup();
            if (e.key === 'Escape' && menu.classList.contains('show')) {
                closeMenu();
                return;
            }
            if (!isActive() || isTyping(e.target)) return;

            const keys = selectedKeys();
            const single = keys.length === 1 ? rowByKey(keys[0]) : null;

            if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'a') {
                e.preventDefault();
                state.selected = new Set(rows().map(keyOf));
                renderSelection();
            } else if (e.key === 'Escape') {
                clearSelection();
            } else if (e.key === 'Delete' && keys.length > 0) {
                e.preventDefault();
                openDeleteModal(keys);
            } else if (e.key === 'F2' && single) {
                e.preventDefault();
                renameItem(single);
            } else if (e.key === 'Enter' && single) {
                e.preventDefault();
                openItem(single);
            } else if (e.key === 'Backspace' || (e.altKey && e.key === 'ArrowUp')) {
                e.preventDefault();
                goUp();
            } else if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
                const all = rows();
                if (all.length === 0) return;
                e.preventDefault();
                const current = single ? all.indexOf(single) : (e.key === 'ArrowDown' ? -1 : all.length);
                const next = all[Math.max(0, Math.min(all.length - 1, current + (e.key === 'ArrowDown' ? 1 : -1)))];
                selectOnly(next);
                next.scrollIntoView({ block: 'nearest' });
            }
        };

        const onDocumentClick = (e) => {
            if (!root.isConnected) return cleanup();
            if (!menu.contains(e.target) && !e.target.closest('[data-files-row-action="menu"]')) closeMenu();
        };

        // Dropping a file slightly outside the drop zone must not make the browser open the file.
        const onWindowDrag = (e) => {
            if (!root.isConnected) return cleanup();
            if (isExternal(e) && !dropzone.contains(e.target)) e.preventDefault();
        };

        const onBeforeUnload = (e) => {
            if (!root.isConnected) return cleanup();
            if (uploading) {
                e.preventDefault();
                e.returnValue = '';
            }
        };

        const onScrollOrResize = () => closeMenu();

        const cleanup = () => {
            document.removeEventListener('keydown', onKeyDown);
            document.removeEventListener('click', onDocumentClick);
            window.removeEventListener('dragover', onWindowDrag);
            window.removeEventListener('drop', onWindowDrag);
            window.removeEventListener('beforeunload', onBeforeUnload);
            window.removeEventListener('resize', onScrollOrResize);
            window.removeEventListener('scroll', onScrollOrResize, true);
        };

        document.addEventListener('keydown', onKeyDown);
        document.addEventListener('click', onDocumentClick);
        window.addEventListener('dragover', onWindowDrag);
        window.addEventListener('drop', onWindowDrag);
        window.addEventListener('beforeunload', onBeforeUnload);
        window.addEventListener('resize', onScrollOrResize);
        window.addEventListener('scroll', onScrollOrResize, true);

        afterPanelRender();
    };

    document.querySelectorAll('[data-activity-files]').forEach(initRoot);
})();
