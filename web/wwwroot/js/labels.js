// ─── Labels (fælles) ──────────────────────────────────────────────────────────
// Bruges alle steder med labels (Værktøjer → Labels, labels på en linje i en arbejdsliste), så en
// rettelse her gælder overalt:
// • FvLabels.createEditor(root): label-rækker (tekst + antal kopier) —
//   Views/Shared/Partials/_LabelRowsEditor.cshtml.
// • FvLabels.initPrintModal(modalEl, options): "Print labels" — A4-forhåndsvisning, antal labels/ark og
//   "Generér PDF" (åbner PDF'en i en ny fane) — Views/Shared/Partials/_PrintLabelsModal.cshtml.
// Server-side: Infrastructure/Labels (LabelRows, LabelSheetPdf).
(() => {
    // ─── Label-rækker ──────────────────────────────────────────────────────────
    const createEditor = (root) => {
        if (!root) return null;
        if (root.fvLabelEditor) return root.fvLabelEditor;

        const rows = root.querySelector('[data-label-rows]');
        const template = root.querySelector('[data-label-template]');
        const empty = root.querySelector('[data-label-rows-empty]');

        const syncEmpty = () => { empty.hidden = rows.children.length > 0; };

        const addRow = (label) => {
            rows.appendChild(template.content.cloneNode(true));
            const row = rows.lastElementChild;
            if (label) {
                row.querySelector('[data-label-text]').value = label.text || '';
                row.querySelector('[data-label-quantity]').value = label.quantity || 1;
            }
            syncEmpty();
            return row;
        };

        root.querySelector('[data-label-add]')?.addEventListener('click', () => {
            addRow(null).querySelector('[data-label-text]').focus();
        });
        rows.addEventListener('click', (e) => {
            const btn = e.target.closest('[data-label-remove]');
            if (!btn) return;
            btn.closest('[data-label-row]').remove();
            syncEmpty();
        });

        const editor = {
            /** No rows and no "empty" text — while the labels are loading. */
            clear: () => {
                rows.replaceChildren();
                empty.hidden = true;
            },
            /** Replaces the rows. With no labels it opens with one empty label, ready to type. */
            set: (labels) => {
                rows.replaceChildren();
                (labels || []).forEach(addRow);
                if (rows.children.length === 0) addRow(null);
            },
            /** Adds the rows to the form data as a LabelInputViewModel list: Labels[i].Text / Labels[i].Quantity. */
            appendTo: (fd, prefix = 'Labels') => {
                rows.querySelectorAll('[data-label-row]').forEach((row, i) => {
                    fd.append(`${prefix}[${i}].Text`, row.querySelector('[data-label-text]').value);
                    fd.append(`${prefix}[${i}].Quantity`, row.querySelector('[data-label-quantity]').value);
                });
            },
            focusFirst: () => rows.querySelector('[data-label-text]')?.focus()
        };
        root.fvLabelEditor = editor;
        return editor;
    };

    // ─── Print labels ──────────────────────────────────────────────────────────
    // A4 split into "i bredden" × "i højden" labels with no page margin (same maths as LabelSheetPdf).
    // The sheet setup is remembered per browser and shared by every "Print labels" — the same label
    // paper is usually used again.
    const storageKey = 'klubplan.printLabels';

    const formatMm = (mm) => mm.toLocaleString('da-DK', { minimumFractionDigits: 1, maximumFractionDigits: 1 });

    /**
     * options:
     *   pdfUrl       – the PDF action (GET); gets params() plus Across, Down, Landscape, CutMarks.
     *   params       – ({ settings, trigger }) => URLSearchParams saying what to print (trigger = the button that opened the modal).
     *   loadTotals   – ({ settings, trigger }) => { copies, lines } or a Promise of it; throw when it can't be counted.
     *   emptyText    – ({ settings }) => text when there are no labels to print.
     *   linesNoun    – optional ['linje', 'linjer'] → "12 labels fra 3 linjer · 1 ark".
     *   title        – optional (trigger) => modal title (default "Print labels").
     */
    const initPrintModal = (modalEl, options) => {
        if (!modalEl || modalEl.dataset.labelPrintInit) return;
        modalEl.dataset.labelPrintInit = '1';

        const form = modalEl.querySelector('[data-label-print-form]');
        const across = form.querySelector('[data-label-print-across]');
        const down = form.querySelector('[data-label-print-down]');
        const sheet = modalEl.querySelector('[data-label-print-sheet]');
        const size = modalEl.querySelector('[data-label-print-size]');
        const count = modalEl.querySelector('[data-label-print-count]');
        const submit = modalEl.querySelector('[data-label-print-submit]');
        const titleEl = modalEl.querySelector('[data-label-print-title]');
        const totals = { copies: 0, lines: 0, loaded: false };
        let trigger = null;
        let countRequest = 0;

        const settings = () => ({
            across: Number(across.value),
            down: Number(down.value),
            landscape: form.querySelector('[data-label-print-orientation]:checked')?.value === 'true',
            cutMarks: form.querySelector('[data-label-print-cut-marks]')?.checked === true,
            filtered: form.querySelector('[data-label-print-scope]:checked')?.value === 'filter'
        });

        const isValidGrid = (s) => Number.isInteger(s.across) && Number.isInteger(s.down)
            && s.across >= 1 && s.across <= Number(across.max)
            && s.down >= 1 && s.down <= Number(down.max);

        const describeTotals = (perSheet) => {
            const sheets = Math.ceil(totals.copies / perSheet);
            const from = options.linesNoun && totals.lines
                ? ` fra ${totals.lines} ${totals.lines === 1 ? options.linesNoun[0] : options.linesNoun[1]}`
                : '';
            return `${totals.copies} ${totals.copies === 1 ? 'label' : 'labels'}${from} · ${sheets} ark`;
        };

        const renderPreview = () => {
            const s = settings();
            sheet.classList.toggle('is-landscape', s.landscape);
            sheet.replaceChildren();
            if (!isValidGrid(s)) {
                size.textContent = `Angiv 1–${across.max} labels i bredden og 1–${down.max} i højden.`;
                submit.disabled = true;
                return;
            }

            const perSheet = s.across * s.down;
            sheet.style.gridTemplateColumns = `repeat(${s.across}, 1fr)`;
            sheet.style.gridTemplateRows = `repeat(${s.down}, 1fr)`;
            // Filled cells = what the first sheet will hold, starting top left.
            const filled = Math.min(totals.copies, perSheet);
            for (let n = 0; n < perSheet; n++) {
                const cell = document.createElement('span');
                if (n < filled) cell.className = 'is-filled';
                sheet.appendChild(cell);
            }

            const [pageWidth, pageHeight] = s.landscape ? [297, 210] : [210, 297];
            size.textContent = `${formatMm(pageWidth / s.across)} × ${formatMm(pageHeight / s.down)} mm pr. label · ${perSheet} pr. ark`;
            if (!totals.loaded) return;
            count.textContent = totals.copies === 0 ? options.emptyText({ settings: s }) : describeTotals(perSheet);
            submit.disabled = totals.copies === 0;
        };

        const refreshTotals = async () => {
            const request = ++countRequest;
            totals.loaded = false;
            count.textContent = 'Tæller labels…';
            submit.disabled = true;
            try {
                const data = await options.loadTotals({ settings: settings(), trigger });
                if (request !== countRequest) return;
                totals.copies = data.copies || 0;
                totals.lines = data.lines || 0;
                totals.loaded = true;
                renderPreview();
            } catch {
                if (request !== countRequest) return;
                count.textContent = 'Antallet af labels kunne ikke hentes.';
            }
        };

        const loadSettings = () => {
            try {
                const saved = JSON.parse(localStorage.getItem(storageKey) || 'null');
                if (!saved) return;
                if (saved.across) across.value = saved.across;
                if (saved.down) down.value = saved.down;
                const orientation = form.querySelector(`[data-label-print-orientation][value="${saved.landscape ? 'true' : 'false'}"]`);
                if (orientation) orientation.checked = true;
                const cutMarks = form.querySelector('[data-label-print-cut-marks]');
                if (cutMarks) cutMarks.checked = saved.cutMarks === true;
            } catch { /* no storage — keep the defaults */ }
        };
        const saveSettings = (s) => {
            try {
                localStorage.setItem(storageKey, JSON.stringify({ across: s.across, down: s.down, landscape: s.landscape, cutMarks: s.cutMarks }));
            } catch { /* no storage */ }
        };

        modalEl.addEventListener('show.bs.modal', (e) => {
            if (e.target !== modalEl) return;
            trigger = e.relatedTarget || null;
            if (titleEl) titleEl.textContent = options.title?.(trigger) || 'Print labels';
            // Another page's "Print labels" may have saved a newer sheet setup since this one loaded.
            loadSettings();
            renderPreview();
            refreshTotals();
        });
        form.addEventListener('input', (e) => {
            if (e.target.matches('[data-label-print-across], [data-label-print-down]')) renderPreview();
        });
        form.addEventListener('change', (e) => {
            if (e.target.matches('[data-label-print-orientation]')) renderPreview();
            if (e.target.matches('[data-label-print-scope]')) refreshTotals();
        });
        form.addEventListener('submit', (e) => {
            e.preventDefault();
            const s = settings();
            if (!isValidGrid(s) || totals.copies === 0) return;
            const params = options.params({ settings: s, trigger });
            params.set('Across', s.across);
            params.set('Down', s.down);
            params.set('Landscape', s.landscape);
            params.set('CutMarks', s.cutMarks);
            saveSettings(s);
            window.open(`${options.pdfUrl}?${params}`, '_blank', 'noopener');
            bootstrap.Modal.getInstance(modalEl)?.hide();
        });
    };

    window.FvLabels = { createEditor, initPrintModal };
})();
