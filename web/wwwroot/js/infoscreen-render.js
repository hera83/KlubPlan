// ─── Infoskærm: fælles visning af en side ─────────────────────────────────────
// Bruges af designeren (Views/InfoScreens/Designer.cshtml), dens miniaturer og den kørende skærm
// (Views/Infoskaerm/Index.cshtml), så en side ser ens ud alle tre steder. Position/størrelse er % af
// siden og skriftstørrelse er % af sidens højde (cqh), så en side skalerer til enhver størrelse.
// Styles: wwwroot/css/infoscreen.css (.isc-*). Datamodel: Repositories/InfoScreens/Dtos/InfoScreenDesignDto.cs.
(() => {
    const ASPECTS = { '16:9': 16 / 9, '16:10': 16 / 10, '4:3': 4 / 3 };
    const ratio = (key) => ASPECTS[key] || ASPECTS['16:9'];

    // Keep in sync with FvDate in site.js / Infrastructure/DateTimeDisplayExtensions.cs — the
    // public player page doesn't load site.js, so the names live here too.
    const MONTHS = ['jan.', 'feb.', 'mar.', 'apr.', 'maj', 'jun.', 'jul.', 'aug.', 'sep.', 'okt.', 'nov.', 'dec.'];
    const WEEKDAYS = ['søndag', 'mandag', 'tirsdag', 'onsdag', 'torsdag', 'fredag', 'lørdag'];
    const pad = (n) => String(n).padStart(2, '0');

    /** time → "14:30", date → "tirsdag den 22. sep. 2026", datetime → "tirsdag den 22. sep. 2026 kl. 14:30". */
    const formatClock = (format, d = new Date()) => {
        const time = `${pad(d.getHours())}:${pad(d.getMinutes())}`;
        const date = `${WEEKDAYS[d.getDay()]} den ${d.getDate()}. ${MONTHS[d.getMonth()]} ${d.getFullYear()}`;
        if (format === 'date') return date;
        if (format === 'datetime') return `${date} kl. ${time}`;
        return time;
    };

    const FONTS = {
        sans: "'Inter', 'Segoe UI', Arial, sans-serif",
        serif: "Georgia, 'Times New Roman', serif",
        mono: "Consolas, 'Courier New', monospace"
    };
    const JUSTIFY = { top: 'flex-start', middle: 'center', bottom: 'flex-end' };

    // One timer keeps every clock on the page up to date (designer canvas, thumbnails, player).
    let clockTimer = null;
    const startClocks = () => {
        if (clockTimer) return;
        clockTimer = setInterval(() => {
            document.querySelectorAll('[data-isc-clock]').forEach((node) => {
                const text = formatClock(node.dataset.iscClock);
                if (node.textContent !== text) node.textContent = text;
            });
        }, 1000);
    };

    const placeholder = (icon, text) => {
        const box = document.createElement('div');
        box.className = 'isc-placeholder';
        const i = document.createElement('i');
        i.className = `bi ${icon}`;
        const span = document.createElement('span');
        span.textContent = text;
        box.append(i, span);
        return box;
    };

    /**
     * One element as a positioned box. opts.mode: 'edit' (designer canvas — placeholders, paused
     * video), 'thumb' (slide list) or 'play' (running screen — autoplaying video, no placeholders).
     * opts.mediaUrl(mediaId) and opts.qrUrl(text, color) build the file/QR links.
     */
    const renderElement = (el, opts) => {
        const node = document.createElement('div');
        node.className = `isc-el isc-${el.type}`;
        node.dataset.elementId = el.id;
        node.style.left = `${el.x}%`;
        node.style.top = `${el.y}%`;
        node.style.width = `${el.w}%`;
        node.style.height = `${el.h}%`;
        const showPlaceholders = opts.mode !== 'play';

        switch (el.type) {
            case 'text':
            case 'clock': {
                node.style.color = el.color || '#ffffff';
                node.style.fontSize = `${el.fontSize || 8}cqh`;
                node.style.fontFamily = FONTS[el.fontFamily] || FONTS.sans;
                node.style.fontWeight = el.bold ? '700' : '400';
                node.style.fontStyle = el.italic ? 'italic' : 'normal';
                node.style.textAlign = el.align || 'center';
                node.style.justifyContent = JUSTIFY[el.vAlign] || 'center';
                if (el.background) node.style.background = el.background;
                const inner = document.createElement('div');
                inner.className = 'isc-content';
                if (el.type === 'clock') {
                    inner.dataset.iscClock = el.format || 'time';
                    inner.textContent = formatClock(el.format);
                    startClocks();
                } else if (el.text) {
                    inner.textContent = el.text;
                } else if (opts.mode === 'edit') {
                    node.classList.add('is-placeholder');
                    inner.textContent = 'Dobbeltklik for at skrive';
                }
                node.appendChild(inner);
                break;
            }
            case 'image': {
                if (!el.mediaId) {
                    if (showPlaceholders) node.appendChild(placeholder('bi-image', 'Vælg billede'));
                    break;
                }
                const img = document.createElement('img');
                img.src = opts.mediaUrl(el.mediaId);
                img.alt = '';
                img.draggable = false;
                img.style.objectFit = el.fit || 'contain';
                node.appendChild(img);
                break;
            }
            case 'video': {
                if (!el.mediaId) {
                    if (showPlaceholders) node.appendChild(placeholder('bi-film', 'Vælg video'));
                    break;
                }
                const video = document.createElement('video');
                video.muted = true;
                video.playsInline = true;
                video.style.objectFit = el.fit || 'contain';
                if (opts.mode === 'play') {
                    video.autoplay = true;
                    video.loop = true;
                    video.preload = 'auto';
                    video.src = opts.mediaUrl(el.mediaId);
                } else {
                    // A still frame is enough while designing.
                    video.preload = 'metadata';
                    video.src = `${opts.mediaUrl(el.mediaId)}#t=0.5`;
                }
                node.appendChild(video);
                break;
            }
            case 'qr': {
                if (!el.text) {
                    if (showPlaceholders) node.appendChild(placeholder('bi-qr-code', 'Skriv et link'));
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

    /** A whole slide (background + elements in stacking order), filling its container. */
    const renderSlide = (slide, opts) => {
        const node = document.createElement('div');
        node.className = 'isc-slide';
        node.style.background = slide.background || '#111827';
        (slide.elements || []).forEach((el) => node.appendChild(renderElement(el, opts)));
        return node;
    };

    window.FvInfoScreen = { ratio, formatClock, renderSlide, renderElement };
})();
