(() => {
    const debounce = (fn, delay) => {
        let timer;
        return (...args) => {
            clearTimeout(timer);
            timer = setTimeout(() => fn(...args), delay);
        };
    };

    const collectParams = (root, page) => {
        const params = new URLSearchParams();
        const search = root.querySelector('[data-table-search]');
        if (search && search.value.trim()) {
            params.set('SearchText', search.value.trim());
        }

        root.querySelectorAll('[data-table-filter-panel] [name]').forEach((field) => {
            if (field.type === 'checkbox') {
                if (field.checked) params.append(field.name, field.value);
            } else if (field.value) {
                params.set(field.name, field.value);
            }
        });

        const pageSize = root.querySelector('[data-table-page-size]');
        if (pageSize && pageSize.value) {
            params.set('PageSize', pageSize.value);
        }

        params.set('Page', String(page || 1));
        return params;
    };

    const loadTable = (root, page, options) => {
        const endpoint = root.dataset.tableEndpoint;
        const region = root.querySelector('[data-table-region]');
        if (!endpoint || !region) return;

        const silent = !!(options && options.silent);
        const resolvedPage = page || parseInt(root.dataset.currentPage || '1', 10);
        const params = collectParams(root, resolvedPage);
        root.dataset.currentPage = String(resolvedPage);
        if (!silent) region.classList.add('is-loading');

        fetch(`${endpoint}?${params.toString()}`, { headers: { 'X-Requested-With': 'fetch' } })
            .then((res) => {
                if (!res.ok) throw new Error('table fetch failed');
                return res.text();
            })
            .then((html) => {
                // Ved en stille opdatering (fx baggrundspolling) undgås et layout-skift,
                // hvis svaret er identisk med det, der allerede vises.
                if (silent && region.innerHTML === html) return;
                region.innerHTML = html;
            })
            .catch(() => {})
            .finally(() => { if (!silent) region.classList.remove('is-loading'); });
    };

    const reload = (root, options) => {
        if (!root) return;
        loadTable(root, parseInt(root.dataset.currentPage || '1', 10), options);
    };

    const closeFilterPanel = (btn) => {
        const panel = btn.closest('.collapse');
        if (panel && window.bootstrap) {
            window.bootstrap.Collapse.getOrCreateInstance(panel).hide();
        }
    };

    const initDataTable = (root) => {
        const searchInput = root.querySelector('[data-table-search]');
        if (searchInput) {
            searchInput.addEventListener('input', debounce(() => loadTable(root, 1), 300));
        }

        root.querySelectorAll('[data-table-filter-save]').forEach((btn) => {
            btn.addEventListener('click', () => {
                loadTable(root, 1);
                closeFilterPanel(btn);
            });
        });

        root.querySelectorAll('[data-table-filter-reset]').forEach((btn) => {
            btn.addEventListener('click', () => {
                const panel = btn.closest('[data-table-filter-panel]');
                panel?.querySelectorAll('[name]').forEach((field) => {
                    if (field.tagName === 'SELECT') {
                        field.selectedIndex = 0;
                    } else if (field.type === 'checkbox') {
                        field.checked = false;
                    } else {
                        field.value = '';
                    }
                });
                loadTable(root, 1);
                closeFilterPanel(btn);
            });
        });

        root.addEventListener('click', (e) => {
            const pageBtn = e.target.closest('[data-table-page]');
            if (!pageBtn || pageBtn.disabled) return;
            e.preventDefault();
            loadTable(root, parseInt(pageBtn.dataset.tablePage, 10));
        });

        root.addEventListener('change', (e) => {
            if (!e.target.closest('[data-table-page-size]')) return;
            loadTable(root, 1);
        });

        // Gruppe-badges (Personer): klik fastholder det udvidede badge (nyttigt uden mus/hover).
        root.addEventListener('click', (e) => {
            const badge = e.target.closest('[data-group-badge]');
            if (!badge) return;
            const wasExpanded = badge.classList.contains('expanded');
            root.querySelectorAll('[data-group-badge].expanded').forEach((el) => el.classList.remove('expanded'));
            if (!wasExpanded) badge.classList.add('expanded');
        });
    };

    const initAll = (container) => {
        (container || document).querySelectorAll('[data-table-root]').forEach((root) => {
            if (root.dataset.tableInitialized) return;
            root.dataset.tableInitialized = 'true';
            initDataTable(root);
        });
    };

    document.addEventListener('click', (e) => {
        if (e.target.closest('[data-group-badge]')) return;
        document.querySelectorAll('[data-group-badge].expanded').forEach((el) => el.classList.remove('expanded'));
    });
    document.addEventListener('keydown', (e) => {
        if (e.key !== 'Escape') return;
        document.querySelectorAll('[data-group-badge].expanded').forEach((el) => el.classList.remove('expanded'));
    });

    window.FvDataTable = { initAll, reload };
    initAll();
})();
