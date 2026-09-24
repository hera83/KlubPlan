// ─── Offentlig arbejdsliste (/Arbejdsliste?Id=..&UId=..) ───────────────────────
// En ekstern kontakt i arbejdsgruppen arbejder på sine egne linjer uden login.
// • Status, Note og ekstra kolonner gemmes automatisk, når feltet ændres.
// • Klik på en status i fremdriften filtrerer tabellen (data-table.js står for søgning/paginering).
// Markup: Views/Arbejdsliste/*.cshtml. Den loggede-ind udgave er activity-list.js.
(() => {
    const page = document.querySelector('[data-public-list-page]');
    if (!page) return;

    const ds = page.dataset;
    const tableRoot = page.querySelector('[data-table-root]');
    const statusFilter = page.querySelector('[data-list-filter-status]');
    const token = () => page.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

    // ─── Fremdrift ─────────────────────────────────────────────────────────────
    const renderCounts = (statusCounts) => {
        const total = Object.values(statusCounts).reduce((a, b) => a + b, 0);
        page.querySelectorAll('.list-progress-segment[data-status-id]').forEach((seg) => {
            const count = statusCounts[seg.dataset.statusId] || 0;
            seg.style.width = total === 0 ? '0%' : `${(count * 100) / total}%`;
            seg.title = seg.title.replace(/: \d+$/, `: ${count}`);
        });
        page.querySelectorAll('[data-list-status-count]').forEach((el) => {
            el.textContent = statusCounts[el.dataset.listStatusCount] || 0;
        });
        page.querySelectorAll('[data-list-total]').forEach((el) => { el.textContent = total; });
        const handled = page.querySelector('[data-list-handled]');
        if (handled) handled.textContent = total - (statusCounts[ds.defaultStatusId] || 0);
    };

    // ─── Inline autosave ───────────────────────────────────────────────────────
    const valueOf = (el) => (el.type === 'checkbox' ? (el.checked ? 'true' : '') : el.value);

    const flash = (cell, state) => {
        cell.classList.remove('is-saving', 'is-saved', 'is-error');
        if (!state) return;
        cell.classList.add(state);
        if (state !== 'is-saving') setTimeout(() => cell.classList.remove(state), 1500);
    };

    tableRoot.addEventListener('focusin', (e) => {
        const field = e.target.closest('[data-list-field]');
        if (field) field.dataset.prev = valueOf(field);
    });

    tableRoot.addEventListener('change', async (e) => {
        const field = e.target.closest('[data-list-field]');
        if (!field) return;
        const row = field.closest('[data-list-item]');
        const cell = field.closest('td');
        const value = valueOf(field);

        if (field.dataset.listField === 'status') {
            field.dataset.color = field.selectedOptions[0]?.dataset.color || 'muted';
        }

        const fd = new FormData();
        fd.append('__RequestVerificationToken', token());
        fd.append('ListId', ds.listId);
        fd.append('ItemId', row.dataset.listItem);
        fd.append('Field', field.dataset.listField);
        fd.append('ColumnId', field.dataset.columnId || '');
        fd.append('Value', value);

        flash(cell, 'is-saving');
        let data;
        try {
            const res = await fetch(ds.urlUpdateField, { method: 'POST', body: fd, headers: { 'X-Requested-With': 'fetch' } });
            data = await res.json();
        } catch {
            data = { success: false, message: 'Netværksfejl. Prøv igen.' };
        }

        if (!data.success) {
            flash(cell, 'is-error');
            window.FvToast?.show('error', data.message || 'Feltet kunne ikke gemmes.');
            // Put the old value back so the screen matches what's stored.
            if (field.dataset.prev !== undefined) {
                if (field.type === 'checkbox') field.checked = field.dataset.prev === 'true';
                else field.value = field.dataset.prev;
                if (field.dataset.listField === 'status') field.dataset.color = field.selectedOptions[0]?.dataset.color || 'muted';
            }
            return;
        }

        field.dataset.prev = value;
        flash(cell, 'is-saved');
        if (data.statusCounts) renderCounts(data.statusCounts);
    });

    // Notes grow with their content.
    const autosize = (el) => {
        el.style.height = 'auto';
        el.style.height = `${el.scrollHeight + 2}px`;
    };
    tableRoot.addEventListener('input', (e) => {
        if (e.target.matches('.list-note')) autosize(e.target);
    });
    const autosizeAll = () => tableRoot.querySelectorAll('.list-note').forEach(autosize);
    new MutationObserver(autosizeAll).observe(tableRoot.querySelector('[data-table-region]'), { childList: true });
    autosizeAll();

    // ─── Status-filter via fremdriftens legend ─────────────────────────────────
    page.querySelectorAll('[data-list-status-filter]').forEach((btn) => {
        btn.addEventListener('click', () => {
            statusFilter.value = statusFilter.value === btn.dataset.listStatusFilter ? '' : btn.dataset.listStatusFilter;
            page.querySelectorAll('[data-list-status-filter]').forEach((b) => {
                b.classList.toggle('active', statusFilter.value === b.dataset.listStatusFilter);
            });
            tableRoot.dataset.currentPage = '1';
            window.FvDataTable?.reload(tableRoot);
        });
    });
})();
