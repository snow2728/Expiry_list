<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="approval.aspx.cs" Inherits="Expiry_list.ReorderForm.approval" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">

      <%-- Viewer1 Form For Supers --%>
     <style>
         table thead tr,th{
             line-height: 15px !important;
         }
         table.dataTable thead tr th {
              border-bottom: none;
          }

          table td{
              border-bottom: none;
              border-spacing: 0;
              box-shadow: none;
          }

          table.dataTable thead tr th{
              border-right: none !important;
          }

          /* Base table borders */
          table.dataTable th,
          table.dataTable td {
              border-right: 1px solid #ccc;
          }

          /* Remove per-cell borders on sticky columns */
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

     </style>

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
                  scrollY: "63vh",
                  scrollCollapse: true,
                  autoWidth: false,
                  stateSave: true,
                  processing: true,
                  searching: true, 
                  ajax: {
                      url: 'approval.aspx',
                      type: 'POST',
                      data: function (d) {
                          return {
                              draw: d.draw,
                              start: d.start,
                              length: d.length,
                              order: d.order,
                              search: d.search.value,

                              status: $('#<%= ddlApproveFilter.ClientID %>').val(),
                              store: $('#<%= lstStoreFilter.ClientID %>').val(),
                              item: $('#<%= item.ClientID %>').val(),
                              vendor: $('#<%= vendor.ClientID %>').val(),
                              regDate: $('#<%= txtRegDateFilter.ClientID %>').val()
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
                          data: 'checkbox', orderable: false, width: "30px", render: function (data, type, row, meta) {
                              return '<input type="checkbox" class="rowCheckbox" data-id="' + row.id + '" onclick="handleSingleSelection(this)"/>';
                          }
                      },
                      {
                          data: null,
                          width: "30px",
                          orderable: false,
                          visible: false,
                          render: function (data, type, row, meta) {
                              return meta.settings._iDisplayStart + meta.row + 1;
                          }
                      },
                      { data: 'no', width: "100px" },
                      { data: 'itemNo', width: "100px" },
                      { data: 'description', width: "297px" },
                      { data: 'barcodeNo', width: "127px" },
                      { data: 'qty', width: "97px" },
                      { data: 'uom', width: "97px" },
                      { data: 'packingInfo', width: "120px" },
                      { data: 'divisionCode', width: "120px" },
                      { data: 'storeNo', width: "120px" },
                      { data: 'vendorNo', width: "120px" },
                      { data: 'vendorName', width: "170px" },
                      {
                          data: 'regeDate',
                          width: "120px",
                          render: function (data, type) {
                              if (type === 'sort') {
                                  return new Date(data).getTime();
                              }
                              const date = new Date(data);
                              return date.toLocaleDateString('en-GB');
                          }
                      },
                      {
                          data: 'approved',
                          width: "120px",
                          render: function (data, type, row) {
                              if (type === 'display') {
                                  if (!data) return 'PENDING';
                                  return data.toUpperCase();
                              }
                              return data;
                          }
                      },
                      { data: 'note', width: "125px" },
                      {
                          data: null,
                          orderable: false,
                          defaultContent: '',
                          className: 'dt-center',
                          visible: false
                      }
                  ],
                  order: [1, 'asc'],
                  columnDefs: [
                      {
                          targets: [16],
                          visible: false,
                          searchable: false
                      },
                      {
                          targets: [14],
                          visible: true
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

          document.addEventListener('DOMContentLoaded', function () {
              document.getElementById("link_home").href = "../AdminDashboard.aspx";
          });

          function focusOnEditedRow() {
              const rowId = $('#<%= hfSelectedIDs.ClientID %>').val();
              if (!rowId) return;

                  const grid = $("#<%= GridView2.ClientID %>");

                  if ($.fn.DataTable.isDataTable(grid)) {
                      const dt = grid.DataTable();

                      dt.order([]).search('').draw();
                      const rowIndex = dt.rows().indexes().toArray().findIndex(index => {
                          const rowData = dt.row(index).data();
                          return rowData && rowData.id === rowId;
                      });

                  if (rowIndex !== -1) {
                      // Move row to top
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
                  '<%= filterStore.ClientID %>': '<%= storeFilterGroup.ClientID %>',
                  '<%= filterItem.ClientID %>': '<%= itemFilterGroup.ClientID %>',
                  '<%= filterVendor.ClientID %>': '<%= vendorFilterGroup.ClientID %>',
                  '<%= filterRegistrationDate.ClientID %>': '<%= regeDateFilterGroup.ClientID %>',
                  '<%= filterDivisionCode.ClientID %>': '<%= divisionCodeFilterGroup.ClientID %>'
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

              } else {
                  filterPane.style.display = "none";
                  gridCol.classList.remove("col-md-10");
                  gridCol.classList.add("col-md-12");

              }
          }

      const filterMap = {
          store: {
              checkboxId: '<%= filterStore.ClientID %>',
              controlId: '<%= lstStoreFilter.ClientID %>',
              groupId: '<%= storeFilterGroup.ClientID %>'
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
          registrationDate: { 
              checkboxId: '<%= filterRegistrationDate.ClientID %>', 
              controlId: '<%= txtRegDateFilter.ClientID %>',
              groupId: '<%= regeDateFilterGroup.ClientID %>'
          },
          divisionCode: {
              checkboxId: '<%= filterDivisionCode.ClientID %>',
               controlId: '<%= txtDivisionCodeFilter.ClientID %>',
               groupId: '<%= divisionCodeFilterGroup.ClientID %>'
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

                  console.log("Select2 initialized for item and vendor filters.");
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

              document.getElementById('gridCol').style.height = "74vh";
              document.getElementById('gridCol').style.width = "auto";

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
        
<div class="container-fluid col-lg-12 col-md-12 mt-0">
    <div class="card shadow-md border-0" style="background-color: #F1B4D1;">
        <div class="card-header" style="background-color:#A10D54;">
             <h4 class="text-center fw-bold text-white">Approval List</h4>
         </div>

        <div class="card-body">
            <div class="col-lg-12 col-md-12 mb-2">
                <div class="row g-2 align-items-center">
                    <!-- Filter Button -->
                    <div class="col-6 col-md-auto">
                        <asp:Button ID="btnFilter" runat="server"
                            CssClass="btn text-white w-100"
                            Style="background: #A10D54;"
                            Text="Show Filter"
                            OnClientClick="toggleFilter(); return false;" />
                    </div>


                    <%
                       var panelPermissions = Session["formPermissions"] as Dictionary<string, string>;
                       string panelExpiryPerm = panelPermissions != null && panelPermissions.ContainsKey("ReorderQuantity") ? panelPermissions["ReorderQuantity"] : null;
                       bool panelCanViewOnly = !string.IsNullOrEmpty(panelExpiryPerm) && panelExpiryPerm != "edit";
                   %>

                    <% if (panelCanViewOnly) { %>
                           
                         <div class="col-12 col-md-auto">
                             <asp:Button ID="btnApproveSelected" runat="server"
                                 CssClass="btn w-100"
                                 Style="background: #ffffff; color:#A10D54;"
                                 Text="Approve"
                                 OnClick="btnApproveSelected_Click" />
                         </div>

                          <div class="col-12 col-md-auto">
                              <asp:Button ID="btnDeclineSelected" runat="server"
                                  CssClass="btn text-white w-100"
                                  Style="background: #A10D54;"
                                  Text="Decline"
                                  OnClick="btnDeclineSelected_Click" />
                          </div>

                              <!-- Edit Button -->
                            <div class="col-6 col-md-auto">
                                <asp:Button Text="Edit" runat="server"
                                    CssClass="btn btn-secondary text-white w-100"
                                    ID="btnEdit"
                                    OnClick="btnEdit_Click" />
                            </div>

                    <% } %>
                   
                </div>
            </div>

                <div class="d-flex p-2 pt-0 col-lg-12 col-md-12 overflow-x-auto overflow-y-auto">
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
                                                    <button class="btn dropdown-toggle text-white" Style="background: #A10D54;"
                                                        type="button" id="filterDropdownButton" data-bs-toggle="dropdown"
                                                        aria-haspopup="true" aria-expanded="false" runat="server">
                                                        Select Filters
                                                    </button>
                                                    <div class="dropdown-menu p-3" aria-labelledby="filterDropdownButton" style="max-height: 250px; overflow-y: auto;">
                                                        <!-- Checkboxes for each filter -->
                                                        <div class="form-check">
                                                            <asp:CheckBox ID="filterStore" runat="server" CssClass="form-check-input" />
                                                            <label class="form-check-label" for="<%= filterStore.ClientID %>">Location</label>
                                                        </div>
                                                        <div class="form-check">
                                                            <asp:CheckBox ID="filterItem" runat="server" CssClass="form-check-input" />
                                                            <label class="form-check-label" for="<%= filterItem.ClientID %>">Item No</label>
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
                                                           <asp:CheckBox ID="filterDivisionCode" runat="server" CssClass="form-check-input" />
                                                           <label class="form-check-label" for="<%= filterDivisionCode.ClientID %>">Division Code</label>
                                                       </div>
                                                    </div>
                                                </div>
                                            </div>

                                            <!-- Dynamic Filters Container -->
                                            <div id="dynamicFilters">

                                                <!-- Status Filter -->
                                                <div class="form-group mt-3 filter-group" id="approveFilterGroup" runat="server" style="display: none">
                                                    <label for="<%= ddlApproveFilter.ClientID %>">Approval Status</label>
                                                    <asp:DropDownList ID="ddlApproveFilter" runat="server" CssClass="form-control">
                                                        <asp:ListItem Value="0" Text="Not Approved"></asp:ListItem>
                                                        <asp:ListItem Value="1" Text="Approved"></asp:ListItem>
                                                    </asp:DropDownList>
                                                </div>

                                                <!-- Store Filter -->
                                                <div class="form-group mt-3 filter-group" id="storeFilterGroup" runat="server" style="display: none">
                                                    <label for="<%=lstStoreFilter.ClientID %>">Location</label>

                                                    <asp:ListBox ID="lstStoreFilter" runat="server" CssClass="form-control select2-multi-check w-100"
                                                        SelectionMode="Multiple" Style="display: none;"></asp:ListBox>
                                                    <div id="locationPillsContainer" class="location-pills-container mb-2 w-100"></div>

                                                    <div class="location-pill-template" style="display: block"></div>
                                                </div>

                                                <!-- Item No Filter -->
                                                <div class="form-group mt-3 filter-group" id="itemFilterGroup" runat="server" style="display: none">
                                                    <label for="<%= item.ClientID %>" style="display: block">Item No</label>
                                                    <asp:DropDownList ID="item" runat="server" CssClass="form-control select2-init" Style="width: 333px">
                                                        <asp:ListItem Text="" Value="" />
                                                    </asp:DropDownList>
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

                                                 <!-- Division Code Filter -->
                                                 <div class="form-group mt-3 filter-group" id="divisionCodeFilterGroup" runat="server" style="display: none">
                                                     <label for="<%= txtDivisionCodeFilter.ClientID %>">Division Code</label>
                                                     <asp:TextBox ID="txtDivisionCodeFilter" runat="server" CssClass="form-control"></asp:TextBox>
                                                 </div>
                                            </div>

                                            <!-- Filter Buttons -->
                                            <div class="form-group mt-3">
                                                <asp:Button ID="btnApplyFilter" runat="server"
                                                    CssClass="btn text-white mb-1"
                                                    Style="background: #A10D54;"
                                                    Text="Apply Filters"
                                                    OnClientClick="return handleApplyFilters();" OnClick="ApplyFilters_Click"
                                                    CausesValidation="false" />

                                                <asp:Button ID="btnResetFilter" runat="server"
                                                    CssClass="btn text-white"
                                                    Style="background: #A10D54;"
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
                    <div class="col-md-12 ms-4" id="gridCol">
                    <asp:UpdatePanel ID="UpdatePanel2" runat="server" UpdateMode="Conditional">
                        <ContentTemplate>
                            <asp:Panel ID="pnlNoData" runat="server" Visible="false">
                                <div class="alert alert-info">No items to Filter</div>
                            </asp:Panel>

                            <div class="gridview-container p-2">
                                   <asp:GridView ID="GridView2" runat="server"
                                        CssClass="table table-striped table-hover border-2 shadow-lg sticky-grid overflow-x-auto overflow-y-auto"
                                        AutoGenerateColumns="False"
                                        DataKeyNames="id"
                                        UseAccessibleHeader="true"
                                        OnRowEditing="GridView2_RowEditing"
                                        OnRowUpdating="GridView2_RowUpdating"
                                        OnRowCancelingEdit="GridView2_RowCancelingEdit"
                                        OnRowDataBound="GridView2_RowDataBound"
                                        OnSorting="GridView1_Sorting"
                                        OnRowCreated="GridView1_RowCreated"
                                        AllowPaging="false"
                                        PageSize="100"
                                        CellPadding="4"
                                        ForeColor="#333333"
                                        GridLines="None"
                                        AutoGenerateEditButton="false" ShowHeaderWhenEmpty="true"  >
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

                                            <asp:TemplateField HeaderText="ID" Visible="false">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblId" runat="server" Text='<%# Eval("id") %>' CssClass="row-id" />
                                                </ItemTemplate>
                                                <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1" ItemStyle-CssClass="fixed-column-1">
                                                <HeaderTemplate>
                                                    <asp:CheckBox ID="chkAll1" runat="server" />
                                                </HeaderTemplate>
                                                <ItemTemplate>
                                                    <input type="checkbox" class="rowCheckbox" data-id='<%# Eval("id") %>' runat="server" id="CheckBox1" />
                                                </ItemTemplate>
                                                <ControlStyle Width="50px" />
                                                <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>

                                            <asp:TemplateField ItemStyle-HorizontalAlign="Justify" HeaderText="No" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header2" ItemStyle-CssClass="fixed-column-2">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblLinesNo" runat="server" Text='<%# Container.DataItemIndex + 1 %>' />
                                                </ItemTemplate>
                                                <ControlStyle Width="50px" />
                                                <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Lines No" ItemStyle-Width="100px" SortExpression="no" HeaderStyle-ForeColor="White" ItemStyle-HorizontalAlign="Justify" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header3" ItemStyle-CssClass="fixed-column-3">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblNo" runat="server" Text='<%# Eval("no") %>' />
                                                </ItemTemplate>
                                                <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                                <ControlStyle Width="100px" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Item No" SortExpression="itemNo" HeaderStyle-ForeColor="White" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header4" ItemStyle-CssClass="fixed-column-4">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblItemNo" runat="server" Text='<%# Eval("itemNo") %>' />
                                                </ItemTemplate>
                                                <ControlStyle Width="117px" />
                                                <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Description" SortExpression="description" HeaderStyle-ForeColor="Black" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header5" ItemStyle-CssClass="fixed-column-5">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblDesc" runat="server" Text='<%# Eval("description") %>' />
                                                </ItemTemplate>
                                                <ControlStyle Width="257px" />
                                                <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>
                                            
                                           <asp:TemplateField HeaderText="Barcode No" SortExpression="barcodeNo" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0" ItemStyle-CssClass="fixed-column-6">
                                               <ItemTemplate>
                                                   <asp:Label ID="lblBarcode" runat="server" Text='<%# Eval("barcodeNo") %>' />
                                               </ItemTemplate>
                                               <ControlStyle Width="127px" />
                                               <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                               <ItemStyle HorizontalAlign="Justify" />
                                           </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Qty" SortExpression="qty" HeaderStyle-ForeColor="Black" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0" ItemStyle-CssClass="fixed-column-7">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblQty" runat="server" Text='<%# Eval("qty") %>' />
                                                </ItemTemplate>
                                                <EditItemTemplate>
                                                    <asp:PlaceHolder ID="phEditQty" runat="server" Visible='<%# (string)Eval("FormPermission") != "edit" %>'>
                                                        <asp:TextBox ID="txtQty" runat="server" Text='<%# Bind("qty") %>' CssClass="form-control" Width="100px" />
                                                    </asp:PlaceHolder>
                                                    <asp:PlaceHolder ID="phLabelQty" runat="server" Visible='<%# (string)Eval("FormPermission") == "edit" %>'>
                                                        <asp:Label ID="lblQtyEdit" runat="server" Text='<%# Eval("qty") %>' />
                                                    </asp:PlaceHolder>
                                                </EditItemTemplate>
                                                <ControlStyle Width="117px" />
                                                <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="UOM" SortExpression="uom" HeaderStyle-ForeColor="White" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0" ItemStyle-CssClass="fixed-column-8">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblUom" runat="server" Text='<%# Eval("uom") %>' />
                                                </ItemTemplate>
                                                <EditItemTemplate>
                                                    <asp:PlaceHolder runat="server" Visible='<%# (string)Eval("FormPermission") != "edit" %>'>
                                                        <asp:DropDownList ID="ddlUom" runat="server" 
                                                            DataSource='<%# GetUOMsByItemNo(Eval("itemNo").ToString()) %>'
                                                            AppendDataBoundItems="true" CssClass="form-control" Width="100px">
                                                            <asp:ListItem Text="-- Select UOM --" Value="" />
                                                        </asp:DropDownList>
                                                    </asp:PlaceHolder>
                                                    <asp:PlaceHolder runat="server" Visible='<%# (string)Eval("FormPermission") == "edit" %>'>
                                                        <asp:Label ID="lblUomEdit" runat="server" Text='<%# Eval("uom") %>' />
                                                    </asp:PlaceHolder>
                                                </EditItemTemplate>
                                                <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Packing Info" SortExpression="packingInfo" HeaderStyle-ForeColor="Black" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0" ItemStyle-CssClass="fixed-column-9">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblPacking" runat="server" Text='<%# Eval("packingInfo") %>' />
                                                </ItemTemplate>
                                                <ControlStyle Width="110px" />
                                                <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Division" SortExpression="divisionCode" HeaderStyle-ForeColor="Black" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0" ItemStyle-CssClass="fixed-column-9">
                                                 <ItemTemplate>
                                                     <asp:Label ID="lblDivisionCode" runat="server" Text='<%# Eval("divisionCode") %>' />
                                                 </ItemTemplate>
                                                 <ControlStyle Width="110px" />
                                                 <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                                 <ItemStyle HorizontalAlign="Justify" />
                                             </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Location" SortExpression="storeNo" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0" ItemStyle-CssClass="fixed-column-51">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblStoreNo" runat="server" Text='<%# Eval("storeNo") %>' />
                                                </ItemTemplate>
                                                <ControlStyle Width="120px" />
                                                <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Vendor No" SortExpression="vendorNo" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0" ItemStyle-CssClass="fixed-column-54">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblVendorNo" runat="server" Text='<%# Eval("vendorNo") %>' />
                                                </ItemTemplate>
                                                <ControlStyle Width="120px" />
                                                <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Vendor Name" ItemStyle-Width="170px" SortExpression="vendorName" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0" ItemStyle-CssClass="fixed-column-55">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblVendorName" runat="server" Text=' <%# Eval("vendorName") %>' />
                                                </ItemTemplate>
                                                <ControlStyle Width="170px" />
                                                <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Regi Date" SortExpression="regeDate" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0" ItemStyle-CssClass="fixed-column-56">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblRege" runat="server" Text='<%# Eval("regeDate", "{0:dd-MM-yyyy}") %>' />
                                                </ItemTemplate>
                                                <ControlStyle Width="120px" />
                                                <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Approval Status" SortExpression="approved" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0" 
                                                ItemStyle-CssClass="fixed-column-58">
                                                <EditItemTemplate>
                                                    <!-- Show dropdown only for view-only users -->
                                                    <asp:PlaceHolder runat="server" Visible='<%# (string)Eval("FormPermission") != "edit" %>'>
                                                        <asp:DropDownList ID="ddlApprovalEdit" runat="server">
                                                            <asp:ListItem Text="Not Approved" Value="Not Approved" />
                                                            <asp:ListItem Text="Approved" Value="Approved" />
                                                            <asp:ListItem Text="Declined" Value="Declined" />
                                                        </asp:DropDownList>
                                                    </asp:PlaceHolder>
        
                                                    <!-- Show read-only label for edit-permission users -->
                                                    <asp:PlaceHolder runat="server" Visible='<%# (string)Eval("FormPermission") == "edit" %>'>
                                                        <asp:Label ID="lblStatusEdit" runat="server" 
                                                            Text='<%# Eval("approved").ToString().ToUpperInvariant() %>' CssClass="form-control-plaintext" />
                                                    </asp:PlaceHolder>
                                                </EditItemTemplate>
    
                                                <ItemTemplate>
                                                    <asp:Label ID="lblStatus" runat="server" 
                                                       Text='<%# Eval("approved").ToString().ToUpperInvariant() %>' />
                                                </ItemTemplate>
    
                                                <ControlStyle Width="120px" />
                                                <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Note" SortExpression="note" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0" ItemStyle-CssClass="fixed-column-59">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblNote" runat="server" Text=' <%# Eval("note") %>'></asp:Label>
                                                </ItemTemplate>
                                                <ControlStyle Width="125px" />
                                                <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                                <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>

                                          <%--   <asp:TemplateField HeaderText="Completed Date" SortExpression="completedDate" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" HeaderStyle-CssClass="position-sticky top-0">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblStaff" runat="server" Text=' <%# Eval("completedDate", "{0:dd-MM-yyyy}") %>' />
                                                </ItemTemplate>
                                                <ControlStyle Width="120px" />
                                                 <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                                 <ItemStyle HorizontalAlign="Justify" />
                                            </asp:TemplateField>--%>

                                            <asp:CommandField ShowEditButton="true" ShowCancelButton="true" ControlStyle-CssClass="m-1 text-white" HeaderStyle-CssClass="position-sticky top-0"
                                                EditText="-" UpdateText="<i class='fa-solid fa-file-arrow-up'></i>"
                                                CancelText="<i class='fa-solid fa-xmark'></i>">
                                                <ControlStyle CssClass="btn m-1 text-white" Width="105px" BackColor="#bd467f" />
                                                <HeaderStyle ForeColor="White" BackColor="#bd467f" />
                                                <ItemStyle HorizontalAlign="Justify" BackColor="White"/>
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
