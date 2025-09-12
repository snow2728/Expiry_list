

function formatDate(data, type) {
    if (!data) return '';

    const date = new Date(data);
    if (isNaN(date)) return data;

    if (type === 'display' || type === 'filter') {
        return date.toLocaleDateString('en-US', {
            month: 'long',
            year: 'numeric'
        });
    }

    return data;

}

function scrollToEditedRow() {
    const rowId = $('#<%= hfSelectedIDs.ClientID %>').val();
    if (rowId) {
        const $row = $(`tr[data-id='${rowId}']`);
        if ($row.length) {
            const $table = $('.dataTables_scrollBody');
            const rowTop = $row.position().top;
            $table.animate({
                scrollTop: rowTop + $table.scrollTop()
            }, 500);
        }
        $('#<%= hfSelectedIDs.ClientID %>').val('');
    }
}

function editRow(id) {
    document.getElementById('<%= hfSelectedIDs.ClientID %>').value = id;
    __doPostBack('', ''); 
}

function blockInvalidChars(event) {
    // Allow: Backspace, Delete, Arrow keys, Tab
    if ([8, 9, 37, 39, 46].includes(event.keyCode)) {
        return;
    }

    // Block if not a digit (0-9)
    if (!/[0-9]/.test(event.key)) {
        event.preventDefault();
    }
}

document.querySelectorAll('.nav-link').forEach(link => {
    link.addEventListener('click', () => {
        const navbar = document.getElementById('mainNav');
        if (window.innerWidth < 992 && navbar.classList.contains('show')) {
            new bootstrap.Collapse(navbar).hide();
        }
    });
});

// js code for reorder/viewer3.aspx
function onDeleteSuccess(result) {
    if (result) {
        Swal.fire('Deleted!', 'Your record has been deleted.', 'success');
        refreshDataTable();
    } else {
        Swal.fire('Error!', 'Failed to delete record.', 'error');
    }
}



function onDeleteError(error) {
    Swal.fire('Error!', 'An error occurred while deleting: ' + error.get_message(), 'error');
}

// ?From reorder/viewer1.aspx
// viewer.js

// Initialize Select2 with checkboxes
function initializeStoreSelect2($select, allOptionId = "all") {
    $select.select2({
        placeholder: "-- Select stores --",
        closeOnSelect: false,
        width: '100%',
        allowClear: true,
        dropdownParent: $select.closest('.filter-group'),
        minimumResultsForSearch: 1,
        escapeMarkup: m => m,
        templateResult: function (data) {
            if (!data.id) return data.text;
            const isAll = data.id === allOptionId;
            const selectedValues = $select.val() || [];
            const hasAll = selectedValues.includes(allOptionId);
            const isChecked = isAll ? hasAll : selectedValues.includes(data.id);
            const isDisabled = hasAll && !isAll;

            return $(`
                <div class="select2-checkbox-option d-flex align-items-center">
                    <input type="checkbox" class="select2-checkbox me-2"
                           ${isChecked ? 'checked' : ''}
                           ${isAll ? 'data-is-all="true"' : ''}
                           ${isDisabled ? 'disabled' : ''}>
                    <div class="select2-text${isAll ? ' fw-bold' : ''}">${data.text}</div>
                </div>
            `);
        },
        templateSelection: () => ''
    });

    $select.data('select2').$dropdown.on('click', '.select2-checkbox', function () {
        const data = $(this).closest('.select2-results__option').data('data');
        if (!data) return;

        let selected = $select.val() || [];
        const isAll = data.id === allOptionId;

        if (isAll) {
            $select.val(this.checked ? [allOptionId] : []).trigger('change');
        } else {
            const index = selected.indexOf(data.id);
            if (this.checked && index === -1) selected.push(data.id);
            if (!this.checked && index !== -1) selected.splice(index, 1);
            $select.val(selected).trigger('change');
        }
    });
}

