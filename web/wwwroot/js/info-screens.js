// ─── Infoskærme (tabel) ───────────────────────────────────────────────────────
// Tabel over skærme (data-table.js står for søgning/filter/paginering): opret (→ designeren),
// aktivér/deaktivér, kopiér link og slet. Markup: Views/InfoScreens/Index.cshtml + partials.
(() => {
    const initPage = (page) => {
        if (page.dataset.infoScreensInit) return;
        page.dataset.infoScreensInit = '1';

        const tableRoot = page.querySelector('[data-table-root]');
        const token = () => page.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
        const reloadTable = () => window.FvDataTable?.reload(tableRoot);

        const post = async (url, fields) => {
            const fd = new FormData();
            Object.entries(fields).forEach(([key, value]) => fd.append(key, value));
            fd.append('__RequestVerificationToken', token());
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

        // ─── Rækkehandlinger: aktivér/deaktivér og kopiér link ──────────────────
        tableRoot.addEventListener('click', async (e) => {
            const toggle = e.target.closest('[data-info-screen-toggle]');
            if (toggle) {
                toggle.disabled = true;
                const data = await post(page.dataset.urlSetActive, { id: toggle.dataset.id, isActive: toggle.dataset.active !== 'true' });
                toggle.disabled = false;
                if (data.success) reloadTable();
                return;
            }

            const copy = e.target.closest('[data-copy-link]');
            if (copy) {
                try {
                    await navigator.clipboard.writeText(copy.dataset.copyLink);
                    if (copy.dataset.active === 'true') {
                        window.FvToast?.show('success', 'Linket er kopieret til udklipsholderen.');
                    } else {
                        window.FvToast?.show('info', 'Linket er kopieret. Skærmen er inaktiv, så den viser først diasshowet, når du aktiverer den.');
                    }
                } catch {
                    window.FvToast?.show('error', 'Linket kunne ikke kopieres.');
                }
            }
        });

        // ─── Opret → designeren ────────────────────────────────────────────────
        const createModalEl = page.querySelector('#infoScreenCreateModal');
        const createForm = createModalEl.querySelector('[data-info-screen-create-form]');
        createModalEl.addEventListener('show.bs.modal', () => createForm.reset());
        createModalEl.addEventListener('shown.bs.modal', () => createForm.querySelector('[name="Title"]').focus());
        createForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const submitBtn = createForm.querySelector('button[type="submit"]');
            submitBtn.disabled = true;
            try {
                const res = await fetch(createForm.action, { method: 'POST', body: new FormData(createForm), headers: { 'X-Requested-With': 'fetch' } });
                const data = await res.json();
                if (data.success && data.redirectUrl) {
                    window.location.href = data.redirectUrl;
                    return;
                }
                window.FvToast?.show('error', data.message || 'Infoskærmen kunne ikke oprettes.');
            } catch {
                window.FvToast?.show('error', 'Netværksfejl. Prøv igen.');
            }
            submitBtn.disabled = false;
        });

        // ─── Slet ──────────────────────────────────────────────────────────────
        const deleteModalEl = page.querySelector('#infoScreenDeleteModal');
        const deleteForm = deleteModalEl.querySelector('[data-info-screen-delete-form]');
        deleteModalEl.addEventListener('show.bs.modal', (e) => {
            const btn = e.relatedTarget;
            if (!btn) return;
            deleteForm.querySelector('[data-info-screen-delete-id]').value = btn.dataset.id;
            deleteModalEl.querySelector('[data-info-screen-delete-title]').textContent = btn.dataset.title;
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
    };

    document.querySelectorAll('[data-info-screens-page]').forEach(initPage);
})();
