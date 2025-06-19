<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" EnableEventValidation="false" CodeBehind="itemList.aspx.cs" Inherits="Expiry_list.itemList" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
<script src="js/customJS.js"></script>
<script type="text/javascript">

    $(document).ready(function () {
        InitializeStoreFilter();
        if (typeof (Sys) !== 'undefined') {
            Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
                updateFilterVisibility();
                InitializeStoreFilter();

                // Reattach change handlers after postback
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
        focusOnEditedRow();
        setupFilterToggle();
    });

    let isDataTableInitialized = false;

    function initializeComponents() {
        const grid = $("#<%= GridView2.ClientID %>");

        if (<%= GridView2.EditIndex >= 0 ? "true" : "false" %>) {
            if ($.fn.DataTable.isDataTable(grid)) {
                grid.DataTable().destroy();
                grid.removeAttr('style');
            }
            return;
        }

        if (!$.fn.DataTable.isDataTable(grid)) {
            if (grid.find('thead').length === 0) {
                grid.prepend($('<thead/>').append(grid.find('tr:first').detach()));
            }

            grid.DataTable({
                responsive: true,
                ordering: true,
                serverSide: true,
                paging: true,
                filter: true,
                scrollX: true,
                scrollY: 497,
                scrollCollapse: true,
                autoWidth: false,
                stateSave: true,
                processing: true,
                ajax: {
                    url: 'itemList.aspx',
                    type: 'POST',
                    data: function (d) {
                            <%-- console.log(d);
                             $('#<%= hflength.ClientID %>').val(d.length);--%>
                        return {
                            draw: d.draw,
                            start: d.start,
                            length: d.length,
                            order: d.order,
                            search: d.search.value,

                            action: $('#<%= ddlActionFilter.ClientID %>').val(),
                            status: $('#<%= ddlStatusFilter.ClientID %>').val(),
                            store: $('#<%= lstStoreFilter.ClientID %>').val(),
                            item: $('#<%= item.ClientID %>').val(),
                            expiryDate: $('#<%= txtExpiryDateFilter.ClientID %>').val(),
                            staff: $('#<%= txtstaffFilter.ClientID %>').val(),
                            batch: $('#<%= txtBatchNoFilter.ClientID %>').val(),
                            vendor: $('#<%= vendor.ClientID %>').val(),
                            regDate: $('#<%= txtRegDateFilter.ClientID %>').val()
                        };
                    }
                },
                fixedColumns: {
                    leftColumns: 5,
                    rightColumns: 0,
                    heightMatch: 'none'
                },
                columns: [
                    //{
                    //    data: 'id',
                    //    visible: false
                    //},
                    {
                        data: 'checkbox', orderable: false, width: "30px", render: function (data, type, row, meta) {
                            return '<input type="checkbox" class="rowCheckbox" data-id="' + row.id + '" onclick="handleSingleSelection(this)"/>';
                        }
                    },
                    {
                        data: null,
                        width: "30px",
                        orderable: false,
                        render: function (data, type, row, meta) {
                            return meta.settings._iDisplayStart + meta.row + 1;
                        }
                    },
                    { data: 'no', width: "100px" },
                    { data: 'itemNo', width: "50px" },
                    { data: 'description', width: "297px" },
                    { data: 'barcodeNo', width: "137px" },
                    { data: 'qty', width: "97px" },
                    { data: 'uom', width: "97px" },
                    { data: 'packingInfo', width: "120px" },
                    { data: 'expiryDate', width: "120px", render: function (data, type) { return formatDate(data, type); } },
                    { data: 'storeNo', width: "120px" },
                    { data: 'staffName', width: "120px" },
                    { data: 'batchNo', width: "120px" },
                    { data: 'vendorNo', width: "120px" },
                    { data: 'vendorName', width: "170px" },
                    {
                        data: 'regeDate', width: "120px", render: function (data, type) {
                            const date = new Date(data);
                            return date.toLocaleDateString('en-GB');
                        }
                    },
                    { data: 'action', width: "120px" },
                    { data: 'status', width: "120px" },
                    { data: 'note', width: "125px" },
                    { data: 'remark', width: "125px" },
                    {
                        data: 'completedDate',
                        width: "125px",
                        render: function (data, type, row) {
                            if (type === 'display' && data) {
                                var date = new Date(data);
                                var day = ('0' + date.getDate()).slice(-2);
                                var month = ('0' + (date.getMonth() + 1)).slice(-2);
                                var year = date.getFullYear();
                                return day + '/' + month + '/' + year;
                            }
                            return data;
                        }
                    },
                    {
                        data: null,
                        orderable: false,
                        defaultContent: '',
                        className: 'dt-center',
                        visible: false
                    },
                ],
                order: [[1, 'asc'], [2, 'asc']],
                columnDefs: [
                    {
                        targets: [21],
                        visible: false,
                        searchable: false
                    }
                ],
                select: { style: 'multi', selector: 'td:first-child' },
                lengthMenu: [[100, 500, 1000], [100, 500, 1000]],
                initComplete: function (settings, json) {
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

                    $('[id*=chkAll1]').on('click', function () {
                        const isChecked = this.checked;
                        $('.rowCheckbox', grid).prop('checked', isChecked);
                        updateSelectedIDs();
                    });

                }
            });

            $('.select2-init').select2({
                placeholder: "Search or Select",
                allowClear: true,
                minimumResultsForSearch: 5
            });
        };
    };

    function focusOnEditedRow() {
        const rowId = $('#<%= hfSelectedIDs.ClientID %>').val();
         if (!rowId) return;

         const grid = $("#<%= GridView2.ClientID %>");

         if ($.fn.DataTable.isDataTable(grid)) {
             const dt = grid.DataTable();

             dt.order([]).search('').draw();

             const rowIndex = dt.rows().indexes().find(index =>
                 dt.row(index).data().id === rowId
             );

             if (typeof rowIndex !== 'undefined') {
                 dt.row(rowIndex).move(0).draw(false);

                 grid.parent().animate({ scrollTop: 0 }, 500);
                 dt.row(0).nodes().to$()
                     .addClass('highlight-row')
                     .trigger('focus');
             }
         } else {
             const $row = grid.find(`tr[data-id='${rowId}']`);
             if ($row.length) {
                 const $tbody = grid.find('tbody');
                 $row.prependTo($tbody);

                 $('html, body').animate({
                     scrollTop: grid.offset().top - 100
                 }, 500);
                 $row.addClass('highlight-row');

             }
         }

             $('#<%= hfSelectedIDs.ClientID %>').val('');
    }

        document.addEventListener('DOMContentLoaded', function () {
            updateLocationPillsDisplay();

            const listBox = document.getElementById('<%= lstStoreFilter.ClientID %>');
            if (listBox) {
                listBox.addEventListener('change', updateLocationPillsDisplay);
            }
        });

    function setupFilterToggle() {
        const filterMappings = {
        '<%= filterAction.ClientID %>': '<%= actionFilterGroup.ClientID %>',
        '<%= filterStatus.ClientID %>': '<%= statusFilterGroup.ClientID %>',
        '<%= filterStore.ClientID %>': '<%= storeFilterGroup.ClientID %>',
        '<%= filterItem.ClientID %>': '<%= itemFilterGroup.ClientID %>',
        '<%= filterExpiryDate.ClientID %>': '<%= expiryDateFilterGroup.ClientID %>',
        '<%= filterStaff.ClientID %>': '<%= staffFilterGroup.ClientID %>',
        '<%= filterBatch.ClientID %>': '<%= batchFilterGroup.ClientID %>',
        '<%= filterVendor.ClientID %>': '<%= vendorFilterGroup.ClientID %>',
            '<%= filterRegistrationDate.ClientID %>': '<%= regeDateFilterGroup.ClientID %>'
        };

        Object.entries(filterMappings).forEach(([checkboxId, filterGroupId]) => {
            const checkbox = document.getElementById(checkboxId);
            const filterGroup = document.getElementById(filterGroupId);

            if (checkbox && filterGroup) {
                filterGroup.style.display = checkbox.checked ? "block" : "none";

                checkbox.addEventListener("change", function () {
                    filterGroup.style.display = this.checked ? "block" : "none";
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

            document.getElementById("<%= btnExport.ClientID %>").style.display = "block";
            document.getElementById("<%= excel.ClientID %>").style.display = "none";
        } else {
            filterPane.style.display = "none";
            gridCol.classList.remove("col-md-10");
            gridCol.classList.add("col-md-12");

            document.getElementById("<%= excel.ClientID %>").style.display = "block";
            document.getElementById("<%= btnExport.ClientID %>").style.display = "none";
        }
    }

     function exportToExcel() {
         window.location.href = "itemList.aspx?action=export";
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
        expiryDate: { 
            checkboxId: '<%= filterExpiryDate.ClientID %>', 
            controlId: '<%= txtExpiryDateFilter.ClientID %>',
            groupId: '<%= expiryDateFilterGroup.ClientID %>'
        },
        staff: { 
            checkboxId: '<%= filterStaff.ClientID %>', 
            controlId: '<%= txtstaffFilter.ClientID %>',
            groupId: '<%= staffFilterGroup.ClientID %>'
        },
        batch: { 
            checkboxId: '<%= filterBatch.ClientID %>', 
            controlId: '<%= txtBatchNoFilter.ClientID %>',
            groupId: '<%= batchFilterGroup.ClientID %>'
        },
        registrationDate: { 
            checkboxId: '<%= filterRegistrationDate.ClientID %>', 
            controlId: '<%= txtRegDateFilter.ClientID %>',
           groupId: '<%= regeDateFilterGroup.ClientID %>'
       }
   };

    function updateFilterVisibility() {
        Object.keys(filterMap).forEach(key => {
            const mapping = filterMap[key];
            const checkbox = document.getElementById(mapping.checkboxId);
            const filterGroup = document.getElementById(mapping.groupId);

            if (checkbox && filterGroup) {
                filterGroup.style.display = checkbox.checked ? "block" : "none";
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
        console.log('Selected IDs:', selectedIDs);
    }

    function InitializeStoreFilter() {
        var $select = $('#<%= lstStoreFilter.ClientID %>');
        var allOptionId = "all";

        // Remove duplicate "All" option if exists
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

            // Add search configuration
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
            $('#<%= item.ClientID %>, #<%= vendor.ClientID %>').select2({
                placeholder: 'Select item or vendor',
                allowClear: true,
                minimumResultsForSearch: 1
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
        const listBox = document.getElementById('<%= lstStoreFilter.ClientID %>');
        const container = document.getElementById('locationPillsContainer');

        if (!listBox || !container) return;

        container.innerHTML = '';

        const hasAll = Array.from(listBox.selectedOptions).some(opt => opt.value === "all");
    
        Array.from(listBox.selectedOptions).forEach(option => {
            if (option.value === "all") return;
        
            const pill = document.createElement('span');
            pill.className = 'location-pill';
            pill.innerHTML = `
                <span class="pill-text">${option.text}</span>
                <span class="pill-remove" data-value="${option.value}">×</span>
            `;

            pill.querySelector('.pill-remove').addEventListener('click', function (e) {
                e.preventDefault();
                option.selected = false;
                updateLocationPillsDisplay();
                $('#<%= lstStoreFilter.ClientID %>').trigger('change');
            });

            container.appendChild(pill);
        });

       container.style.display = listBox.selectedOptions.length > 0 ? 'flex' : 'none';
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
        let anyFilterActive = false;
        for (const [key, mapping] of Object.entries(filterMap)) {
            const checkbox = document.getElementById(mapping.checkboxId);
            const control = document.getElementById(mapping.controlId);

            if (!checkbox || !control) continue;

            if (checkbox.checked) {
                let hasValue = false;
                if (control.tagName === 'SELECT') {
                    hasValue = control.multiple ?
                        control.selectedOptions.length > 0 :
                        control.selectedIndex > 0 && control.value !== "";
                }
                else if (control.tagName === 'INPUT') {
                    hasValue = control.value.trim() !== "";
                }

                if (hasValue) {
                    anyFilterActive = true;
                    break;
                }
            }
        }

        if (!anyFilterActive) {
            Swal.fire('Warning!', 'Please select at least one filter to apply and ensure it has a value.', 'warning');
            return false;
        }

        return true;
    }

    Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {

        initializeComponents();
        setupFilterToggle();
        setupEventDelegation();
        InitializeStoreFilter();
        InitializeItemVendorFilter();
        setupFilterToggle();


    });

    history.pushState(null, null, location.href);
    window.addEventListener("popstate", function (event) {
        location.reload();
    });

</script>
</asp:Content>  
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

   <a href="AdminDashboard.aspx" class="btn text-white ms-2" style="background-color : #158396;"><i class="fa-solid fa-left-long"></i> Home</a>

    <div class="container-fluid col-lg-12 col-md-12">
        <div class="card shadow-md border-dark-subtle">
            <div class="card-header" style="background-color:#1995ad;">
                <h4 class="text-center text-white">Expiry List</h4>
            </div>

            <div class="card-body">
                <div class="col-lg-12 col-md-12">
                    <div class="row g-2 align-items-center">
                        <!-- Filter Button -->
                        <div class="col-6 col-md-auto">
                            <asp:Button ID="btnFilter" runat="server" 
                                CssClass="btn text-white w-100 py-2" 
                                style="background:#1995ad;" 
                                Text="Show Filter" 
                                OnClientClick="toggleFilter(); return false;" />
                        </div>

                        <!-- Edit Button -->
                        <div class="col-6 col-md-auto">
                            <asp:Button Text="Edit" runat="server"
                                CssClass="btn btn-secondary text-white w-100 py-2"
                                ID="btnEdit"
                                OnClick="btnEdit_Click" />
                        </div>

                        <!-- Action Dropdown -->
                        <div class="col-12 col-md-auto">
                            <asp:DropDownList ID="ddlAction" runat="server" 
                                CssClass="form-select w-100 py-2">
                               <asp:ListItem Text="-- Select Action --" Value="0" />
                               <asp:ListItem Text="Informed To Supplier" Value="1" />
                               <asp:ListItem Text="Informed To Owner" Value="2" />
                               <asp:ListItem Text="Supplier Sales" Value="3" />
                               <asp:ListItem Text="Owner Sales" Value="4" />
                               <asp:ListItem Text="Store's Responsibility" Value="5" />
                               <asp:ListItem Text="Store Exchange" Value="6" />
                               <asp:ListItem Text="Store Return" Value="7" />
                               <asp:ListItem Text="No Date To Check" Value="8" />
                           </asp:DropDownList>
                     </div>

                        <div class="col-12 col-md-auto">
                            <asp:Button ID="btnUpdateSelected" runat="server"
                                CssClass="btn text-white w-100 py-2"
                                style="background:#1995ad;"
                                Text="Update Action"
                                OnClick="btnUpdateSelected_Click" />
                        </div>

                          <div class="col-12 col-md-auto">
                            <asp:DropDownList ID="ddlStatus" runat="server"
                                CssClass="form-select w-100 py-2">
                               <asp:ListItem Text="-- Select Status --" Value="0" />
                               <asp:ListItem Value="1" Text="Progess" />
                               <asp:ListItem Value="2" Text="Exchange" />
                               <asp:ListItem Value="3" Text="No Exchange" />
                               <asp:ListItem Value="4" Text="No Action" />
                           </asp:DropDownList>
                          </div>

                           <div class="col-12 col-md-auto">
                                <asp:Button ID="btnStatusSelected" runat="server"
                                    CssClass="btn text-white w-100 py-2"
                                    style="background:#1995ad;"
                                    Text="Update Status"
                                    OnClick="btnStatusSelected_Click" />
                            </div>

                        <div class="col-12 col-md-auto">
                            <asp:LinkButton ID="excel" runat="server"
                                CssClass="btn text-white w-100 py-2"
                                style="background:#1995ad;"
                                OnClientClick="exportToExcel(); return false;">
                                Export To Excel <i class="ms-1 fa fa-file-excel"></i>
                            </asp:LinkButton>
                            <asp:Button ID="btnExport" runat="server" CssClass="btn text-white me-2" Text="Export to Excel" ForeColor="White" Font-Bold="True" Font-Size="Medium" style="background:#1995ad; display:none;" OnClick="btnExport_Click" />
                        </div>
                  </div>
                </div>

                <div class="d-flex p-2 col-lg-12 col-md-12 overflow-x-auto overflow-y-auto">
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
                                                    <button class="btn dropdown-toggle text-white" style="background-color:#1995ad;"
                                                        type="button" id="filterDropdownButton" data-bs-toggle="dropdown"
                                                        aria-haspopup="true" aria-expanded="false" runat="server">
                                                        Select Filters
                                                    </button>
                                                    <div class="dropdown-menu p-3" aria-labelledby="filterDropdownButton" style="max-height: 250px; overflow-y: auto;">
                                                        <!-- Checkboxes for each filter -->
                                                        <div class="form-check">
                                                            <asp:CheckBox ID="filterAction" runat="server" CssClass="form-check-input" />
                                                            <label class="form-check-label" for="<%= filterAction.ClientID %>">Action</label>
                                                        </div>
                                                        <div class="form-check">
                                                            <asp:CheckBox ID="filterStatus" runat="server" CssClass="form-check-input" />
                                                            <label class="form-check-label" for="<%= filterStatus.ClientID %>">Status</label>
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
                                                            <asp:CheckBox ID="filterExpiryDate" runat="server" CssClass="form-check-input" />
                                                            <label class="form-check-label" for="<%= filterExpiryDate.ClientID %>">Expiry Date</label>
                                                        </div>
                                                        <div class="form-check">
                                                            <asp:CheckBox ID="filterStaff" runat="server" CssClass="form-check-input" />
                                                            <label class="form-check-label" for="<%= filterStaff.ClientID %>">Staff Name</label>
                                                        </div>
                                                        <div class="form-check">
                                                            <asp:CheckBox ID="filterBatch" runat="server" CssClass="form-check-input" />
                                                            <label class="form-check-label" for="<%= filterBatch.ClientID %>">Batch No</label>
                                                        </div>
                                                        <div class="form-check">
                                                            <asp:CheckBox ID="filterVendor" runat="server" CssClass="form-check-input" />
                                                            <label class="form-check-label" for="<%= filterVendor.ClientID %>">Vendor</label>
                                                        </div>
                                                        <div class="form-check">
                                                            <asp:CheckBox ID="filterRegistrationDate" runat="server" CssClass="form-check-input" />
                                                            <label class="form-check-label" for="<%= filterRegistrationDate.ClientID %>">Registration Date</label>
                                                        </div>
                                                    </div>
                                                </div>
                                            </div>

                                            <!-- Dynamic Filters Container -->
                                            <div id="dynamicFilters">
                                                <!-- Action Filter -->
                                                <div class="form-group mt-3 filter-group" id="actionFilterGroup" runat="server" style="display:none">
                                                    <label for="<%= ddlActionFilter.ClientID %>">Action</label>
                                                    <asp:DropDownList ID="ddlActionFilter" runat="server" CssClass="form-control">
                                                        <asp:ListItem Text="-- Select Action --" Value="" />
                                                        <asp:ListItem Text="Informed To Supplier" Value="1" />
                                                        <asp:ListItem Text="Informed To Owner" Value="2" />
                                                        <asp:ListItem Text="Supplier Sales" Value="3" />
                                                        <asp:ListItem Text="Owner Sales" Value="4" />
                                                        <asp:ListItem Text="Store's Responsibility" Value="5" />
                                                        <asp:ListItem Text="Store Exchange" Value="6" />
                                                        <asp:ListItem Text="Store Return" Value="7" />
                                                        <asp:ListItem Text="No Date To Check" Value="8" />
                                                    </asp:DropDownList>
                                                </div>

                                                <!-- Status Filter -->
                                                <div class="form-group mt-3 filter-group" id="statusFilterGroup" runat="server" style="display:none">
                                                    <label for="<%= ddlStatusFilter.ClientID %>">Status</label>
                                                    <asp:DropDownList ID="ddlStatusFilter" runat="server" CssClass="form-control">
                                                        <asp:ListItem Text="-- Select Status --" Value="" />
                                                        <asp:ListItem Value="1" Text="Progress"></asp:ListItem>
                                                        <asp:ListItem Value="2" Text="Exchange"></asp:ListItem>
                                                        <asp:ListItem Value="3" Text="No Exchange"></asp:ListItem>
                                                        <asp:ListItem Value="4" Text="No Action"></asp:ListItem>
                                                    </asp:DropDownList>
                                                </div>

                                                <!-- Store Filter -->
                                                <div class="form-group mt-3 filter-group" id="storeFilterGroup" runat="server" style="display:none">
                                                    <label for="<%=lstStoreFilter.ClientID %>">Location</label>
                                                    <%--<asp:ListBox ID="lstStoreFilter" runat="server" CssClass="form-control select2-multi-check" SelectionMode="Multiple"></asp:ListBox>--%>
                                               
                                                    <asp:ListBox ID="lstStoreFilter" runat="server" CssClass="form-control select2-multi-check" 
                                                        SelectionMode="Multiple" style="display:none"></asp:ListBox>
   
                                                    <div id="locationPillsContainer" class="location-pills-container mb-2"></div>
    
                                                    <div class="location-pill-template" style="display:block"></div>
                                                </div>

                                                <!-- Item No Filter -->
                                                <div class="form-group mt-3 filter-group" id="itemFilterGroup" runat="server" style="display:none">
                                                    <label for="<%= item.ClientID %>" style="display:block">Item No</label>
                                                    <asp:DropDownList ID="item" runat="server" CssClass="form-control select2-init" style="width:333px">
                                                        <asp:ListItem Text="" Value="" />
                                                    </asp:DropDownList>
                                                </div>

                                                <!-- Expiry Date Filter -->
                                                 <div class="form-group mt-3 filter-group" id="expiryDateFilterGroup" runat="server" style="display:none">
                                                     <label for="<%= txtExpiryDateFilter.ClientID %>">Expiry Month</label>
                                                     <asp:TextBox ID="txtExpiryDateFilter" runat="server" CssClass="form-control" ></asp:TextBox>
                                                 </div>

                                                <!-- Staff Filter -->
                                                <div class="form-group mt-3 filter-group" id="staffFilterGroup" runat="server" style="display:none">
                                                    <label for="<%= txtstaffFilter.ClientID %>">Staff Name</label>
                                                    <asp:TextBox ID="txtstaffFilter" runat="server" CssClass="form-control" Placeholder="Enter staff name"></asp:TextBox>
                                                </div>

                                                <!-- Batch No Filter -->
                                                <div class="form-group mt-3 filter-group" id="batchFilterGroup" runat="server" style="display:none">
                                                    <label for="<%= txtBatchNoFilter.ClientID %>">Batch No</label>
                                                    <asp:TextBox ID="txtBatchNoFilter" runat="server" CssClass="form-control" Placeholder="Enter batch number"></asp:TextBox>
                                                </div>

                                                <!-- Vendor Filter -->
                                                <div class="form-group mt-3 filter-group" id="vendorFilterGroup" runat="server" style="display:none">
                                                    <label for="<%= vendor.ClientID %>" style="display:block">Vendor</label>
                                                    <asp:DropDownList ID="vendor" runat="server" CssClass="form-control select2-init" style="width:333px">
                                                        <asp:ListItem Text="" Value="" />
                                                    </asp:DropDownList>
                                                </div>

                                                <!-- Registration Date Filter -->
                                                <div class="form-group mt-3 filter-group" id="regeDateFilterGroup" runat="server" style="display:none">
                                                    <label for="<%= txtRegDateFilter.ClientID %>">Registration Date</label>
                                                    <asp:TextBox ID="txtRegDateFilter" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                                                </div>
                                            </div>

                                            <!-- Filter Buttons -->
                                            <div class="form-group mt-3">
                                                <asp:Button ID="btnApplyFilter" runat="server" 
                                                    CssClass="btn text-white mb-1" 
                                                    style="background-color:#1995ad;" 
                                                    Text="Apply Filters" 
                                                    OnClientClick="return handleApplyFilters();" OnClick="ApplyFilters_Click" 
                                                    CausesValidation="false" />
                        
                                                <asp:Button ID="btnResetFilter" runat="server" 
                                                    CssClass="btn text-white" 
                                                    style="background-color:#1995ad;" 
                                                    Text="Reset Filters" 
                                                    OnClick="ResetFilters_Click" 
                                                    CausesValidation="false"
                                                    UseSubmitBehavior="true" />
                                            </div>
                                        </ContentTemplate>
                                        <Triggers>
                                            <asp:AsyncPostBackTrigger ControlID="btnApplyFilter" EventName="Click" />
                                            <asp:AsyncPostBackTrigger ControlID="btnResetFilter" EventName="Click" />
                                        </Triggers>
                                    </asp:UpdatePanel>
                                </div>
                            </asp:Panel>
                        </div>
                    </div>

                    <asp:HiddenField ID="hfSelectedRows" runat="server" />
                    <asp:HiddenField ID="hfSelectedIDs" runat="server" />
                    <asp:HiddenField ID="hflength" runat="server" />   
                    <asp:HiddenField ID="hfEditId" runat="server" />
                    <asp:HiddenField ID="hfEditedRowId" runat="server" />

                    <!-- Table -->
                    <div class="col-md-12 ms-3" id="gridCol">
                        <asp:UpdatePanel ID="UpdatePanel2" runat="server" UpdateMode="Conditional">
                        <ContentTemplate>

                           <asp:Panel ID="pnlNoData" runat="server" Visible="false">
                                 <div class="alert alert-info">No items to Filter</div>
                           </asp:Panel>

                            <div class="table-responsive gridview-container " style="height: 673px">
                                <asp:GridView ID="GridView2" runat="server"
                                    CssClass="table table-striped table-bordered table-hover border border-2 shadow-lg sticky-grid mt-1 overflow-x-auto overflow-y-auto"
                                    AutoGenerateColumns="False"
                                    DataKeyNames="id"
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
                                    AutoGenerateEditButton="false" ShowHeaderWhenEmpty="true"  >

                                    <EditRowStyle BackColor="#999999" />
                                    <FooterStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />

                                    <HeaderStyle Wrap="true" BackColor="#808080" Font-Bold="True" ForeColor="White"></HeaderStyle>
                                    <PagerStyle CssClass="pagination-wrapper" HorizontalAlign="Center" VerticalAlign="Middle" />
                                    <RowStyle CssClass="table-row data-row" BackColor="#F7F6F3" ForeColor="#333333"></RowStyle>
                                    <AlternatingRowStyle CssClass="table-alternating-row" BackColor="White" ForeColor="#284775"></AlternatingRowStyle>

                                    <EmptyDataTemplate>
                                        <div class="alert alert-info">No items to Filter</div>
                                    </EmptyDataTemplate>

                                    <Columns>

                                        <asp:TemplateField HeaderText="ID" Visible="false" >
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
                                           <ControlStyle Width="50px" />
                                            <HeaderStyle ForeColor="White" BackColor="Gray" />
                                            <ItemStyle HorizontalAlign="Justify" />
                                        </asp:TemplateField>

                                        <asp:TemplateField ItemStyle-HorizontalAlign="Justify" HeaderText="No" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header2" ItemStyle-CssClass="fixed-column-2">
                                            <ItemTemplate>
                                                <asp:Label ID="lblLinesNo" runat="server" Text='<%# Container.DataItemIndex + 1 %>' />
                                            </ItemTemplate>
                                            <ControlStyle Width="50px" />
                                            <HeaderStyle ForeColor="White" BackColor="Gray" />
                                            <ItemStyle HorizontalAlign="Justify" />
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Lines No" ItemStyle-Width="100px" SortExpression="no" HeaderStyle-ForeColor="White" ItemStyle-HorizontalAlign="Justify" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header3" ItemStyle-CssClass="fixed-column-3">
                                            <ItemTemplate>
                                                <asp:Label ID="lblNo" runat="server" Text='<%# Eval("no") %>' />
                                            </ItemTemplate><HeaderStyle ForeColor="White" BackColor="Gray" />
                                            <ControlStyle Width="100px" />
                                            <ItemStyle HorizontalAlign="Justify" />
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Item No" SortExpression="itemNo" HeaderStyle-ForeColor="White" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header4" ItemStyle-CssClass="fixed-column-4">
                                            <ItemTemplate>
                                                <asp:Label ID="lblItemNo" runat="server" Text='<%# Eval("itemNo") %>' />
                                            </ItemTemplate>
                                            <ControlStyle Width="117px" />
                                            <HeaderStyle ForeColor="White" BackColor="Gray" />
                                            <ItemStyle HorizontalAlign="Justify" />
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Description" SortExpression="description" HeaderStyle-ForeColor="Black" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header5" ItemStyle-CssClass="fixed-column-5">
                                            <ItemTemplate>
                                                <asp:Label ID="lblDesc" runat="server" Text='<%# Eval("description") %>' />
                                            </ItemTemplate>
                                            <ControlStyle Width="257px" />
                                            <HeaderStyle ForeColor="White" BackColor="Gray" />
                                            <ItemStyle HorizontalAlign="Justify" />
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Barcode No" SortExpression="barcodeNo" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0" ItemStyle-CssClass="fixed-column-6">
                                            <ItemTemplate>
                                                <asp:Label ID="lblBarcode" runat="server" Text='<%# Eval("barcodeNo") %>' />
                                            </ItemTemplate>
                                            <ControlStyle Width="127px" />
                                            <HeaderStyle ForeColor="White" BackColor="Gray" />
                                            <ItemStyle HorizontalAlign="Justify" />
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Qty" SortExpression="qty" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0" ItemStyle-CssClass="fixed-column-7">
                                            <ItemTemplate>
                                                <asp:Label ID="lblQty" runat="server" Text='<%# Eval("qty") %>' />
                                            </ItemTemplate>
                                            <ControlStyle Width="97px" />
                                            <HeaderStyle ForeColor="White" BackColor="Gray" />
                                            <ItemStyle HorizontalAlign="Justify" />
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="UOM" SortExpression="uom" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0" ItemStyle-CssClass="fixed-column-8">
                                            <ItemTemplate>
                                                <asp:Label ID="lblUom" runat="server" Text='<%# Eval("uom") %>' />
                                            </ItemTemplate>
                                            <ControlStyle Width="97px" />
                                            <HeaderStyle ForeColor="White" BackColor="Gray" />
                                            <ItemStyle HorizontalAlign="Justify" />
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Packing Info" SortExpression="packingInfo" HeaderStyle-ForeColor="Black" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0" ItemStyle-CssClass="fixed-column-9">
                                            <ItemTemplate>
                                                <asp:Label ID="lblPacking" runat="server" Text='<%# Eval("packingInfo") %>' />
                                            </ItemTemplate>
                                            <ControlStyle Width="110px" />
                                            <HeaderStyle ForeColor="White" BackColor="Gray" />
                                            <ItemStyle HorizontalAlign="Justify" />
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Expiry Date" SortExpression="expiryDate" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0" ItemStyle-CssClass="fixed-column-10">
                                            <ItemTemplate>
                                                <asp:Label ID="lblExpiryDate" runat="server" Text='<%# Eval("expiryDate", "{0:MMM/yyyy}") %>' />
                                            </ItemTemplate>
                                            <ControlStyle Width="120px" />
                                            <HeaderStyle ForeColor="White" BackColor="Gray" />
                                            <ItemStyle HorizontalAlign="Justify" />
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Location" SortExpression="storeNo" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0" ItemStyle-CssClass="fixed-column-51">
                                            <ItemTemplate>
                                                <asp:Label ID="lblStoreNo" runat="server" Text='<%# Eval("storeNo") %>' />
                                            </ItemTemplate>
                                            <ControlStyle Width="120px" />
                                            <HeaderStyle ForeColor="White" BackColor="Gray" />
                                            <ItemStyle HorizontalAlign="Justify" />
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Staff" SortExpression="staffName" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0" ItemStyle-CssClass="fixed-column-52">
                                            <ItemTemplate>
                                                <asp:Label ID="lblStaff" runat="server" Text='<%# Eval("staffName") %>' />
                                            </ItemTemplate>
                                            <ControlStyle Width="120px" />
                                            <HeaderStyle ForeColor="White" BackColor="Gray" />
                                            <ItemStyle HorizontalAlign="Justify" />
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Batch No" SortExpression="batchNo" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0" ItemStyle-CssClass="fixed-column-53">
                                            <ItemTemplate>
                                                <asp:Label ID="lblBatchNo" runat="server" Text='<%# Eval("batchNo") %>' />
                                            </ItemTemplate>
                                            <ControlStyle Width="120px" />
                                            <HeaderStyle ForeColor="White" BackColor="Gray" />
                                            <ItemStyle HorizontalAlign="Justify" />
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Vendor No" SortExpression="vendorNo" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0" ItemStyle-CssClass="fixed-column-54">
                                            <ItemTemplate>
                                                <asp:Label ID="lblVendorNo" runat="server" Text='<%# Eval("vendorNo") %>' />
                                            </ItemTemplate>
                                            <ControlStyle Width="120px" />
                                            <HeaderStyle ForeColor="White" BackColor="Gray" />
                                            <ItemStyle HorizontalAlign="Justify" />
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Vendor Name" ItemStyle-Width="170px" SortExpression="vendorName" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0" ItemStyle-CssClass="fixed-column-55">
                                            <ItemTemplate>
                                                <asp:Label ID="lblVendorName" runat="server" Text=' <%# Eval("vendorName") %>' />
                                            </ItemTemplate>
                                            <ControlStyle Width="170px" />
                                            <HeaderStyle ForeColor="White" BackColor="Gray" />
                                            <ItemStyle HorizontalAlign="Justify" />
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Registration Date" SortExpression="regeDate" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0" ItemStyle-CssClass="fixed-column-56">
                                            <ItemTemplate>
                                                <asp:Label ID="lblRege" runat="server" Text='<%# Eval("regeDate", "{0:dd-MM-yyyy}") %>' />
                                            </ItemTemplate>
                                            <ControlStyle Width="120px" />
                                            <HeaderStyle ForeColor="White" BackColor="Gray" />
                                            <ItemStyle HorizontalAlign="Justify" />
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Action" SortExpression="action" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0" ItemStyle-CssClass="fixed-column-57">
                                            <EditItemTemplate>
                                                <asp:DropDownList ID="ddlActionEdit" runat="server">
                                                    <asp:ListItem Text="-- Select Action --" Value="0" />
                                                    <asp:ListItem Text="Informed To Supplier" Value="1" />
                                                    <asp:ListItem Text="Informed To Owner" Value="2" />
                                                    <asp:ListItem Text="Supplier Sales" Value="3" />
                                                    <asp:ListItem Text="Owner Sales" Value="4" />
                                                    <asp:ListItem Text="Store's Responsibility" Value="5" />
                                                    <asp:ListItem Text="Store Exchange" Value="6" />
                                                    <asp:ListItem Text="Store Return" Value="7" />
                                                    <asp:ListItem Text="No Date To Check" Value="8" />
                                                </asp:DropDownList>
                                            </EditItemTemplate>
                                            <ItemTemplate>
                                                <asp:Label ID="lblAction" runat="server" Text='<%# Eval("action") %>'></asp:Label>
                                            </ItemTemplate>
                                            <ControlStyle Width="120px" />
                                            <HeaderStyle ForeColor="White" BackColor="Gray" />
                                            <ItemStyle HorizontalAlign="Justify" />
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Status" SortExpression="status" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0" ItemStyle-CssClass="fixed-column-58">
                                            <EditItemTemplate>
                                                <asp:DropDownList ID="ddlStatusEdit" runat="server">
                                                    <asp:ListItem Text="-- Select Status --" Value="0" />
                                                    <asp:ListItem Value="1" Text="Progress"></asp:ListItem>
                                                    <asp:ListItem Value="2" Text="Exchange"></asp:ListItem>
                                                    <asp:ListItem Value="3" Text="No Exchange"></asp:ListItem>
                                                    <asp:ListItem Value="4" Text="No Action"></asp:ListItem>
                                                </asp:DropDownList>
                                            </EditItemTemplate>
                                            <ItemTemplate>
                                                <asp:Label ID="lblStatus" runat="server" Text='<%# Eval("status") %>'></asp:Label>
                                            </ItemTemplate>
                                            <ControlStyle Width="120px" />
                                            <HeaderStyle ForeColor="White" BackColor="Gray" />
                                            <ItemStyle HorizontalAlign="Justify" />
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Note" SortExpression="note" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0" ItemStyle-CssClass="fixed-column-59">
                                            <ItemTemplate>
                                                <asp:Label ID="lblNote" runat="server" Text=' <%# Eval("note") %>'></asp:Label>
                                            </ItemTemplate>
                                            <ControlStyle Width="125px" />
                                            <HeaderStyle ForeColor="White" BackColor="Gray" />
                                            <ItemStyle HorizontalAlign="Justify" />
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Remark" SortExpression="remark" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0" ItemStyle-CssClass="fixed-column-60">
                                            <ItemTemplate>
                                                <asp:Label ID="lblRemark" runat="server" Text='<%# Eval("Remark") %>'></asp:Label>
                                            </ItemTemplate>
                                            <EditItemTemplate>
                                                <asp:TextBox ID="txtRemark" runat="server" Text='<%# Bind("Remark") %>' Width="157px"></asp:TextBox>
                                            </EditItemTemplate>
                                            <ControlStyle Width="125px" />
                                            <HeaderStyle ForeColor="White" BackColor="Gray" />
                                            <ItemStyle HorizontalAlign="Justify" />
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Completed Date" SortExpression="completedDate" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0" ItemStyle-CssClass="fixed-column-61">
                                            <ItemTemplate>
                                                <asp:Label ID="lblCompleted" runat="server" Text=' <%# Eval("completedDate", "{0:dd-MM-yyyy}") %>' />
                                            </ItemTemplate>
                                            <EditItemTemplate>
                                                <asp:TextBox ID="txtCompleted" runat="server" type="date" Text='<%# Bind("completedDate", "{0:yyyy-MM-dd}") %>' Width="157px"></asp:TextBox>
                                            </EditItemTemplate>
                                            <ControlStyle Width="125px" />
                                            <HeaderStyle ForeColor="White" BackColor="Gray" />
                                            <ItemStyle HorizontalAlign="Justify" />
                                        </asp:TemplateField>

                                        <asp:CommandField ShowEditButton="true" ShowCancelButton="true" ControlStyle-CssClass="btn btn-outline-primary m-1 text-white"
                                            EditText="-" UpdateText="<i class='fa-solid fa-file-arrow-up'></i> Update"
                                            CancelText="<i class='fa-solid fa-xmark'></i> Cancel">
                                            <ControlStyle CssClass="btn btn-outline-primary m-1 text-white" Width="105px" BackColor="#158396" />
                                            <HeaderStyle ForeColor="White" BackColor="Gray" />
                                            <ItemStyle HorizontalAlign="Justify" />
                                        </asp:CommandField>

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
                                 <asp:PostBackTrigger ControlID="excel" />
                                <asp:AsyncPostBackTrigger ControlID="btnApplyFilter" EventName="Click" />
                                <asp:AsyncPostBackTrigger ControlID="btnResetFilter" EventName="Click" />
                            </Triggers>
                        </asp:UpdatePanel>
                    </div>
                </div>

            </div>
        </div>
    
    </div>
</asp:Content>