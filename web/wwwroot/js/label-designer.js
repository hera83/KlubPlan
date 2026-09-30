// ─── Label-designer (Labels/Designer) ─────────────────────────────────────────
// Bygget som infoskærm-designeren (infoscreen-designer.js), så de to virker ens:
// • Hele samlingen (navn + label-ark + labels + elementer) holdes i hukommelsen og gemmes samlet (Gem / Ctrl+S).
// • Labels: liste til venstre (klik, træk for at flytte, ny/duplikér/slet), antal kopier og baggrund.
// • Lærred: elementer (tekst, billede, QR-kode, løbenummer) flyttes og skaleres med musen inden for labelen,
//   med hjælpelinjer til labelens midte/kanter og de andre elementer. Dobbeltklik på tekst for at skrive direkte.
// • Placering vises i mm og skriftstørrelse i pt ud fra label-arket; gemt som % af labelen, så designet
//   følger med, hvis arket ændres.
// • Fortryd/gentag (Ctrl+Z / Ctrl+Y), kopiér/indsæt elementer mellem labels (Ctrl+C / Ctrl+V).
// • Billedbibliotek pr. samling: upload (knap eller træk filer ind) og genbrug af billeder.
// Labels tegnes af den fælles label-design.js, så lærredet ser ud som print-forhåndsvisningen og PDF'en.
// Markup: Views/Labels/Designer.cshtml + _Designer*.cshtml.
(() => {
    const DEFAULT_BACKGROUND = '#ffffff';
    const DEFAULT_FONT_SIZE = 16;
    const MAX_QUANTITY = 500;
    const MIN_SIZE = 1;
    const SNAP = 1;
    const TYPE_LABELS = { text: 'Tekst', image: 'Billede', qr: 'QR-kode', serial: 'Løbenummer' };
    const TYPE_ICONS = { text: 'bi-fonts', image: 'bi-image', qr: 'bi-qr-code', serial: 'bi-123' };
    const IMAGE_TYPES = ['.png', '.jpg', '.jpeg', '.webp'];

    const uid = () => `e${Math.random().toString(36).slice(2, 10)}`;
    const round = (v) => Math.round(v * 100) / 100;
    const round1 = (v) => Math.round(v * 10) / 10;
    const clamp = (v, min, max) => Math.min(max, Math.max(min, v));
    const clone = (o) => JSON.parse(JSON.stringify(o));
    const newLabel = (base) => ({
        quantity: base?.quantity || 1,
        background: base?.background || DEFAULT_BACKGROUND,
        elements: []
    });
    const formatBytes = (b) => (b >= 1024 * 1024
        ? `${(b / (1024 * 1024)).toLocaleString('da-DK', { maximumFractionDigits: 1 })} MB`
        : `${Math.max(1, Math.round(b / 1024))} KB`);
    const toast = (type, message) => window.FvToast?.show(type, message);

    const init = (page) => {
        if (page.dataset.ldInit) return;
        page.dataset.ldInit = '1';

        const L = window.FvLabelDesign;
        const ds = page.dataset;
        const q = (sel) => page.querySelector(sel);
        const token = () => q('input[name="__RequestVerificationToken"]')?.value || '';

        const seed = JSON.parse(q('[data-ld-seed]').textContent);
        const state = seed.design;
        state.labels = state.labels || [];
        if (state.labels.length === 0) state.labels.push(newLabel());
        let media = seed.media || [];

        let current = 0;
        let selectedId = null;
        let dirty = false;
        let changeCount = 0;
        let clipboard = null;
        let editingNode = null;
        const undoStack = [];
        const redoStack = [];
        let lastUndoKey = null;
        let lastUndoAt = 0;

        // Document/window listeners are dropped when the page leaves the DOM (menu navigation swaps the content).
        const docListeners = new AbortController();
        const alive = () => {
            if (page.isConnected) return true;
            docListeners.abort();
            return false;
        };

        const labelList = q('[data-ld-label-list]');
        const stageWrap = q('[data-ld-stage-wrap]');
        const stage = q('[data-ld-stage]');
        const canvas = q('[data-ld-canvas]');
        const selection = q('[data-ld-selection]');
        const guideV = q('[data-ld-guide-v]');
        const guideH = q('[data-ld-guide-h]');
        const labelPanel = q('[data-ld-panel="label"]');
        const elementPanel = q('[data-ld-panel="element"]');
        const undoBtn = q('[data-ld-undo]');
        const redoBtn = q('[data-ld-redo]');
        const saveBtn = q('[data-ld-save]');

        const mediaUrl = (id) => `${ds.urlMedia}?id=${encodeURIComponent(id)}`;
        const qrUrl = (text, color) => `${ds.urlQr}?${new URLSearchParams({ text, color })}`;
        const opts = (mode) => ({ mode, mediaUrl, qrUrl, copyIndex: 0 });
        const label = () => state.labels[current];
        const selected = () => (selectedId ? label().elements.find((e) => e.id === selectedId) || null : null);
        const findMedia = (id) => media.find((m) => m.id === id);
        const mediaInUse = (id) => state.labels.some((l) => l.elements.some((e) => e.mediaId === id));

        // ─── Enheder: % af labelen ↔ mm og pt ──────────────────────────────────
        const sizeMm = () => L.labelSizeMm(state.across, state.down, state.landscape);
        const ratio = () => sizeMm().width / sizeMm().height;
        const axisMm = (key) => (key === 'x' || key === 'w' ? sizeMm().width : sizeMm().height);
        const ptPerPercent = () => (sizeMm().height / 100) * L.PT_PER_MM;

        // ─── Top bar ───────────────────────────────────────────────────────────
        const syncTop = () => {
            q('[data-ld-title]').textContent = state.name;
            q('[data-ld-dirty]').hidden = !dirty;
            q('[data-ld-size]').textContent = `${L.formatSize(sizeMm())} · ${state.across} × ${state.down} pr. ark`;
        };
        const markDirty = () => {
            dirty = true;
            changeCount++;
            syncTop();
        };
        const syncUndo = () => {
            undoBtn.disabled = undoStack.length === 0;
            redoBtn.disabled = redoStack.length === 0;
        };

        // ─── Fortryd / gentag ──────────────────────────────────────────────────
        const snapshot = () => JSON.stringify({ state, current, selectedId });

        /** Saves the state before a change. Repeated changes with the same key (typing, nudging) become one step. */
        const pushUndo = (key = null) => {
            const now = Date.now();
            if (key && key === lastUndoKey && now - lastUndoAt < 1500) {
                lastUndoAt = now;
                return;
            }
            undoStack.push(snapshot());
            if (undoStack.length > 100) undoStack.shift();
            redoStack.length = 0;
            lastUndoKey = key;
            lastUndoAt = now;
            syncUndo();
        };
        const restore = (snap) => {
            const s = JSON.parse(snap);
            Object.keys(state).forEach((k) => delete state[k]);
            Object.assign(state, s.state);
            current = clamp(s.current, 0, state.labels.length - 1);
            selectedId = s.selectedId && label().elements.some((e) => e.id === s.selectedId) ? s.selectedId : null;
            lastUndoKey = null;
            markDirty();
            renderAll();
        };
        const undo = () => {
            if (!undoStack.length) return;
            finishTextEdit();
            redoStack.push(snapshot());
            restore(undoStack.pop());
            syncUndo();
        };
        const redo = () => {
            if (!redoStack.length) return;
            finishTextEdit();
            undoStack.push(snapshot());
            restore(redoStack.pop());
            syncUndo();
        };

        // ─── Tegning ───────────────────────────────────────────────────────────
        const layoutStage = () => {
            const r = ratio();
            page.style.setProperty('--isd-ratio', r);
            const box = stageWrap.getBoundingClientRect();
            let w = Math.max(0, box.width - 48);
            let h = Math.max(0, box.height - 48);
            if (h * r < w) w = h * r;
            else h = w / r;
            stage.style.width = `${w}px`;
            stage.style.height = `${h}px`;
        };

        const elementNode = (id) => canvas.querySelector(`.lbl-el[data-element-id="${CSS.escape(id)}"]`);

        const renderSelection = () => {
            const el = selected();
            selection.hidden = !el;
            if (!el) return;
            selection.style.left = `${el.x}%`;
            selection.style.top = `${el.y}%`;
            selection.style.width = `${el.w}%`;
            selection.style.height = `${el.h}%`;
        };

        const renderCanvas = () => {
            canvas.replaceChildren(L.renderLabel(label(), opts('edit')));
            L.fitText(canvas);
            renderSelection();
        };

        const renderElementNode = (el) => {
            const old = elementNode(el.id);
            if (old) {
                const node = L.renderElement(el, opts('edit'));
                old.replaceWith(node);
                L.fitText(node);
            }
            renderSelection();
        };

        const applyGeometry = (el) => {
            const node = elementNode(el.id);
            if (!node) return;
            node.style.left = `${el.x}%`;
            node.style.top = `${el.y}%`;
            node.style.width = `${el.w}%`;
            node.style.height = `${el.h}%`;
            L.fitText(node);
        };

        const renderLabelItem = (i) => {
            const l = state.labels[i];
            const item = document.createElement('div');
            item.className = 'isd-slide-item';
            item.classList.toggle('is-current', i === current);
            item.draggable = true;
            item.tabIndex = 0;
            item.dataset.index = i;
            item.setAttribute('role', 'button');
            item.setAttribute('aria-label', `Label ${i + 1}, ${l.quantity} stk.`);

            const num = document.createElement('span');
            num.className = 'isd-slide-num';
            num.textContent = i + 1;
            const thumb = document.createElement('div');
            thumb.className = 'isd-thumb';
            thumb.appendChild(L.renderLabel(l, opts('thumb')));
            const meta = document.createElement('span');
            meta.className = 'isd-slide-meta';
            meta.innerHTML = '<i class="bi bi-files"></i> ';
            meta.append(`${l.quantity} stk.`);

            const body = document.createElement('div');
            body.className = 'isd-slide-body';
            body.append(thumb, meta);
            item.append(num, body);
            return item;
        };

        const renderTotals = () => {
            const copies = state.labels.reduce((sum, l) => sum + l.quantity, 0);
            const sheets = Math.ceil(copies / (state.across * state.down));
            const totals = q('[data-ld-totals]');
            totals.textContent = `${copies} stk. · ${sheets} ark`;
            totals.title = `${state.labels.length} ${state.labels.length === 1 ? 'label' : 'forskellige labels'}, ${copies} kopier i alt – ${sheets} A4-ark`;
        };

        const renderLabelList = () => {
            labelList.replaceChildren(...state.labels.map((_, i) => renderLabelItem(i)));
            L.fitText(labelList);
            renderTotals();
        };

        const refreshLabelItem = (i) => {
            const item = renderLabelItem(i);
            labelList.querySelector(`[data-index="${i}"]`)?.replaceWith(item);
            L.fitText(item);
            renderTotals();
        };

        let thumbTimer = null;
        const refreshThumbSoon = () => {
            clearTimeout(thumbTimer);
            const i = current;
            thumbTimer = setTimeout(() => refreshLabelItem(i), 200);
        };

        // ─── Egenskaber ────────────────────────────────────────────────────────
        const setValue = (input, value) => {
            if (input === document.activeElement) return;
            if (input.type === 'checkbox') input.checked = !!value;
            else if (input.type === 'radio') input.checked = input.value === value;
            else if (input.type === 'color') input.value = value || '#000000';
            else input.value = value ?? '';
        };

        const renderSerialRange = (el) => {
            const quantity = label().quantity;
            q('[data-ld-serial-range]').textContent = quantity === 1
                ? `Labelen printes 1 gang: ${L.serialText(el, 0)}`
                : `Labelen printes ${quantity} gange: ${L.serialText(el, 0)} – ${L.serialText(el, quantity - 1)}`;
        };

        const renderProps = () => {
            const el = selected();
            labelPanel.hidden = !!el;
            elementPanel.hidden = !el;
            if (!el) {
                const l = label();
                q('[data-ld-label-heading]').textContent = `Label ${current + 1} af ${state.labels.length}`;
                labelPanel.querySelectorAll('[data-ld-label-prop]').forEach((input) => setValue(input, l[input.dataset.ldLabelProp]));
                q('[data-ld-delete-label]').disabled = state.labels.length <= 1;
                return;
            }

            q('[data-ld-element-label]').innerHTML = `<i class="bi ${TYPE_ICONS[el.type]} me-1"></i>`;
            q('[data-ld-element-label]').append(TYPE_LABELS[el.type]);
            elementPanel.querySelectorAll('[data-ld-show]').forEach((group) => {
                group.hidden = !group.dataset.ldShow.split(' ').includes(el.type);
            });
            elementPanel.querySelectorAll('[data-ld-prop]').forEach((input) => {
                const value = el[input.dataset.ldProp];
                setValue(input, input.tagName === 'SELECT' ? String(value ?? '') : value);
            });
            elementPanel.querySelectorAll('[data-ld-geom]').forEach((input) => {
                const key = input.dataset.ldGeom;
                setValue(input, round1((el[key] / 100) * axisMm(key)));
            });

            if (el.type === 'text' || el.type === 'serial') {
                const pt = round1((el.fontSize || DEFAULT_FONT_SIZE) * ptPerPercent());
                const maxPt = Math.max(8, Math.round(100 * ptPerPercent()));
                elementPanel.querySelectorAll('[data-ld-font-pt]').forEach((input) => {
                    input.max = maxPt;
                    setValue(input, pt);
                });
            }
            if (el.type === 'serial') renderSerialRange(el);

            const bg = q('[data-ld-background]');
            setValue(bg, el.background || '#ffffff');
            bg.disabled = !el.background;
            setValue(q('[data-ld-background-none]'), !el.background);
            q('[data-ld-color-label]').textContent = el.type === 'qr' ? 'Farve på koden' : 'Tekstfarve';
            q('[data-ld-media-name]').textContent = el.mediaId ? (findMedia(el.mediaId)?.name || 'Billedet findes ikke længere') : 'Intet billede valgt';

            const list = label().elements;
            const index = list.indexOf(el);
            elementPanel.querySelectorAll('[data-ld-layer="front"], [data-ld-layer="forward"]').forEach((b) => { b.disabled = index === list.length - 1; });
            elementPanel.querySelectorAll('[data-ld-layer="back"], [data-ld-layer="backward"]').forEach((b) => { b.disabled = index === 0; });
        };

        const renderAll = () => {
            layoutStage();
            renderLabelList();
            renderCanvas();
            renderProps();
            syncTop();
            syncUndo();
        };

        /**
         * Every change goes through here: undo step, change, dirty, redraw.
         * scope: 'element' (the selected element), 'label' (current label + its thumbnail) or 'all'.
         */
        const commit = (key, mutate, scope = 'element') => {
            pushUndo(key);
            mutate();
            markDirty();
            if (scope === 'all') {
                renderAll();
            } else if (scope === 'label') {
                refreshLabelItem(current);
                renderCanvas();
                renderProps();
            } else {
                const el = selected();
                if (el) renderElementNode(el);
                renderProps();
                refreshThumbSoon();
            }
        };

        /** Keeps a box inside the label — on the sheet, anything outside would print on the neighbouring label. */
        const keepInside = (el) => {
            el.w = round(clamp(el.w, 0.5, 100));
            el.h = round(clamp(el.h, 0.5, 100));
            el.x = round(clamp(el.x, 0, 100 - el.w));
            el.y = round(clamp(el.y, 0, 100 - el.h));
        };

        // ─── Markering og labels ───────────────────────────────────────────────
        const select = (id) => {
            if (selectedId === id) return;
            finishTextEdit();
            selectedId = id;
            renderSelection();
            renderProps();
        };

        const goToLabel = (i) => {
            if (i < 0 || i >= state.labels.length) return;
            finishTextEdit();
            current = i;
            selectedId = null;
            labelList.querySelectorAll('.isd-slide-item').forEach((item) => item.classList.toggle('is-current', Number(item.dataset.index) === current));
            labelList.querySelector(`[data-index="${current}"]`)?.scrollIntoView({ block: 'nearest' });
            renderCanvas();
            renderProps();
        };

        const addLabel = () => commit(null, () => {
            state.labels.splice(current + 1, 0, newLabel(label()));
            current++;
            selectedId = null;
        }, 'all');

        const duplicateLabel = () => {
            const copy = clone(label());
            copy.elements.forEach((e) => { e.id = uid(); });
            commit(null, () => {
                state.labels.splice(current + 1, 0, copy);
                current++;
                selectedId = null;
            }, 'all');
        };

        const deleteLabel = () => {
            if (state.labels.length <= 1) return;
            commit(null, () => {
                state.labels.splice(current, 1);
                current = Math.min(current, state.labels.length - 1);
                selectedId = null;
            }, 'all');
            toast('info', 'Labelen er slettet. Fortryd med Ctrl+Z.');
        };

        const moveLabel = (from, to) => {
            if (from === to) return;
            commit(null, () => {
                const [moved] = state.labels.splice(from, 1);
                state.labels.splice(to, 0, moved);
                current = to;
                selectedId = null;
            }, 'all');
        };

        // ─── Elementer ─────────────────────────────────────────────────────────
        const textDefaults = () => ({ fontSize: DEFAULT_FONT_SIZE, bold: false, italic: false, align: 'center', vAlign: 'middle', color: '#000000', background: '' });

        const addElement = (el) => {
            const l = label();
            // A new element never lands exactly on top of another one (as long as there is room to move it).
            while (l.elements.some((e) => e.x === el.x && e.y === el.y) && el.x + el.w + 3 <= 100 && el.y + el.h + 3 <= 100) {
                el.x = round(el.x + 3);
                el.y = round(el.y + 3);
            }
            keepInside(el);
            finishTextEdit();
            commit(null, () => {
                l.elements.push(el);
                selectedId = el.id;
            }, 'label');
        };

        /** Width:height of an image, so it's inserted in its own shape. Null when unknown. */
        const naturalRatio = (m) => new Promise((resolve) => {
            const done = (value) => resolve(Number.isFinite(value) && value > 0 ? value : null);
            setTimeout(() => done(null), 4000);
            const img = new Image();
            img.onload = () => done(img.naturalWidth / img.naturalHeight);
            img.onerror = () => done(null);
            img.src = mediaUrl(m.id);
        });

        const addImageElement = async (m) => {
            const r = ratio();
            const imageRatio = (await naturalRatio(m)) || 1;
            // In % of the label: w% / h% = imageRatio / labelRatio.
            let h = 80;
            let w = (h * imageRatio) / r;
            if (w > 80) {
                w = 80;
                h = (w * r) / imageRatio;
            }
            addElement({ id: uid(), type: 'image', x: round((100 - w) / 2), y: round((100 - h) / 2), w: round(w), h: round(h), mediaId: m.id });
        };

        /** The first of the rows (y positions) where a centred box doesn't cover another element. */
        const freeRow = (w, h, rows) => {
            const overlaps = (a, b) => a.x < b.x + b.w && b.x < a.x + a.w && a.y < b.y + b.h && b.y < a.y + a.h;
            const x = round((100 - w) / 2);
            const y = rows.find((top) => !label().elements.some((e) => overlaps({ x, y: top, w, h }, e)));
            return { x, y: y ?? rows[0] };
        };

        const insert = (type) => {
            const r = ratio();
            switch (type) {
                case 'text': {
                    const spot = freeRow(90, 36, [32, 4, 60]);
                    addElement({ id: uid(), type, ...spot, w: 90, h: 36, text: '', ...textDefaults() });
                    startTextEdit();
                    break;
                }
                case 'serial': {
                    const spot = freeRow(60, 30, [35, 66, 4]);
                    addElement({ id: uid(), type, ...spot, w: 60, h: 30, start: 1, digits: 3, prefix: 'Nr. ', suffix: '', ...textDefaults(), fontSize: 20, bold: true });
                    q('#ldSerialStart')?.focus();
                    break;
                }
                case 'qr': {
                    // Square on paper: w% = h% / labelRatio.
                    let h = 80;
                    let w = h / r;
                    if (w > 80) {
                        w = 80;
                        h = w * r;
                    }
                    const marginX = 5 / r;
                    addElement({ id: uid(), type, x: round(100 - marginX - w), y: round((100 - h) / 2), w: round(w), h: round(h), text: '', color: '#000000' });
                    q('#ldQrText')?.focus();
                    break;
                }
                case 'image':
                    openMedia(addImageElement);
                    break;
            }
        };

        const removeSelected = () => {
            const el = selected();
            if (!el) return;
            finishTextEdit();
            commit(null, () => {
                label().elements = label().elements.filter((e) => e !== el);
                selectedId = null;
            }, 'label');
        };

        const duplicateSelected = () => {
            const el = selected();
            if (!el) return;
            addElement({ ...clone(el), id: uid(), x: round(el.x + 3), y: round(el.y + 3) });
        };

        const paste = () => {
            if (!clipboard) return;
            addElement({ ...clone(clipboard), id: uid() });
        };

        const moveLayer = (where) => {
            const el = selected();
            if (!el) return;
            const list = label().elements;
            const from = list.indexOf(el);
            const to = { front: list.length - 1, back: 0, forward: Math.min(from + 1, list.length - 1), backward: Math.max(from - 1, 0) }[where];
            if (from === to) return;
            commit(null, () => {
                list.splice(from, 1);
                list.splice(to, 0, el);
            }, 'label');
        };

        const arrange = (how) => {
            const el = selected();
            if (!el) return;
            commit(null, () => {
                if (how === 'fill') Object.assign(el, { x: 0, y: 0, w: 100, h: 100 });
                if (how === 'center-h') el.x = round((100 - el.w) / 2);
                if (how === 'center-v') el.y = round((100 - el.h) / 2);
            });
        };

        // ─── Skriv direkte på labelen ──────────────────────────────────────────
        const startTextEdit = () => {
            const el = selected();
            if (!el || el.type !== 'text' || editingNode) return;
            const node = elementNode(el.id);
            const content = node?.querySelector('.lbl-content');
            if (!content) return;

            node.classList.remove('is-placeholder');
            content.textContent = el.text || '';
            content.contentEditable = 'plaintext-only';
            if (content.contentEditable !== 'plaintext-only') content.contentEditable = 'true';
            editingNode = content;
            selection.classList.add('is-editing');
            content.focus();
            const range = document.createRange();
            range.selectNodeContents(content);
            const sel = window.getSelection();
            sel.removeAllRanges();
            sel.addRange(range);

            content.addEventListener('blur', finishTextEdit, { once: true });
            content.addEventListener('keydown', (ev) => {
                if (ev.key === 'Escape') {
                    ev.preventDefault();
                    content.blur();
                }
            });
        };

        function finishTextEdit() {
            if (!editingNode) return;
            const content = editingNode;
            editingNode = null;
            selection.classList.remove('is-editing');
            content.contentEditable = 'false';
            const id = content.closest('.lbl-el')?.dataset.elementId;
            const el = label().elements.find((e) => e.id === id);
            if (!el) return;
            const text = content.innerText.replace(/\n$/, '');
            if (text !== (el.text || '')) {
                pushUndo(null);
                el.text = text;
                markDirty();
                refreshThumbSoon();
            }
            renderElementNode(el);
            if (el === selected()) renderProps();
        }

        // ─── Flyt og skalér med musen ──────────────────────────────────────────
        const showGuides = (v, h) => {
            guideV.hidden = v === null;
            guideH.hidden = h === null;
            if (v !== null) guideV.style.left = `${v}%`;
            if (h !== null) guideH.style.top = `${h}%`;
        };

        /** mode: 'move' or a handle (n, ne, e, se, s, sw, w, nw). */
        const startDrag = (e, mode) => {
            const el = selected();
            if (!el || editingNode) return;
            e.preventDefault();
            const rect = stage.getBoundingClientRect();
            const start = { x: e.clientX, y: e.clientY, geo: { x: el.x, y: el.y, w: el.w, h: el.h } };
            const others = label().elements.filter((o) => o !== el);
            const vLines = [0, 50, 100, ...others.flatMap((o) => [o.x, o.x + o.w / 2, o.x + o.w])];
            const hLines = [0, 50, 100, ...others.flatMap((o) => [o.y, o.y + o.h / 2, o.y + o.h])];
            const nearest = (values, lines) => {
                let best = null;
                values.forEach((v) => lines.forEach((line) => {
                    const d = line - v;
                    if (Math.abs(d) <= SNAP && (!best || Math.abs(d) < Math.abs(best.d))) best = { d, line };
                }));
                return best;
            };
            let moved = false;

            const onMove = (ev) => {
                if (!moved) {
                    if (Math.abs(ev.clientX - start.x) + Math.abs(ev.clientY - start.y) < 3) return;
                    pushUndo(null);
                    moved = true;
                }
                const dx = ((ev.clientX - start.x) / rect.width) * 100;
                const dy = ((ev.clientY - start.y) / rect.height) * 100;
                const snap = !ev.altKey;
                const g = start.geo;
                let { x, y, w, h } = g;
                let gv = null;
                let gh = null;

                if (mode === 'move') {
                    x = clamp(x + dx, 0, 100 - w);
                    y = clamp(y + dy, 0, 100 - h);
                    if (snap) {
                        const sx = nearest([x, x + w / 2, x + w], vLines);
                        if (sx) { x += sx.d; gv = sx.line; }
                        const sy = nearest([y, y + h / 2, y + h], hLines);
                        if (sy) { y += sy.d; gh = sy.line; }
                    }
                } else {
                    const keepShape = ev.shiftKey && mode.length === 2;
                    if (mode.includes('e')) w = g.w + dx;
                    if (mode.includes('w')) { w = g.w - dx; x = g.x + dx; }
                    if (mode.includes('s')) h = g.h + dy;
                    if (mode.includes('n')) { h = g.h - dy; y = g.y + dy; }
                    if (keepShape) {
                        const shape = g.w / g.h;
                        if (Math.abs(w - g.w) / g.w >= Math.abs(h - g.h) / g.h) {
                            const nh = w / shape;
                            if (mode.includes('n')) y = g.y + g.h - nh;
                            h = nh;
                        } else {
                            const nw = h * shape;
                            if (mode.includes('w')) x = g.x + g.w - nw;
                            w = nw;
                        }
                    } else if (snap) {
                        if (mode.includes('e')) { const s = nearest([x + w], vLines); if (s) { w += s.d; gv = s.line; } }
                        if (mode.includes('w')) { const s = nearest([x], vLines); if (s) { x += s.d; w -= s.d; gv = s.line; } }
                        if (mode.includes('s')) { const s = nearest([y + h], hLines); if (s) { h += s.d; gh = s.line; } }
                        if (mode.includes('n')) { const s = nearest([y], hLines); if (s) { y += s.d; h -= s.d; gh = s.line; } }
                    }
                    if (w < MIN_SIZE) { if (mode.includes('w')) x -= MIN_SIZE - w; w = MIN_SIZE; }
                    if (h < MIN_SIZE) { if (mode.includes('n')) y -= MIN_SIZE - h; h = MIN_SIZE; }
                    // The edge being dragged stops at the label's edge.
                    if (x < 0) { w += x; x = 0; }
                    if (y < 0) { h += y; y = 0; }
                    w = Math.min(w, 100 - x);
                    h = Math.min(h, 100 - y);
                }

                Object.assign(el, { x: round(x), y: round(y), w: round(w), h: round(h) });
                applyGeometry(el);
                renderSelection();
                showGuides(gv, gh);
            };
            const onUp = () => {
                window.removeEventListener('pointermove', onMove);
                window.removeEventListener('pointerup', onUp);
                window.removeEventListener('pointercancel', onUp);
                showGuides(null, null);
                if (!moved) return;
                markDirty();
                renderProps();
                refreshThumbSoon();
            };
            window.addEventListener('pointermove', onMove);
            window.addEventListener('pointerup', onUp);
            window.addEventListener('pointercancel', onUp);
        };

        canvas.addEventListener('pointerdown', (e) => {
            if (e.button !== 0) return;
            if (editingNode && editingNode.contains(e.target)) return;
            const node = e.target.closest('.lbl-el');
            if (!node) {
                select(null);
                return;
            }
            select(node.dataset.elementId);
            startDrag(e, 'move');
        });
        selection.addEventListener('pointerdown', (e) => {
            if (e.button !== 0) return;
            const handle = e.target.closest('[data-handle]');
            startDrag(e, handle ? handle.dataset.handle : 'move');
        });
        stageWrap.addEventListener('pointerdown', (e) => {
            if (e.target === stageWrap) select(null);
        });
        stage.addEventListener('dblclick', () => {
            const el = selected();
            if (!el) return;
            if (el.type === 'text') startTextEdit();
            else if (el.type === 'image') changeMedia();
            else if (el.type === 'qr') q('#ldQrText')?.focus();
            else if (el.type === 'serial') q('#ldSerialStart')?.focus();
        });

        // ─── Paneler og knapper ────────────────────────────────────────────────
        q('.isd-toolbar').addEventListener('click', (e) => {
            const add = e.target.closest('[data-ld-add]');
            if (add) insert(add.dataset.ldAdd);
        });
        undoBtn.addEventListener('click', undo);
        redoBtn.addEventListener('click', redo);
        q('[data-ld-add-label]').addEventListener('click', addLabel);

        const onLabelProp = (e) => {
            const input = e.target.closest('[data-ld-label-prop]');
            if (!input) return;
            const prop = input.dataset.ldLabelProp;
            let value = input.value;
            if (prop === 'quantity') {
                value = parseInt(value, 10);
                if (!Number.isFinite(value) || value < 1) return;
                value = Math.min(value, MAX_QUANTITY);
            }
            if (label()[prop] === value) return;
            commit(`label:${current}:${prop}`, () => { label()[prop] = value; }, 'label');
        };
        labelPanel.addEventListener('input', onLabelProp);
        labelPanel.addEventListener('change', onLabelProp);
        labelPanel.addEventListener('click', (e) => {
            const swatch = e.target.closest('[data-ld-swatches] [data-color]');
            if (swatch) {
                if (label().background !== swatch.dataset.color) commit(null, () => { label().background = swatch.dataset.color; }, 'label');
                return;
            }
            if (e.target.closest('[data-ld-quantity-all]')) {
                const quantity = label().quantity;
                commit(null, () => state.labels.forEach((l) => { l.quantity = quantity; }), 'all');
                toast('success', `Alle labels printes nu i ${quantity} ${quantity === 1 ? 'kopi' : 'kopier'}.`);
                return;
            }
            if (e.target.closest('[data-ld-duplicate-label]')) duplicateLabel();
            if (e.target.closest('[data-ld-delete-label]')) deleteLabel();
        });

        const onElementProp = (e) => {
            const el = selected();
            if (!el) return;
            const t = e.target;
            // A field of another element type (e.g. the QR link still focused when a text is clicked) fires
            // its change on blur after the selection moved — it must not land on the new element.
            if (t.closest('[data-ld-show][hidden]')) return;
            if (t.matches('[data-ld-prop]')) {
                const prop = t.dataset.ldProp;
                let value;
                if (t.type === 'checkbox') value = t.checked;
                else if (t.type === 'radio') {
                    if (!t.checked) return;
                    value = t.value;
                } else if (t.type === 'number' || prop === 'digits') {
                    value = parseInt(t.value, 10);
                    if (!Number.isFinite(value)) return;
                    if (prop === 'start') value = clamp(value, 0, 999999);
                } else value = t.value;
                if (el[prop] === value) return;
                commit(`el:${el.id}:${prop}`, () => { el[prop] = value; });
            } else if (t.matches('[data-ld-font-pt]')) {
                const pt = parseFloat(t.value);
                if (!Number.isFinite(pt) || pt <= 0) return;
                const value = round(clamp(pt / ptPerPercent(), 1, 100));
                if (el.fontSize === value) return;
                commit(`el:${el.id}:fontSize`, () => { el.fontSize = value; });
            } else if (t.matches('[data-ld-geom]')) {
                const key = t.dataset.ldGeom;
                const mm = parseFloat(t.value);
                if (!Number.isFinite(mm)) return;
                const value = round((mm / axisMm(key)) * 100);
                if (el[key] === value) return;
                commit(`el:${el.id}:${key}`, () => {
                    el[key] = value;
                    keepInside(el);
                });
            } else if (t.matches('[data-ld-background]')) {
                if (el.background === t.value) return;
                commit(`el:${el.id}:background`, () => { el.background = t.value; });
            } else if (t.matches('[data-ld-background-none]') && e.type === 'change') {
                const color = q('[data-ld-background]').value || '#ffffff';
                commit(null, () => { el.background = t.checked ? '' : color; });
            }
        };
        elementPanel.addEventListener('input', onElementProp);
        elementPanel.addEventListener('change', onElementProp);
        elementPanel.addEventListener('click', (e) => {
            const btn = e.target.closest('button');
            if (!btn) return;
            if (btn.dataset.ldArrange) arrange(btn.dataset.ldArrange);
            else if (btn.dataset.ldLayer) moveLayer(btn.dataset.ldLayer);
            else if (btn.hasAttribute('data-ld-duplicate')) duplicateSelected();
            else if (btn.hasAttribute('data-ld-delete')) removeSelected();
            else if (btn.hasAttribute('data-ld-change-media')) changeMedia();
            else if (btn.hasAttribute('data-ld-deselect')) select(null);
        });

        // ─── Label-listen: vælg og træk for at flytte ──────────────────────────
        let dragFrom = null;
        const clearDropMarks = () => labelList.querySelectorAll('.drop-before, .drop-after').forEach((n) => n.classList.remove('drop-before', 'drop-after'));
        labelList.addEventListener('click', (e) => {
            const item = e.target.closest('.isd-slide-item');
            if (item) goToLabel(Number(item.dataset.index));
        });
        labelList.addEventListener('keydown', (e) => {
            const item = e.target.closest('.isd-slide-item');
            if (item && (e.key === 'Enter' || e.key === ' ')) {
                e.preventDefault();
                goToLabel(Number(item.dataset.index));
            }
        });
        labelList.addEventListener('dragstart', (e) => {
            const item = e.target.closest('.isd-slide-item');
            if (!item) return;
            dragFrom = Number(item.dataset.index);
            e.dataTransfer.effectAllowed = 'move';
            e.dataTransfer.setData('text/plain', String(dragFrom));
            item.classList.add('is-dragging');
        });
        labelList.addEventListener('dragover', (e) => {
            if (dragFrom === null) return;
            e.preventDefault();
            clearDropMarks();
            const item = e.target.closest('.isd-slide-item');
            if (!item) return;
            const r = item.getBoundingClientRect();
            item.classList.add(e.clientY > r.top + r.height / 2 ? 'drop-after' : 'drop-before');
        });
        labelList.addEventListener('drop', (e) => {
            if (dragFrom === null) return;
            e.preventDefault();
            const item = e.target.closest('.isd-slide-item');
            let to = state.labels.length - 1;
            if (item) {
                const r = item.getBoundingClientRect();
                to = Number(item.dataset.index) + (e.clientY > r.top + r.height / 2 ? 1 : 0);
                if (to > dragFrom) to--;
            }
            const from = dragFrom;
            dragFrom = null;
            clearDropMarks();
            moveLabel(from, to);
        });
        labelList.addEventListener('dragend', () => {
            dragFrom = null;
            clearDropMarks();
            labelList.querySelectorAll('.is-dragging').forEach((n) => n.classList.remove('is-dragging'));
        });

        // ─── Billedbibliotek ───────────────────────────────────────────────────
        const mediaModalEl = q('#ldMediaModal');
        const mediaGrid = mediaModalEl.querySelector('[data-ld-media-grid]');
        const mediaInput = mediaModalEl.querySelector('[data-ld-media-input]');
        const mediaProgress = mediaModalEl.querySelector('[data-ld-media-progress]');
        const mediaDrop = mediaModalEl.querySelector('[data-ld-media-drop]');
        const maxMb = Number(ds.maxImageMb);
        const mediaHint = `PNG, JPG eller WEBP – højst ${maxMb} MB. Brug gerne en god opløsning, så billedet står skarpt på print.`;
        mediaModalEl.querySelector('[data-ld-media-hint]').textContent = mediaHint;
        let onMediaPick = null;

        const renderMediaGrid = () => {
            mediaModalEl.querySelector('[data-ld-media-empty]').hidden = media.length > 0;
            mediaModalEl.querySelector('[data-ld-media-count]').textContent = media.length
                ? `${media.length} ${media.length === 1 ? 'billede' : 'billeder'} i denne samling – klik for at vælge`
                : '';
            mediaGrid.replaceChildren(...media.map((m) => {
                const card = document.createElement('div');
                card.className = 'isd-media-card';

                const pick = document.createElement('button');
                pick.type = 'button';
                pick.className = 'isd-media-pick';
                pick.dataset.mediaId = m.id;
                pick.title = `Vælg ${m.name}`;
                const preview = document.createElement('img');
                preview.loading = 'lazy';
                preview.alt = '';
                preview.src = mediaUrl(m.id);
                const name = document.createElement('span');
                name.className = 'isd-media-name';
                name.textContent = m.name;
                const size = document.createElement('small');
                size.className = 'text-muted';
                size.textContent = formatBytes(m.sizeBytes);
                pick.append(preview, name, size);

                const del = document.createElement('button');
                del.type = 'button';
                del.className = 'btn btn-sm btn-outline-danger btn-icon isd-media-delete';
                del.dataset.mediaDelete = m.id;
                const used = mediaInUse(m.id);
                del.disabled = used;
                del.title = used ? 'Bruges på en label – fjern det derfra først' : 'Slet billede';
                del.setAttribute('aria-label', del.title);
                del.innerHTML = '<i class="bi bi-trash"></i>';
                card.append(pick, del);
                return card;
            }));
        };

        function openMedia(onPick) {
            onMediaPick = onPick;
            renderMediaGrid();
            bootstrap.Modal.getOrCreateInstance(mediaModalEl).show();
        }

        function changeMedia() {
            const el = selected();
            if (!el || el.type !== 'image') return;
            openMedia((m) => {
                if (el.mediaId === m.id) return;
                commit(null, () => { el.mediaId = m.id; });
            });
        }

        const uploadOne = (file) => new Promise((resolve) => {
            const fd = new FormData();
            fd.append('__RequestVerificationToken', token());
            fd.append('id', ds.collectionId);
            fd.append('file', file);
            const bar = mediaProgress.querySelector('.progress-bar');
            bar.style.width = '0%';
            bar.textContent = file.name;
            mediaProgress.hidden = false;
            const xhr = new XMLHttpRequest();
            xhr.open('POST', ds.urlUpload);
            xhr.setRequestHeader('X-Requested-With', 'fetch');
            xhr.upload.onprogress = (e) => {
                if (e.lengthComputable) bar.style.width = `${Math.round((e.loaded / e.total) * 100)}%`;
            };
            xhr.onload = () => {
                try {
                    resolve(JSON.parse(xhr.responseText));
                } catch {
                    resolve({ success: false, message: xhr.status === 413 ? 'Filen er for stor.' : 'Filen kunne ikke uploades.' });
                }
            };
            xhr.onerror = () => resolve({ success: false, message: 'Netværksfejl. Prøv igen.' });
            xhr.send(fd);
        });

        const uploadFiles = async (fileList) => {
            if (!onMediaPick) return;
            const files = [...fileList];
            const uploaded = [];
            for (const file of files) {
                const ext = `.${file.name.split('.').pop().toLowerCase()}`;
                if (!IMAGE_TYPES.includes(ext)) {
                    toast('error', `"${file.name}" kan ikke bruges her. Brug et billede i PNG, JPG eller WEBP.`);
                    continue;
                }
                if (file.size > maxMb * 1024 * 1024) {
                    toast('error', `"${file.name}" er for stor (højst ${maxMb} MB).`);
                    continue;
                }
                const data = await uploadOne(file);
                if (data.success && data.media) {
                    media.unshift(data.media);
                    uploaded.push(data.media);
                    renderMediaGrid();
                } else {
                    toast('error', data.message || 'Filen kunne ikke uploades.');
                }
            }
            mediaProgress.hidden = true;
            if (uploaded.length === 1 && files.length === 1) {
                // One file uploaded = "use this one".
                const onPick = onMediaPick;
                bootstrap.Modal.getInstance(mediaModalEl)?.hide();
                onPick(uploaded[0]);
            } else if (uploaded.length > 0) {
                toast('success', `${uploaded.length} ${uploaded.length === 1 ? 'billede er' : 'billeder er'} uploadet – klik på det, du vil bruge.`);
            }
        };

        mediaInput.addEventListener('change', () => {
            uploadFiles(mediaInput.files);
            mediaInput.value = '';
        });
        mediaDrop.addEventListener('dragover', (e) => {
            if (![...(e.dataTransfer?.types || [])].includes('Files')) return;
            e.preventDefault();
            mediaDrop.classList.add('is-dragover');
        });
        mediaDrop.addEventListener('dragleave', (e) => {
            if (!mediaDrop.contains(e.relatedTarget)) mediaDrop.classList.remove('is-dragover');
        });
        mediaDrop.addEventListener('drop', (e) => {
            if (!e.dataTransfer?.files?.length) return;
            e.preventDefault();
            mediaDrop.classList.remove('is-dragover');
            uploadFiles(e.dataTransfer.files);
        });
        mediaGrid.addEventListener('click', async (e) => {
            const del = e.target.closest('[data-media-delete]');
            if (del) {
                // Two clicks: the first asks "Slet?", the second deletes (no alert()).
                if (!del.dataset.confirm) {
                    del.dataset.confirm = '1';
                    del.classList.remove('btn-outline-danger', 'btn-icon');
                    del.classList.add('btn-danger');
                    del.innerHTML = '<i class="bi bi-trash me-1"></i> Slet?';
                    setTimeout(() => { if (del.isConnected) renderMediaGrid(); }, 3000);
                    return;
                }
                const id = del.dataset.mediaDelete;
                const fd = new FormData();
                fd.append('__RequestVerificationToken', token());
                fd.append('id', ds.collectionId);
                fd.append('mediaId', id);
                del.disabled = true;
                try {
                    const res = await fetch(ds.urlDeleteMedia, { method: 'POST', body: fd, headers: { 'X-Requested-With': 'fetch' } });
                    const data = await res.json();
                    toast(data.type || (data.success ? 'success' : 'error'), data.message);
                    if (data.success) media = media.filter((m) => m.id !== id);
                } catch {
                    toast('error', 'Netværksfejl. Prøv igen.');
                }
                renderMediaGrid();
                return;
            }
            const pick = e.target.closest('[data-media-id]');
            if (!pick || !onMediaPick) return;
            const m = media.find((x) => x.id === pick.dataset.mediaId);
            const onPick = onMediaPick;
            bootstrap.Modal.getInstance(mediaModalEl)?.hide();
            if (m) onPick(m);
        });

        // ─── Indstillinger ─────────────────────────────────────────────────────
        const settingsModalEl = q('#ldSettingsModal');
        const settingsForm = settingsModalEl.querySelector('[data-ld-settings-form]');
        const settingsName = settingsForm.querySelector('[data-ld-setting="name"]');
        const sheetFields = L.bindSheetFields(settingsForm);
        page.querySelectorAll('[data-ld-open-settings]').forEach((btn) => btn.addEventListener('click', () => {
            bootstrap.Modal.getOrCreateInstance(settingsModalEl).show();
        }));
        settingsModalEl.addEventListener('show.bs.modal', () => {
            settingsName.value = state.name;
            sheetFields.write({ across: state.across, down: state.down, landscape: state.landscape });
        });
        settingsModalEl.addEventListener('shown.bs.modal', () => settingsName.focus());
        settingsForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const name = settingsName.value.trim();
            if (!name) {
                toast('error', 'Navn skal udfyldes.');
                return;
            }
            if (!sheetFields.isValid()) {
                toast('error', 'Angiv et gyldigt antal labels i bredden og højden.');
                return;
            }
            const sheet = sheetFields.read();
            commit(null, () => {
                state.name = name;
                state.across = sheet.across;
                state.down = sheet.down;
                state.landscape = sheet.landscape;
            }, 'all');
            bootstrap.Modal.getInstance(settingsModalEl)?.hide();
            await save();
        });

        // ─── Gem og print ──────────────────────────────────────────────────────
        let saving = false;
        async function save() {
            if (saving) return false;
            finishTextEdit();
            saving = true;
            saveBtn.disabled = true;
            const savedChange = changeCount;
            const fd = new FormData();
            fd.append('__RequestVerificationToken', token());
            fd.append('Id', ds.collectionId);
            fd.append('DesignJson', JSON.stringify(state));
            let ok = false;
            try {
                const res = await fetch(ds.urlSave, { method: 'POST', body: fd, headers: { 'X-Requested-With': 'fetch' } });
                const data = await res.json();
                toast(data.type || (data.success ? 'success' : 'error'), data.message);
                ok = !!data.success;
            } catch {
                toast('error', 'Netværksfejl. Prøv igen.');
            }
            saving = false;
            saveBtn.disabled = false;
            // Changes made while saving are still unsaved.
            if (ok && changeCount === savedChange) dirty = false;
            syncTop();
            return ok;
        }

        saveBtn.addEventListener('click', () => save());

        // The PDF is made from the saved labels, so unsaved changes are saved first.
        const printModalEl = q('#labelDesignPrintModal');
        L.initPrintModal(printModalEl, {
            pdfUrl: ds.urlPdf,
            mediaUrl,
            qrUrl,
            load: () => ({ id: ds.collectionId, design: clone(state) })
        });
        q('[data-ld-print]').addEventListener('click', async () => {
            if (dirty && !(await save())) return;
            bootstrap.Modal.getOrCreateInstance(printModalEl).show();
        });

        // ─── Forlad ikke med ikke-gemte ændringer ──────────────────────────────
        const leaveModalEl = q('#ldLeaveModal');
        let pendingHref = null;
        document.addEventListener('click', (e) => {
            if (!alive() || !dirty) return;
            const a = e.target.closest('a[href]');
            if (!a || a.target === '_blank' || a.getAttribute('href').startsWith('#') || e.ctrlKey || e.metaKey || e.shiftKey) return;
            // Capture phase: stops site.js's menu navigation too.
            e.preventDefault();
            e.stopImmediatePropagation();
            pendingHref = a.href;
            bootstrap.Modal.getOrCreateInstance(leaveModalEl).show();
        }, { capture: true, signal: docListeners.signal });
        window.addEventListener('beforeunload', (e) => {
            if (!alive() || !dirty) return;
            e.preventDefault();
            e.returnValue = '';
        }, { signal: docListeners.signal });
        leaveModalEl.querySelector('[data-ld-leave-discard]').addEventListener('click', () => {
            dirty = false;
            window.location.href = pendingHref;
        });
        leaveModalEl.querySelector('[data-ld-leave-save]').addEventListener('click', async () => {
            bootstrap.Modal.getInstance(leaveModalEl)?.hide();
            if (await save()) window.location.href = pendingHref;
        });

        // ─── Tastatur ──────────────────────────────────────────────────────────
        document.addEventListener('keydown', (e) => {
            if (!alive()) return;
            if (document.querySelector('.modal.show')) return;
            const ctrl = e.ctrlKey || e.metaKey;
            const key = e.key.toLowerCase();
            if (ctrl && key === 's') {
                e.preventDefault();
                // Blur first so a field being typed in is committed.
                if (document.activeElement && document.activeElement !== document.body) document.activeElement.blur();
                save();
                return;
            }
            if (e.target.closest?.('input, textarea, select, [contenteditable="true"], [contenteditable="plaintext-only"]')) return;

            if (ctrl && key === 'z' && !e.shiftKey) { e.preventDefault(); undo(); return; }
            if (ctrl && (key === 'y' || (key === 'z' && e.shiftKey))) { e.preventDefault(); redo(); return; }
            if (ctrl && key === 'v') {
                if (clipboard) { e.preventDefault(); paste(); }
                return;
            }

            const el = selected();
            if (!el) {
                if (e.key === 'PageDown' || e.key === 'ArrowDown') { e.preventDefault(); goToLabel(current + 1); }
                if (e.key === 'PageUp' || e.key === 'ArrowUp') { e.preventDefault(); goToLabel(current - 1); }
                return;
            }
            if (ctrl && key === 'c') {
                e.preventDefault();
                clipboard = clone(el);
                toast('info', 'Elementet er kopieret – indsæt det med Ctrl+V, også på en anden label.');
                return;
            }
            if (ctrl && key === 'd') { e.preventDefault(); duplicateSelected(); return; }
            if (e.key === 'Delete' || e.key === 'Backspace') { e.preventDefault(); removeSelected(); return; }
            if (e.key === 'Escape') { select(null); return; }
            if (e.key === 'Enter' && el.type === 'text') { e.preventDefault(); startTextEdit(); return; }

            const step = e.shiftKey ? 5 : 0.5;
            const moves = { ArrowLeft: [-step, 0], ArrowRight: [step, 0], ArrowUp: [0, -step], ArrowDown: [0, step] };
            if (moves[e.key]) {
                e.preventDefault();
                const [mx, my] = moves[e.key];
                commit(`nudge:${el.id}`, () => {
                    el.x = round(el.x + mx);
                    el.y = round(el.y + my);
                    keepInside(el);
                });
            }
        }, { signal: docListeners.signal });

        // ─── Start ─────────────────────────────────────────────────────────────
        const resizeObserver = new ResizeObserver(() => {
            if (!alive()) {
                resizeObserver.disconnect();
                return;
            }
            layoutStage();
            L.fitText(canvas);
        });
        resizeObserver.observe(stageWrap);
        renderAll();
    };

    document.querySelectorAll('[data-label-designer]').forEach(init);
})();
