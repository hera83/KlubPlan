// ─── Arbejdsliste (ActivityLists/Details) ─────────────────────────────────────
// • Status, Tilknyttet, Note og ekstra kolonner gemmes automatisk, når feltet ændres.
// • Sortering (klik på kolonneoverskrift), "Kun mine" og klik på en status i
//   fremdriften filtrerer tabellen (data-table.js står for søgning/filter/paginering).
// • Modals: rediger/tilføj linje, fordel linjer, send links, kolonner, statusser, rediger liste.
// Markup: Views/ActivityLists/*.cshtml.
(() => {
    const initPage = (page) => {
        if (page.dataset.listInit) return;
        page.dataset.listInit = '1';

        const ds = page.dataset;
        const listId = ds.listId;
        const tableRoot = page.querySelector('[data-table-root]');
        const token = () => page.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

        const post = async (url, fields, { toast = true } = {}) => {
            const fd = fields instanceof FormData ? fields : new FormData();
            if (!(fields instanceof FormData)) {
                Object.entries(fields).forEach(([key, value]) => {
                    if (Array.isArray(value)) value.forEach((v) => fd.append(key, v));
                    else if (value !== null && value !== undefined) fd.append(key, value);
                });
            }
            if (!fd.has('__RequestVerificationToken')) fd.append('__RequestVerificationToken', token());
            if (!fd.has('ListId')) fd.append('ListId', listId);
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

        const reloadTable = (firstPage = false) => {
            if (firstPage) tableRoot.dataset.currentPage = '1';
            window.FvDataTable?.reload(tableRoot);
        };

        // ─── Counts / progress ─────────────────────────────────────────────────
        const counts = { total: 0, unassigned: 0, statusCounts: {}, memberCounts: {} };

        const renderCounts = () => {
            const total = Object.values(counts.statusCounts).reduce((a, b) => a + b, 0);
            counts.total = total;
            page.querySelectorAll('.list-progress-segment[data-status-id]').forEach((seg) => {
                const count = counts.statusCounts[seg.dataset.statusId] || 0;
                seg.style.width = total === 0 ? '0%' : `${(count * 100) / total}%`;
                seg.title = seg.title.replace(/: \d+$/, `: ${count}`);
            });
            page.querySelectorAll('[data-list-status-count]').forEach((el) => {
                el.textContent = counts.statusCounts[el.dataset.listStatusCount] || 0;
            });
            document.querySelectorAll('[data-list-total]').forEach((el) => { el.textContent = total; });
            document.querySelectorAll('[data-list-unassigned]').forEach((el) => { el.textContent = counts.unassigned; });
            const handled = page.querySelector('[data-list-handled]');
            if (handled) handled.textContent = total - (counts.statusCounts[ds.defaultStatusId] || 0);
            document.querySelectorAll('[data-list-member-count]').forEach((el) => {
                el.textContent = counts.memberCounts[el.dataset.listMemberCount] || 0;
            });
        };

        const refreshCounts = async () => {
            try {
                const res = await fetch(ds.urlCounts, { headers: { 'X-Requested-With': 'fetch' } });
                if (!res.ok) return;
                const data = await res.json();
                counts.unassigned = data.unassigned;
                counts.statusCounts = data.statusCounts || {};
                counts.memberCounts = data.memberCounts || {};
                renderCounts();
            } catch { /* keep old numbers */ }
        };

        // ─── Inline autosave ───────────────────────────────────────────────────
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

            flash(cell, 'is-saving');
            const data = await post(ds.urlUpdateField, {
                ItemId: row.dataset.listItem,
                Field: field.dataset.listField,
                ColumnId: field.dataset.columnId || '',
                Value: value
            }, { toast: false });

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
            counts.statusCounts = data.statusCounts || counts.statusCounts;
            counts.unassigned = data.unassignedCount ?? counts.unassigned;
            renderCounts();
            const history = row.querySelector('[data-list-history]');
            if (history && data.updatedText) {
                history.title = data.updatedText;
                history.hidden = false;
            }
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

        // ─── Sorting, "Kun mine", status legend ────────────────────────────────
        const sortColumn = page.querySelector('[data-list-sort-column]');
        const sortDir = page.querySelector('[data-list-sort-dir]');
        const assignedSelect = page.querySelector('[data-list-filter-assigned]');
        const statusSelect = page.querySelector('[data-list-filter-status]');
        const mineToggle = page.querySelector('[data-list-mine-toggle]');

        tableRoot.addEventListener('click', (e) => {
            const sortBtn = e.target.closest('[data-list-sort]');
            if (!sortBtn) return;
            // Cycle: ascending → descending → back to the list's own order.
            const key = sortBtn.dataset.listSort;
            const current = sortBtn.dataset.listSortActiveDir;
            if (current === 'desc') {
                sortColumn.value = '';
                sortDir.value = '';
            } else {
                sortColumn.value = key;
                sortDir.value = current === 'asc' ? 'desc' : 'asc';
            }
            reloadTable(true);
        });

        const syncFilterButtons = () => {
            mineToggle?.classList.toggle('active', assignedSelect?.value === 'mine');
            page.querySelectorAll('[data-list-status-filter]').forEach((btn) => {
                btn.classList.toggle('active', statusSelect?.value === btn.dataset.listStatusFilter);
            });
        };

        mineToggle?.addEventListener('click', () => {
            assignedSelect.value = assignedSelect.value === 'mine' ? '' : 'mine';
            syncFilterButtons();
            reloadTable(true);
        });

        page.querySelectorAll('[data-list-status-filter]').forEach((btn) => {
            btn.addEventListener('click', () => {
                statusSelect.value = statusSelect.value === btn.dataset.listStatusFilter ? '' : btn.dataset.listStatusFilter;
                syncFilterButtons();
                reloadTable(true);
            });
        });

        page.querySelectorAll('[data-table-filter-save], [data-table-filter-reset]').forEach((btn) => {
            btn.addEventListener('click', () => setTimeout(syncFilterButtons));
        });

        page.querySelector('[data-list-export-filtered]')?.addEventListener('click', () => {
            const params = new URLSearchParams({ listId });
            const search = tableRoot.querySelector('[data-table-search]')?.value.trim();
            if (search) params.set('SearchText', search);
            tableRoot.querySelectorAll('[data-table-filter-panel] [name]').forEach((field) => {
                if (field.name !== 'ListId' && field.value) params.set(field.name, field.value);
            });
            window.location.href = `${ds.urlExport}?${params}`;
        });

        // ─── Rediger / tilføj linje ────────────────────────────────────────────
        const itemModalEl = document.getElementById('listItemModal');
        const itemForm = itemModalEl?.querySelector('[data-list-item-form]');
        const deleteConfirm = itemModalEl?.querySelector('[data-list-item-delete-confirm]');
        const deleteBtn = itemModalEl?.querySelector('[data-list-item-delete]');

        const openItemModal = (row) => {
            if (!itemModalEl) return;
            const values = row ? JSON.parse(row.dataset.values || '{}') : {};
            itemForm.querySelector('[data-list-item-id]').value = row ? row.dataset.listItem : '';
            itemModalEl.querySelector('[data-list-item-title]').textContent = row ? `Rediger linje ${row.dataset.rowNumber}` : 'Tilføj linje';
            itemModalEl.querySelector('[data-list-item-icon]').className = `bi ${row ? 'bi-pencil' : 'bi-plus-lg'} me-2`;
            itemForm.querySelectorAll('[data-list-item-value]').forEach((input) => {
                const value = values[input.dataset.listItemValue] ?? '';
                if (input.type === 'checkbox') input.checked = value === 'true';
                else input.value = value;
            });
            deleteBtn.hidden = !row;
            deleteConfirm.hidden = true;
            bootstrap.Modal.getOrCreateInstance(itemModalEl).show();
        };

        itemModalEl?.addEventListener('shown.bs.modal', () => itemForm.querySelector('[data-list-item-value]')?.focus());

        page.querySelector('[data-list-add-item]')?.addEventListener('click', () => openItemModal(null));
        tableRoot.addEventListener('click', (e) => {
            const btn = e.target.closest('[data-list-edit-item]');
            if (btn) openItemModal(btn.closest('[data-list-item]'));
        });

        itemForm?.addEventListener('submit', async (e) => {
            e.preventDefault();
            // Built by hand so an unticked Ja/nej box is sent as "" (a plain form would leave it out).
            const fd = new FormData();
            fd.append('__RequestVerificationToken', token());
            fd.append('ListId', listId);
            const itemId = itemForm.querySelector('[data-list-item-id]').value;
            if (itemId) fd.append('ItemId', itemId);
            itemForm.querySelectorAll('[data-list-item-value]').forEach((input) => {
                fd.append(`Values[${input.dataset.listItemValue}]`, input.type === 'checkbox' ? (input.checked ? 'true' : '') : input.value);
            });
            const submitBtn = itemForm.querySelector('button[type="submit"]');
            submitBtn.disabled = true;
            const data = await post(ds.urlSaveItem, fd);
            submitBtn.disabled = false;
            if (data.success) {
                bootstrap.Modal.getInstance(itemModalEl)?.hide();
                reloadTable();
                refreshCounts();
            }
        });

        deleteBtn?.addEventListener('click', () => { deleteConfirm.hidden = false; });
        itemModalEl?.querySelector('[data-list-item-delete-cancel]')?.addEventListener('click', () => { deleteConfirm.hidden = true; });
        itemModalEl?.querySelector('[data-list-item-delete-yes]')?.addEventListener('click', async (e) => {
            const btn = e.currentTarget;
            btn.disabled = true;
            const data = await post(ds.urlDeleteItem, { ItemId: itemForm.querySelector('[data-list-item-id]').value });
            btn.disabled = false;
            if (data.success) {
                bootstrap.Modal.getInstance(itemModalEl)?.hide();
                reloadTable();
                refreshCounts();
            }
        });

        // ─── Fordel linjer ─────────────────────────────────────────────────────
        const distributeModalEl = document.getElementById('listDistributeModal');
        const distributeForm = distributeModalEl?.querySelector('[data-list-distribute-form]');
        const preview = distributeModalEl?.querySelector('[data-list-distribute-preview]');
        const distributeSubmit = distributeModalEl?.querySelector('[data-list-distribute-submit]');

        // Same algorithm as ActivityListService.DistributeAsync, so the preview matches the result.
        const updatePreview = () => {
            if (!distributeForm || !preview) return;
            const selected = Array.from(distributeForm.querySelectorAll('[data-list-distribute-member]:checked'));
            const onlyUnassigned = distributeForm.querySelector('[data-list-distribute-mode]:checked')?.value === 'true';
            const lines = onlyUnassigned ? counts.unassigned : counts.total;
            distributeSubmit.disabled = selected.length === 0 || lines === 0;
            if (selected.length === 0) {
                preview.hidden = true;
                return;
            }
            if (lines === 0) {
                preview.hidden = false;
                preview.textContent = onlyUnassigned ? 'Alle linjer har allerede en tilknytning.' : 'Listen har ingen linjer.';
                return;
            }
            const totals = selected.map((cb) => (onlyUnassigned ? counts.memberCounts[cb.value] || 0 : 0));
            const quota = selected.map(() => 0);
            for (let n = 0; n < lines; n++) {
                const next = totals.indexOf(Math.min(...totals));
                totals[next]++;
                quota[next]++;
            }
            const escapeHtml = (text) => text.replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]);
            const names = selected.map((cb) => cb.dataset.name || '');
            preview.hidden = false;
            preview.innerHTML = `<i class="bi bi-info-circle me-1"></i>${lines} linjer fordeles: ` +
                names.map((name, i) => `<strong>${escapeHtml(name)}</strong> ${quota[i]}`).join(', ');
        };

        distributeModalEl?.addEventListener('show.bs.modal', async () => {
            await refreshCounts();
            updatePreview();
        });
        distributeForm?.addEventListener('change', updatePreview);
        distributeModalEl?.querySelector('[data-list-distribute-all]')?.addEventListener('click', () => {
            const boxes = distributeForm.querySelectorAll('[data-list-distribute-member]');
            const allChecked = Array.from(boxes).every((b) => b.checked);
            boxes.forEach((b) => { b.checked = !allChecked; });
            updatePreview();
        });
        distributeForm?.addEventListener('submit', async (e) => {
            e.preventDefault();
            distributeSubmit.disabled = true;
            const data = await post(ds.urlDistribute, new FormData(distributeForm));
            distributeSubmit.disabled = false;
            if (data.success) {
                bootstrap.Modal.getInstance(distributeModalEl)?.hide();
                reloadTable();
                refreshCounts();
            }
        });

        // ─── Send links (eksterne kontakter) ───────────────────────────────────
        const sendModalEl = document.getElementById('listSendLinksModal');
        const sendForm = sendModalEl?.querySelector('[data-list-send-links-form]');
        const sendSubmit = sendModalEl?.querySelector('[data-list-send-submit]');

        const updateSendSubmit = () => {
            if (!sendForm || !sendSubmit) return;
            const anyMember = sendForm.querySelector('[data-list-send-member]:checked');
            const anyChannel = sendForm.querySelector('[data-list-send-channel]:checked');
            sendSubmit.disabled = !anyMember || !anyChannel;
        };

        sendModalEl?.addEventListener('show.bs.modal', refreshCounts);
        sendForm?.addEventListener('change', updateSendSubmit);
        sendModalEl?.querySelector('[data-list-send-all]')?.addEventListener('click', () => {
            const boxes = Array.from(sendForm.querySelectorAll('[data-list-send-member]:not(:disabled)'));
            const allChecked = boxes.every((b) => b.checked);
            boxes.forEach((b) => { b.checked = !allChecked; });
            updateSendSubmit();
        });
        sendModalEl?.addEventListener('click', async (e) => {
            const btn = e.target.closest('[data-list-copy-link]');
            if (!btn) return;
            try {
                await navigator.clipboard.writeText(btn.dataset.listCopyLink);
                window.FvToast?.show('success', 'Linket er kopieret.');
            } catch {
                window.FvToast?.show('error', 'Linket kunne ikke kopieres.');
            }
        });
        sendForm?.addEventListener('submit', async (e) => {
            e.preventDefault();
            sendSubmit.disabled = true;
            const data = await post(ds.urlSendLinks, new FormData(sendForm));
            sendSubmit.disabled = false;
            if (data.success) {
                bootstrap.Modal.getInstance(sendModalEl)?.hide();
                sendForm.reset();
                updateSendSubmit();
            }
        });

        // ─── Kolonner ──────────────────────────────────────────────────────────
        const columnsModalEl = document.getElementById('listColumnsModal');
        let columnsChanged = false;

        columnsModalEl?.addEventListener('hidden.bs.modal', () => {
            if (columnsChanged) window.location.reload();
        });

        columnsModalEl?.querySelector('[data-list-note-switch]')?.addEventListener('change', async (e) => {
            const data = await post(ds.urlNoteVisible, { Visible: e.target.checked ? 'true' : 'false' });
            if (data.success) columnsChanged = true;
            else e.target.checked = !e.target.checked;
        });

        columnsModalEl?.addEventListener('change', async (e) => {
            const visibleSwitch = e.target.closest('[data-list-column-visible]');
            if (!visibleSwitch) return;
            const row = visibleSwitch.closest('[data-list-column-row]');
            visibleSwitch.disabled = true;
            const data = await post(ds.urlColumnHidden, {
                ColumnId: row.dataset.listColumnRow,
                Hidden: visibleSwitch.checked ? 'false' : 'true'
            });
            visibleSwitch.disabled = false;
            if (data.success) columnsChanged = true;
            else visibleSwitch.checked = !visibleSwitch.checked;
        });

        columnsModalEl?.addEventListener('click', async (e) => {
            const row = e.target.closest('[data-list-column-row]');
            if (!row) return;

            const saveBtn = e.target.closest('[data-list-column-save]');
            if (saveBtn) {
                saveBtn.disabled = true;
                const data = await post(ds.urlSaveColumn, {
                    ColumnId: row.dataset.listColumnRow,
                    Name: row.querySelector('[data-list-column-name]').value,
                    Options: row.querySelector('[data-list-column-options]')?.value ?? ''
                });
                saveBtn.disabled = false;
                if (data.success) columnsChanged = true;
                return;
            }

            const deleteColBtn = e.target.closest('[data-list-column-delete]');
            if (deleteColBtn) {
                // Two-step: first click arms the button, second click deletes (no alert()).
                if (!deleteColBtn.dataset.armed) {
                    deleteColBtn.dataset.armed = '1';
                    deleteColBtn.classList.replace('btn-outline-danger', 'btn-danger');
                    deleteColBtn.classList.remove('btn-icon');
                    deleteColBtn.innerHTML = '<i class="bi bi-trash me-1"></i> Slet kolonne?';
                    setTimeout(() => {
                        if (!deleteColBtn.isConnected || !deleteColBtn.dataset.armed) return;
                        delete deleteColBtn.dataset.armed;
                        deleteColBtn.classList.replace('btn-danger', 'btn-outline-danger');
                        deleteColBtn.classList.add('btn-icon');
                        deleteColBtn.innerHTML = '<i class="bi bi-trash"></i>';
                    }, 4000);
                    return;
                }
                deleteColBtn.disabled = true;
                const data = await post(ds.urlDeleteColumn, { ColumnId: row.dataset.listColumnRow });
                if (data.success) {
                    columnsChanged = true;
                    row.remove();
                } else {
                    deleteColBtn.disabled = false;
                }
            }
        });

        const addColumnForm = columnsModalEl?.querySelector('[data-list-column-add]');
        const kindSelect = addColumnForm?.querySelector('[data-list-column-kind]');
        kindSelect?.addEventListener('change', () => {
            addColumnForm.querySelector('[data-list-column-options-wrap]').hidden = kindSelect.value !== 'Choice';
        });
        addColumnForm?.addEventListener('submit', async (e) => {
            e.preventDefault();
            const data = await post(ds.urlSaveColumn, new FormData(addColumnForm));
            if (data.success) {
                columnsChanged = true;
                bootstrap.Modal.getInstance(columnsModalEl)?.hide();
            }
        });

        // ─── Statusser ─────────────────────────────────────────────────────────
        const statusesModalEl = document.getElementById('listStatusesModal');
        const statusRows = statusesModalEl?.querySelector('[data-list-status-rows]');
        const statusTemplate = statusesModalEl?.querySelector('[data-list-status-template]');

        const updateRemoveButtons = () => {
            const rows = statusRows.querySelectorAll('[data-list-status-row]');
            rows.forEach((r) => { r.querySelector('[data-list-status-remove]').disabled = rows.length <= 1; });
        };

        statusesModalEl?.querySelector('[data-list-status-add]')?.addEventListener('click', () => {
            statusRows.appendChild(statusTemplate.content.cloneNode(true));
            updateRemoveButtons();
            statusRows.lastElementChild?.querySelector('[data-list-status-name]')?.focus();
        });
        statusRows?.addEventListener('click', (e) => {
            const btn = e.target.closest('[data-list-status-remove]');
            if (!btn) return;
            const row = btn.closest('[data-list-status-row]');
            const wasDefault = row.querySelector('[data-list-status-default]').checked;
            row.remove();
            if (wasDefault) statusRows.querySelector('[data-list-status-default]').checked = true;
            updateRemoveButtons();
        });
        statusRows?.addEventListener('change', (e) => {
            if (!e.target.matches('[data-list-status-color]')) return;
            e.target.closest('[data-list-status-row]').querySelector('[data-list-status-dot]').className = `list-legend-dot list-color-${e.target.value}`;
        });
        if (statusRows) updateRemoveButtons();

        statusesModalEl?.querySelector('[data-list-statuses-form]')?.addEventListener('submit', async (e) => {
            e.preventDefault();
            const fd = new FormData();
            let defaultIndex = 0;
            statusRows.querySelectorAll('[data-list-status-row]').forEach((row, i) => {
                if (row.dataset.id) fd.append(`Statuses[${i}].Id`, row.dataset.id);
                fd.append(`Statuses[${i}].Name`, row.querySelector('[data-list-status-name]').value);
                fd.append(`Statuses[${i}].Color`, row.querySelector('[data-list-status-color]').value);
                if (row.querySelector('[data-list-status-default]').checked) defaultIndex = i;
            });
            fd.append('DefaultIndex', defaultIndex);
            const data = await post(ds.urlSaveStatuses, fd);
            if (data.success) window.location.reload();
        });

        // ─── Rediger liste (titel/beskrivelse) ─────────────────────────────────
        document.querySelectorAll('[data-list-reload-form]').forEach((form) => {
            form.addEventListener('submit', async (e) => {
                e.preventDefault();
                const data = await window.FvForm.submit(form);
                if (data.success) window.location.reload();
            });
        });

        syncFilterButtons();
        refreshCounts();
    };

    document.querySelectorAll('[data-list-page]').forEach(initPage);
})();
