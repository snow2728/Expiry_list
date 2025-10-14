<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="viewer1.aspx.cs" Inherits="Expiry_list.ReorderForm.viewer1" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        table thead tr, th {
            line-height: 15px !important;
        }

        table.dataTable thead tr th {
            border: none;
        }

        table td {
            border-bottom: none;
            border-spacing: 0;
            box-shadow: none;
        }

        table.dataTable thead tr th {
            border-right: none !important;
        }

        table.dataTable th,
        table.dataTable td {
            border-right: 1px solid #ccc;
        }

        table.dataTable th.dtfc-fixed-left,
        table.dataTable td.dtfc-fixed-left {
            border-right: none !important;
        }

        table.dataTable td:nth-child(5) {
            border-left: 1px solid #ccc;
        }

        table.dataTable td:last-child {
            border-right: none;
        }

        .grip {
            width: 3px;
            background-color: transparent;
            cursor: col-resize;
            height: 100%;
            position: absolute;
            top: 0;
            right: -1.5px;
            z-index: 1;
            transition: background-color 0.2s ease-in-out;
        }

        .grip:hover {
            background-color: #337ab7;
            opacity: 0.5;
        }

        .dragging {
            background-color: #000000;
            opacity: 0.8;
        }

        #drag-indicator {
            position: absolute;
            top: 0;
            left: 0;
            width: 1px;
            background-color: #337ab7;
            z-index: 9999;
            height: 100%;
            display: none;
        }

        .fixed-column-1, .sticky-header1 {
            left: 0;
            min-width: 30px !important;
        }

        .fixed-column-2, .sticky-header2 {
            left: 30px;
            min-width: 70px !important;
        }

        .fixed-column-3, .sticky-header3 {
            left: 107px;
            min-width: 107px !important;
        }

        .fixed-column-4, .sticky-header4 {
            left: 207px;
            min-width: 50px;
        }
    </style>

    <%
        var permissions = Session["formPermissions"] as Dictionary<string, string>;
        string expiryPerm = permissions != null && permissions.ContainsKey("ReorderQuantity") ? permissions["ReorderQuantity"] : "";
    %>

    <script type="text/javascript">
        var expiryPermission = '<%= expiryPerm %>';
    </script>

    <script type="text/javascript">

        $(document).ready(function () {
            //initializeDataTable();
            InitializeStoreFilter();
            //initializeResizableColumns(); 

            if (typeof (Sys) !== 'undefined') {
                Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
                    updateFilterVisibility();
                    InitializeStoreFilter();
                    //initializeResizableColumns(); 
                    makeColumnsResizable('#<%= GridView2.ClientID %>', 'GridView2ColumnWidths');
                    Object.keys(filterMap).forEach(key => {
                        const checkbox = document.getElementById(filterMap[key].checkboxId);
                        if (checkbox) {
                            checkbox.addEventListener('change', updateFilterVisibility);
                        }
                    });
                });
            }

            initializeComponents();
            setupEventDelegation();
            InitializeItemVendorFilter();
            setupFilterToggle();
        });

        //For Note Preview
        $(document).on('click', '.truncated-note', function (e) {
            e.preventDefault();

            var fullNote = $(this).data('fullnote');
            $('#noteModal .modal-body').text(fullNote);

            var modal = new bootstrap.Modal(document.getElementById('noteModal'));
            modal.show();
        });

        function addColGroup(tableSelector, columnCount) {
            if ($(tableSelector).find("colgroup").length === 0) {
                let colgroup = $("<colgroup></colgroup>");
                for (let i = 0; i < columnCount; i++) {
                    colgroup.append('<col style="width:auto;">');
                }
                $(tableSelector).prepend(colgroup);
            }
        }

        // DATATABLE RESIZABLE
        function makeColumnsResizable(tableSelector) {
            let pressed = false, startX, startWidth, $th, index;

            const $table = $(tableSelector);
            const $wrapper = $table.closest('.dataTables_wrapper');
            const $headerCells = $wrapper.find('.dataTables_scrollHead thead th');

            $headerCells.each(function () {
                if ($(this).find('.resizer').length === 0) {
                    $(this).css('position', 'relative')
                        .append('<div class="resizer" style="position:absolute;top:0;right:0;width:5px;cursor:col-resize;user-select:none;height:100%;z-index:10;"></div>');
                }
            });

            $wrapper.find('.resizer').off('mousedown').on('mousedown', function (e) {
                pressed = true;
                startX = e.pageX;
                $th = $(this).parent();
                index = $th.index();
                startWidth = $th.outerWidth();

                e.preventDefault();
                e.stopPropagation();
            });

            $(document).off('mousemove.resizer').on('mousemove.resizer', function (e) {
                if (!pressed) return;

                let delta = e.pageX - startX;
                let newWidth = startWidth + delta;

                if (newWidth < 40) newWidth = 40;

                $headerCells.eq(index).css('width', newWidth);

                $wrapper.find('.dataTables_scrollBody tbody tr').each(function () {
                    $(this).find('td').eq(index).css('width', newWidth);
                });

                const $col = $table.find('colgroup col').eq(index);
                if ($col.length) $col.css('width', newWidth + 'px');
            });

            $(document).off('mouseup.resizer').on('mouseup.resizer', function () {
                pressed = false;
            });
        }

        let dataTable;
        let isDataTableInitialized = false;
        function initializeComponents() {
            const grid = $("#<%= GridView2.ClientID %>");

            addColGroup("#<%= GridView2.ClientID %>", 21);

            if ($.fn.DataTable.isDataTable(grid)) {
                grid.DataTable().clear().destroy();
                grid.removeAttr('style');
                grid.parent().find('.dataTables_scrollHead, .dtfc-fixed-left, .dtfc-fixed-right, .dataTables_wrapper').remove();
            }

            dataTable = grid.DataTable({
                responsive: true,
                ordering: true,
                serverSide: true,
                paging: true,
                filter: true,
                scrollX: true,
                scrollY: "63vh",
                scrollCollapse: true,
                autoWidth: false,
                stateSave: true,
                processing: true,
                searching: true,
                ajax: {
                    url: 'viewer1.aspx',
                    type: 'POST',
                    data: function (d) {
                        return {
                            draw: d.draw,
                            start: d.start,
                            length: d.length,
                            "order[0][column]": d.order[0].column,
                            "order[0][dir]": d.order[0].dir,
                            "search[value]": d.search.value,
                            
                            //Filters
                            store: $('#<%= lstStoreFilter.ClientID %>').val()?.join(",") || "",
                            item: $('#<%= item.ClientID %>').val() || "",
                            vendor: $('#<%= vendor.ClientID %>').val() || "",
                            action: $('#<%= ddlActionFilter.ClientID %>').val() || "",
                            status: $('#<%= ddlStatusFilter.ClientID %>').val() || "",
                            staff: $('#<%= txtstaffFilter.ClientID %>').val() || "",
                            division: $('#<%= txtDivisionCodeFilter.ClientID %>').val() || "",
                            regDate: $('#<%= txtRegDateFilter.ClientID %>').val() || "",
                            approveDate: $('#<%= txtApproveDateFilter.ClientID %>').val() || ""
                        };
                    }
                },
                colReorder: true,
                fixedColumns: {
                    leftColumns: 4,
                    rightColumns: 0,
                    heightMatch: 'none'
                },
                columns: [
                    {
                        data: 'checkbox', orderable: false, width: "50px", render: function (data, type, row) {
                            return '<input type="checkbox" class="rowCheckbox" data-id="' + row.id + '" />';
                        }
                    },
                    { data: 'no', width: "100px" },
                    { data: 'storeNo', width: "120px" },
                    { data: 'divisionCode', width: "90px" },
                    {
                        data: 'approveDate', width: "120px", type: 'date', render: function (data, type) {
                            if (!data) return '';
                            if (type === 'sort') return data;
                            const date = new Date(data);
                            return date.toLocaleDateString('en-GB');
                        }
                    },
                    { data: 'itemNo', width: "117px" },
                    { data: 'description', width: "257px" },
                    { data: 'packingInfo', width: "110px" },
                    /* { data: 'barcodeNo', width: "127px" },*/
                    { data: 'qty', width: "97px", type: 'num' },
                    { data: 'uom', width: "97px" },
                    { data: 'action', width: "120px" },
                    { data: 'status', width: "120px" },
                    { data: 'remark', width: "125px" },
                    { data: 'approver', width: "120px" },
                    {
                        data: 'note', width: "125px", render: function (data, type, row) {
                            if (!data) return '';
                            if (type === 'display') {
                                var words = data.split(/\s+/);
                                var truncated = words.slice(0, 5).join(' ');
                                if (words.length > 5) truncated += ' ...';
                                return '<span class="truncated-note text-black-50" data-fullnote="' +
                                    $('<div/>').text(data).html() + '">' + truncated + '</span>';
                            }
                            return data;
                        }
                    },
                    { data: 'vendorNo', width: "120px" },
                    { data: 'vendorName', width: "170px" },
                    {
                        data: 'regeDate', width: "120px", type: 'date', render: function (data, type) {
                            if (!data) return '';
                            if (type === 'sort') return data;
                            const date = new Date(data);
                            return date.toLocaleDateString('en-GB');
                        }
                    },
                ],
                order: [[1, 'asc']],
                columnDefs: [
                    { targets: '_all', orderSequence: ["asc", "desc", ""] }
                ],
                drawCallback: function (settings) {
                    const api = this.api();
                    const searchTerm = api.search();

                    api.rows().nodes().to$().removeClass('highlight-match');

                    if (searchTerm) {
                        api.rows({ search: 'applied' }).nodes().to$().addClass('highlight-match');
                    }
                    makeColumnsResizable('#<%= GridView2.ClientID %>');

                },
                select: { style: 'multi', selector: 'td:first-child' },
                initComplete: function () {
                    const api = this.api();
                    const headerCheckbox = $('[id*=chkAll1]');
                    headerCheckbox.on('click', function () {
                        const isChecked = this.checked;
                        api.rows().nodes().to$().find('.rowCheckbox').prop('checked', isChecked);
                        updateSelectedIDs();
                    });
                    grid.on('click', '.rowCheckbox', function () {
                        const allChecked = $('.rowCheckbox:checked').length === $('.rowCheckbox').length;
                        headerCheckbox.prop('checked', allChecked);
                        updateSelectedIDs();
                    });
                }
            });

            // Inline editing on double click
            $('#<%= GridView2.ClientID %> tbody').off('dblclick').on('dblclick', 'td', function () {
                    let cell = dataTable.cell(this);
                    if (!cell.any()) return;

                    let colIndex = cell.index().column;
                    let colName = dataTable.column(colIndex).dataSrc();
                    if (!['status', 'action', 'remark'].includes(colName)) return;

                    let oldValue = cell.data();
                    if ($(this).find('input, select').length > 0) return;

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
                        editor = $('<input type="text" class="form-control form-control-sm">').val(oldValue);
                    }

                    $(this).empty().append(editor);
                    editor.val(oldValue).focus();

                    editor.on('blur change', function () {
                        let newValue = $(this).val();
                        let rowData = dataTable.row(cell.index().row).data();
                        let rowId = rowData.id;

                        if (newValue === oldValue) {
                            cell.data(oldValue).draw();
                            return;
                        }

                        $.ajax({
                            type: "POST",
                            url: "viewer1.aspx/UpdateCell",
                            contentType: "application/json; charset=utf-8",
                            data: JSON.stringify({ id: rowId, column: colName, value: newValue || "" }),
                            success: function () { cell.data(newValue).draw(); },
                            error: function () { cell.data(oldValue).draw(); }
                        });
                    });
                });

            $('.select2-init').select2({ placeholder: "Search or Select", allowClear: true, minimumResultsForSearch: 5 });
        }

        document.addEventListener('DOMContentLoaded', function () {
            updateLocationPillsDisplay();

            const listBox = document.getElementById('<%= lstStoreFilter.ClientID %>');
            if (listBox) {
                listBox.addEventListener('change', updateLocationPillsDisplay);
            }

            document.getElementById("link_home").href = "../AdminDashboard.aspx";
        });

        function setupFilterToggle() {
            Object.entries(filterMap).forEach(([key, mapping]) => {
                const checkbox = document.getElementById(mapping.checkboxId);
                const filterGroup = document.getElementById(mapping.groupId);

                if (checkbox && filterGroup) {
                    filterGroup.style.display = checkbox.checked ? "block" : "none";

                    checkbox.addEventListener("change", function () {
                        const isChecked = this.checked;
                        filterGroup.style.display = isChecked ? "block" : "none";

                        // Clear filter value when unchecked
                        if (!isChecked) {
                            clearFilterControl(mapping.controlId);
                        }
                    });
                }
            });
        }

        function toggleFilter() {
            const filterPane = document.getElementById("filterPane");
            const gridCol = document.getElementById("gridCol");

            if (filterPane.style.display === "none" || filterPane.style.display === "") {
                filterPane.style.display = "block";
                gridCol.classList.remove("col-md-12");
                gridCol.classList.add("col-md-10");
            } else {
                filterPane.style.display = "none";
                gridCol.classList.remove("col-md-10");
                gridCol.classList.add("col-md-12");
            }
        }

        const filterMap = {
            action: {
                checkboxId: '<%= filterAction.ClientID %>',
                controlId: '<%= ddlActionFilter.ClientID %>',
                groupId: '<%= actionFilterGroup.ClientID %>'
            },
            status: {
                checkboxId: '<%= filterStatus.ClientID %>',
                controlId: '<%= ddlStatusFilter.ClientID %>',
                groupId: '<%= statusFilterGroup.ClientID %>'
            },
            store: {
                checkboxId: '<%= filterStore.ClientID %>',
                controlId: '<%= lstStoreFilter.ClientID %>',
                groupId: '<%= storeFilterGroup.ClientID %>'
            },
            item: {
                checkboxId: '<%= filterItem.ClientID%>',
                controlId: '<%= item.ClientID %>',
                groupId: '<%= itemFilterGroup.ClientID %>'
            },
            vendor: {
                checkboxId: '<%= filterVendor.ClientID %>',
                controlId: '<%= vendor.ClientID %>',
                groupId: '<%= vendorFilterGroup.ClientID %>'
            },
            staff: {
                checkboxId: '<%= filterStaff.ClientID %>',
                 controlId: '<%= txtstaffFilter.ClientID %>',
                 groupId: '<%= staffFilterGroup.ClientID %>'
            },
            registrationDate: {
                checkboxId: '<%= filterRegistrationDate.ClientID %>',
                 controlId: '<%= txtRegDateFilter.ClientID %>',
                 groupId: '<%= regeDateFilterGroup.ClientID %>'
            },
            approveDate: {
                checkboxId: '<%= filterApproveDate.ClientID %>',
                controlId: '<%= txtApproveDateFilter.ClientID %>',
                groupId: '<%= approveDateFilterGroup.ClientID %>'
            },
            divisionCode: {
                checkboxId: '<%= filterDivisionCode.ClientID %>',
                controlId: '<%= txtDivisionCodeFilter.ClientID %>',
                groupId: '<%= divisionCodeFilterGroup.ClientID %>'
            }
        };

        //Clear the previous filter values
        function clearFilterControl(controlId) {
            const control = document.getElementById(controlId);
            if (!control) return;

            if (control.type === 'select-one') {
                control.selectedIndex = 0; 
            } else if (control.type === 'text' || control.type === 'date') {
                control.value = '';
            } else if (control.tagName === 'SELECT' && control.multiple) {
                $(control).val(null).trigger('change');
            }

            if ($(control).hasClass('select2-hidden-accessible')) {
                $(control).val(null).trigger('change');
            }
        }

        function updateFilterVisibility() {
            Object.keys(filterMap).forEach(key => {
                const mapping = filterMap[key];
                const checkbox = document.getElementById(mapping.checkboxId);
                const filterGroup = document.getElementById(mapping.groupId);
                const control = document.getElementById(mapping.controlId);

                if (checkbox && filterGroup) {
                    const isChecked = checkbox.checked;
                    filterGroup.style.display = isChecked ? "block" : "none";

                    // Clear filter
                    if (!isChecked && control) {
                        clearFilterControl(mapping.controlId);
                    }
                }
            });
        }

        function updateSelectedIDs() {
            var selectedIDs = [];
            $('.rowCheckbox:checked').each(function () {
                var id = $(this).data('id');
                if (id) {
                    selectedIDs.push(id);
                }
            });
            $('#<%= hfSelectedIDs.ClientID %>').val(selectedIDs.join(','));
            //console.log('Selected IDs:', selectedIDs);
        }

        function InitializeStoreFilter() {
            var $select = $('#<%= lstStoreFilter.ClientID %>');
            var allOptionId = "all";

            $select.find('option').filter(function () {
                return $(this).val() === allOptionId;
            }).slice(1).remove();

            $select.select2({
                placeholder: "-- Select stores --",
                closeOnSelect: false,
                width: '100%',
                allowClear: true,
                dropdownParent: $select.closest('.filter-group'),
                minimumResultsForSearch: 1,
                escapeMarkup: function (m) { return m; },

                language: {
                    noResults: function () {
                        return "No stores found";
                    }
                },
                matcher: function (params, data) {
                    if ($.trim(params.term) === '') {
                        return data;
                    }

                    // Check search term
                    if (data.text.toLowerCase().indexOf(params.term.toLowerCase()) > -1) {
                        return data;
                    }

                    return null;
                },

                templateResult: function (data) {
                    if (!data.id) return data.text;
                    var isAll = data.id === allOptionId;
                    var selectedValues = $select.val() || [];
                    var hasAll = $select.val()?.includes(allOptionId);
                    var isChecked = isAll ? hasAll : selectedValues.includes(data.id);
                    var isDisabled = hasAll && !isAll;

                    return $(
                        '<div class="select2-checkbox-option d-flex align-items-center">' +
                        '  <input type="checkbox" class="select2-checkbox me-2" ' +
                        (isChecked ? 'checked' : '') +
                        (isAll ? ' data-is-all="true"' : '') +
                        (isDisabled ? ' disabled' : '') + '>' +
                        '  <div class="select2-text' + (isAll ? ' fw-bold"' : '"') + '>' + data.text + '</div>' +
                        '</div>'
                    );
                },

                templateSelection: function (data, container) {
                    return '';
                }
            });

            // checkbox
            $select.data('select2').$dropdown.on('click', '.select2-checkbox', function (e) {
                var $option = $(this).closest('.select2-results__option');
                var data = $option.data('data');
                if (data) {
                    var isAll = data.id === allOptionId;
                    var selected = $select.val() || [];

                    if (isAll) {
                        if (this.checked) {
                            $select.val([allOptionId]).trigger('change');
                        } else {
                            $select.val([]).trigger('change');
                        }
                    } else {
                        var index = selected.indexOf(data.id);
                        if (this.checked && index === -1) {
                            selected.push(data.id);
                        } else if (!this.checked && index !== -1) {
                            selected.splice(index, 1);
                        }
                        $select.val(selected).trigger('change');
                    }
                }
            });

            // Update display on change
            $select.on('change', function () {
                var values = $select.val() || [];
                var hasAll = values.includes(allOptionId);

                if (hasAll) {
                    if (values.length > 1) {
                        $select.val([allOptionId]).trigger('change');
                        return;
                    }
                } else {
                    values = values.filter(v => v !== allOptionId);
                    if (values.length !== $select.val().length) {
                        $select.val(values).trigger('change');
                    }
                }

                setTimeout(() => {
                    $select.select2('close');
                    $select.select2('open');
                }, 50);

                // Update checkboxes in dropdown
                updateSelect2Checkboxes($select);
                updateLocationPillsDisplay();
            });


            // Initialize display
            updateLocationPillsDisplay();
            updateSelect2Checkboxes($select);

            $select.on('select2:selecting select2:unselecting', function (e) {
                if (e.params?.args?.originalEvent &&
                    !$(e.params.args.originalEvent.target).hasClass('select2-checkbox')) {
                    e.preventDefault();
                }
            });

            $select.on('select2:clearing', function (e) {
                $select.data('select2').$dropdown.find('.select2-checkbox')
                    .prop('checked', false)
                    .prop('disabled', false);
                $select.trigger('input.select2');
            });
        }

        function updateSelect2Checkboxes($select) {
            var values = $select.val() || [];
            var allOptionId = "all";
            var hasAll = values.includes(allOptionId);

            var $checkboxes = $select.data('select2').$dropdown.find('.select2-checkbox');
            $checkboxes.each(function () {
                var $cb = $(this);
                var data = $cb.closest('.select2-results__option').data('data');
                if (data && data.id) {
                    var isAll = data.id === allOptionId;
                    var isChecked = isAll ? hasAll : values.includes(data.id);
                    var isDisabled = hasAll && !isAll;

                    $cb.prop('checked', isChecked)
                        .prop('disabled', isDisabled);
                }
            });
        }

        function InitializeItemVendorFilter() {
            try {
                $('#<%= item.ClientID %>, #<%= vendor.ClientID %>').each(function () {
                    var $el = $(this);

                    if ($el.hasClass("select2-hidden-accessible")) {
                        $el.select2('destroy');
                    }

                    $el.removeClass('select2-hidden-accessible')
                        .next('.select2-container').remove();

                    $el.select2({
                        placeholder: 'Select item or vendor',
                        allowClear: true,
                        minimumResultsForSearch: 1,
                        width: '100%',
                        dropdownParent: $el.closest('.filter-group'),

                        templateResult: function (data) {
                            return data.text;
                        },

                        templateSelection: function (data) {
                            if (!data.id) return data.text;
                            return data.id;
                        }
                    });
                });
            } catch (err) {
                Swal.fire({
                    icon: 'error',
                    title: 'Initialization Failed',
                    text: 'There was an error initializing item/vendor dropdowns: ' + err.message
                });
            }
        }

        function setupEventDelegation() {
            $(document).off('click', '[id*=chkAll1]').on('click', '[id*=chkAll1]', function () {
                const isChecked = this.checked;

                const grid = $("#<%= GridView2.ClientID %>");
            const rowCheckboxes = grid.find("input[type='checkbox'][id*='CheckBox1']");

            rowCheckboxes.prop('checked', isChecked);

            const selectedIDs = [];
            rowCheckboxes.each(function () {
                if ($(this).is(':checked')) {
                    const dataId = $(this).attr('data-id');
                    if (dataId) {
                        selectedIDs.push(dataId);
                    } else {
                        selectedIDs.push($(this).attr('id'));
                    }
                }
            });

            $('#<%= hfSelectedIDs.ClientID %>').val(selectedIDs.join(','));

            updateSelectedIDs();
        });

            $(document).on('change', '.rowCheckbox', function () {
                updateSelectedIDs();
            });

            if (typeof (Sys) !== 'undefined' && Sys.WebForms && Sys.WebForms.PageRequestManager) {
                const prm = Sys.WebForms.PageRequestManager.getInstance();
                if (!prm._events._list["endRequest"]) {
                    prm.add_endRequest(initializeComponents);
                }
            }
        }

        function pageLoad() {
            updateLocationPillsDisplay();

            const listBox = document.getElementById('<%= lstStoreFilter.ClientID %>');
            if (listBox) {
                listBox.addEventListener('change', updateLocationPillsDisplay);
            }
        }

        function updateLocationPillsDisplay() {
            var $select = $('#<%= lstStoreFilter.ClientID %>');
            var listBox = $select[0];
            var container = document.getElementById('locationPillsContainer');
            var allOptionId = "all";

            if (!listBox || !container) return;

            container.innerHTML = '';

            var values = $select.val() || [];
            var hasAll = values.includes(allOptionId);

            if (hasAll) {
                var pill = document.createElement('span');
                pill.className = 'location-pill';
                pill.innerHTML = `
               <span class="pill-text">All Stores</span>
               <span class="pill-remove" data-value="all">×</span>
            `;

                pill.querySelector('.pill-remove').addEventListener('click', function (e) {
                    e.preventDefault();
                    $select.val(values.filter(v => v !== allOptionId)).trigger('change');
                });

                container.appendChild(pill);
            } else {
                values.forEach(function (value) {
                    if (value === allOptionId) return;

                    var option = $select.find('option[value="' + value + '"]')[0];
                    if (!option) return;

                    var pill = document.createElement('span');
                    pill.className = 'location-pill';
                    pill.innerHTML = `
                       <span class="pill-text">${option.text}</span>
                       <span class="pill-remove" data-value="${value}">×</span>
                    `;

                    pill.querySelector('.pill-remove').addEventListener('click', function (e) {
                        e.preventDefault();
                        $select.val(values.filter(v => v !== value)).trigger('change');
                    });

                    container.appendChild(pill);
                });
            }

            container.style.display = values.length > 0 ? 'grid' : 'none';
        }

        function resetLocationPills() {
            const listBox = document.getElementById('<%= lstStoreFilter.ClientID %>');
            if (listBox) {
                Array.from(listBox.options).forEach(option => {
                    option.selected = false;
                });
                updateLocationPillsDisplay();
            }
        }

        function handleApplyFilters() {
            const grid = $('#<%= GridView2.ClientID %>');

            // Check if at least one filter has a value
            let hasAnyFilter =
                ($('#<%= filterStore.ClientID %>').is(':checked') && $('#<%= lstStoreFilter.ClientID %>').val()?.length > 0) ||
                ($('#<%= filterItem.ClientID %>').is(':checked') && $('#<%= item.ClientID %>').val()) ||
                ($('#<%= filterVendor.ClientID %>').is(':checked') && $('#<%= vendor.ClientID %>').val()) ||
                ($('#<%= filterStatus.ClientID %>').is(':checked') && $('#<%= ddlStatusFilter.ClientID %>').val()) ||
                ($('#<%= filterAction.ClientID %>').is(':checked') && $('#<%= ddlActionFilter.ClientID %>').val()) ||
                ($('#<%= filterRegistrationDate.ClientID %>').is(':checked') && $('#<%= txtRegDateFilter.ClientID %>').val()) ||
                ($('#<%= filterStaff.ClientID %>').is(':checked') && $('#<%= txtstaffFilter.ClientID %>').val()) ||
                ($('#<%= filterDivisionCode.ClientID %>').is(':checked') && $('#<%= txtDivisionCodeFilter.ClientID %>').val()) ||
                ($('#<%= filterApproveDate.ClientID %>').is(':checked') && $('#<%= txtApproveDateFilter.ClientID %>').val());

            if (!hasAnyFilter) {
                Swal.fire('Warning!', 'Please select at least one filter with a value.', 'warning');
                return false;
            }

            if ($.fn.DataTable.isDataTable(grid)) {
                grid.DataTable().ajax.reload();
            } else {
                initializeComponents();
            }
        }

        Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {

                initializeComponents();
                setupFilterToggle();
                setupEventDelegation();
                InitializeStoreFilter();
                InitializeItemVendorFilter();
        });

        history.pushState(null, null, location.href);
        window.addEventListener("popstate", function (event) {
            location.reload();
        });

        function enableGridViewInlineEditing() {
            const $grid = $('#<%= GridView2.ClientID %>');

            $grid.find('thead th').each(function (index) {
                if (!$(this).attr("data-column")) {
                    let text = $(this).text().trim().toLowerCase();

                    if (text.includes("reason")) $(this).attr("data-column", "action"); else if (text.includes("action")) $(this).attr("data-column", "status"); else if (text.includes("remark")) $(this).attr("data-column", "remark"); else $(this).attr("data-column", text);
                }
            });

            $grid.find('tbody').off('dblclick').on('dblclick', 'td', function () {
                let $cell = $(this);
                let colIndex = $cell.index();
                let colName = $grid.find('thead th').eq(colIndex).data("column");

                if (!['status', 'action', 'remark'].includes(colName)) return;
                if ($cell.find('input, select').length > 0) return;

                let oldValue = $cell.text().trim();
                let editor; if (colName === 'action') {
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
                <option value="Supplier Discon">Supplier Discon</option> </select>`);
                } else if (colName === 'status') {
                    editor = $(`<select class="form-select form-select-sm">
                <option value="">-- Select Action --</option>
                <option value="Reorder Done">Reorder Done</option> 
                <option value="No Reordering">No Reordering</option> 
                </select>`);
                } else {
                    editor = $('<input type="text" class="form-control form-control-sm">').val(oldValue);
                }

                $cell.empty().append(editor);
                editor.val(oldValue).focus();
                editor.on('blur change', function () {
                    let newValue = $(this).val();
                    let $row = $cell.closest('tr');
                    let rowId = $row.find("input.rowCheckbox").data("id");

                    if (newValue === oldValue) {
                        $cell.text(oldValue); return;
                    }
                    $.ajax({
                        type: "POST",
                        url: "viewer1.aspx/UpdateCell",
                        contentType: "application/json; charset=utf-8",
                        data: JSON.stringify({
                            id: rowId,
                            column: colName,
                            value: newValue || ""
                        }),
                        success: function () {
                            $cell.text(newValue);
                        }, error: function () { $cell.text(oldValue); }
                    });
                });
            });
        }

    </script>

    <%--<style>
            table.dataTable > thead > tr > th {
                background-color: #BD467F !important;
                color: white !important;
            }

            table.dataTable thead th.dtfc-fixed-left,
            table.dataTable thead th.dtfc-fixed-right {
                background-color: #BD467F !important;
                color: white !important;
            }
        </style>  --%>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <%
        var permissions = Session["formPermissions"] as Dictionary<string, string>;
        string expiryPerm = permissions != null && permissions.ContainsKey("ReorderQuantity") ? permissions["ReorderQuantity"] : null;
        bool canViewOnly = !string.IsNullOrEmpty(expiryPerm) && expiryPerm != "admin";
    %>

    <div class="container-fluid col-lg-12 col-md-12 mt-0">
        <div class="card shadow-md border-dark-subtle" style="background-color: #F1B4D1;">
            <div class="card-header" style="background-color: #BD467F;">
                <h4 class="text-center fw-bold text-white">Reorder Quantity List</h4>
            </div>

            <div class="card-body">
                <div class="col-lg-12 col-md-12">
                    <div class="row g-2 align-items-center">
                        <!-- Filter Button -->
                        <div class="col-6 col-md-auto">
                            <asp:Button ID="btnFilter" runat="server"
                                CssClass="btn text-white w-100"
                                Style="background: #A10D54;"
                                Text="Show Filter"
                                OnClientClick="toggleFilter(); return false;" />
                        </div>

                        <!-- Edit Button -->
                        <%--      <div class="col-6 col-md-auto">
                         <asp:Button Text="Edit" runat="server"
                             CssClass="btn btn-secondary text-white w-100"
                             ID="btnEdit" OnClientClick="document.getElementById('<%= hfLastSearch.ClientID %>').value = $('.dataTables_filter input').val();"
                             OnClick="btnEdit_Click" />
                     </div>--%>

                        <!-- Action Dropdown -->
                        <div class="col-12 col-md-auto">
                            <asp:DropDownList ID="ddlAction" runat="server"
                                CssClass="form-select w-100">
                                <asp:ListItem Text="-- Select Reason --" Value="0" />
                                <asp:ListItem Text="None" Value="1" />
                                <asp:ListItem Text="Overstock and Redistribute" Value="2" />
                                <asp:ListItem Text="Redistribute" Value="3" />
                                <asp:ListItem Text="Allocation Item" Value="4" />
                                <asp:ListItem Text="Shortage Item" Value="5" />
                                <asp:ListItem Text="System Enough" Value="6" />
                                <asp:ListItem Text="Tail Off Item" Value="7" />
                                <asp:ListItem Text="Purchase Blocked" Value="8" />
                                <asp:ListItem Text="Already Added Reorder" Value="9" />
                                <asp:ListItem Text="Customer Requested Item" Value="10" />
                                <asp:ListItem Text="No Hierarchy" Value="11" />
                                <asp:ListItem Text="Near Expiry Item" Value="12" />
                                <asp:ListItem Text="Reorder Qty is large, Need to adjust Qty" Value="13" />
                                <asp:ListItem Text="Discon Item" Value="14" />
                                <asp:ListItem Text="Supplier Discon" Value="15" />
                            </asp:DropDownList>
                        </div>

                        <div class="col-12 col-md-auto">
                            <asp:Button ID="btnUpdateSelected" runat="server"
                                CssClass="btn text-white w-100"
                                Style="background: #A10D54;"
                                Text="Update Reason"
                                OnClick="btnUpdateSelected_Click" />
                        </div>

                        <div class="col-12 col-md-auto">
                            <asp:DropDownList ID="ddlStatus" runat="server"
                                CssClass="form-select w-100">
                                <asp:ListItem Text="-- Select Action --" Value="0" />
                                <asp:ListItem Value="1" Text="Reorder Done"></asp:ListItem>
                                <asp:ListItem Value="2" Text="No Reordering"></asp:ListItem>
                            </asp:DropDownList>
                        </div>

                        <div class="col-12 col-md-auto">
                            <asp:Button ID="btnStatusSelected" runat="server"
                                CssClass="btn text-white w-100"
                                Style="background: #A10D54;"
                                Text="Update Action"
                                OnClick="btnStatusSelected_Click" />
                        </div>

                        <%
                            var panelPermissions = Session["formPermissions"] as Dictionary<string, string>;
                            string panelExpiryPerm = panelPermissions != null && panelPermissions.ContainsKey("ReorderQuantity") ? panelPermissions["ReorderQuantity"] : null;
                            bool panelCanViewOnly = !string.IsNullOrEmpty(panelExpiryPerm) && panelExpiryPerm == "admin";
                        %>

                        <% if (panelCanViewOnly)
                        { %>

                        <div class="col-12 col-md-auto">
                            <asp:Button Text="Delete" runat="server"
                                CssClass="btn btn-danger text-white w-100"
                                ID="btnDelete" OnClick="btnDelete_Click" />
                        </div>

                        <% } %>
                    </div>
                </div>

                <div class="d-flex pe-2 pt-1 col-lg-12 col-md-12 overflow-x-auto overflow-y-auto">
                    <div class="row">
                        <!-- Filter Panel (Hidden by default) -->
                        <div class="col" id="filterPane" style="display: none;">
                            <asp:Panel ID="Panel1" runat="server">
                                <div class="filter-panel p-2 bg-light border me-2">
                                    <h4>Filters</h4>
                                    <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePartialRendering="true">
                                        <Scripts>
                                            <asp:ScriptReference Name="MicrosoftAjax.js" />
                                            <asp:ScriptReference Name="MicrosoftAjaxWebForms.js" />
                                        </Scripts>
                                    </asp:ScriptManager>

                                    <asp:UpdatePanel ID="UpdatePanel1" runat="server" UpdateMode="Conditional">
                                        <ContentTemplate>
                                            <!-- Filter Selection Dropdown -->
                                            <div class="mb-3">
                                                <label for="<%= filterDropdownButton.ClientID %>">Choose Fields to Filter</label>
                                                <div class="dropdown">
                                                    <button class="btn dropdown-toggle text-white" style="background-color: #BD467F;"
                                                        type="button" id="filterDropdownButton" data-bs-toggle="dropdown"
                                                        aria-haspopup="true" aria-expanded="false" runat="server">
                                                        Select Filters
                                                    </button>
                                                    <div class="dropdown-menu p-3" aria-labelledby="filterDropdownButton" style="max-height: 250px; overflow-y: auto;">
                                                        <!-- Checkboxes for each filter -->
                                                        <div class="form-check">
                                                            <asp:CheckBox ID="filterAction" runat="server" CssClass="form-check-input" />
                                                            <label class="form-check-label" for="<%= filterAction.ClientID %>">Reason</label>
                                                        </div>
                                                        <div class="form-check">
                                                            <asp:CheckBox ID="filterStatus" runat="server" CssClass="form-check-input" />
                                                            <label class="form-check-label" for="<%= filterStatus.ClientID %>">Action</label>
                                                        </div>
                                                        <div class="form-check">
                                                            <asp:CheckBox ID="filterStore" runat="server" CssClass="form-check-input" />
                                                            <label class="form-check-label" for="<%= filterStore.ClientID %>">Location</label>
                                                        </div>
                                                        <div class="form-check">
                                                            <asp:CheckBox ID="filterItem" runat="server" CssClass="form-check-input" />
                                                            <label class="form-check-label" for="<%= filterItem.ClientID %>">Item No</label>
                                                        </div>
                                                        <div class="form-check">
                                                            <asp:CheckBox ID="filterStaff" runat="server" CssClass="form-check-input" />
                                                            <label class="form-check-label" for="<%= filterStaff.ClientID %>">Approver</label>
                                                        </div>
                                                        <div class="form-check">
                                                            <asp:CheckBox ID="filterVendor" runat="server" CssClass="form-check-input" />
                                                            <label class="form-check-label" for="<%= filterVendor.ClientID %>">Vendor</label>
                                                        </div>
                                                        <div class="form-check">
                                                            <asp:CheckBox ID="filterRegistrationDate" runat="server" CssClass="form-check-input" />
                                                            <label class="form-check-label" for="<%= filterRegistrationDate.ClientID %>">Registration Date</label>
                                                        </div>
                                                        <div class="form-check">
                                                            <asp:CheckBox ID="filterApproveDate" runat="server" CssClass="form-check-input" />
                                                            <label class="form-check-label" for="<%= filterApproveDate.ClientID %>">Approved Date</label>
                                                        </div>
                                                        <div class="form-check">
                                                            <asp:CheckBox ID="filterDivisionCode" runat="server" CssClass="form-check-input" />
                                                            <label class="form-check-label" for="<%= filterDivisionCode.ClientID %>">Division Code</label>
                                                        </div>
                                                    </div>
                                                </div>
                                            </div>

                                            <!-- Dynamic Filters Container -->
                                            <div id="dynamicFilters">
                                                <!-- Action Filter -->
                                                <div class="form-group mt-3 filter-group" id="actionFilterGroup" runat="server" style="display: none">
                                                    <label for="<%= ddlActionFilter.ClientID %>">Reason</label>
                                                    <asp:DropDownList ID="ddlActionFilter" runat="server" CssClass="form-control">
                                                        <asp:ListItem Text="-- Select Reason --" Value="0" />
                                                        <asp:ListItem Text="None" Value="1" />
                                                        <asp:ListItem Text="Overstock and Redistribute" Value="2" />
                                                        <asp:ListItem Text="Redistribute" Value="3" />
                                                        <asp:ListItem Text="Allocation Item" Value="4" />
                                                        <asp:ListItem Text="Shortage Item" Value="5" />
                                                        <asp:ListItem Text="System Enough" Value="6" />
                                                        <asp:ListItem Text="Tail Off Item" Value="7" />
                                                        <asp:ListItem Text="Purchase Blocked" Value="8" />
                                                        <asp:ListItem Text="Already Added Reorder" Value="9" />
                                                        <asp:ListItem Text="Customer Requested Item" Value="10" />
                                                        <asp:ListItem Text="No Hierarchy" Value="11" />
                                                        <asp:ListItem Text="Near Expiry Item" Value="12" />
                                                        <asp:ListItem Text="Reorder Qty is large, Need to adjust Qty" Value="13" />
                                                        <asp:ListItem Text="Discon Item" Value="14" />
                                                        <asp:ListItem Text="Supplier Discon" Value="15" />
                                                    </asp:DropDownList>
                                                </div>

                                                <!-- Status Filter -->
                                                <div class="form-group mt-3 filter-group" id="statusFilterGroup" runat="server" style="display: none">
                                                    <label for="<%= ddlStatusFilter.ClientID %>">Action</label>
                                                    <asp:DropDownList ID="ddlStatusFilter" runat="server" CssClass="form-control">
                                                        <asp:ListItem Text="-- Select Action --" Value="" />
                                                        <asp:ListItem Value="1" Text="Reorder Done"></asp:ListItem>
                                                        <asp:ListItem Value="2" Text="No Reordering"></asp:ListItem>
                                                    </asp:DropDownList>
                                                </div>

                                                <!-- Store Filter -->
                                                <div class="form-group mt-3 filter-group" id="storeFilterGroup" runat="server" style="display: none">
                                                    <label for="<%=lstStoreFilter.ClientID %>">Location</label>

                                                    <asp:ListBox ID="lstStoreFilter" runat="server" CssClass="form-control select2-multi-check"
                                                        SelectionMode="Multiple" Style="display: none"></asp:ListBox>

                                                    <div id="locationPillsContainer" class="location-pills-container mb-2"></div>

                                                    <div class="location-pill-template" style="display: block"></div>
                                                </div>

                                                <!-- Item No Filter -->
                                                <div class="form-group mt-3 filter-group" id="itemFilterGroup" runat="server" style="display: none">
                                                    <label for="<%= item.ClientID %>" style="display: block">Item No</label>
                                                    <asp:DropDownList ID="item" runat="server" CssClass="form-control select2-init" Style="width: 333px">
                                                        <asp:ListItem Text="" Value="" />
                                                    </asp:DropDownList>
                                                </div>

                                                <!-- Staff Filter -->
                                                <div class="form-group mt-3 filter-group" id="staffFilterGroup" runat="server" style="display: none">
                                                    <label for="<%= txtstaffFilter.ClientID %>">Approver</label>
                                                    <asp:TextBox ID="txtstaffFilter" runat="server" CssClass="form-control" Placeholder="Enter approver name"></asp:TextBox>
                                                </div>

                                                <!-- Vendor Filter -->
                                                <div class="form-group mt-3 filter-group" id="vendorFilterGroup" runat="server" style="display: none">
                                                    <label for="<%= vendor.ClientID %>" style="display: block">Vendor</label>
                                                    <asp:DropDownList ID="vendor" runat="server" CssClass="form-control select2-init" Style="width: 333px">
                                                        <asp:ListItem Text="" Value="" />
                                                    </asp:DropDownList>
                                                </div>

                                                <!-- Registration Date Filter -->
                                                <div class="form-group mt-3 filter-group" id="regeDateFilterGroup" runat="server" style="display: none">
                                                    <label for="<%= txtRegDateFilter.ClientID %>">Registration Date</label>
                                                    <asp:TextBox ID="txtRegDateFilter" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                                                </div>

                                                <!-- Approved Date Filter -->
                                                <div class="form-group mt-3 filter-group" id="approveDateFilterGroup" runat="server" style="display: none">
                                                    <label for="<%= txtApproveDateFilter.ClientID %>">Approved Date</label>
                                                    <asp:TextBox ID="txtApproveDateFilter" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                                                </div>

                                                <!-- division Filter -->
                                                <div class="form-group mt-3 filter-group" id="divisionCodeFilterGroup" runat="server" style="display: none">
                                                    <label for="<%= txtDivisionCodeFilter.ClientID %>">Division Code</label>
                                                    <asp:TextBox ID="txtDivisionCodeFilter" runat="server" CssClass="form-control" Placeholder="Enter division code"></asp:TextBox>
                                                </div>
                                            </div>

                                            <!-- Filter Buttons -->
                                            <div class="form-group mt-3">
                                                <asp:Button ID="btnApplyFilter" runat="server"
                                                    CssClass="btn text-white mb-1"
                                                    Style="background-color: #BD467F;"
                                                    Text="Apply Filters"
                                                    OnClientClick="handleApplyFilters(); return false;"
                                                    CausesValidation="false" />

                                                <asp:Button ID="btnResetFilter" runat="server"
                                                    CssClass="btn text-white"
                                                    Style="background-color: #BD467F;"
                                                    Text="Reset Filters"
                                                    OnClick="ResetFilters_Click"
                                                    CausesValidation="false"
                                                    UseSubmitBehavior="true" />
                                            </div>
                                        </ContentTemplate>
                                        <Triggers>
                                            <asp:AsyncPostBackTrigger ControlID="btnApplyFilter" EventName="Click" />
                                            <asp:AsyncPostBackTrigger ControlID="btnResetFilter" EventName="Click" />
                                            <asp:AsyncPostBackTrigger ControlID="btnUpdateSelected" EventName="Click" />
                                        </Triggers>
                                    </asp:UpdatePanel>
                                </div>
                            </asp:Panel>
                        </div>
                    </div>

                    <asp:HiddenField ID="hfSelectedRows" runat="server" />
                    <asp:HiddenField ID="hfSelectedIDs" runat="server" />
                    <asp:HiddenField ID="hflength" runat="server" />
                   <!-- <asp:HiddenField ID="hfEditedRowId" runat="server" /> -->
                    <asp:HiddenField ID="hfIsFiltered" runat="server" Value="false" />
                    <asp:HiddenField ID="hfEditInitiatedByButton" runat="server" />

                    <!-- Table -->
                    <div class="col-md-12 ms-4" id="gridCol">
                        <asp:UpdatePanel ID="UpdatePanel2" runat="server" UpdateMode="Conditional">
                            <ContentTemplate>

                                <asp:Panel ID="pnlNoData" runat="server" Visible="false">
                                    <div class="alert alert-info">No items to Filter</div>
                                </asp:Panel>

                                <div class=" table-responsive gridview-container pt-2 pe-2 rounded-1">
                                    <asp:GridView ID="GridView2" runat="server"
                                        CssClass="table table-striped ResizableGrid table-hover border-2 shadow-lg sticky-grid overflow-x-auto overflow-y-auto display"
                                        AutoGenerateColumns="False"
                                        DataKeyNames="id" ClientIDMode="Static"
                                        UseAccessibleHeader="true"
                                        OnRowEditing="GridView2_RowEditing"
                                        OnRowUpdating="GridView2_RowUpdating"
                                        OnRowCancelingEdit="GridView2_RowCancelingEdit"
                                        OnSorting="GridView1_Sorting"
                                        OnRowDataBound="GridView2_RowDataBound"
                                        OnRowCreated="GridView1_RowCreated"
                                        AllowPaging="false"
                                        PageSize="100"
                                        CellPadding="4"
                                        ForeColor="#333333"
                                        GridLines="None"
                                        AutoGenerateEditButton="false" ShowHeaderWhenEmpty="true">

                                        <EditRowStyle BackColor="white" />
                                        <FooterStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
                                        <HeaderStyle Wrap="false" BackColor="#BD467F" Font-Bold="True" ForeColor="White"></HeaderStyle>
                                        <PagerStyle CssClass="pagination-wrapper" HorizontalAlign="Center" VerticalAlign="Middle" />
                                        <RowStyle CssClass="table-row data-row" BackColor="#F7F6F3" ForeColor="#333333"></RowStyle>
                                        <AlternatingRowStyle CssClass="table-alternating-row" BackColor="White" ForeColor="#284775"></AlternatingRowStyle>

                                        <EmptyDataTemplate>
                                            <div class="alert alert-info">No items to Filter</div>
                                        </EmptyDataTemplate>

                                        <Columns>

                                            <asp:TemplateField HeaderText="ID" Visible="false">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblId" runat="server" Text='<%# Eval("id") %>' CssClass="row-id" />
                                                </ItemTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1" ItemStyle-CssClass="fixed-column-1">
                                                <HeaderTemplate>
                                                    <asp:CheckBox ID="chkAll1" runat="server" />
                                                </HeaderTemplate>
                                                <ItemTemplate>
                                                    <input type="checkbox" class="rowCheckbox" data-id='<%# Eval("id") %>' runat="server" id="CheckBox1" />
                                                </ItemTemplate>
                                                <ControlStyle Width="70px" />
                                                <HeaderStyle ForeColor="White" BackColor="#BD467F" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Lines No" ItemStyle-Width="100px" SortExpression="no" HeaderStyle-ForeColor="White" ItemStyle-HorizontalAlign="Justify" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header2" ItemStyle-CssClass="fixed-column-2">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblNo" runat="server" Text='<%# Eval("no") %>' />
                                                    <controlstyle width="150px" />
                                                </ItemTemplate>
                                                <HeaderStyle ForeColor="White" BackColor="#BD467F" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Location" SortExpression="storeNo" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header3" ItemStyle-CssClass="fixed-column-3">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblStoreNo" runat="server" Text='<%# Eval("storeNo") %>' />
                                                </ItemTemplate>
                                                <ControlStyle Width="120px" />
                                                <HeaderStyle ForeColor="White" BackColor="#BD467F" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Division" SortExpression="divisionCode" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header4" ItemStyle-CssClass="fixed-column-4">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblDivisionCode" runat="server" Text='<%# Eval("divisionCode") %>' />
                                                </ItemTemplate>
                                                <ControlStyle Width="90px" />
                                                <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Approved Date" SortExpression="approveDate" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0 border-1" ItemStyle-CssClass="fixed-column-6 border-1">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblApprove" runat="server" Text='<%# Eval("approveDate", "{0:dd-MM-yyyy}") %>' />
                                                </ItemTemplate>
                                                <ControlStyle Width="120px" />
                                                <HeaderStyle ForeColor="White" BackColor="#BD467F" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Item No" SortExpression="itemNo" HeaderStyle-ForeColor="White" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0 border-1" ItemStyle-CssClass="fixed-column-8 border-1">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblItemNo" runat="server" Text='<%# Eval("itemNo") %>' />
                                                </ItemTemplate>
                                                <HeaderStyle ForeColor="White" BackColor="#BD467F" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Description" SortExpression="description" HeaderStyle-ForeColor="Black" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0 border-1" ItemStyle-CssClass="fixed-column-8 border-1">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblDesc" runat="server" Text='<%# Eval("description") %>' />
                                                </ItemTemplate>
                                                <HeaderStyle ForeColor="White" BackColor="#BD467F" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Packing Info" SortExpression="packingInfo" HeaderStyle-ForeColor="Black" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0 border-1" ItemStyle-CssClass="fixed-column-9 border-1">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblPacking" runat="server" Text='<%# Eval("packingInfo") %>' />
                                                </ItemTemplate>
                                                <HeaderStyle ForeColor="White" BackColor="#BD467F" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>

                                            <%--     <asp:TemplateField HeaderText="Barcode" SortExpression="barcodeNo" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0" ItemStyle-CssClass="fixed-column-51">
                                                   <ItemTemplate>
                                                       <asp:Label ID="lblBarcode" runat="server" Text='<%# Eval("barcodeNo") %>' />
                                                   </ItemTemplate>
                                                   <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                                   <ItemStyle HorizontalAlign="Justify" />
                                               </asp:TemplateField>--%>

                                            <asp:TemplateField HeaderText="Qty" SortExpression="qty" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0 border-1" ItemStyle-CssClass="fixed-column-52 border-1">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblQty" runat="server" Text='<%# Eval("qty") %>' />
                                                </ItemTemplate>
                                                <HeaderStyle ForeColor="White" BackColor="#BD467F" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="UOM" SortExpression="uom" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0 border-1" ItemStyle-CssClass="fixed-column-53 border-1">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblUom" runat="server" Text='<%# Eval("uom") %>' />
                                                </ItemTemplate>
                                                <HeaderStyle ForeColor="White" BackColor="#BD467F" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Reason" SortExpression="action" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0 border-1" ItemStyle-CssClass="fixed-column-54 border-1">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblAction" runat="server" Text='<%# Eval("action") %>'></asp:Label>
                                                </ItemTemplate>
                                                <HeaderStyle ForeColor="White" BackColor="#BD467F" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Action" SortExpression="status" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0 border-1" ItemStyle-CssClass="fixed-column-55 border-1">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblStatus" runat="server" Text='<%# Eval("status") %>'></asp:Label>
                                                </ItemTemplate>
                                                <HeaderStyle ForeColor="White" BackColor="#BD467F" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Remark" SortExpression="remark" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0 border-1" ItemStyle-CssClass="fixed-column-56 border-1">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblRemark" runat="server" Text='<%# Eval("Remark") %>'></asp:Label>
                                                </ItemTemplate>
                                                <HeaderStyle ForeColor="White" BackColor="#BD467F" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Approver" SortExpression="approver" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0 border-1" ItemStyle-CssClass="fixed-column-58 border-1">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblStaff" runat="server" Text='<%# Eval("approver") %>' />
                                                </ItemTemplate>
                                                <HeaderStyle ForeColor="White" BackColor="#BD467F" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Note" SortExpression="note" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0 border-1" ItemStyle-CssClass="fixed-column-59 border-1">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblNote" runat="server"
                                                        Text='<%# TruncateWords(Eval("note").ToString(), 5) %>'
                                                        data-fullnote='<%# HttpUtility.HtmlEncode(Eval("note").ToString()) %>'
                                                        CssClass="truncated-note text-black-50" />
                                                </ItemTemplate>
                                                <HeaderStyle ForeColor="White" BackColor="#BD467F" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Vendor No" SortExpression="vendorNo" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0 border-1" ItemStyle-CssClass="fixed-column-61 border-1">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblVendorNo" runat="server" Text='<%# Eval("vendorNo") %>' />
                                                </ItemTemplate>
                                                <HeaderStyle ForeColor="White" BackColor="#BD467F" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Vendor Name" ItemStyle-Width="170px" SortExpression="vendorName" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0 border-1" ItemStyle-CssClass="fixed-column-55 border-1">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblVendorName" runat="server" Text=' <%# Eval("vendorName") %>' />
                                                </ItemTemplate>
                                                <HeaderStyle ForeColor="White" BackColor="#BD467F" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Regi Date" SortExpression="regeDate" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0 border-1" ItemStyle-CssClass="fixed-column-56 border-1">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblRege" runat="server" Text='<%# Eval("regeDate", "{0:dd-MM-yyyy}") %>' />
                                                </ItemTemplate>
                                                <HeaderStyle ForeColor="White" BackColor="#BD467F" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>

                                       <%-- <asp:TemplateField HeaderText="Completed Date" SortExpression="completedDate" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0" ItemStyle-CssClass="fixed-column-61">
                                                 <ItemTemplate>
                                                     <asp:Label ID="lblCompleted" runat="server" Text=' <%# Eval("completedDate", "{0:dd-MM-yyyy}") %>' />
                                                 </ItemTemplate>
                                                 <ControlStyle Width="125px" />
                                                 <HeaderStyle ForeColor="White" BackColor="#BD467F" />
                                                 <ItemStyle HorizontalAlign="Justify" />
                                             </asp:TemplateField>--%>                       <%--  <asp:Commaeld ShowEditButton="true" ShowCancelButton="true" HeaderStyle-CssClass="position-sticky top-0" ItemStyle-BackColor="white" ControlStyle-CssClass="btn m-1 text-white"
                                                 EditText="-" UpdateText="<i class='fa-solid fa-file-arrow-up'></i>"
                                                 CancelText="<i class='fa-solid fa-xmark'></i>">
                                                 <ControlStyle CssClass="btn m-1 text-white" Width="75px" BackColor="#BD467F" />
                                                 <HeaderStyle ForeColor="White" BackColor="#BD467F" />
                                                 <ItemStyle HorizontalAlign="Justify" />
                                             </asp:CommandField>--%>
                                        </Columns>


                                        <SelectedRowStyle BackColor="#E2DED6" Font-Bold="True" ForeColor="#333333" />
                                        <SortedAscendingCellStyle BackColor="#E9E7E2" />
                                        <SortedAscendingHeaderStyle BackColor="#506C8C" />
                                        <SortedDescendingCellStyle BackColor="#FFFDF8" />
                                        <SortedDescendingHeaderStyle BackColor="#6F8DAE" />
                                    </asp:GridView>
                                </div>
                            </ContentTemplate>
                            <Triggers>
                                <asp:AsyncPostBackTrigger ControlID="btnApplyFilter" EventName="Click" />
                                <asp:AsyncPostBackTrigger ControlID="btnResetFilter" EventName="Click" />
                                <asp:AsyncPostBackTrigger ControlID="btnUpdateSelected" EventName="Click" />
                            </Triggers>
                        </asp:UpdatePanel>
                    </div>
                </div>

            </div>
        </div>
    </div>

    <!-- Note Modal -->
    <div class="modal fade" id="noteModal" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Full Note</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                </div>
                <div class="modal-body">
                    <!-- Full note will appear here -->
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                </div>
            </div>
        </div>
    </div>

</asp:Content>
