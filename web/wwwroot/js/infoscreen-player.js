// ─── Kørende infoskærm (/Infoskaerm?Id=...) ───────────────────────────────────
// Henter siderne, viser dem i ring med den valgte overgang og tjekker for ændringer hvert
// PollSeconds: en gemt ændring tages i brug ved næste sideskift, og (de)aktivering slår igennem af
// sig selv — så projektoren kan bare stå tændt. Holder skærmen vågen (Wake Lock), skjuler musen og
// knapperne, når den står stille. Taster: ← / → skifter side, F = fuld skærm.
// Siderne tegnes af den fælles infoscreen-render.js. Markup: Views/Infoskaerm/Index.cshtml.
(() => {
    const root = document.querySelector('[data-infoscreen-player]');
    if (!root) return;

    const R = window.FvInfoScreen;
    const ds = root.dataset;
    const stage = root.querySelector('[data-isp-stage]');
    const message = root.querySelector('[data-isp-message]');
    const messageText = root.querySelector('[data-isp-message-text]');
    const renderOpts = {
        mode: 'play',
        mediaUrl: (id) => `${ds.urlMedia}?id=${encodeURIComponent(id)}`,
        qrUrl: (text, color) => `${ds.urlQr}?${new URLSearchParams({ text, color })}`
    };
    const TRANSITION_MS = 800;

    let data = null;
    let pending = null;
    let index = Math.max(0, Number(ds.startSlide || 1) - 1);
    let timer = null;
    let currentNode = null;

    const showMessage = (icon, text) => {
        clearTimeout(timer);
        timer = null;
        stage.hidden = true;
        stage.replaceChildren();
        currentNode = null;
        message.querySelector('i').className = `bi ${icon}`;
        messageText.textContent = text;
        message.hidden = false;
    };

    // Images for the next slide are fetched ahead, so they're ready when it's shown.
    const preload = (i) => {
        const slide = data.slides[i];
        (slide?.elements || []).forEach((el) => {
            if (el.type === 'image' && el.mediaId) new Image().src = renderOpts.mediaUrl(el.mediaId);
        });
    };

    const show = (i) => {
        const slides = data.slides;
        clearTimeout(timer);
        timer = null;
        if (!slides.length) {
            showMessage('bi-easel', 'Der er ingen sider at vise endnu.');
            return;
        }
        message.hidden = true;
        stage.hidden = false;
        index = ((i % slides.length) + slides.length) % slides.length;

        const transition = data.transition || 'fade';
        const node = R.renderSlide(slides[index], renderOpts);
        node.classList.add(`isp-${transition}`);
        const old = currentNode;
        if (old && transition !== 'none') node.classList.add('isp-enter');
        stage.appendChild(node);
        currentNode = node;

        if (old) {
            if (transition === 'none') {
                old.remove();
            } else {
                // Two frames so the start position is painted before the transition begins.
                requestAnimationFrame(() => requestAnimationFrame(() => {
                    node.classList.add('is-in');
                    old.classList.add('is-out');
                }));
                setTimeout(() => old.remove(), TRANSITION_MS + 100);
            }
        }

        preload((index + 1) % slides.length);
        if (slides.length > 1) timer = setTimeout(next, Math.max(1, slides[index].durationSeconds) * 1000);
    };

    const apply = (next) => {
        data = next;
        stage.style.setProperty('--isp-ratio', R.ratio(data.aspectRatio));
        document.title = `${data.title} - KlubPlan`;
    };

    function next() {
        if (pending) {
            apply(pending);
            pending = null;
        }
        show(index + 1);
    }

    const prev = () => show(index - 1);

    const handle = (incoming) => {
        if (incoming.status === 'notfound') {
            data = null;
            showMessage('bi-question-circle', 'Infoskærmen findes ikke. Tjek linket.');
            return false;
        }
        if (incoming.status === 'inactive') {
            data = null;
            pending = null;
            showMessage('bi-pause-circle', 'Infoskærmen er ikke aktiv. Den starter af sig selv, når den bliver aktiveret.');
            return true;
        }

        if (!data) {
            // First load, or just activated.
            apply(incoming);
            show(index);
        } else if (incoming.version !== data.version) {
            // Saved changes: taken into use at the next slide change (right away if nothing is changing).
            pending = incoming;
            if (!timer) next();
        }
        return true;
    };

    const load = async () => {
        const res = await fetch(ds.urlData, { cache: 'no-store', headers: { 'X-Requested-With': 'fetch' } });
        if (!res.ok) throw new Error(res.statusText);
        return res.json();
    };

    let pollTimer = null;
    const poll = async () => {
        try {
            if (!handle(await load())) clearInterval(pollTimer);
        } catch {
            // Offline for a moment: keep showing what we have and try again at the next poll.
            if (!data) showMessage('bi-wifi-off', 'Kunne ikke hente infoskærmen. Prøver igen…');
        }
    };

    // ─── Skærm vågen, mus/knapper skjules ──────────────────────────────────
    let wakeLock = null;
    const keepAwake = async () => {
        try {
            if ('wakeLock' in navigator && document.visibilityState === 'visible' && !wakeLock) {
                wakeLock = await navigator.wakeLock.request('screen');
                wakeLock.addEventListener('release', () => { wakeLock = null; });
            }
        } catch { /* not allowed — the screen's own sleep settings apply */ }
    };
    document.addEventListener('visibilitychange', keepAwake);

    let idleTimer = null;
    const wake = () => {
        document.body.classList.remove('is-idle');
        clearTimeout(idleTimer);
        idleTimer = setTimeout(() => document.body.classList.add('is-idle'), 3000);
    };
    ['mousemove', 'pointerdown', 'keydown'].forEach((type) => document.addEventListener(type, wake));

    const toggleFullscreen = () => {
        if (document.fullscreenElement) document.exitFullscreen?.();
        else document.documentElement.requestFullscreen?.().catch(() => { });
    };
    document.querySelector('[data-isp-fullscreen]')?.addEventListener('click', toggleFullscreen);
    document.querySelector('[data-isp-next]')?.addEventListener('click', () => data && next());
    document.querySelector('[data-isp-prev]')?.addEventListener('click', () => data && prev());
    root.addEventListener('dblclick', toggleFullscreen);
    document.addEventListener('keydown', (e) => {
        if (e.key === 'f' || e.key === 'F') toggleFullscreen();
        if (!data) return;
        if (e.key === 'ArrowRight' || e.key === 'PageDown' || e.key === ' ') { e.preventDefault(); next(); }
        if (e.key === 'ArrowLeft' || e.key === 'PageUp') { e.preventDefault(); prev(); }
    });

    wake();
    keepAwake();
    poll();
    pollTimer = setInterval(poll, Math.max(5, Number(ds.pollSeconds) || 30) * 1000);
})();
