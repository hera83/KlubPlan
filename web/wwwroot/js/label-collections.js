// ─── Labels (Værktøjer) ───────────────────────────────────────────────────────
// Tabel med label-samlinger (data-table.js står for søgning/paginering): opret (→ designeren),
// print (første ark tegnes med de rigtige labels) og slet. Selve labels designes i label-designer.js;
// visningen og "Print labels" er de fælles fra label-design.js (FvLabelDesign).
// Markup: Views/Labels/Index.cshtml + partials.
(() => {
    const initPage = (page) => {
        if (page.dataset.labelCollectionsInit) return;
        page.dataset.labelCollectionsInit = '1';

        const ds = page.dataset;
        const tableRoot = page.querySelector('[data-table-root]');
        const reloadTable = () => window.FvDataTable?.reload(tableRoot);

        // ─── Opret → designeren ────────────────────────────────────────────────
        const createModalEl = page.querySelector('#labelCollectionCreateModal');
        const createForm = createModalEl.querySelector('[data-label-collection-create-form]');
        const sheetFields = window.FvLabelDesign.bindSheetFields(createForm);
        createModalEl.addEventListener('show.bs.modal', () => {
            createForm.reset();
            sheetFields.refresh();
        });
        createModalEl.addEventListener('shown.bs.modal', () => createForm.querySelector('[name="Name"]').focus());
        createForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            if (!sheetFields.isValid()) {
                window.FvToast?.show('error', 'Angiv et gyldigt antal labels i bredden og højden.');
                return;
            }
            const submitBtn = createForm.querySelector('button[type="submit"]');
            submitBtn.disabled = true;
            try {
                const res = await fetch(createForm.action, { method: 'POST', body: new FormData(createForm), headers: { 'X-Requested-With': 'fetch' } });
                const data = await res.json();
                if (data.success && data.redirectUrl) {
                    window.location.href = data.redirectUrl;
                    return;
                }
                window.FvToast?.show('error', data.message || 'Label-samlingen kunne ikke oprettes.');
            } catch {
                window.FvToast?.show('error', 'Netværksfejl. Prøv igen.');
            }
            submitBtn.disabled = false;
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

        // ─── Print labels ──────────────────────────────────────────────────────
        // The saved design is fetched, so the first sheet can be drawn with the real labels.
        window.FvLabelDesign.initPrintModal(page.querySelector('#labelDesignPrintModal'), {
            pdfUrl: ds.urlPdf,
            mediaUrl: (id) => `${ds.urlMedia}?id=${encodeURIComponent(id)}`,
            qrUrl: (text, color) => `${ds.urlQr}?${new URLSearchParams({ text, color })}`,
            load: async (trigger) => {
                const id = trigger.dataset.id;
                const res = await fetch(`${ds.urlDesign}?${new URLSearchParams({ id })}`, { headers: { 'X-Requested-With': 'fetch' } });
                const data = await res.json();
                if (!data.success) throw new Error(data.message);
                return { id, design: data.design };
            }
        });
    };

    document.querySelectorAll('[data-label-collections-page]').forEach(initPage);
})();