// Update pills for multi-select
function updateLocationPillsDisplay($select, containerId, allOptionId = "all") {
    const container = document.getElementById(containerId);
    if (!container || !$select.length) return;

    container.innerHTML = '';
    const values = $select.val() || [];
    const hasAll = values.includes(allOptionId);

    if (hasAll) {
        const pill = document.createElement('span');
        pill.className = 'location-pill';
        pill.innerHTML = `<span class="pill-text">All Stores</span>
                          <span class="pill-remove" data-value="all">×</span>`;
        pill.querySelector('.pill-remove').addEventListener('click', e => {
            e.preventDefault();
            $select.val(values.filter(v => v !== allOptionId)).trigger('change');
        });
        container.appendChild(pill);
    } else {
        values.forEach(value => {
            const option = $select.find(`option[value='${value}']`)[0];
            if (!option) return;

            const pill = document.createElement('span');
            pill.className = 'location-pill';
            pill.innerHTML = `<span class="pill-text">${option.text}</span>
                              <span class="pill-remove" data-value="${value}">×</span>`;
            pill.querySelector('.pill-remove').addEventListener('click', e => {
                e.preventDefault();
                $select.val(values.filter(v => v !== value)).trigger('change');
            });
            container.appendChild(pill);
        });
    }

    container.style.display = values.length > 0 ? 'grid' : 'none';
}

// Apply search highlighting
function applyManualSearchHighlighting(gridContainerSelector, searchTerm) {
    if (!searchTerm) return;
    const lowerTerm = searchTerm.toLowerCase().trim();

    $(`${gridContainerSelector} tr`).each(function () {
        const $row = $(this);
        if ($row.hasClass('static-header')) return;

        const rowText = $row.text().toLowerCase();
        const isMatch = rowText.includes(lowerTerm);

        $row.toggleClass('highlight-match', isMatch);
        $row.toggle(isMatch);
    });
}

// Initialize inline editing for DataTable cells
function enableInlineEditing($grid, editableCols = ['status', 'action', 'remark']) {
    $grid.find('tbody').off('dblclick').on('dblclick', 'td', function () {
        const $cell = $(this);
        const colIndex = $cell.index();
        const colName = $grid.find('thead th').eq(colIndex).data('column');
        if (!editableCols.includes(colName)) return;
        if ($cell.find('input, select').length > 0) return;

        const oldValue = $cell.text().trim();
        let editor;

        if (colName === 'action') {
            editor = $(`<select class="form-select form-select-sm">
                <option value="">-- Select Reason --</option>
                <option value="None">None</option>
                <option value="Overstock and Redistribute">Overstock and Redistribute</option>
                <option value="Redistribute">Redistribute</option>
                <option value="Allocation Item">Allocation Item</option>
                <option value="Shortage Item">Shortage Item</option>
                <option value="System Enough">System Enough</option>
                <option value="Tail Off Item">Tail Off Item</option>
                <option value="Purchase Blocked">Purchase Blocked</option>
                <option value="Already Added Reorder">Already Added Reorder</option>
                <option value="Customer Requested Item">Customer Requested Item</option>
                <option value="No Hierarchy">No Hierarchy</option>
                <option value="Near Expiry Item">Near Expiry Item</option>
                <option value="Reorder Qty is large, Need to adjust Qty">Reorder Qty is large, Need to adjust Qty</option>
                <option value="Discon Item">Discon Item</option>
                <option value="Supplier Discon">Supplier Discon</option>
            </select>`);
        } else if (colName === 'status') {
            editor = $(`<select class="form-select form-select-sm">
                <option value="">-- Select Action --</option>
                <option value="Reorder Done">Reorder Done</option>
                <option value="No Reordering">No Reordering</option>
            </select>`);
        } else {
            editor = $(`<input type="text" class="form-control form-control-sm">`).val(oldValue);
        }

        $cell.empty().append(editor);
        editor.focus();

        editor.on('blur change', function () {
            const newValue = $(this).val();
            if (newValue === oldValue) {
                $cell.text(oldValue);
                return;
            }
            // Perform AJAX update here using rowId
            $cell.text(newValue);
        });
    });
}

