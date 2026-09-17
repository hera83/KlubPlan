(() => {
    // Vagter har ingen meningsfuld manuel rækkefølge (de vises efter starttidspunkt), og
    // krav-rækker er bare hvad admin skrev — så i modsætning til forms-builder.js er der
    // bevidst ingen drag-and-drop-omsortering her, kun tilføj/fjern.
    let nextClientId = 1;
    const state = { shifts: [] };
    let activeClientId = null;
    let els = null;

    const newRequirement = (seed) => ({
        clientId: 'r' + (nextClientId++),
        id: seed?.Id || 0,
        text: seed?.Text || ''
    });

    const newShift = (seed) => ({
        clientId: 's' + (nextClientId++),
        id: seed?.Id || 0,
        title: seed?.Title || '',
        start: seed?.Start || '',
        end: seed?.End || '',
        location: seed?.Location || '',
        neededCount: seed?.NeededCount || 1,
        requirements: (seed?.Requirements || []).map(newRequirement)
    });

    // ─── Rendering ──────────────────────────────────────────────────────────

    function renderAll() {
        els.cardsContainer.innerHTML = '';
        state.shifts.forEach((shift) => els.cardsContainer.appendChild(renderCard(shift)));
    }

    function shiftSummary(shift) {
        if (!shift.start || !shift.end) return 'Tidspunkt ikke sat endnu';
        const fmt = (v) => v.replace('T', ' kl. ');
        return `${fmt(shift.start)} – ${shift.end.split('T')[1] || ''}`;
    }

    function renderCard(shift) {
        const isActive = shift.clientId === activeClientId;

        const card = document.createElement('div');
        card.className = 'arrangement-shift-card' + (isActive ? ' active' : '');
        card.dataset.clientId = shift.clientId;

        const header = document.createElement('div');
        header.className = 'form-field-card-header';
        header.innerHTML = `
            <i class="bi bi-clock-history form-field-type-icon"></i>
            <span class="form-field-summary">${escapeHtml(shift.title) || '<span class="text-muted">Unavngiven vagt</span>'}</span>
            <span class="form-field-type-badge">${escapeHtml(shiftSummary(shift))} · ${shift.neededCount} plads${shift.neededCount === 1 ? '' : 'er'}</span>
            <span class="form-field-card-actions">
                <button type="button" class="btn btn-sm btn-outline-danger btn-icon" data-action="delete" title="Slet vagt" aria-label="Slet vagt"><i class="bi bi-trash"></i></button>
            </span>
        `;
        header.addEventListener('click', (e) => {
            const actionBtn = e.target.closest('[data-action]');
            if (actionBtn) {
                e.stopPropagation();
                if (actionBtn.dataset.action === 'delete') deleteShift(shift.clientId);
                return;
            }
            if (!e.target.closest('.form-field-card-actions')) {
                activeClientId = isActive ? null : shift.clientId;
                renderAll();
            }
        });
        card.appendChild(header);

        if (isActive) {
            card.appendChild(renderBody(shift));
        }

        return card;
    }

    function renderBody(shift) {
        const body = document.createElement('div');
        body.className = 'form-field-card-body';

        const titleRow = document.createElement('div');
        titleRow.className = 'mb-2';
        titleRow.innerHTML = '<label class="form-label">Titel</label>';
        const titleInput = document.createElement('input');
        titleInput.type = 'text';
        titleInput.className = 'form-control';
        titleInput.value = shift.title;
        titleInput.placeholder = 'Fx Grillvagt';
        titleInput.addEventListener('input', () => {
            shift.title = titleInput.value;
            refreshHeader(shift);
        });
        titleRow.appendChild(titleInput);
        body.appendChild(titleRow);

        const timeRow = document.createElement('div');
        timeRow.className = 'row g-2 mb-2';

        const startCol = document.createElement('div');
        startCol.className = 'col-md-6';
        startCol.innerHTML = '<label class="form-label">Start</label>';
        const startInput = document.createElement('input');
        startInput.type = 'datetime-local';
        startInput.className = 'form-control';
        startInput.value = shift.start;
        startInput.addEventListener('input', () => { shift.start = startInput.value; refreshHeader(shift); });
        startCol.appendChild(startInput);
        timeRow.appendChild(startCol);

        const endCol = document.createElement('div');
        endCol.className = 'col-md-6';
        endCol.innerHTML = '<label class="form-label">Slut</label>';
        const endInput = document.createElement('input');
        endInput.type = 'datetime-local';
        endInput.className = 'form-control';
        endInput.value = shift.end;
        endInput.addEventListener('input', () => { shift.end = endInput.value; refreshHeader(shift); });
        endCol.appendChild(endInput);
        timeRow.appendChild(endCol);

        body.appendChild(timeRow);

        const detailsRow = document.createElement('div');
        detailsRow.className = 'row g-2 mb-3';

        const locationCol = document.createElement('div');
        locationCol.className = 'col-md-8';
        locationCol.innerHTML = '<label class="form-label">Sted (valgfri)</label>';
        const locationInput = document.createElement('input');
        locationInput.type = 'text';
        locationInput.className = 'form-control';
        locationInput.value = shift.location;
        locationInput.addEventListener('input', () => { shift.location = locationInput.value; });
        locationCol.appendChild(locationInput);
        detailsRow.appendChild(locationCol);

        const countCol = document.createElement('div');
        countCol.className = 'col-md-4';
        countCol.innerHTML = '<label class="form-label">Antal pladser</label>';
        const countInput = document.createElement('input');
        countInput.type = 'number';
        countInput.min = '1';
        countInput.className = 'form-control';
        countInput.value = shift.neededCount;
        countInput.addEventListener('input', () => {
            shift.neededCount = parseInt(countInput.value, 10) || 1;
            refreshHeader(shift);
        });
        countCol.appendChild(countInput);
        detailsRow.appendChild(countCol);

        body.appendChild(detailsRow);

        body.appendChild(renderRequirementsEditor(shift));

        return body;
    }

    function refreshHeader(shift) {
        const card = els.cardsContainer.querySelector(`[data-client-id="${shift.clientId}"]`);
        const summary = card?.querySelector('.form-field-summary');
        if (summary) summary.innerHTML = escapeHtml(shift.title) || '<span class="text-muted">Unavngiven vagt</span>';
        const badge = card?.querySelector('.form-field-type-badge');
        if (badge) badge.textContent = `${shiftSummary(shift)} · ${shift.neededCount} plads${shift.neededCount === 1 ? '' : 'er'}`;
    }

    function renderRequirementsEditor(shift) {
        const wrap = document.createElement('div');
        wrap.className = 'form-field-options-editor mb-2';
        wrap.innerHTML = '<label class="form-label">Krav for at tilmelde sig denne vagt (valgfri)</label>';

        const list = document.createElement('div');
        shift.requirements.forEach((req) => list.appendChild(renderRequirementRow(shift, req)));
        wrap.appendChild(list);

        const addBtn = document.createElement('button');
        addBtn.type = 'button';
        addBtn.className = 'btn btn-sm btn-link form-field-add-option';
        addBtn.innerHTML = '<i class="bi bi-plus-lg me-1"></i> Tilføj krav';
        addBtn.addEventListener('click', () => {
            const req = newRequirement(null);
            shift.requirements.push(req);
            renderAll();
            const inputs = els.cardsContainer.querySelectorAll(`[data-client-id="${shift.clientId}"] .form-field-option-input`);
            inputs[inputs.length - 1]?.focus();
        });
        wrap.appendChild(addBtn);

        return wrap;
    }

    function renderRequirementRow(shift, req) {
        const row = document.createElement('div');
        row.className = 'form-field-option-row';

        const icon = document.createElement('i');
        icon.className = 'bi bi-check2-square';
        row.appendChild(icon);

        const input = document.createElement('input');
        input.type = 'text';
        input.className = 'form-control form-field-option-input';
        input.value = req.text;
        input.placeholder = 'Fx "Jeg har kørekort"';
        input.addEventListener('input', () => { req.text = input.value; });
        row.appendChild(input);

        const removeBtn = document.createElement('button');
        removeBtn.type = 'button';
        removeBtn.className = 'btn btn-sm btn-link form-field-remove-option';
        removeBtn.innerHTML = '<i class="bi bi-x-lg"></i>';
        removeBtn.title = 'Fjern krav';
        removeBtn.setAttribute('aria-label', 'Fjern krav');
        removeBtn.addEventListener('click', () => {
            shift.requirements = shift.requirements.filter((r) => r.clientId !== req.clientId);
            renderAll();
        });
        row.appendChild(removeBtn);

        return row;
    }

    // ─── State mutations ────────────────────────────────────────────────────

    function addShift() {
        const shift = newShift(null);
        state.shifts.push(shift);
        activeClientId = shift.clientId;
        renderAll();
        els.cardsContainer.querySelector(`[data-client-id="${shift.clientId}"]`)?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }

    function deleteShift(clientId) {
        state.shifts = state.shifts.filter((s) => s.clientId !== clientId);
        if (activeClientId === clientId) activeClientId = null;
        renderAll();
    }

    // ─── Submit ─────────────────────────────────────────────────────────────

    function validate() {
        const errors = [];
        state.shifts.forEach((shift, idx) => {
            const label = shift.title || `Vagt ${idx + 1}`;
            if (!shift.title.trim()) errors.push(`Vagt ${idx + 1} mangler en titel.`);
            if (!shift.start || !shift.end) errors.push(`'${label}' mangler start- eller sluttidspunkt.`);
            else if (shift.end <= shift.start) errors.push(`'${label}' skal have et sluttidspunkt efter starttidspunktet.`);
            if (!shift.neededCount || shift.neededCount < 1) errors.push(`'${label}' skal have mindst 1 plads.`);
        });
        return errors;
    }

    function serializeToHiddenInputs() {
        els.hiddenContainer.innerHTML = '';
        state.shifts.forEach((shift, idx) => {
            const values = {
                [`Shifts[${idx}].Id`]: shift.id,
                [`Shifts[${idx}].Title`]: shift.title,
                [`Shifts[${idx}].Start`]: shift.start,
                [`Shifts[${idx}].End`]: shift.end,
                [`Shifts[${idx}].Location`]: shift.location || '',
                [`Shifts[${idx}].NeededCount`]: shift.neededCount,
                [`Shifts[${idx}].Order`]: idx
            };
            Object.entries(values).forEach(([name, value]) => addHiddenInput(name, value));

            shift.requirements
                .filter((r) => r.text.trim())
                .forEach((req, rIdx) => {
                    addHiddenInput(`Shifts[${idx}].Requirements[${rIdx}].Id`, req.id);
                    addHiddenInput(`Shifts[${idx}].Requirements[${rIdx}].Text`, req.text);
                    addHiddenInput(`Shifts[${idx}].Requirements[${rIdx}].Order`, rIdx);
                });
        });
    }

    function addHiddenInput(name, value) {
        const input = document.createElement('input');
        input.type = 'hidden';
        input.name = name;
        input.value = value;
        els.hiddenContainer.appendChild(input);
    }

    function escapeHtml(str) {
        return (str || '').replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    }

    function init(options) {
        els = options;
        state.shifts = (options.seed.Shifts || []).map(newShift);
        if (state.shifts.length === 1) activeClientId = state.shifts[0].clientId;

        els.addShiftBtn?.addEventListener('click', addShift);
        renderAll();

        els.formEl.addEventListener('submit', (e) => {
            const errors = validate();
            if (errors.length > 0) {
                e.preventDefault();
                els.alertEl.innerHTML = errors.map((err) => escapeHtml(err)).join('<br>');
                els.alertEl.classList.remove('d-none');
                els.alertEl.scrollIntoView({ behavior: 'smooth', block: 'center' });
                return;
            }
            els.alertEl.classList.add('d-none');
            serializeToHiddenInputs();
        });
    }

    window.FvArrangementShiftsBuilder = { init };
})();
