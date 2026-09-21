(() => {
    // Lader en registrant tilføje ekstra, frit navngivne medtilmeldte pr. vagt (fx en
    // forælder/værge), når arrangementet tillader det. Indekserne i name-attributterne
    // (Companions[i].ShiftId/.Name) genberegnes ved hver tilføj/fjern, så der aldrig opstår
    // huller i listen — simplere end at bygge skjulte felter op ved submit.

    function reindex(formEl) {
        var names = formEl.querySelectorAll('.arrangement-shift-companion-name');
        var shiftIds = formEl.querySelectorAll('.arrangement-shift-companion-shiftid');
        for (var i = 0; i < names.length; i++) {
            names[i].name = 'Companions[' + i + '].Name';
            shiftIds[i].name = 'Companions[' + i + '].ShiftId';
        }
    }

    function companionCount(container) {
        return container.querySelectorAll('.arrangement-shift-companion-row').length;
    }

    function addCompanionRow(container, shiftId, value) {
        var row = document.createElement('div');
        row.className = 'arrangement-shift-companion-row';
        row.innerHTML =
            '<input type="text" class="form-control form-control-sm arrangement-shift-companion-name" placeholder="Navn på den, der tager vagten" />' +
            '<input type="hidden" class="arrangement-shift-companion-shiftid" />' +
            '<button type="button" class="btn btn-sm btn-outline-danger btn-icon arrangement-shift-companion-remove" title="Fjern" aria-label="Fjern"><i class="bi bi-trash"></i></button>';

        row.querySelector('.arrangement-shift-companion-name').value = value || '';
        row.querySelector('.arrangement-shift-companion-shiftid').value = shiftId;
        container.appendChild(row);
        return row;
    }

    // Første klik på "+" for en vagt opretter to felter på én gang — ét forudfyldt med
    // registrantens eget navn, ét tomt — så det aldrig er tvivlsomt, om man har taget vagten
    // selv, sammen med én til, eller helt givet den videre (begge felter er redigerbare, så en
    // forælder kan fx skrive begge forældres navne i stedet for barnets). Senere klik tilføjer
    // blot ét tomt felt mere.
    function addNamesForClick(container, shiftId, max, personName) {
        var addedRows = [];
        if (companionCount(container) === 0) {
            addedRows.push(addCompanionRow(container, shiftId, personName));
            if (companionCount(container) < max) {
                addedRows.push(addCompanionRow(container, shiftId, ''));
            }
        } else {
            addedRows.push(addCompanionRow(container, shiftId, ''));
        }
        return addedRows;
    }

    function updateAddButtonState(formEl, shiftId) {
        var container = formEl.querySelector('.arrangement-shift-companions[data-shift-id="' + shiftId + '"]');
        var btn = formEl.querySelector('.arrangement-shift-add-companion-btn[data-shift-id="' + shiftId + '"]');
        var checkbox = formEl.querySelector('#shiftSelect_' + shiftId);
        if (!container || !btn || !checkbox) return;

        var max = parseInt(container.getAttribute('data-max'), 10) || 0;
        btn.disabled = !checkbox.checked || companionCount(container) >= max;
    }

    function init(options) {
        var formEl = options.formEl;
        if (!formEl) return;

        var personName = formEl.dataset.personName || '';

        formEl.querySelectorAll('.arrangement-shift-companions').forEach(function (container) {
            updateAddButtonState(formEl, container.getAttribute('data-shift-id'));
        });

        formEl.addEventListener('click', function (e) {
            var addBtn = e.target.closest('.arrangement-shift-add-companion-btn');
            if (addBtn) {
                var shiftId = addBtn.getAttribute('data-shift-id');
                var container = formEl.querySelector('.arrangement-shift-companions[data-shift-id="' + shiftId + '"]');
                if (!container) return;

                var max = parseInt(container.getAttribute('data-max'), 10) || 0;
                if (companionCount(container) >= max) return;

                var addedRows = addNamesForClick(container, shiftId, max, personName);
                reindex(formEl);
                updateAddButtonState(formEl, shiftId);

                var rowToFocus = addedRows[addedRows.length - 1];
                rowToFocus?.querySelector('.arrangement-shift-companion-name')?.focus();
                return;
            }

            var removeBtn = e.target.closest('.arrangement-shift-companion-remove');
            if (removeBtn) {
                var row = removeBtn.closest('.arrangement-shift-companion-row');
                var container = row?.closest('.arrangement-shift-companions');
                row?.remove();
                reindex(formEl);
                if (container) updateAddButtonState(formEl, container.getAttribute('data-shift-id'));
            }
        });

        formEl.querySelectorAll('.arrangement-shift-checkbox').forEach(function (checkbox) {
            checkbox.addEventListener('change', function () {
                var shiftId = checkbox.value;
                var container = formEl.querySelector('.arrangement-shift-companions[data-shift-id="' + shiftId + '"]');
                if (!checkbox.checked && container) {
                    container.innerHTML = '';
                    reindex(formEl);
                }
                updateAddButtonState(formEl, shiftId);
            });
        });

        reindex(formEl);
    }

    window.FvArrangementShiftCompanions = { init: init };
})();
