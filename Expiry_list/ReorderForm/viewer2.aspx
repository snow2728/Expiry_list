<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="viewer2.aspx.cs" Inherits="Expiry_list.ReorderForm.viewer2" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">

 <script src="../js/customJS.js"></script>
 <script type="text/javascript">

     <%-- Viewer2 Form For Store User (expiryList) --%>

     $(document).ready(function () {
         // Initialize components only after ScriptManager is ready
         if (typeof (Sys) !== 'undefined') {
             Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
                 updateFilterVisibility();

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

     document.addEventListener('DOMContentLoaded', function () {
         document.getElementById("link_home").href = "../AdminDashboard.aspx";
     });

     $(document).on('click', '.truncated-note', function (e) {
         e.preventDefault();

         var fullNote = $(this).data('fullnote');
         $('#noteModal .modal-body').text(fullNote);

         var modal = new bootstrap.Modal(document.getElementById('noteModal'));


         modal.show();
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
                 searching: true,
                 ajax: {
                     url: 'viewer2.aspx',
                     type: 'POST',
                     data: function (d) {
                         return {
                             draw: d.draw,
                             start: d.start,
                             length: d.length,
                             order: d.order,
                             search: d.search.value, 
                             month: $('#monthFilter').val(),

                             action: $('#<%= ddlActionFilter.ClientID %>').val(),
                             status: $('#<%= ddlStatusFilter.ClientID %>').val(),
                             item: $('#<%= item.ClientID %>').val(),
                             staff: $('#<%= txtstaffFilter.ClientID %>').val(),
                             vendor: $('#<%= vendor.ClientID %>').val(),
                             regDate: $('#<%= txtRegDateFilter.ClientID %>').val(),

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
                     { data: 'qty', width: "97px" },
                     { data: 'uom', width: "97px" },
                     { data: 'packingInfo', width: "120px" },
                     { data: 'storeNo', width: "120px" },
                     { data: 'vendorNo', width: "120px" },
                     { data: 'vendorName', width: "170px" },
                     {
                         data: 'regeDate', width: "120px", render: function (data, type) {
                             const date = new Date(data);
                             return date.toLocaleDateString('en-GB');
                         }
                     },
                     {
                         data: 'approveDate', width: "120px", render: function (data, type) {
                             const date = new Date(data);
                             return date.toLocaleDateString('en-GB');
                         }
                     },
                     { data: 'approver', width: "125px" },
                     {
                         data: 'note',
                         width: "125px",
                         render: function (data, type, row) {
                             if (type === 'display') {
                                 if (!data) {
                                     return '';
                                 }
                                 var words = data.split(/\s+/);
                                 var truncated = words.slice(0, 5).join(' ');
                                 if (words.length > 5) {
                                     truncated += ' ...';
                                 }
                                 return '<span class="truncated-note text-black-50" data-fullnote="' +
                                     $('<div/>').text(data).html() + '">' + truncated + '</span>';
                             }
                             return data;
                         }
                     },
                     { data: 'action', width: "120px" },
                     { data: 'status', width: "120px" },
                     {
                         data: 'remark',
                         width: "125px",
                         render: function (data, type, row) {
                             if (type === 'display') {
                                 if (!data) {
                                     return '';
                                 }
                                 var words = data.split(/\s+/);
                                 var truncated = words.slice(0, 5).join(' ');
                                 if (words.length > 5) {
                                     truncated += ' ...';
                                 }
                                 return '<span class="truncated-note text-black-50" data-fullnote="' +
                                     $('<div/>').text(data).html() + '">' + truncated + '</span>';
                             }
                             return data;
                         }
                     },
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
              '<%= filterStaff.ClientID %>': '<%= staffFilterGroup.ClientID %>',
              '<%= filterVendor.ClientID %>': '<%= vendorFilterGroup.ClientID %>',
             '<%= filterRegistrationDate.ClientID %>': '<%= regeDateFilterGroup.ClientID %>',
             '<%= filterApproveDate.ClientID %>': '<%= approveDateFilterGroup.ClientID %>'
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

    
        <style>
            table.dataTable > thead > tr > th {
                background-color: #BD467F !important;
                color: white !important;
            }

            table.dataTable thead th.dtfc-fixed-left,
            table.dataTable thead th.dtfc-fixed-right {
                background-color: #BD467F !important;
                color: white !important;
            }
        </style>

</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
     
<div class="container-fluid col-lg-12 col-md-12">
    <div class="card shadow-md border-0" style="background-color: #F1B4D1;">
        <div class="card-header" style="background-color: #BD467F;">
          <h4 class="text-center text-white">Reorder Quantity List</h4>
      </div>
        <div class="card-body">

            <div class="d-flex align-items-center flex-wrap mb-2">
                
                <div>
                    <asp:Button ID="btnFilter" class="btn me-1 text-white"  Style="background: #A10D54;" runat="server" Text="Show Filter" CausesValidation="false" OnClientClick="toggleFilter(); return false;" OnClick="btnFilter_Click1" />
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
                                             <button class="btn dropdown-toggle text-white" Style="background: #BD467F;"
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

                                            </div>
                                        </div>
                                    </div>

                                    <!-- Dynamic Filters Container -->
                                    <div id="dynamicFilters">
                                        <!-- Action Filter -->
                                        <div class="form-group mt-3 filter-group" id="actionFilterGroup" runat="server" style="display:none">
                                            <label for="<%= ddlActionFilter.ClientID %>">Reason</label>
                                            <asp:DropDownList ID="ddlActionFilter" runat="server" CssClass="form-control">
                                                <asp:ListItem Text="-- Select Reason --" Value="" />
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
                                        <div class="form-group mt-3 filter-group" id="statusFilterGroup" runat="server" style="display:none">
                                            <label for="<%= ddlStatusFilter.ClientID %>">Action</label>
                                            <asp:DropDownList ID="ddlStatusFilter" runat="server" CssClass="form-control">
                                                <asp:ListItem Text="-- Select Action --" Value="" />
                                                <asp:ListItem Value="1" Text="Reorder Done"></asp:ListItem>
                                                <asp:ListItem Value="2" Text="No Reordering"></asp:ListItem>
                                            </asp:DropDownList>
                                        </div>

                                        <!-- Item No Filter -->
                                        <div class="form-group mt-3 filter-group" id="itemFilterGroup" runat="server" style="display:none">
                                            <label for="<%= item.ClientID %>" style="display:block">Item No</label>
                                            <asp:DropDownList ID="item" runat="server" CssClass="form-control select2-init" style="width:333px">
                                                <asp:ListItem Text="" Value="" />
                                            </asp:DropDownList>
                                        </div>

                                        <!-- Staff Filter -->
                                        <div class="form-group mt-3 filter-group" id="staffFilterGroup" runat="server" style="display:none">
                                            <label for="<%= txtstaffFilter.ClientID %>">Approver</label>
                                            <asp:TextBox ID="txtstaffFilter" runat="server" CssClass="form-control" Placeholder="Enter approver name"></asp:TextBox>
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

                                         <!-- Approved Date Filter -->
                                         <div class="form-group mt-3 filter-group" id="approveDateFilterGroup" runat="server" style="display:none">
                                             <label for="<%= txtApproveDateFilter.ClientID %>">Approved Date</label>
                                             <asp:TextBox ID="txtApproveDateFilter" runat="server" CssClass="form-control" TextMode="Date"></asp:TextBox>
                                         </div>
                                    </div>

                                           <!-- Filter Buttons -->
                                            <div class="form-group mt-3">
                                                <asp:Button ID="btnApplyFilter" runat="server" 
                                                    CssClass="btn text-white mb-1" Style="background: #BD467F;"
                                                    Text="Apply Filters" 
                                                    OnClientClick="return handleApplyFilters();" OnClick="ApplyFilters_Click" 
                                                    CausesValidation="false" />
                    
                                                <asp:Button ID="btnResetFilter" runat="server" 
                                                    CssClass="btn text-white" Style="background: #BD467F;"
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

                         <div class="table-responsive gridview-container ps-3 pe-1 " style="height: 535px;">
                             <asp:GridView ID="GridView2" runat="server"
                                 CssClass="table table-striped table-bordered table-hover border border-2 shadow-lg sticky-grid mt-1 overflow-x-auto overflow-y-auto"
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
                                 AutoGenerateEditButton="false" ShowHeaderWhenEmpty="true">

                                    <EditRowStyle BackColor="white" />
                                    <FooterStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />
                                    <HeaderStyle Wrap="false" BackColor="#bd467f" Font-Bold="True" ForeColor="White"></HeaderStyle>
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
                                             <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                             <ItemStyle HorizontalAlign="Justify" />
                                         </asp:TemplateField>

                                      <asp:TemplateField HeaderText="Lines No" SortExpression="no" HeaderStyle-ForeColor="White" ItemStyle-HorizontalAlign="Justify" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header22" ItemStyle-CssClass="fixed-column-22" >
                                         <ItemTemplate>
                                             <asp:Label ID="lblNo" runat="server" Text='<%# Eval("no") %>' />
                                         </ItemTemplate>
                                         <ControlStyle Width="100px" />
                                         <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                         <ItemStyle HorizontalAlign="Justify" />
                                     </asp:TemplateField>

                                     <asp:TemplateField HeaderText="Item No" SortExpression="itemNo" HeaderStyle-ForeColor="White" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header33" ItemStyle-CssClass="fixed-column-33">
                                         <ItemTemplate>
                                             <asp:Label ID="lblItemNo" runat="server" Text='<%# Eval("itemNo") %>' />
                                         </ItemTemplate>
                                         <ControlStyle Width="120px" />
                                         <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                         <ItemStyle HorizontalAlign="Justify" />
                                     </asp:TemplateField>

                                     <asp:TemplateField HeaderText="Description" SortExpression="description" HeaderStyle-ForeColor="Black" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header44" ItemStyle-CssClass="fixed-column-44" >
                                         <ItemTemplate>
                                             <asp:Label ID="lblDesc" runat="server" Text='<%# Eval("description") %>' />
                                         </ItemTemplate>
                                         <ControlStyle Width="297px" />
                                         <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                         <ItemStyle HorizontalAlign="Justify" />
                                     </asp:TemplateField>

                                     <asp:TemplateField HeaderText="Qty" SortExpression="qty" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0">
                                         <ItemTemplate>
                                             <asp:Label ID="lblQty" runat="server" Text='<%# Eval("qty") %>' />
                                         </ItemTemplate>
                                         <ControlStyle Width="97px" />
                                          <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                          <ItemStyle HorizontalAlign="Justify" />
                                     </asp:TemplateField>

                                     <asp:TemplateField HeaderText="UOM" SortExpression="uom" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0">
                                         <ItemTemplate>
                                             <asp:Label ID="lblUom" runat="server" Text='<%# Eval("uom") %>' />
                                         </ItemTemplate>
                                         <ControlStyle Width="97px" />
                                          <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                          <ItemStyle HorizontalAlign="Justify" />
                                     </asp:TemplateField>

                                     <asp:TemplateField HeaderText="Packing Info" SortExpression="packingInfo" HeaderStyle-ForeColor="Black" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0">
                                         <ItemTemplate>
                                             <asp:Label ID="lblPacking" runat="server" Text='<%# Eval("packingInfo") %>' />
                                         </ItemTemplate>
                                         <ControlStyle Width="110px" />
                                         <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                         <ItemStyle HorizontalAlign="Justify" />
                                     </asp:TemplateField>

                                     <asp:TemplateField HeaderText="Location" SortExpression="storeNo" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0">
                                         <ItemTemplate>
                                             <asp:Label ID="lblStoreNo" runat="server" Text='<%# Eval("storeNo") %>' />
                                         </ItemTemplate>
                                         <ControlStyle Width="120px" />
                                          <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                          <ItemStyle HorizontalAlign="Justify" />
                                     </asp:TemplateField>

                                     <asp:TemplateField HeaderText="Vendor No" SortExpression="vendorNo" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0">
                                         <ItemTemplate>
                                             <asp:Label ID="lblVendorNo" runat="server" Text='<%# Eval("vendorNo") %>' />
                                         </ItemTemplate>
                                        <ControlStyle Width="120px" />
                                          <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                          <ItemStyle HorizontalAlign="Justify" />
                                     </asp:TemplateField>

                                     <asp:TemplateField HeaderText="Vendor Name" SortExpression="vendorName" ItemStyle-Width="170px" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0">
                                         <ItemTemplate>
                                             <asp:Label ID="lblVendorName" runat="server" Text=' <%# Eval("vendorName") %>' />
                                         </ItemTemplate>
                                        <ControlStyle Width="170px" />
                                          <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                          <ItemStyle HorizontalAlign="Justify" />
                                     </asp:TemplateField>

                                     <asp:TemplateField HeaderText="Registration Date" SortExpression="regeDate" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0">
                                         <ItemTemplate>
                                             <asp:Label ID="lblRege" runat="server" Text='<%# Eval("regeDate", "{0:dd-MM-yyyy}") %>' />
                                         </ItemTemplate>
                                         <ControlStyle Width="120px" />
                                          <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                          <ItemStyle HorizontalAlign="Justify" />
                                     </asp:TemplateField>
                                         
                                      <asp:TemplateField HeaderText="Approved Date" SortExpression="approveDate" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0">
                                          <ItemTemplate>
                                              <asp:Label ID="lblApproveDate" runat="server" Text='<%# Eval("approveDate", "{0:dd-MM-yyyy}") %>' />
                                          </ItemTemplate>
                                          <ControlStyle Width="120px" />
                                           <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                           <ItemStyle HorizontalAlign="Justify" />
                                      </asp:TemplateField>
                                         

                                        <%-- <asp:TemplateField HeaderText="Approval Status" SortExpression="approval" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0">
                                              <ItemTemplate>
                                                 <asp:Label ID="lblApprove" runat="server" Text=' <%# Eval("approved") %>'></asp:Label>
                                             </ItemTemplate>
                                              <ControlStyle Width="125px" />
                                              <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                              <ItemStyle HorizontalAlign="Justify" />
                                         </asp:TemplateField>--%>
                                      
                                     <asp:TemplateField HeaderText="Approver" SortExpression="staffName" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0">
                                         <ItemTemplate>
                                             <asp:Label ID="lblStaff" runat="server" Text='<%# Eval("approver") %>' />
                                         </ItemTemplate>
                                         <ControlStyle Width="120px" />
                                          <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                          <ItemStyle HorizontalAlign="Justify" />
                                     </asp:TemplateField>
                                         
                                    <asp:TemplateField HeaderText="Note" SortExpression="note" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0" ItemStyle-CssClass="fixed-column-59">
                                        <ItemTemplate>
                                            <asp:Label ID="lblNote" runat="server" 
                                                Text='<%# TruncateWords(Eval("note").ToString(), 5) %>'
                                                data-fullnote='<%# HttpUtility.HtmlEncode(Eval("note").ToString()) %>'
                                                CssClass="truncated-note text-black-50" />
                                        </ItemTemplate>
                                        <ControlStyle Width="125px" />
                                        <HeaderStyle ForeColor="White" BackColor="#BD467F" />
                                        <ItemStyle HorizontalAlign="Justify" />
                                    </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Reason" SortExpression="action" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0">
                                            <ItemTemplate>
                                                <asp:Label ID="lblAction" runat="server" Text='<%# Eval("action") %>'></asp:Label>
                                            </ItemTemplate>
                                            <ControlStyle Width="120px" />
                                             <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                             <ItemStyle HorizontalAlign="Justify" />
                                        </asp:TemplateField>

                                          <asp:TemplateField HeaderText="Action" SortExpression="status" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0">
                                            <ItemTemplate>
                                                <asp:Label ID="lblStatus" runat="server" Text='<%# Eval("status") %>'></asp:Label>
                                            </ItemTemplate>
                                           <ControlStyle Width="120px" />
                                            <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                            <ItemStyle HorizontalAlign="Justify" />
                                        </asp:TemplateField>

                                       <asp:TemplateField HeaderText="Remark" SortExpression="remark" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0" ItemStyle-CssClass="fixed-column-59">
                                             <ItemTemplate>
                                                 <asp:Label ID="lblRemark" runat="server" 
                                                     Text='<%# TruncateWords(Eval("Remark").ToString(), 5) %>'
                                                     data-fullnote='<%# HttpUtility.HtmlEncode(Eval("Remark").ToString()) %>'
                                                     CssClass="truncated-note text-black-50" />
                                             </ItemTemplate>
                                             <ControlStyle Width="125px" />
                                             <HeaderStyle ForeColor="White" BackColor="#BD467F" />
                                             <ItemStyle HorizontalAlign="Justify" />
                                         </asp:TemplateField>

                                         <asp:TemplateField HeaderText="Completed Date" SortExpression="completedDate" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0">
                                              <ItemTemplate>
                                                  <asp:Label ID="lblCompleted" runat="server" Text=' <%# Eval("completedDate", "{0:dd-MM-yyyy}") %>' />
                                             </ItemTemplate>
                                              <ControlStyle Width="125px" />
                                              <HeaderStyle ForeColor="White" BackColor="#bd467f" />
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
