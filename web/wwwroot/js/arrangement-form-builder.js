(() => {
    // Field types the builder can add, mirroring Constants/FormFieldTypes.cs (Danish labels +
    // Bootstrap Icons). Kept as a small static table here rather than round-tripped from the
    // server since it never changes without a matching server-side change anyway.
    const FIELD_TYPES = [
        { type: 'ShortText', label: 'Kort svar', icon: 'bi-input-cursor-text' },
        { type: 'LongText', label: 'Langt svar', icon: 'bi-text-paragraph' },
        { type: 'Number', label: 'Tal', icon: 'bi-123' },
        { type: 'Email', label: 'Email', icon: 'bi-envelope' },
        { type: 'Phone', label: 'Telefonnummer', icon: 'bi-telephone' },
        { type: 'Date', label: 'Dato', icon: 'bi-calendar3' },
        { type: 'Dropdown', label: 'Dropdown', icon: 'bi-menu-button-wide' },
        { type: 'MultipleChoice', label: 'Multiple choice', icon: 'bi-ui-radios' },
        { type: 'Checkboxes', label: 'Afkrydsningsfelter', icon: 'bi-ui-checks' },
        { type: 'SectionHeading', label: 'Informationstekst', icon: 'bi-card-text' }
    ];
    const OPTION_TYPES = new Set(['Dropdown', 'MultipleChoice', 'Checkboxes']);
    const typeMeta = (type) => FIELD_TYPES.find((t) => t.type === type) || FIELD_TYPES[0];

    // Informationstekst er ren statisk tekst til respondenten (ikke et spørgsmål), så den må
    // gerne stå uden overskrift — vis en neutral pladsholder i stedet for "Uden titel".
    const summaryFallback = (field) => field.fieldType === 'SectionHeading' ? 'Informationstekst' : 'Uden titel';

    let nextClientId = 1;
    const state = { fields: [] };
    let activeClientId = null;
    let els = null;
    let readOnly = false;

    const newField = (type, seedField) => ({
        clientId: 'c' + (nextClientId++),
        id: seedField?.Id || 0,
        label: seedField?.Label || '',
        helpText: seedField?.HelpText || '',
        fieldType: type,
        isRequired: !!seedField?.IsRequired,
        options: parseOptions(seedField?.OptionsJson) || (OPTION_TYPES.has(type) ? ['Mulighed 1'] : [])
    });

    function parseOptions(json) {
        if (!json) return null;
        try {
            const arr = JSON.parse(json);
            return Array.isArray(arr) ? arr : null;
        } catch {
            return null;
        }
    }

    // ─── Rendering ──────────────────────────────────────────────────────────

    function renderAll() {
        els.cardsContainer.innerHTML = '';
        state.fields.forEach((field) => els.cardsContainer.appendChild(renderCard(field)));
    }

    function renderCard(field) {
        const meta = typeMeta(field.fieldType);
        const isActive = field.clientId === activeClientId;
        const isSection = field.fieldType === 'SectionHeading';

        const card = document.createElement('div');
        card.className = 'form-field-card' + (isActive ? ' active' : '') + (isSection ? ' is-section' : '');
        card.dataset.clientId = field.clientId;
        card.draggable = !readOnly;

        const header = document.createElement('div');
        header.className = 'form-field-card-header';
        header.innerHTML = `
            <span class="form-field-drag-handle" title="Træk for at omsortere" aria-label="Træk for at omsortere">
                <i class="bi bi-grip-vertical"></i>
            </span>
            <i class="bi ${meta.icon} form-field-type-icon"></i>
            <span class="form-field-summary">${escapeHtml(field.label) || `<span class="text-muted">${summaryFallback(field)}</span>`}</span>
            <span class="form-field-type-badge">${meta.label}</span>
            <span class="form-field-card-actions">
                ${readOnly ? '' : `
                <button type="button" class="btn btn-sm btn-secondary btn-icon" data-action="duplicate" title="Dupliker" aria-label="Dupliker"><i class="bi bi-copy"></i></button>
                <button type="button" class="btn btn-sm btn-outline-danger btn-icon" data-action="delete" title="Slet spørgsmål" aria-label="Slet spørgsmål"><i class="bi bi-trash"></i></button>
                `}
            </span>
        `;
        if (readOnly) header.querySelector('.form-field-drag-handle').classList.add('d-none');
        header.addEventListener('mousedown', (e) => {
            if (e.target.closest('.form-field-drag-handle')) card.dataset.dragArmed = 'true';
        });
        header.addEventListener('click', (e) => {
            const actionBtn = e.target.closest('[data-action]');
            if (actionBtn) {
                e.stopPropagation();
                if (actionBtn.dataset.action === 'duplicate') duplicateField(field.clientId);
                if (actionBtn.dataset.action === 'delete') deleteField(field.clientId);
                return;
            }
            if (!e.target.closest('.form-field-card-actions')) {
                activeClientId = isActive ? null : field.clientId;
                renderAll();
            }
        });
        card.appendChild(header);

        if (isActive) {
            card.appendChild(renderBody(field));
        }

        card.addEventListener('dragstart', (e) => {
            if (card.dataset.dragArmed !== 'true') { e.preventDefault(); return; }
            e.dataTransfer.effectAllowed = 'move';
            e.dataTransfer.setData('text/plain', field.clientId);
            requestAnimationFrame(() => card.classList.add('dragging'));
        });
        card.addEventListener('dragend', () => {
            card.classList.remove('dragging');
            card.dataset.dragArmed = 'false';
            els.cardsContainer.querySelectorAll('.form-field-card').forEach((c) => c.classList.remove('drop-before', 'drop-after'));
        });
        card.addEventListener('dragover', (e) => {
            e.preventDefault();
            const rect = card.getBoundingClientRect();
            const before = e.clientY < rect.top + rect.height / 2;
            card.classList.toggle('drop-before', before);
            card.classList.toggle('drop-after', !before);
        });
        card.addEventListener('dragleave', () => card.classList.remove('drop-before', 'drop-after'));
        card.addEventListener('drop', (e) => {
            e.preventDefault();
            const draggedId = e.dataTransfer.getData('text/plain');
            const before = card.classList.contains('drop-before');
            card.classList.remove('drop-before', 'drop-after');
            reorder(draggedId, field.clientId, before);
        });

        return card;
    }

    function renderBody(field) {
        const body = document.createElement('div');
        body.className = 'form-field-card-body';
        const isSection = field.fieldType === 'SectionHeading';

        const labelRow = document.createElement('div');
        labelRow.className = 'mb-2';
        labelRow.innerHTML = `<label class="form-label">${isSection ? 'Overskrift (valgfri)' : 'Spørgsmål'}</label>`;
        const labelInput = document.createElement('input');
        labelInput.type = 'text';
        labelInput.className = 'form-control';
        labelInput.value = field.label;
        labelInput.placeholder = isSection ? 'Overskrift (valgfri)' : 'Skriv dit spørgsmål her';
        labelInput.disabled = readOnly;
        labelInput.addEventListener('input', () => {
            field.label = labelInput.value;
            const summary = body.parentElement.querySelector('.form-field-summary');
            if (summary) summary.textContent = field.label || summaryFallback(field);
        });
        labelRow.appendChild(labelInput);
        body.appendChild(labelRow);

        const helpRow = document.createElement('div');
        helpRow.className = 'mb-3';
        helpRow.innerHTML = `<label class="form-label">${isSection ? 'Informationstekst (valgfri)' : 'Hjælpetekst (valgfri)'}</label>`;
        const helpInput = document.createElement(isSection ? 'textarea' : 'input');
        if (isSection) {
            helpInput.rows = 3;
            helpInput.placeholder = 'Skriv den information respondenten skal se her, fx vejledning eller kontekst til tilmeldingsformularen.';
        } else {
            helpInput.type = 'text';
        }
        helpInput.className = 'form-control';
        helpInput.value = field.helpText;
        helpInput.disabled = readOnly;
        helpInput.addEventListener('input', () => { field.helpText = helpInput.value; });
        helpRow.appendChild(helpInput);
        body.appendChild(helpRow);

        // Type-vælgeren vises altid (også på et Informationstekst-kort), så man kan skifte
        // begge veje mellem et rigtigt spørgsmål og en statisk tekstblok.
        const typeRow = document.createElement('div');
        typeRow.className = 'mb-3';
        typeRow.innerHTML = '<label class="form-label">Type</label>';
        const typeSelect = document.createElement('select');
        typeSelect.className = 'form-select';
        typeSelect.innerHTML = FIELD_TYPES
            .map((t) => `<option value="${t.type}" ${t.type === field.fieldType ? 'selected' : ''}>${t.label}</option>`)
            .join('');
        typeSelect.disabled = readOnly;
        typeSelect.addEventListener('change', () => {
            field.fieldType = typeSelect.value;
            if (OPTION_TYPES.has(field.fieldType) && field.options.length === 0) {
                field.options = ['Mulighed 1'];
            }
            renderAll();
        });
        typeRow.appendChild(typeSelect);
        body.appendChild(typeRow);

        if (!isSection) {
            if (OPTION_TYPES.has(field.fieldType)) {
                body.appendChild(renderOptionsEditor(field));
            } else {
                body.appendChild(renderPreview(field));
            }

            const requiredRow = document.createElement('div');
            requiredRow.className = 'form-check form-switch mt-2';
            requiredRow.innerHTML = `
                <input class="form-check-input" type="checkbox" id="req_${field.clientId}" ${field.isRequired ? 'checked' : ''} ${readOnly ? 'disabled' : ''}>
                <label class="form-check-label" for="req_${field.clientId}">Påkrævet</label>
            `;
            requiredRow.querySelector('input').addEventListener('change', (e) => { field.isRequired = e.target.checked; });
            body.appendChild(requiredRow);
        }

        return body;
    }

    function renderPreview(field) {
        const wrap = document.createElement('div');
        wrap.className = 'form-field-preview mb-2';
        switch (field.fieldType) {
            case 'LongText':
                wrap.innerHTML = '<textarea class="form-control" rows="2" disabled placeholder="Respondentens svar"></textarea>';
                break;
            case 'Number':
                wrap.innerHTML = '<input type="number" class="form-control" disabled placeholder="0">';
                break;
            case 'Email':
                wrap.innerHTML = '<input type="email" class="form-control" disabled placeholder="navn@eksempel.dk">';
                break;
            case 'Phone':
                wrap.innerHTML = '<input type="tel" class="form-control" disabled placeholder="12 34 56 78">';
                break;
            case 'Date':
                wrap.innerHTML = '<input type="date" class="form-control" disabled>';
                break;
            default:
                wrap.innerHTML = '<input type="text" class="form-control" disabled placeholder="Respondentens svar">';
        }
        return wrap;
    }

    function renderOptionsEditor(field) {
        const wrap = document.createElement('div');
        wrap.className = 'form-field-options-editor mb-2';

        const list = document.createElement('div');
        field.options.forEach((opt, idx) => list.appendChild(renderOptionRow(field, idx)));
        wrap.appendChild(list);

        if (!readOnly) {
            const addBtn = document.createElement('button');
            addBtn.type = 'button';
            addBtn.className = 'btn btn-sm btn-link form-field-add-option';
            addBtn.innerHTML = `<i class="bi bi-plus-lg me-1"></i> Tilføj mulighed`;
            addBtn.addEventListener('click', () => {
                field.options.push('');
                renderAll();
                const inputs = els.cardsContainer.querySelectorAll(`[data-client-id="${field.clientId}"] .form-field-option-input`);
                inputs[inputs.length - 1]?.focus();
            });
            wrap.appendChild(addBtn);
        }

        return wrap;
    }

    function renderOptionRow(field, idx) {
        const row = document.createElement('div');
        row.className = 'form-field-option-row';
        row.dataset.clientId = field.clientId;

        const icon = document.createElement('i');
        icon.className = field.fieldType === 'MultipleChoice' ? 'bi bi-circle' : (field.fieldType === 'Checkboxes' ? 'bi bi-square' : 'bi bi-chevron-down');
        row.appendChild(icon);

        const input = document.createElement('input');
        input.type = 'text';
        input.className = 'form-control form-field-option-input';
        input.value = field.options[idx];
        input.placeholder = `Mulighed ${idx + 1}`;
        input.disabled = readOnly;
        input.addEventListener('input', () => { field.options[idx] = input.value; });
        row.appendChild(input);

        if (!readOnly) {
            const removeBtn = document.createElement('button');
            removeBtn.type = 'button';
            removeBtn.className = 'btn btn-sm btn-link form-field-remove-option';
            removeBtn.innerHTML = '<i class="bi bi-x-lg"></i>';
            removeBtn.title = 'Fjern mulighed';
            removeBtn.setAttribute('aria-label', 'Fjern mulighed');
            removeBtn.addEventListener('click', () => {
                field.options.splice(idx, 1);
                renderAll();
            });
            row.appendChild(removeBtn);
        }

        return row;
    }

    // ─── State mutations ────────────────────────────────────────────────────

    function addField(type) {
        const field = newField(type, null);
        state.fields.push(field);
        activeClientId = field.clientId;
        renderAll();
        els.cardsContainer.querySelector(`[data-client-id="${field.clientId}"]`)?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }

    function duplicateField(clientId) {
        const idx = state.fields.findIndex((f) => f.clientId === clientId);
        if (idx === -1) return;
        const source = state.fields[idx];
        const copy = { ...source, clientId: 'c' + (nextClientId++), id: 0, options: [...source.options] };
        state.fields.splice(idx + 1, 0, copy);
        activeClientId = copy.clientId;
        renderAll();
    }

    function deleteField(clientId) {
        state.fields = state.fields.filter((f) => f.clientId !== clientId);
        if (activeClientId === clientId) activeClientId = null;
        renderAll();
    }

    function reorder(draggedClientId, targetClientId, before) {
        if (draggedClientId === targetClientId) return;
        const draggedIdx = state.fields.findIndex((f) => f.clientId === draggedClientId);
        if (draggedIdx === -1) return;
        const [dragged] = state.fields.splice(draggedIdx, 1);
        let targetIdx = state.fields.findIndex((f) => f.clientId === targetClientId);
        if (targetIdx === -1) targetIdx = state.fields.length;
        state.fields.splice(before ? targetIdx : targetIdx + 1, 0, dragged);
        renderAll();
    }

    // ─── Type picker ────────────────────────────────────────────────────────

    function initTypePicker() {
        els.typePicker.innerHTML = FIELD_TYPES.map((t) => `
            <button type="button" class="form-builder-type-picker-item" data-type="${t.type}">
                <i class="bi ${t.icon}"></i>
                <span>${t.label}</span>
            </button>
        `).join('');
        els.typePicker.addEventListener('click', (e) => {
            const btn = e.target.closest('[data-type]');
            if (!btn) return;
            addField(btn.dataset.type);
            els.typePicker.classList.add('d-none');
        });
        els.addFieldBtn.addEventListener('click', () => els.typePicker.classList.toggle('d-none'));
        document.addEventListener('click', (e) => {
            if (!els.typePicker.contains(e.target) && e.target !== els.addFieldBtn && !els.addFieldBtn.contains(e.target)) {
                els.typePicker.classList.add('d-none');
            }
        });
    }

    // ─── Submit ─────────────────────────────────────────────────────────────

    function validate() {
        const errors = [];
        state.fields.forEach((field, idx) => {
            // Informationstekst er ren statisk tekst til respondenten, ikke et spørgsmål —
            // den må gerne stå uden overskrift.
            if (field.fieldType !== 'SectionHeading' && !field.label.trim()) {
                errors.push(`Spørgsmål ${idx + 1} i tilmeldingsformularen mangler en label.`);
            }
            if (OPTION_TYPES.has(field.fieldType) && field.options.filter((o) => o.trim()).length === 0) {
                errors.push(`'${field.label || 'Spørgsmål ' + (idx + 1)}' skal have mindst én mulighed.`);
            }
        });
        return errors;
    }

    function serializeToHiddenInputs() {
        els.hiddenContainer.innerHTML = '';
        state.fields.forEach((field, idx) => {
            const options = OPTION_TYPES.has(field.fieldType) ? field.options.filter((o) => o.trim()) : null;
            const values = {
                [`FormFields[${idx}].Id`]: field.id,
                [`FormFields[${idx}].Label`]: field.label,
                [`FormFields[${idx}].HelpText`]: field.helpText || '',
                [`FormFields[${idx}].FieldType`]: field.fieldType,
                [`FormFields[${idx}].IsRequired`]: field.isRequired ? 'true' : 'false',
                [`FormFields[${idx}].Order`]: idx,
                [`FormFields[${idx}].OptionsJson`]: options ? JSON.stringify(options) : ''
            };
            Object.entries(values).forEach(([name, value]) => {
                const input = document.createElement('input');
                input.type = 'hidden';
                input.name = name;
                input.value = value;
                els.hiddenContainer.appendChild(input);
            });
        });
    }

    function escapeHtml(str) {
        return (str || '').replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    }

    function init(options) {
        els = options;
        readOnly = !!options.readOnly;
        state.fields = (options.seed.FormFields || []).map((f) => newField(f.FieldType, f));
        if (state.fields.length === 1) activeClientId = state.fields[0].clientId;

        if (!readOnly && els.addFieldBtn && els.typePicker) initTypePicker();
        renderAll();

        if (readOnly) return;

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

    window.FvArrangementFormBuilder = { init };
})();
