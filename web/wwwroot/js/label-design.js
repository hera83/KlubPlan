// ─── Labels (Værktøjer): fælles visning af en designet label ──────────────────
// Bruges af label-designeren (Views/Labels/Designer.cshtml), dens miniaturer og "Print labels"
// (tabellen og designeren), så en label ser ens ud alle steder — og som i PDF'en
// (Repositories/LabelCollections/LabelDesignPdf.cs): Lato, linjehøjde 1.2, og tekst der ikke kan være
// i sit felt gøres mindre. Position/størrelse er % af labelen og skriftstørrelse % af labelens højde (cqh).
// • FvLabelDesign.renderLabel / renderElement / fitText — tegner en label.
// • FvLabelDesign.bindSheetFields(root) — ark-opsætningen (i bredden × højden, papirretning) med størrelse og miniature-ark.
// • FvLabelDesign.initPrintModal(modalEl, options) — "Print labels": første ark med de rigtige labels + skæremærker → PDF.
// Styles: site.css (.lbl-*). Datamodel: Repositories/LabelCollections/Dtos/LabelCollectionDesignDto.cs.
(() => {
    const JUSTIFY = { top: 'flex-start', middle: 'center', bottom: 'flex-end' };
    const PT_PER_MM = 72 / 25.4;

    /** Label size in mm for a sheet grid — A4 split into equal cells with no margin (LabelDesignRules.LabelSizeMm). */
    const labelSizeMm = (across, down, landscape) => ({
        width: (landscape ? 297 : 210) / across,
        height: (landscape ? 210 : 297) / down
    });
    const formatMm = (mm) => mm.toLocaleString('da-DK', { maximumFractionDigits: 1 });
    const formatSize = (size) => `${formatMm(size.width)} × ${formatMm(size.height)} mm`;

    /** Prefix + (start + copy) padded to digits + suffix — keep in sync with LabelDesignPdf.SerialText. */
    const serialText = (el, copyIndex = 0) => {
        const number = String((el.start ?? 1) + copyIndex).padStart(el.digits || 0, '0');
        return `${el.prefix || ''}${number}${el.suffix || ''}`;
    };

    /** Lets the browser break a line after "/" like the PDF does, so a link wraps the same way in both. */
    const breakable = (text) => text.replace(/\//g, '/​');

    const placeholder = (icon, text) => {
        const box = document.createElement('div');
        box.className = 'lbl-placeholder';
        const i = document.createElement('i');
        i.className = `bi ${icon}`;
        box.appendChild(i);
        if (text) {
            const span = document.createElement('span');
            span.textContent = text;
            box.appendChild(span);
        }
        return box;
    };

    /**
     * One element as a positioned box. opts.mode: 'edit' (designer canvas — placeholders), 'thumb'
     * (miniatures — icon-only placeholders) or 'print' (no placeholders). opts.copyIndex numbers the
     * løbenummer (0 = first copy). opts.mediaUrl(mediaId) and opts.qrUrl(text, color) build the links.
     */
    const renderElement = (el, opts) => {
        const node = document.createElement('div');
        node.className = `lbl-el lbl-${el.type}`;
        node.dataset.elementId = el.id;
        node.style.left = `${el.x}%`;
        node.style.top = `${el.y}%`;
        node.style.width = `${el.w}%`;
        node.style.height = `${el.h}%`;
        const placeholderText = (text) => (opts.mode === 'edit' ? text : null);

        switch (el.type) {
            case 'text':
            case 'serial': {
                node.classList.add('lbl-text');
                node.dataset.fontSize = el.fontSize || 16;
                node.style.color = el.color || '#000000';
                node.style.fontSize = `${node.dataset.fontSize}cqh`;
                node.style.fontWeight = el.bold ? '700' : '400';
                node.style.fontStyle = el.italic ? 'italic' : 'normal';
                node.style.textAlign = el.align || 'center';
                node.style.justifyContent = JUSTIFY[el.vAlign] || 'center';
                if (el.background) node.style.background = el.background;
                const inner = document.createElement('div');
                inner.className = 'lbl-content';
                if (el.type === 'serial') {
                    inner.textContent = breakable(serialText(el, opts.copyIndex || 0));
                } else if (el.text) {
                    inner.textContent = breakable(el.text);
                } else if (opts.mode === 'edit') {
                    node.classList.add('is-placeholder');
                    inner.textContent = 'Dobbeltklik for at skrive';
                }
                node.appendChild(inner);
                break;
            }
            case 'image': {
                if (!el.mediaId) {
                    if (opts.mode !== 'print') node.appendChild(placeholder('bi-image', placeholderText('Vælg billede')));
                    break;
                }
                const img = document.createElement('img');
                img.src = opts.mediaUrl(el.mediaId);
                img.alt = '';
                img.draggable = false;
                node.appendChild(img);
                break;
            }
            case 'qr': {
                if (!el.text) {
                    if (opts.mode !== 'print') node.appendChild(placeholder('bi-qr-code', placeholderText('Skriv et link')));
                    break;
                }
                const img = document.createElement('img');
                img.src = opts.qrUrl(el.text, el.color || '#000000');
                img.alt = '';
                img.draggable = false;
                node.appendChild(img);
                break;
            }
        }
        return node;
    };

    /** A whole label (background + elements in stacking order), filling its container. Call fitText once it is in the page. */
    const renderLabel = (label, opts) => {
        const node = document.createElement('div');
        node.className = 'lbl-label';
        node.style.background = label.background || '#ffffff';
        (label.elements || []).forEach((el) => node.appendChild(renderElement(el, opts)));
        return node;
    };

    /**
     * Shrinks text that doesn't fit its box, like QuestPDF's ScaleToFit in the PDF. Only works on
     * elements that are laid out (in the page and visible); the result is in cqh, so it stays right
     * when the label is later drawn larger or smaller.
     */
    const fitNode = (node) => {
        const content = node.firstElementChild;
        const base = Number(node.dataset.fontSize) || 16;
        node.style.fontSize = `${base}cqh`;
        if (!content || !node.clientHeight) return;
        const style = getComputedStyle(node);
        const available = node.clientHeight - parseFloat(style.paddingTop) - parseFloat(style.paddingBottom);
        const fits = () => content.scrollHeight <= available + 0.5 && content.scrollWidth <= content.clientWidth + 0.5;
        if (fits()) return;

        let lo = 0.02;
        let hi = 1;
        for (let n = 0; n < 9; n++) {
            const mid = (lo + hi) / 2;
            node.style.fontSize = `${base * mid}cqh`;
            if (fits()) lo = mid;
            else hi = mid;
        }
        node.style.fontSize = `${base * lo}cqh`;
    };
    const fitText = (root) => {
        if (!root) return;
        if (root.matches?.('.lbl-text')) fitNode(root);
        root.querySelectorAll('.lbl-text').forEach(fitNode);
    };

    // Lato may still be loading the first time a label is drawn — refit everything once it is ready.
    document.fonts?.load("16px 'Lato'").then(() => document.fonts.ready).then(() => fitText(document.body)).catch(() => { });

    // ─── Ark-opsætning ─────────────────────────────────────────────────────────
    /**
     * Fields [data-sheet-across], [data-sheet-down] and radios [data-sheet-orientation] inside root;
     * shows the label size in [data-sheet-size] and a miniature A4 sheet in [data-sheet-preview].
     * Returns { read, write, refresh }.
     */
    const bindSheetFields = (root) => {
        const across = root.querySelector('[data-sheet-across]');
        const down = root.querySelector('[data-sheet-down]');
        const size = root.querySelector('[data-sheet-size]');
        const preview = root.querySelector('[data-sheet-preview]');

        const read = () => ({
            across: Number(across.value),
            down: Number(down.value),
            landscape: root.querySelector('[data-sheet-orientation]:checked')?.value === 'true'
        });
        const isValid = (s) => Number.isInteger(s.across) && Number.isInteger(s.down)
            && s.across >= 1 && s.across <= Number(across.max) && s.down >= 1 && s.down <= Number(down.max);

        const refresh = () => {
            const s = read();
            preview.classList.toggle('is-landscape', s.landscape);
            preview.replaceChildren();
            if (!isValid(s)) {
                size.textContent = `Angiv 1–${across.max} labels i bredden og 1–${down.max} i højden.`;
                return;
            }
            preview.style.gridTemplateColumns = `repeat(${s.across}, 1fr)`;
            preview.style.gridTemplateRows = `repeat(${s.down}, 1fr)`;
            for (let n = 0; n < s.across * s.down; n++) preview.appendChild(document.createElement('span'));
            size.textContent = `${formatSize(labelSizeMm(s.across, s.down, s.landscape))} pr. label · ${s.across * s.down} pr. ark`;
        };
        const write = (s) => {
            across.value = s.across;
            down.value = s.down;
            const radio = root.querySelector(`[data-sheet-orientation][value="${s.landscape ? 'true' : 'false'}"]`);
            if (radio) radio.checked = true;
            refresh();
        };

        root.addEventListener('input', (e) => { if (e.target.matches('[data-sheet-across], [data-sheet-down]')) refresh(); });
        root.addEventListener('change', (e) => { if (e.target.matches('[data-sheet-orientation]')) refresh(); });
        refresh();
        return { read, write, refresh, isValid: () => isValid(read()) };
    };

    // ─── Print labels ──────────────────────────────────────────────────────────
    const storageKey = 'klubplan.labelDesignPrint';

    /**
     * options:
     *   pdfUrl   – the Pdf action (GET ?Id=…&CutMarks=…).
     *   load     – (trigger) => { id, design } or a Promise of it (trigger = the button that opened the modal); throw when it fails.
     *   mediaUrl / qrUrl – as for renderLabel.
     */
    const initPrintModal = (modalEl, options) => {
        if (!modalEl || modalEl.dataset.labelDesignPrintInit) return;
        modalEl.dataset.labelDesignPrintInit = '1';

        const form = modalEl.querySelector('[data-ldp-form]');
        const sheet = modalEl.querySelector('[data-ldp-sheet]');
        const summary = modalEl.querySelector('[data-ldp-summary]');
        const size = modalEl.querySelector('[data-ldp-size]');
        const titleEl = modalEl.querySelector('[data-ldp-title]');
        const cutMarks = modalEl.querySelector('[data-ldp-cut-marks]');
        const submit = modalEl.querySelector('[data-ldp-submit]');
        let current = null;
        let request = 0;

        const renderSheet = () => {
            if (!current || !modalEl.classList.contains('show')) return;
            const { design } = current;
            const perSheet = design.across * design.down;
            const copies = [];
            design.labels.forEach((label) => {
                for (let i = 0; i < label.quantity && copies.length < perSheet; i++) copies.push({ label, i });
            });

            sheet.classList.toggle('is-landscape', design.landscape);
            sheet.style.gridTemplateColumns = `repeat(${design.across}, 1fr)`;
            sheet.style.gridTemplateRows = `repeat(${design.down}, 1fr)`;
            const cells = [];
            for (let n = 0; n < perSheet; n++) {
                const cell = document.createElement('div');
                if (n < copies.length) {
                    cell.appendChild(renderLabel(copies[n].label, { mode: 'print', copyIndex: copies[n].i, mediaUrl: options.mediaUrl, qrUrl: options.qrUrl }));
                } else {
                    cell.className = 'is-empty';
                }
                cells.push(cell);
            }
            sheet.replaceChildren(...cells);
            fitText(sheet);
        };

        const show = (data) => {
            current = data;
            const { design } = data;
            const total = design.labels.reduce((sum, l) => sum + l.quantity, 0);
            const perSheet = design.across * design.down;
            const sheets = Math.ceil(total / perSheet);
            titleEl.textContent = design.name ? `Print labels – ${design.name}` : 'Print labels';
            size.textContent = `${formatSize(labelSizeMm(design.across, design.down, design.landscape))} pr. label · ${perSheet} pr. ark (${design.across} × ${design.down}, ${design.landscape ? 'liggende' : 'stående'})`;
            summary.textContent = total === 0
                ? 'Der er ingen labels i samlingen. Design en label først.'
                : `${total} ${total === 1 ? 'label' : 'labels'} af ${design.labels.length} ${design.labels.length === 1 ? 'design' : 'forskellige designs'} · ${sheets} ark`;
            submit.disabled = total === 0;
            renderSheet();
        };

        modalEl.addEventListener('show.bs.modal', async (e) => {
            if (e.target !== modalEl) return;
            const r = ++request;
            current = null;
            sheet.replaceChildren();
            summary.textContent = 'Henter labels…';
            size.textContent = '';
            submit.disabled = true;
            try {
                cutMarks.checked = localStorage.getItem(storageKey) === 'cutMarks';
            } catch { /* no storage */ }
            try {
                const data = await options.load(e.relatedTarget || null);
                if (r === request) show(data);
            } catch {
                if (r === request) summary.textContent = 'Labels kunne ikke hentes.';
            }
        });
        // The labels' text is fitted once the modal is visible (it needs the layout).
        modalEl.addEventListener('shown.bs.modal', (e) => { if (e.target === modalEl) renderSheet(); });

        form.addEventListener('submit', (e) => {
            e.preventDefault();
            if (!current || submit.disabled) return;
            try {
                localStorage.setItem(storageKey, cutMarks.checked ? 'cutMarks' : '');
            } catch { /* no storage */ }
            const params = new URLSearchParams({ Id: current.id, CutMarks: cutMarks.checked });
            window.open(`${options.pdfUrl}?${params}`, '_blank', 'noopener');
            bootstrap.Modal.getInstance(modalEl)?.hide();
        });
    };

    window.FvLabelDesign = { PT_PER_MM, labelSizeMm, formatMm, formatSize, serialText, renderLabel, renderElement, fitText, bindSheetFields, initPrintModal };
})();
