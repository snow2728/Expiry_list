<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" EnableEventValidation="false" CodeBehind="final2.aspx.cs" Inherits="Expiry_list.final2" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <script src="js/customJS.js"></script>
    <script type="text/javascript">

        $(document).ready(function () {
            // Initialize components only after ScriptManager is ready
            if (typeof (Sys) !== 'undefined') {
                Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
                    updateFilterVisibility();

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
            InitializeItemVendorFilter();
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
                    Searchable: true,
                    ajax: {
                        url: 'final2.aspx',
                        type: 'POST',
                        data: function (d) {
                                 <%-- console.log(d);
                                  $('#<%= hflength.ClientID %>').val(d.length);--%>
                            return {
                                draw: d.draw,
                                start: d.start,
                                length: d.length,
                                order: d.order,
                                search: d.search.value, // Send search term to server
                                month: $('#monthFilter').val(),

                                action: $('#<%= ddlActionFilter.ClientID %>').val(),
                                status: $('#<%= ddlStatusFilter.ClientID %>').val(),
                                item: $('#<%= item.ClientID %>').val(),
                                expiryDate: $('#<%= txtExpiryDateFilter.ClientID %>').val(),
                                staff: $('#<%= txtstaffFilter.ClientID %>').val(),
                                batch: $('#<%= txtBatchNoFilter.ClientID %>').val(),
                                vendor: $('#<%= vendor.ClientID %>').val(),
                                regDate: $('#<%= txtRegDateFilter.ClientID %>').val(),

                                //  month: $('#monthFilter').val()
                            };
                        }
                    },
                    fixedColumns: {
                        leftColumns: 4,
                        rightColumns: 0,
                        heightMatch: 'none'
                    },
                    columns: [
                        {
                            data: null,
                            width: "100px",
                            orderable: false,
                            render: function (data, type, row, meta) {
                                return meta.settings._iDisplayStart + meta.row + 1;
                            }
                        },
                        { data: 'no', width: "100px" },
                        { data: 'itemNo', width: "120px" },
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
                    select: { style: 'multi', selector: 'td:first-child' },
                    lengthMenu: [[100, 500, 1000], [100, 500, 1000]],
                    initComplete: function (settings) {
                        var api = this.api();
                        setTimeout(function () {
                            api.columns.adjust();
                        }, 50);
                    }
                });

                $('.select2-init').select2({
                    placeholder: "Search or Select",
                    allowClear: true,
                    minimumResultsForSearch: 5
                });
            }
        }

        document.addEventListener("DOMContentLoaded", function () {

            const monthInput = document.getElementById("monthFilter");
            const now = new Date();
            const year = now.getFullYear();
            const month = String(now.getMonth() + 1).padStart(2, '0');
            monthInput.value = `${year}-${month}`;

            const filterPane = document.getElementById("filterPane");
            if (filterPane) {
                filterPane.style.display = "<%= ViewState["FilterPanelVisible"] != null ? (bool)ViewState["FilterPanelVisible"] ? "block" : "none" : "none" %>";
            }

        });

        function setupFilterToggle() {
            const filterMappings = {
             '<%= filterAction.ClientID %>': '<%= actionFilterGroup.ClientID %>',
             '<%= filterStatus.ClientID %>': '<%= statusFilterGroup.ClientID %>',

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
            const filterPane = document.getElementById("filterPane"); b
            const gridCol = document.getElementById("gridCol");

            if (filterPane && gridCol) {
                const isVisible = filterPane.style.display === "block";
                filterPane.style.display = isVisible ? "none" : "block";

                // Adjust grid column width
                gridCol.classList.toggle("col-md-10", !isVisible);
                gridCol.classList.toggle("col-md-12", isVisible);
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
             item: {
                 checkboxId: '<%= filterItem.ClientID %>',
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

        function InitializeItemVendorFilter() {
            try {
                $('#<%= item.ClientID %>, #<%= vendor.ClientID %>').select2({
                    placeholder: 'Select item or vendor',
                    allowClear: true,
                    minimumResultsForSearch: 1
                });

                console.log("Select2 initialized for item and vendor filters.");
            } catch (err) {
                Swal.fire({
                    icon: 'error',
                    title: 'Initialization Failed',
                    text: 'There was an error initializing item/vendor dropdowns: ' + err.message
                });
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

            // Hide the monthFilter input
            const monthFilter = document.getElementById('monthFilter');
            if (monthFilter) {
                monthFilter.style.display = 'none';
            }

            return true;
        }

        Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
            const filterPane = document.getElementById("filterPane");
            if (filterPane) {
                filterPane.style.display = <%= Panel1.Visible.ToString().ToLower() %> ? "block" : "none";
            }

          initializeComponents();
          setupFilterToggle();
          InitializeItemVendorFilter();
      });

        function refreshDataTable() {
            const grid = $("#<%= GridView2.ClientID %>");
            if ($.fn.DataTable.isDataTable(grid)) {
                grid.DataTable().ajax.reload();
            }
        }

        history.pushState(null, null, location.href);
        window.addEventListener("popstate", function (event) {
            location.reload();
        });

    </script>
    </asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

     <a href="AdminDashboard.aspx" class="btn text-white ms-2" style="background-color : #158396;"><i class="fa-solid fa-left-long"></i> Home</a>

     <div class="container-fluid col-lg-12">
      <div class="card shadow-md border-dark-subtle">
          <div class="card-header" style="background-color:#1995ad;">
              <h4 class="text-center text-white">Expiry List</h4>
          </div>
            <div class="card-body">

                <div class="d-flex align-items-center flex-wrap mb-2">
                    
                    <div>
                        <asp:Button ID="btnFilter" class="btn me-1 text-white" style="background:#1995ad" runat="server" Text="Show Filter" CausesValidation="false" OnClientClick="toggleFilter(); return false;" OnClick="btnFilter_Click1" />
                    </div>

                         <div class="me-2 shadow-md">
                              <input type="month" id="monthFilter" class="form-control me-2" onchange="refreshDataTable()" />
                          </div>
                    
                </div>

                <div class="d-flex p-2 col-lg-12 col-md-12 overflow-x-auto overflow-y-auto">
                    <div class="row">
                        <!-- Filter Panel (Hidden by default) -->
                       <div class="col" id="filterPane" style="display: none;">
                            <asp:Panel ID="Panel1" runat="server" Visible="true" >
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

                                            <!-- Item No Filter -->
                                            <div class="form-group mt-3 filter-group" id="itemFilterGroup" runat="server" style="display:none">
                                                <label for="<%= item.ClientID %>" style="display:block">Item No</label>
                                                <asp:DropDownList ID="item" runat="server" CssClass="form-control select2-init" style="width:333px">
                                                    <asp:ListItem Text="" Value="" />
                                                </asp:DropDownList>
                                            </div>

                                            <!-- Expiry Month Filter -->
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

                    <!-- Table -->
                    <div class="col-md-12 ms-3" id="gridCol">
                       <asp:UpdatePanel ID="UpdatePanel2" runat="server" UpdateMode="Conditional">
                         <ContentTemplate>

                            <asp:Panel ID="pnlNoData" runat="server" Visible="false">
                                  <div class="alert alert-info">No items to Filter</div>
                            </asp:Panel>

                             <div class="table-responsive gridview-container " style="height: 673px">
                                 <asp:GridView ID="GridView2" runat="server"
                                     CssClass="table table-striped table-bordered table-hover shadow-lg sticky-grid mt-1 overflow-x-auto overflow-y-auto"
                                     AutoGenerateColumns="False"
                                     DataKeyNames="id"
                                     UseAccessibleHeader="true"
                                     OnRowCancelingEdit="GridView2_RowCancelingEdit"
                                     OnRowCreated="GridView1_RowCreated"
                                     OnRowEditing="GridView2_RowEditing"
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

                                            <asp:BoundField DataField="id" HeaderText="ID" Visible="false" />
                                             <asp:TemplateField ItemStyle-HorizontalAlign="Justify" HeaderText="No" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header11" ItemStyle-CssClass="fixed-column-11">
                                                 <ItemTemplate>
                                                     <asp:Label ID="lblLinesNo" runat="server" Text='<%# Container.DataItemIndex + 1 %>' />
                                                 </ItemTemplate>
                                                 <ControlStyle Width="100px" />
                                                 <HeaderStyle ForeColor="White" BackColor="Gray" />
                                                 <ItemStyle HorizontalAlign="Justify" />
                                             </asp:TemplateField>

                                          <asp:TemplateField HeaderText="Lines No" SortExpression="no" HeaderStyle-ForeColor="White" ItemStyle-HorizontalAlign="Justify" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header22" ItemStyle-CssClass="fixed-column-22" >
                                             <ItemTemplate>
                                                 <asp:Label ID="lblNo" runat="server" Text='<%# Eval("no") %>' />
                                             </ItemTemplate>
                                             <ControlStyle Width="100px" />
                                             <HeaderStyle ForeColor="White" BackColor="Gray" />
                                             <ItemStyle HorizontalAlign="Justify" />
                                         </asp:TemplateField>

                                         <asp:TemplateField HeaderText="Item No" SortExpression="itemNo" HeaderStyle-ForeColor="White" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header33" ItemStyle-CssClass="fixed-column-33">
                                             <ItemTemplate>
                                                 <asp:Label ID="lblItemNo" runat="server" Text='<%# Eval("itemNo") %>' />
                                             </ItemTemplate>
                                             <ControlStyle Width="120px" />
                                             <HeaderStyle ForeColor="White" BackColor="Gray" />
                                             <ItemStyle HorizontalAlign="Justify" />
                                         </asp:TemplateField>

                                         <asp:TemplateField HeaderText="Description" SortExpression="description" HeaderStyle-ForeColor="Black" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header44" ItemStyle-CssClass="fixed-column-44" >
                                             <ItemTemplate>
                                                 <asp:Label ID="lblDesc" runat="server" Text='<%# Eval("description") %>' />
                                             </ItemTemplate>
                                             <ControlStyle Width="297px" />
                                             <HeaderStyle ForeColor="White" BackColor="Gray" />
                                             <ItemStyle HorizontalAlign="Justify" />
                                         </asp:TemplateField>

                                         <asp:TemplateField HeaderText="Barcode No" SortExpression="barcodeNo" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0">
                                             <ItemTemplate>
                                                 <asp:Label ID="lblBarcode" runat="server" Text='<%# Eval("barcodeNo") %>' />
                                             </ItemTemplate>
                                            <ControlStyle Width="127px" />
                                             <HeaderStyle ForeColor="White" BackColor="Gray" />
                                             <ItemStyle HorizontalAlign="Justify" />
                                         </asp:TemplateField>

                                         <asp:TemplateField HeaderText="Qty" SortExpression="qty" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0">
                                             <ItemTemplate>
                                                 <asp:Label ID="lblQty" runat="server" Text='<%# Eval("qty") %>' />
                                             </ItemTemplate>
                                             <ControlStyle Width="97px" />
                                              <HeaderStyle ForeColor="White" BackColor="Gray" />
                                              <ItemStyle HorizontalAlign="Justify" />
                                         </asp:TemplateField>

                                         <asp:TemplateField HeaderText="UOM" SortExpression="uom" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0">
                                             <ItemTemplate>
                                                 <asp:Label ID="lblUom" runat="server" Text='<%# Eval("uom") %>' />
                                             </ItemTemplate>
                                             <ControlStyle Width="97px" />
                                              <HeaderStyle ForeColor="White" BackColor="Gray" />
                                              <ItemStyle HorizontalAlign="Justify" />
                                         </asp:TemplateField>

                                         <asp:TemplateField HeaderText="Packing Info" SortExpression="packingInfo" HeaderStyle-ForeColor="Black" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0">
                                             <ItemTemplate>
                                                 <asp:Label ID="lblPacking" runat="server" Text='<%# Eval("packingInfo") %>' />
                                             </ItemTemplate>
                                             <ControlStyle Width="110px" />
                                             <HeaderStyle ForeColor="White" BackColor="Gray" />
                                             <ItemStyle HorizontalAlign="Justify" />
                                         </asp:TemplateField>

                                         <asp:TemplateField HeaderText="Expiry Date" SortExpression="expiryDate" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0">
                                             <ItemTemplate>
                                                 <asp:Label ID="lblExpiryDate" runat="server" Text='<%# Eval("expiryDate", "{0:MMM/yyyy}") %>' />
                                             </ItemTemplate>
                                            <ControlStyle Width="120px" />
                                             <HeaderStyle ForeColor="White" BackColor="Gray" />
                                             <ItemStyle HorizontalAlign="Justify" />
                                         </asp:TemplateField>

                                         <asp:TemplateField HeaderText="Location" SortExpression="storeNo" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0">
                                             <ItemTemplate>
                                                 <asp:Label ID="lblStoreNo" runat="server" Text='<%# Eval("storeNo") %>' />
                                             </ItemTemplate>
                                             <ControlStyle Width="120px" />
                                              <HeaderStyle ForeColor="White" BackColor="Gray" />
                                              <ItemStyle HorizontalAlign="Justify" />
                                         </asp:TemplateField>

                                         <asp:TemplateField HeaderText="Staff" SortExpression="staffName" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0">
                                             <ItemTemplate>
                                                 <asp:Label ID="lblStaff" runat="server" Text='<%# Eval("staffName") %>' />
                                             </ItemTemplate>
                                             <ControlStyle Width="120px" />
                                              <HeaderStyle ForeColor="White" BackColor="Gray" />
                                              <ItemStyle HorizontalAlign="Justify" />
                                         </asp:TemplateField>

                                         <asp:TemplateField HeaderText="Batch No" SortExpression="batchNo" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0">
                                             <ItemTemplate>
                                                 <asp:Label ID="lblBatchNo" runat="server" Text='<%# Eval("batchNo") %>' />
                                             </ItemTemplate>
                                             <ControlStyle Width="120px" />
                                              <HeaderStyle ForeColor="White" BackColor="Gray" />
                                              <ItemStyle HorizontalAlign="Justify" />
                                         </asp:TemplateField>

                                         <asp:TemplateField HeaderText="Vendor No" SortExpression="vendorNo" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0">
                                             <ItemTemplate>
                                                 <asp:Label ID="lblVendorNo" runat="server" Text='<%# Eval("vendorNo") %>' />
                                             </ItemTemplate>
                                            <ControlStyle Width="120px" />
                                              <HeaderStyle ForeColor="White" BackColor="Gray" />
                                              <ItemStyle HorizontalAlign="Justify" />
                                         </asp:TemplateField>

                                         <asp:TemplateField HeaderText="Vendor Name" SortExpression="vendorName" ItemStyle-Width="170px" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0">
                                             <ItemTemplate>
                                                 <asp:Label ID="lblVendorName" runat="server" Text=' <%# Eval("vendorName") %>' />
                                             </ItemTemplate>
                                            <ControlStyle Width="170px" />
                                              <HeaderStyle ForeColor="White" BackColor="Gray" />
                                              <ItemStyle HorizontalAlign="Justify" />
                                         </asp:TemplateField>

                                         <asp:TemplateField HeaderText="Registration Date" SortExpression="regeDate" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0">
                                             <ItemTemplate>
                                                 <asp:Label ID="lblRege" runat="server" Text='<%# Eval("regeDate", "{0:dd-MM-yyyy}") %>' />
                                             </ItemTemplate>
                                             <ControlStyle Width="120px" />
                                              <HeaderStyle ForeColor="White" BackColor="Gray" />
                                              <ItemStyle HorizontalAlign="Justify" />
                                         </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Action" SortExpression="action" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblAction" runat="server" Text='<%# Eval("action") %>'></asp:Label>
                                                </ItemTemplate>
                                                <ControlStyle Width="120px" />
                                                 <HeaderStyle ForeColor="White" BackColor="Gray" />
                                                 <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>

                                              <asp:TemplateField HeaderText="Status" SortExpression="status" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblStatus" runat="server" Text='<%# Eval("status") %>'></asp:Label>
                                                </ItemTemplate>
                                               <ControlStyle Width="120px" />
                                                <HeaderStyle ForeColor="White" BackColor="Gray" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>
                                             
                                             <asp:TemplateField HeaderText="Note" SortExpression="note" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0">
                                                  <ItemTemplate>
                                                     <asp:Label ID="lblNote" runat="server" Text=' <%# Eval("note") %>'></asp:Label>
                                                 </ItemTemplate>
                                                  <ControlStyle Width="125px" />
                                                  <HeaderStyle ForeColor="White" BackColor="Gray" />
                                                  <ItemStyle HorizontalAlign="Justify" />
                                             </asp:TemplateField>

                                             <asp:TemplateField HeaderText="Remark" SortExpression="remark" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0">
                                                 <ItemTemplate>
                                                     <asp:Label ID="lblRemark" runat="server" Text='<%# Eval("Remark") %>'></asp:Label>
                                                 </ItemTemplate>
                                                  <ControlStyle Width="125px" />
                                                  <HeaderStyle ForeColor="White" BackColor="Gray" />
                                                  <ItemStyle HorizontalAlign="Justify" />
                                             </asp:TemplateField>

                                             <asp:TemplateField HeaderText="Completed Date" SortExpression="completedDate" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0">
                                                  <ItemTemplate>
                                                      <asp:Label ID="lblCompleted" runat="server" Text=' <%# Eval("completedDate", "{0:dd-MM-yyyy}") %>' />
                                                 </ItemTemplate>
                                                  <ControlStyle Width="125px" />
                                                  <HeaderStyle ForeColor="White" BackColor="Gray" />
                                                  <ItemStyle HorizontalAlign="Justify" />
                                             </asp:TemplateField>
                                        
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
                            </Triggers>
                        </asp:UpdatePanel>
                    </div>
                </div>

            </div>
        </div>
    </div>
</asp:Content>
