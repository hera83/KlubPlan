// ─── Labels (Værktøjer) ───────────────────────────────────────────────────────
// Tabel med label-samlinger (data-table.js står for søgning/paginering) + modals: opret/rediger,
// slet og print. Label-rækkerne og "Print labels" er de fælles fra labels.js (FvLabels), de samme
// som på en arbejdslistes linjer.
// Markup: Views/Labels/*.cshtml.
(() => {
    const initPage = (page) => {
        if (page.dataset.labelCollectionsInit) return;
        page.dataset.labelCollectionsInit = '1';

        const ds = page.dataset;
        const tableRoot = page.querySelector('[data-table-root]');
        const reloadTable = () => window.FvDataTable?.reload(tableRoot);

        const post = async (url, fd) => {
            try {
                const res = await fetch(url, { method: 'POST', body: fd, headers: { 'X-Requested-With': 'fetch' } });
                const data = await res.json();
                if (data.message) window.FvToast?.show(data.type || (data.success ? 'success' : 'error'), data.message);
                return data;
            } catch {
                window.FvToast?.show('error', 'Netværksfejl. Prøv igen.');
                return { success: false };
            }
        };

        // ─── Opret / rediger ───────────────────────────────────────────────────
        const editModalEl = page.querySelector('#labelCollectionEditModal');
        const editForm = editModalEl.querySelector('[data-label-collection-form]');
        const editTitle = editModalEl.querySelector('[data-label-collection-title]');
        const editId = editForm.querySelector('[data-label-collection-id]');
        const editName = editForm.querySelector('[data-label-collection-name]');
        const editSubmit = editForm.querySelector('[data-label-collection-submit]');
        const editor = window.FvLabels.createEditor(editForm.querySelector('[data-label-editor]'));
        let editRequest = 0;

        const loadCollection = async (id) => {
            const request = ++editRequest;
            try {
                const res = await fetch(`${ds.urlCollection}?${new URLSearchParams({ id })}`, { headers: { 'X-Requested-With': 'fetch' } });
                const data = await res.json();
                if (request !== editRequest) return;
                if (!data.success) {
                    window.FvToast?.show('error', data.message || 'Label-samlingen kunne ikke hentes.');
                    bootstrap.Modal.getInstance(editModalEl)?.hide();
                    return;
                }
                editName.value = data.name || '';
                editor.set(data.labels);
                editSubmit.disabled = false;
            } catch {
                if (request !== editRequest) return;
                window.FvToast?.show('error', 'Netværksfejl. Prøv igen.');
                bootstrap.Modal.getInstance(editModalEl)?.hide();
            }
        };

        editModalEl.addEventListener('show.bs.modal', (e) => {
            if (e.target !== editModalEl) return;
            const id = e.relatedTarget?.dataset?.id;
            editId.value = id || '';
            editName.value = '';
            if (!id) {
                // Opret-knappen
                editRequest++;
                editTitle.innerHTML = '<i class="bi bi-tags me-2"></i>Opret label-samling';
                editor.set([]);
                editSubmit.disabled = false;
                return;
            }

            editTitle.innerHTML = '<i class="bi bi-pencil me-2"></i>Rediger label-samling';
            editor.clear();
            editSubmit.disabled = true;
            loadCollection(id);
        });
        editModalEl.addEventListener('shown.bs.modal', (e) => {
            if (e.target === editModalEl) editName.focus();
        });

        editForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const fd = new FormData(editForm);
            editor.appendTo(fd);
            editSubmit.disabled = true;
            const data = await post(editForm.action, fd);
            editSubmit.disabled = false;
            if (data.success) {
                bootstrap.Modal.getInstance(editModalEl)?.hide();
                reloadTable();
            }
        });

        // ─── Slet ──────────────────────────────────────────────────────────────
        const deleteModalEl = page.querySelector('#labelCollectionDeleteModal');
        const deleteForm = deleteModalEl.querySelector('[data-label-collection-delete-form]');

        deleteModalEl.addEventListener('show.bs.modal', (e) => {
            const btn = e.relatedTarget;
            if (!btn) return;
            deleteForm.querySelector('[data-label-collection-delete-id]').value = btn.dataset.id;
            deleteModalEl.querySelector('[data-label-collection-delete-name]').textContent = btn.dataset.name;
        });

        deleteForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const submitBtn = deleteForm.querySelector('button[type="submit"]');
            submitBtn.disabled = true;
            const data = await window.FvForm.submit(deleteForm);
            submitBtn.disabled = false;
            if (data.success) {
                bootstrap.Modal.getInstance(deleteModalEl)?.hide();
                reloadTable();
            }
        });

        // ─── Print labels (fælles modal) ───────────────────────────────────────
        // The row's print button carries the counts, so nothing needs fetching before the PDF.
        window.FvLabels.initPrintModal(page.querySelector('#labelCollectionPrintModal'), {
            pdfUrl: ds.urlPdf,
            params: ({ trigger }) => new URLSearchParams({ id: trigger.dataset.id }),
            loadTotals: ({ trigger }) => ({ copies: Number(trigger.dataset.copies), lines: Number(trigger.dataset.labels) }),
            emptyText: () => 'Ingen labels i samlingen. Tilføj labels under Rediger først.',
            title: (trigger) => (trigger?.dataset.name ? `Print labels – ${trigger.dataset.name}` : 'Print labels')
        });
    };

    document.querySelectorAll('[data-label-collections-page]').forEach(initPage);
})();
