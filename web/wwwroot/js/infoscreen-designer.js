// ─── Infoskærm-designer (InfoScreens/Designer) ────────────────────────────────
// • Hele designet (indstillinger + sider + elementer) holdes i hukommelsen og gemmes samlet (Gem / Ctrl+S).
// • Sider: liste til venstre (klik, træk for at flytte, ny/duplikér/slet/skjul), visningstid og baggrund.
// • Lærred: elementer (tekst, billede, video, QR-kode, ur/dato) flyttes og skaleres med musen, med
//   hjælpelinjer til sidens midte/kanter og de andre elementer. Dobbeltklik på tekst for at skrive direkte.
// • Fortryd/gentag (Ctrl+Z / Ctrl+Y), kopiér/indsæt elementer mellem sider (Ctrl+C / Ctrl+V).
// • Mediebibliotek pr. skærm: upload (knap eller træk filer ind) og genbrug af billeder/videoer.
// Siderne tegnes af den fælles infoscreen-render.js, så lærredet ser ud som den kørende skærm.
// Markup: Views/InfoScreens/Designer.cshtml + _Designer*.cshtml.
(() => {
    const DEFAULT_DURATION = 10;
    const DEFAULT_BACKGROUND = '#111827';
    const MIN_SIZE = 1;
    const SNAP = 0.8;
    const TYPE_LABELS = { text: 'Tekst', image: 'Billede', video: 'Video', qr: 'QR-kode', clock: 'Ur / dato' };
    const TYPE_ICONS = { text: 'bi-fonts', image: 'bi-image', video: 'bi-film', qr: 'bi-qr-code', clock: 'bi-clock' };

    const uid = () => `e${Math.random().toString(36).slice(2, 10)}`;
    const round = (v) => Math.round(v * 100) / 100;
    const clamp = (v, min, max) => Math.min(max, Math.max(min, v));
    const clone = (o) => JSON.parse(JSON.stringify(o));
    const newSlide = (base) => ({
        durationSeconds: base?.durationSeconds || DEFAULT_DURATION,
        background: base?.background || DEFAULT_BACKGROUND,
        isHidden: false,
        elements: []
    });
    const formatSeconds = (s) => {
        if (s < 60) return `${s} sek`;
        const rest = s % 60;
        return rest ? `${Math.floor(s / 60)} min ${rest} sek` : `${Math.floor(s / 60)} min`;
    };
    const formatBytes = (b) => (b >= 1024 * 1024
        ? `${(b / (1024 * 1024)).toLocaleString('da-DK', { maximumFractionDigits: 1 })} MB`
        : `${Math.max(1, Math.round(b / 1024))} KB`);
    const toast = (type, message) => window.FvToast?.show(type, message);

    const init = (page) => {
        if (page.dataset.isdInit) return;
        page.dataset.isdInit = '1';

        const R = window.FvInfoScreen;
        const ds = page.dataset;
        const q = (sel) => page.querySelector(sel);
        const token = () => q('input[name="__RequestVerificationToken"]')?.value || '';

        const seed = JSON.parse(q('[data-isd-seed]').textContent);
        const state = seed.design;
        state.slides = state.slides || [];
        if (state.slides.length === 0) state.slides.push(newSlide());
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

        const slideList = q('[data-isd-slide-list]');
        const stageWrap = q('[data-isd-stage-wrap]');
        const stage = q('[data-isd-stage]');
        const canvas = q('[data-isd-canvas]');
        const selection = q('[data-isd-selection]');
        const guideV = q('[data-isd-guide-v]');
        const guideH = q('[data-isd-guide-h]');
        const slidePanel = q('[data-isd-panel="slide"]');
        const elementPanel = q('[data-isd-panel="element"]');
        const undoBtn = q('[data-isd-undo]');
        const redoBtn = q('[data-isd-redo]');
        const saveBtn = q('[data-isd-save]');

        const mediaUrl = (id) => `${ds.urlMedia}?id=${encodeURIComponent(id)}`;
        const qrUrl = (text, color) => `${ds.urlQr}?${new URLSearchParams({ text, color })}`;
        const opts = (mode) => ({ mode, mediaUrl, qrUrl });
        const slide = () => state.slides[current];
        const selected = () => (selectedId ? slide().elements.find((e) => e.id === selectedId) || null : null);
        const findMedia = (id) => media.find((m) => m.id === id);
        const mediaInUse = (id) => state.slides.some((s) => s.elements.some((e) => e.mediaId === id));

        // ─── Top bar ───────────────────────────────────────────────────────────
        const syncTop = () => {
            q('[data-isd-title]').textContent = state.title;
            const status = q('[data-isd-status]');
            status.className = state.isActive ? 'badge-ok' : 'badge-warn';
            status.textContent = state.isActive ? 'Aktiv' : 'Inaktiv';
            q('[data-isd-dirty]').hidden = !dirty;
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
            current = clamp(s.current, 0, state.slides.length - 1);
            selectedId = s.selectedId && slide().elements.some((e) => e.id === s.selectedId) ? s.selectedId : null;
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
            const r = R.ratio(state.aspectRatio);
            page.style.setProperty('--isd-ratio', r);
            const box = stageWrap.getBoundingClientRect();
            let w = Math.max(0, box.width - 32);
            let h = Math.max(0, box.height - 32);
            if (h * r < w) w = h * r;
            else h = w / r;
            stage.style.width = `${w}px`;
            stage.style.height = `${h}px`;
        };

        const elementNode = (id) => canvas.querySelector(`.isc-el[data-element-id="${CSS.escape(id)}"]`);

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
            canvas.replaceChildren(R.renderSlide(slide(), opts('edit')));
            renderSelection();
        };

        const renderElementNode = (el) => {
            const old = elementNode(el.id);
            if (old) old.replaceWith(R.renderElement(el, opts('edit')));
            renderSelection();
        };

        const applyGeometry = (el) => {
            const node = elementNode(el.id);
            if (!node) return;
            node.style.left = `${el.x}%`;
            node.style.top = `${el.y}%`;
            node.style.width = `${el.w}%`;
            node.style.height = `${el.h}%`;
        };

        const renderSlideItem = (i) => {
            const s = state.slides[i];
            const item = document.createElement('div');
            item.className = 'isd-slide-item';
            item.classList.toggle('is-current', i === current);
            item.classList.toggle('is-hidden', s.isHidden);
            item.draggable = true;
            item.tabIndex = 0;
            item.dataset.index = i;
            item.setAttribute('role', 'button');
            item.setAttribute('aria-label', `Side ${i + 1}${s.isHidden ? ' (skjult)' : ''}`);

            const num = document.createElement('span');
            num.className = 'isd-slide-num';
            num.textContent = i + 1;
            const thumb = document.createElement('div');
            thumb.className = 'isd-thumb';
            thumb.appendChild(R.renderSlide(s, opts('thumb')));
            const meta = document.createElement('span');
            meta.className = 'isd-slide-meta';
            meta.innerHTML = s.isHidden ? '<i class="bi bi-eye-slash"></i> Skjult · ' : '<i class="bi bi-clock"></i> ';
            meta.append(formatSeconds(s.durationSeconds));

            const body = document.createElement('div');
            body.className = 'isd-slide-body';
            body.append(thumb, meta);
            item.append(num, body);
            return item;
        };

        const renderTotals = () => {
            const visible = state.slides.filter((s) => !s.isHidden);
            const seconds = visible.reduce((sum, s) => sum + s.durationSeconds, 0);
            q('[data-isd-total-duration]').textContent = `${state.slides.length} · ${formatSeconds(seconds)}`;
            q('[data-isd-total-duration]').title = `${visible.length} viste sider, én runde tager ${formatSeconds(seconds)}`;
        };

        const renderSlideList = () => {
            slideList.replaceChildren(...state.slides.map((_, i) => renderSlideItem(i)));
            renderTotals();
        };

        const refreshSlideItem = (i) => {
            slideList.querySelector(`[data-index="${i}"]`)?.replaceWith(renderSlideItem(i));
            renderTotals();
        };

        let thumbTimer = null;
        const refreshThumbSoon = () => {
            clearTimeout(thumbTimer);
            const i = current;
            thumbTimer = setTimeout(() => refreshSlideItem(i), 200);
        };

        // ─── Egenskaber ────────────────────────────────────────────────────────
        const setValue = (input, value) => {
            if (input === document.activeElement) return;
            if (input.type === 'checkbox') input.checked = !!value;
            else if (input.type === 'radio') input.checked = input.value === value;
            else if (input.type === 'color') input.value = value || '#000000';
            else input.value = value ?? '';
        };

        const renderProps = () => {
            const el = selected();
            slidePanel.hidden = !!el;
            elementPanel.hidden = !el;
            if (!el) {
                const s = slide();
                q('[data-isd-slide-label]').textContent = `Side ${current + 1} af ${state.slides.length}`;
                slidePanel.querySelectorAll('[data-isd-slide-prop]').forEach((input) => setValue(input, s[input.dataset.isdSlideProp]));
                q('[data-isd-delete-slide]').disabled = state.slides.length <= 1;
                return;
            }

            q('[data-isd-element-label]').innerHTML = `<i class="bi ${TYPE_ICONS[el.type]} me-1"></i>`;
            q('[data-isd-element-label]').append(TYPE_LABELS[el.type]);
            elementPanel.querySelectorAll('[data-isd-show]').forEach((group) => {
                group.hidden = !group.dataset.isdShow.split(' ').includes(el.type);
            });
            elementPanel.querySelectorAll('[data-isd-prop]').forEach((input) => setValue(input, el[input.dataset.isdProp]));
            elementPanel.querySelectorAll('[data-isd-geom]').forEach((input) => setValue(input, el[input.dataset.isdGeom]));

            const bg = q('[data-isd-background]');
            setValue(bg, el.background || '#000000');
            bg.disabled = !el.background;
            setValue(q('[data-isd-background-none]'), !el.background);
            q('[data-isd-color-label]').textContent = el.type === 'qr' ? 'Farve på koden' : 'Tekstfarve';
            q('[data-isd-media-name]').textContent = el.mediaId ? (findMedia(el.mediaId)?.name || 'Filen findes ikke længere') : 'Ingen fil valgt';

            const list = slide().elements;
            const index = list.indexOf(el);
            elementPanel.querySelectorAll('[data-isd-layer="front"], [data-isd-layer="forward"]').forEach((b) => { b.disabled = index === list.length - 1; });
            elementPanel.querySelectorAll('[data-isd-layer="back"], [data-isd-layer="backward"]').forEach((b) => { b.disabled = index === 0; });
        };

        const renderAll = () => {
            layoutStage();
            renderSlideList();
            renderCanvas();
            renderProps();
            syncTop();
            syncUndo();
        };

        /**
         * Every change goes through here: undo step, change, dirty, redraw.
         * scope: 'element' (the selected element), 'slide' (current slide + its thumbnail) or 'all'.
         */
        const commit = (key, mutate, scope = 'element') => {
            pushUndo(key);
            mutate();
            markDirty();
            if (scope === 'all') {
                renderAll();
            } else if (scope === 'slide') {
                refreshSlideItem(current);
                renderCanvas();
                renderProps();
            } else {
                const el = selected();
                if (el) renderElementNode(el);
                renderProps();
                refreshThumbSoon();
            }
        };

        // ─── Markering og sider ────────────────────────────────────────────────
        const select = (id) => {
            if (selectedId === id) return;
            finishTextEdit();
            selectedId = id;
            renderSelection();
            renderProps();
        };

        const goToSlide = (i) => {
            if (i < 0 || i >= state.slides.length) return;
            finishTextEdit();
            current = i;
            selectedId = null;
            slideList.querySelectorAll('.isd-slide-item').forEach((item) => item.classList.toggle('is-current', Number(item.dataset.index) === current));
            slideList.querySelector(`[data-index="${current}"]`)?.scrollIntoView({ block: 'nearest' });
            renderCanvas();
            renderProps();
        };

        const addSlide = () => commit(null, () => {
            state.slides.splice(current + 1, 0, newSlide(slide()));
            current++;
            selectedId = null;
        }, 'all');

        const duplicateSlide = () => {
            const copy = clone(slide());
            copy.elements.forEach((e) => { e.id = uid(); });
            commit(null, () => {
                state.slides.splice(current + 1, 0, copy);
                current++;
                selectedId = null;
            }, 'all');
        };

        const deleteSlide = () => {
            if (state.slides.length <= 1) return;
            commit(null, () => {
                state.slides.splice(current, 1);
                current = Math.min(current, state.slides.length - 1);
                selectedId = null;
            }, 'all');
            toast('info', 'Siden er slettet. Fortryd med Ctrl+Z.');
        };

        const moveSlide = (from, to) => {
            if (from === to) return;
            commit(null, () => {
                const [moved] = state.slides.splice(from, 1);
                state.slides.splice(to, 0, moved);
                current = to;
                selectedId = null;
            }, 'all');
        };

        // ─── Elementer ─────────────────────────────────────────────────────────
        const textDefaults = () => ({ fontSize: 8, fontFamily: 'sans', bold: false, italic: false, align: 'center', vAlign: 'middle', color: '#ffffff', background: '' });

        const addElement = (el) => {
            const s = slide();
            // A new element never lands exactly on top of another one.
            while (s.elements.some((e) => e.x === el.x && e.y === el.y)) {
                el.x = round(el.x + 2);
                el.y = round(el.y + 2);
            }
            finishTextEdit();
            commit(null, () => {
                s.elements.push(el);
                selectedId = el.id;
            }, 'slide');
        };

        /** Width:height of an image/video, so it's inserted in its own shape. Null when unknown. */
        const naturalRatio = (kind, m) => new Promise((resolve) => {
            const done = (value) => resolve(Number.isFinite(value) && value > 0 ? value : null);
            setTimeout(() => done(null), 4000);
            if (kind === 'image') {
                const img = new Image();
                img.onload = () => done(img.naturalWidth / img.naturalHeight);
                img.onerror = () => done(null);
                img.src = mediaUrl(m.id);
            } else {
                const video = document.createElement('video');
                video.preload = 'metadata';
                video.onloadedmetadata = () => done(video.videoWidth / video.videoHeight);
                video.onerror = () => done(null);
                video.src = mediaUrl(m.id);
            }
        });

        const addMediaElement = async (kind, m) => {
            const r = R.ratio(state.aspectRatio);
            const mediaRatio = (await naturalRatio(kind, m)) || 16 / 9;
            let w = 60;
            let h = (w * r) / mediaRatio;
            if (h > 80) {
                h = 80;
                w = (h * mediaRatio) / r;
            }
            addElement({ id: uid(), type: kind, x: round((100 - w) / 2), y: round((100 - h) / 2), w: round(w), h: round(h), mediaId: m.id, fit: 'contain' });
        };

        /** The first of the rows (y positions) where a full-width box doesn't cover another element. */
        const freeRow = (w, h, rows) => {
            const overlaps = (a, b) => a.x < b.x + b.w && b.x < a.x + a.w && a.y < b.y + b.h && b.y < a.y + a.h;
            const x = round((100 - w) / 2);
            const y = rows.find((top) => !slide().elements.some((e) => overlaps({ x, y: top, w, h }, e)));
            return { x, y: y ?? rows[0] };
        };

        const insert = (type) => {
            const r = R.ratio(state.aspectRatio);
            switch (type) {
                case 'text': {
                    const spot = freeRow(80, 20, [40, 70, 10, 55, 25, 78]);
                    addElement({ id: uid(), type, ...spot, w: 80, h: 20, text: '', ...textDefaults() });
                    startTextEdit();
                    break;
                }
                case 'clock':
                    addElement({ id: uid(), type, x: 62, y: 4, w: 34, h: 12, format: 'time', ...textDefaults(), bold: true, align: 'right' });
                    break;
                case 'qr': {
                    const w = 18;
                    const h = round(w * r);
                    addElement({ id: uid(), type, x: 100 - 4 - w, y: round(100 - 6 - h), w, h, text: '', color: '#000000' });
                    q('#isdQrText')?.focus();
                    break;
                }
                case 'image':
                case 'video':
                    openMedia(type, (m) => addMediaElement(type, m));
                    break;
            }
        };

        const removeSelected = () => {
            const el = selected();
            if (!el) return;
            finishTextEdit();
            commit(null, () => {
                slide().elements = slide().elements.filter((e) => e !== el);
                selectedId = null;
            }, 'slide');
        };

        const duplicateSelected = () => {
            const el = selected();
            if (!el) return;
            addElement({ ...clone(el), id: uid(), x: round(el.x + 2), y: round(el.y + 2) });
        };

        const paste = () => {
            if (!clipboard) return;
            addElement({ ...clone(clipboard), id: uid() });
        };

        const moveLayer = (where) => {
            const el = selected();
            if (!el) return;
            const list = slide().elements;
            const from = list.indexOf(el);
            const to = { front: list.length - 1, back: 0, forward: Math.min(from + 1, list.length - 1), backward: Math.max(from - 1, 0) }[where];
            if (from === to) return;
            commit(null, () => {
                list.splice(from, 1);
                list.splice(to, 0, el);
            }, 'slide');
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

        // ─── Skriv direkte på siden ────────────────────────────────────────────
        const startTextEdit = () => {
            const el = selected();
            if (!el || el.type !== 'text' || editingNode) return;
            const node = elementNode(el.id);
            const content = node?.querySelector('.isc-content');
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
            const id = content.closest('.isc-el')?.dataset.elementId;
            const el = slide().elements.find((e) => e.id === id);
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
            const others = slide().elements.filter((o) => o !== el);
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
                    x += dx;
                    y += dy;
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
                        const ratio = g.w / g.h;
                        if (Math.abs(w - g.w) / g.w >= Math.abs(h - g.h) / g.h) {
                            const nh = w / ratio;
                            if (mode.includes('n')) y = g.y + g.h - nh;
                            h = nh;
                        } else {
                            const nw = h * ratio;
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
            const node = e.target.closest('.isc-el');
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
            else if (el.type === 'image' || el.type === 'video') changeMedia();
            else if (el.type === 'qr') q('#isdQrText')?.focus();
            else if (el.type === 'clock') q('#isdClockFormat')?.focus();
        });

        // ─── Paneler og knapper ────────────────────────────────────────────────
        q('.isd-toolbar').addEventListener('click', (e) => {
            const add = e.target.closest('[data-isd-add]');
            if (add) insert(add.dataset.isdAdd);
        });
        undoBtn.addEventListener('click', undo);
        redoBtn.addEventListener('click', redo);
        q('[data-isd-add-slide]').addEventListener('click', addSlide);

        const onSlideProp = (e) => {
            const input = e.target.closest('[data-isd-slide-prop]');
            if (!input) return;
            const prop = input.dataset.isdSlideProp;
            let value = input.type === 'checkbox' ? input.checked : input.value;
            if (prop === 'durationSeconds') {
                value = parseInt(value, 10);
                if (!Number.isFinite(value) || value < 1) return;
                value = Math.min(value, 3600);
            }
            if (slide()[prop] === value) return;
            commit(`slide:${current}:${prop}`, () => { slide()[prop] = value; }, 'slide');
        };
        slidePanel.addEventListener('input', onSlideProp);
        slidePanel.addEventListener('change', onSlideProp);
        slidePanel.addEventListener('click', (e) => {
            const swatch = e.target.closest('[data-isd-swatches] [data-color]');
            if (swatch) {
                if (slide().background !== swatch.dataset.color) commit(null, () => { slide().background = swatch.dataset.color; }, 'slide');
                return;
            }
            if (e.target.closest('[data-isd-duration-all]')) {
                const seconds = slide().durationSeconds;
                commit(null, () => state.slides.forEach((s) => { s.durationSeconds = seconds; }), 'all');
                toast('success', `Alle sider vises nu i ${formatSeconds(seconds)}.`);
                return;
            }
            if (e.target.closest('[data-isd-duplicate-slide]')) duplicateSlide();
            if (e.target.closest('[data-isd-delete-slide]')) deleteSlide();
        });

        const onElementProp = (e) => {
            const el = selected();
            if (!el) return;
            const t = e.target;
            if (t.matches('[data-isd-prop]')) {
                const prop = t.dataset.isdProp;
                let value;
                if (t.type === 'checkbox') value = t.checked;
                else if (t.type === 'radio') {
                    if (!t.checked) return;
                    value = t.value;
                } else if (t.type === 'number' || t.type === 'range') {
                    value = parseFloat(t.value);
                    if (!Number.isFinite(value)) return;
                    if (prop === 'fontSize') value = clamp(value, 1, 60);
                } else value = t.value;
                if (el[prop] === value) return;
                commit(`el:${el.id}:${prop}`, () => { el[prop] = value; });
            } else if (t.matches('[data-isd-geom]')) {
                const key = t.dataset.isdGeom;
                let value = parseFloat(t.value);
                if (!Number.isFinite(value)) return;
                if (key === 'w' || key === 'h') value = Math.max(0.5, value);
                value = round(value);
                if (el[key] === value) return;
                commit(`el:${el.id}:${key}`, () => { el[key] = value; });
            } else if (t.matches('[data-isd-background]')) {
                if (el.background === t.value) return;
                commit(`el:${el.id}:background`, () => { el.background = t.value; });
            } else if (t.matches('[data-isd-background-none]') && e.type === 'change') {
                const color = q('[data-isd-background]').value || '#000000';
                commit(null, () => { el.background = t.checked ? '' : color; });
            }
        };
        elementPanel.addEventListener('input', onElementProp);
        elementPanel.addEventListener('change', onElementProp);
        elementPanel.addEventListener('click', (e) => {
            const btn = e.target.closest('button');
            if (!btn) return;
            if (btn.dataset.isdArrange) arrange(btn.dataset.isdArrange);
            else if (btn.dataset.isdLayer) moveLayer(btn.dataset.isdLayer);
            else if (btn.hasAttribute('data-isd-duplicate')) duplicateSelected();
            else if (btn.hasAttribute('data-isd-delete')) removeSelected();
            else if (btn.hasAttribute('data-isd-change-media')) changeMedia();
            else if (btn.hasAttribute('data-isd-deselect')) select(null);
        });

        // ─── Sidelisten: vælg og træk for at flytte ────────────────────────────
        let dragFrom = null;
        const clearDropMarks = () => slideList.querySelectorAll('.drop-before, .drop-after').forEach((n) => n.classList.remove('drop-before', 'drop-after'));
        slideList.addEventListener('click', (e) => {
            const item = e.target.closest('.isd-slide-item');
            if (item) goToSlide(Number(item.dataset.index));
        });
        slideList.addEventListener('keydown', (e) => {
            const item = e.target.closest('.isd-slide-item');
            if (item && (e.key === 'Enter' || e.key === ' ')) {
                e.preventDefault();
                goToSlide(Number(item.dataset.index));
            }
        });
        slideList.addEventListener('dragstart', (e) => {
            const item = e.target.closest('.isd-slide-item');
            if (!item) return;
            dragFrom = Number(item.dataset.index);
            e.dataTransfer.effectAllowed = 'move';
            e.dataTransfer.setData('text/plain', String(dragFrom));
            item.classList.add('is-dragging');
        });
        slideList.addEventListener('dragover', (e) => {
            if (dragFrom === null) return;
            e.preventDefault();
            clearDropMarks();
            const item = e.target.closest('.isd-slide-item');
            if (!item) return;
            const r = item.getBoundingClientRect();
            item.classList.add(e.clientY > r.top + r.height / 2 ? 'drop-after' : 'drop-before');
        });
        slideList.addEventListener('drop', (e) => {
            if (dragFrom === null) return;
            e.preventDefault();
            const item = e.target.closest('.isd-slide-item');
            let to = state.slides.length - 1;
            if (item) {
                const r = item.getBoundingClientRect();
                to = Number(item.dataset.index) + (e.clientY > r.top + r.height / 2 ? 1 : 0);
                if (to > dragFrom) to--;
            }
            const from = dragFrom;
            dragFrom = null;
            clearDropMarks();
            moveSlide(from, to);
        });
        slideList.addEventListener('dragend', () => {
            dragFrom = null;
            clearDropMarks();
            slideList.querySelectorAll('.is-dragging').forEach((n) => n.classList.remove('is-dragging'));
        });

        // ─── Mediebibliotek ────────────────────────────────────────────────────
        const mediaModalEl = q('#isdMediaModal');
        const mediaGrid = mediaModalEl.querySelector('[data-isd-media-grid]');
        const mediaInput = mediaModalEl.querySelector('[data-isd-media-input]');
        const mediaProgress = mediaModalEl.querySelector('[data-isd-media-progress]');
        const mediaDrop = mediaModalEl.querySelector('[data-isd-media-drop]');
        const KINDS = {
            image: {
                title: 'Vælg billede', accept: ['.png', '.jpg', '.jpeg', '.gif', '.webp'], maxMb: Number(ds.maxImageMb),
                hint: `PNG, JPG, GIF eller WEBP – højst ${ds.maxImageMb} MB`, one: 'billede', many: 'billeder'
            },
            video: {
                title: 'Vælg video', accept: ['.mp4', '.webm'], maxMb: Number(ds.maxVideoMb),
                hint: `MP4 eller WEBM – højst ${ds.maxVideoMb} MB. Videoen afspilles uden lyd og starter forfra.`, one: 'video', many: 'videoer'
            }
        };
        let mediaPick = null;

        const renderMediaGrid = () => {
            const kind = KINDS[mediaPick.kind];
            const items = media.filter((m) => m.kind === mediaPick.kind);
            mediaModalEl.querySelector('[data-isd-media-empty]').hidden = items.length > 0;
            mediaModalEl.querySelector('[data-isd-media-count]').textContent = items.length
                ? `${items.length} ${items.length === 1 ? kind.one : kind.many} på denne infoskærm – klik for at vælge`
                : '';
            mediaGrid.replaceChildren(...items.map((m) => {
                const card = document.createElement('div');
                card.className = 'isd-media-card';

                const pick = document.createElement('button');
                pick.type = 'button';
                pick.className = 'isd-media-pick';
                pick.dataset.mediaId = m.id;
                pick.title = `Vælg ${m.name}`;
                let preview;
                if (m.kind === 'image') {
                    preview = document.createElement('img');
                    preview.loading = 'lazy';
                    preview.alt = '';
                    preview.src = mediaUrl(m.id);
                } else {
                    preview = document.createElement('video');
                    preview.muted = true;
                    preview.preload = 'metadata';
                    preview.src = `${mediaUrl(m.id)}#t=0.5`;
                }
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
                del.title = used ? 'Bruges på en side – fjern den derfra først' : 'Slet fil';
                del.setAttribute('aria-label', del.title);
                del.innerHTML = '<i class="bi bi-trash"></i>';
                card.append(pick, del);
                return card;
            }));
        };

        function openMedia(kind, onPick) {
            mediaPick = { kind, onPick };
            const cfg = KINDS[kind];
            mediaModalEl.querySelector('[data-isd-media-title]').textContent = cfg.title;
            mediaModalEl.querySelector('[data-isd-media-hint]').textContent = cfg.hint;
            mediaInput.accept = cfg.accept.join(',');
            renderMediaGrid();
            bootstrap.Modal.getOrCreateInstance(mediaModalEl).show();
        }

        function changeMedia() {
            const el = selected();
            if (!el || (el.type !== 'image' && el.type !== 'video')) return;
            openMedia(el.type, (m) => {
                if (el.mediaId === m.id) return;
                commit(null, () => { el.mediaId = m.id; });
            });
        }

        const uploadOne = (file) => new Promise((resolve) => {
            const fd = new FormData();
            fd.append('__RequestVerificationToken', token());
            fd.append('id', ds.screenId);
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
            if (!mediaPick) return;
            const cfg = KINDS[mediaPick.kind];
            const files = [...fileList];
            const uploaded = [];
            for (const file of files) {
                const ext = `.${file.name.split('.').pop().toLowerCase()}`;
                if (!cfg.accept.includes(ext)) {
                    toast('error', `"${file.name}" kan ikke bruges her. ${cfg.hint}.`);
                    continue;
                }
                if (file.size > cfg.maxMb * 1024 * 1024) {
                    toast('error', `"${file.name}" er for stor (højst ${cfg.maxMb} MB).`);
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
                const { onPick } = mediaPick;
                bootstrap.Modal.getInstance(mediaModalEl)?.hide();
                onPick(uploaded[0]);
            } else if (uploaded.length > 0) {
                toast('success', `${uploaded.length} ${uploaded.length === 1 ? 'fil er' : 'filer er'} uploadet – klik på den, du vil bruge.`);
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
                fd.append('id', ds.screenId);
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
            if (!pick || !mediaPick) return;
            const m = media.find((x) => x.id === pick.dataset.mediaId);
            const { onPick } = mediaPick;
            bootstrap.Modal.getInstance(mediaModalEl)?.hide();
            if (m) onPick(m);
        });

        // ─── Indstillinger ─────────────────────────────────────────────────────
        const settingsModalEl = q('#isdSettingsModal');
        const settingsForm = settingsModalEl.querySelector('[data-isd-settings-form]');
        const setting = (name) => settingsForm.querySelector(`[data-isd-setting="${name}"]`);
        page.querySelectorAll('[data-isd-open-settings]').forEach((btn) => btn.addEventListener('click', () => {
            bootstrap.Modal.getOrCreateInstance(settingsModalEl).show();
        }));
        settingsModalEl.addEventListener('show.bs.modal', () => {
            setting('title').value = state.title;
            setting('isActive').checked = state.isActive;
            setting('aspectRatio').value = state.aspectRatio;
            setting('transition').value = state.transition;
        });
        settingsModalEl.addEventListener('shown.bs.modal', () => setting('title').focus());
        settingsForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const title = setting('title').value.trim();
            if (!title) {
                toast('error', 'Titel skal udfyldes.');
                return;
            }
            commit(null, () => {
                state.title = title;
                state.isActive = setting('isActive').checked;
                state.aspectRatio = setting('aspectRatio').value;
                state.transition = setting('transition').value;
            }, 'all');
            bootstrap.Modal.getInstance(settingsModalEl)?.hide();
            await save();
        });

        // ─── Gem, link og afspil ───────────────────────────────────────────────
        let saving = false;
        async function save() {
            if (saving) return false;
            finishTextEdit();
            saving = true;
            saveBtn.disabled = true;
            const savedChange = changeCount;
            const fd = new FormData();
            fd.append('__RequestVerificationToken', token());
            fd.append('Id', ds.screenId);
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

        q('[data-isd-copy-link]').addEventListener('click', async () => {
            try {
                await navigator.clipboard.writeText(ds.publicLink);
                toast(state.isActive ? 'success' : 'info', state.isActive
                    ? 'Linket er kopieret til udklipsholderen.'
                    : 'Linket er kopieret. Skærmen er inaktiv, så den viser først diasshowet, når du aktiverer den.');
            } catch {
                toast('error', 'Linket kunne ikke kopieres.');
            }
        });

        q('[data-isd-play]').addEventListener('click', async () => {
            // Opened right away (inside the click) so the popup isn't blocked while saving.
            const win = window.open('', '_blank');
            if (dirty && !(await save())) {
                win?.close();
                return;
            }
            const visibleIndex = state.slides.slice(0, current).filter((s) => !s.isHidden).length + 1;
            const url = `${ds.publicLink}&preview=true&slide=${visibleIndex}`;
            if (win) {
                win.opener = null;
                win.location.href = url;
            } else {
                window.open(url, '_blank', 'noopener');
            }
        });

        // ─── Forlad ikke med ikke-gemte ændringer ──────────────────────────────
        const leaveModalEl = q('#isdLeaveModal');
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
        leaveModalEl.querySelector('[data-isd-leave-discard]').addEventListener('click', () => {
            dirty = false;
            window.location.href = pendingHref;
        });
        leaveModalEl.querySelector('[data-isd-leave-save]').addEventListener('click', async () => {
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
                if (e.key === 'PageDown' || e.key === 'ArrowDown') { e.preventDefault(); goToSlide(current + 1); }
                if (e.key === 'PageUp' || e.key === 'ArrowUp') { e.preventDefault(); goToSlide(current - 1); }
                return;
            }
            if (ctrl && key === 'c') {
                e.preventDefault();
                clipboard = clone(el);
                toast('info', 'Elementet er kopieret – indsæt det med Ctrl+V, også på en anden side.');
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
        });
        resizeObserver.observe(stageWrap);
        renderAll();
    };

    document.querySelectorAll('[data-infoscreen-designer]').forEach(init);
})();
