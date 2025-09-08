<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="regeForm1.aspx.cs" Inherits="Expiry_list.regeForm1" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
     <script type="text/javascript">
         $(document).ready(function () {
             InitializeGridEditStores();
             setupPermissionToggles();
             if (typeof (Sys) !== 'undefined') {
                 Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
                     InitializeGridEditStores();
                     setupPermissionToggles();
                 });
             }
         });

      document.addEventListener('DOMContentLoaded', function () {
             updateLocationPillsDisplay();

             const listBox = document.getElementById('#lstStoreFilter');
          if (listBox) {
              listBox.addEventListener('change', updateLocationPillsDisplay);
          }
      });

         function InitializeGridEditStores() {
             $('.store-select').each(function () {
                 const $select = $(this);
                 const allOptionId = "all";

                 if ($select.hasClass("select2-hidden-accessible")) {
                     $select.select2('destroy');
                 }

                 $select.select2({
                     placeholder: "-- Select stores --",
                     closeOnSelect: false,
                     width: 'resolve',
                     allowClear: true,
                     dropdownParent: $(document.body),
                     minimumResultsForSearch: 1,
                     dropdownAutoWidth: true,
                     escapeMarkup: m => m,
                     language: {
                         noResults: () => "No stores found"
                     },
                     matcher: function (params, data) {
                         if ($.trim(params.term) === '') return data;
                         if (data.text.toLowerCase().includes(params.term.toLowerCase())) {
                             return data;
                         }
                         return null;
                     },
                     templateResult: function (data) {
                         if (!data.id) return data.text;

                         const selectedValues = $select.val() || [];
                         const isAll = data.id === allOptionId;
                         const hasAll = selectedValues.includes(allOptionId);
                         const isChecked = isAll ? hasAll : selectedValues.includes(data.id);
                         const isDisabled = hasAll && !isAll;

                         return $(`
                      <div class="select2-checkbox-option d-flex align-items-center">
                          <input type="checkbox" class="select2-checkbox me-2"
                              ${isChecked ? 'checked' : ''} 
                              ${isAll ? ' data-is-all="true"' : ''} 
                              ${isDisabled ? 'disabled' : ''}>
                          <div class="select2-text text-truncate ${isAll ? 'fw-bold' : ''}">${data.text}</div>
                      </div>
                  `);
                     },
                     templateSelection: () => ''
                 });

                 const $dropdown = $select.data('select2').$dropdown;

                 $dropdown.off('click.select2Checkbox').on('click.select2Checkbox', '.select2-checkbox', function () {
                     const $option = $(this).closest('.select2-results__option');
                     const data = $option.data('data');
                     if (!data || !data.id) return;

                     let selected = $select.val() || [];

                     if (data.id === allOptionId) {
                         if (!selected.includes(allOptionId)) {
                             $select.val([allOptionId]).trigger('change');
                         }
                     } else {
                         const index = selected.indexOf(data.id);
                         if (this.checked && index === -1) {
                             selected.push(data.id);
                         } else if (!this.checked && index !== -1) {
                             selected.splice(index, 1);
                         }

                         const newVal = selected.filter(v => v);
                         if (JSON.stringify(newVal.sort()) !== JSON.stringify((selected || []).sort())) {
                             $select.val(newVal).trigger('change');
                         } else {
                             $select.val(newVal);
                             updateSelect2Checkboxes($select);
                             updateEditRowPillsDisplay($select);
                         }
                     }
                 });

                 $select.off('change.select2Update').on('change.select2Update', function () {
                     let values = $select.val() || [];
                     const hasAll = values.includes(allOptionId);

                     let changed = false;
                     if (hasAll && values.length > 1) {
                         $select.val([allOptionId]);
                         changed = true;
                     } else {
                         const filtered = values.filter(v => v !== allOptionId);
                         if (filtered.length !== values.length) {
                             $select.val(filtered);
                             changed = true;
                         }
                     }

                     if (changed) $select.trigger('change');

                     updateSelect2Checkboxes($select);
                     updateEditRowPillsDisplay($select);
                 });

                 $select.on('select2:selecting select2:unselecting', function (e) {
                     if (e.params?.args?.originalEvent &&
                         !$(e.params.args.originalEvent.target).hasClass('select2-checkbox')) {
                         e.preventDefault();
                     }
                 });

                 updateSelect2Checkboxes($select);
                 updateEditRowPillsDisplay($select);
             });
         }

         function updateEditRowPillsDisplay($select) {
             const container = $select.closest('td').find('.location-pills-container')[0];
             const allOptionId = "all";

             if (!container) return;
             container.innerHTML = '';

             const values = $select.val() || [];
             const hasAll = values.includes(allOptionId);

             const createPill = (text, value) => {
                 const pill = document.createElement('span');
                 pill.className = 'location-pill';
                 pill.innerHTML = `
                  <span class="pill-text">${text}</span>
                  <span class="pill-remove" data-value="${value}">×</span>
              `;
                 pill.querySelector('.pill-remove').addEventListener('click', function (e) {
                     e.preventDefault();
                     const updated = values.filter(v => v !== value);
                     $select.val(updated).trigger('change');
                 });
                 container.appendChild(pill);
             };

             if (hasAll) {
                 createPill("All Stores", allOptionId);
             } else {
                 values.forEach(value => {
                     const option = $select.find('option[value="' + value + '"]')[0];
                     if (option) {
                         createPill(option.text, value);
                     }
                 });
             }

             container.style.display = values.length > 0 ? 'grid' : 'none';
         }

         function updateSelect2Checkboxes($select) {
             const values = $select.val() || [];
             const allOptionId = "all";
             const hasAll = values.includes(allOptionId);

             const $checkboxes = $select.data('select2').$dropdown.find('.select2-checkbox');
             $checkboxes.each(function () {
                 const $cb = $(this);
                 const data = $cb.closest('.select2-results__option').data('data');
                 if (data && data.id) {
                     const isAll = data.id === allOptionId;
                     const isChecked = isAll ? hasAll : values.includes(data.id);
                     const isDisabled = hasAll && !isAll;
                     $cb.prop('checked', isChecked).prop('disabled', isDisabled);
                 }
             });
         }

       function pageLoad() {
             updateLocationPillsDisplay();

           const listBox = document.getElementById('#lstStoreFilter');
           if (listBox) {
               listBox.addEventListener('change', updateLocationPillsDisplay);
           }
       }

       function updateLocationPillsDisplay() {
           var $select = $('#lstStoreFilter');
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
                      <span class="pill-remove" data-value="${option.value}">×</span>
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
           const listBox = document.getElementById('#lstStoreFilter');
             if (listBox) {
                 Array.from(listBox.options).forEach(option => {
                     option.selected = false;
                 });
                 updateLocationPillsDisplay();
             }
       }

         function setupPermissionToggles() {
             const pairs = [
                 { checkboxId: 'chkExpiry_Enable', sectionId: 'permExpiry' },
             ];

             pairs.forEach(({ checkboxId, sectionId }) => {
                 const cb = document.getElementById("#"+checkboxId);
                 const section = document.getElementById("#"+sectionId);

                 if (cb && section) {
                     togglePermissionsById(cb, section);
                     cb.addEventListener("change", () => togglePermissionsById(cb, section));
                 }
             });
         }

         function togglePermissionsById(checkbox, section) {
             if (!checkbox || !section) return;

             section.style.display = checkbox.checked ? "flex" : "none";

             if (!checkbox.checked) {
                 clearPermissionRadios(section);
             }
         }

         function clearPermissionRadios(container) {
             const radios = container.querySelectorAll('input[type="radio"]');
             radios.forEach(radio => radio.checked = false);
         }

         Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
             setupPermissionToggles();

         });

         history.pushState(null, null, location.href);
         window.addEventListener("popstate", function (event) {
             location.reload();
         });

     </script>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
       <div class="d-flex justify-content-lg-around align-items-center">
           <h2>User Management</h2>
           <a href="regeForm.aspx" class="btn text-white" style="background-color:#188fa6;"><i class="fa-solid fa-user-plus"></i> Add New User</a>
       </div>
        <div class="container overflow-x-scroll overflow-y-scroll" style="height:557px;">
         

            <asp:HiddenField ID="hfSelectedRows" runat="server" />
            <asp:HiddenField ID="hfSelectedIDs" runat="server" />
            <asp:HiddenField ID="hflength" runat="server" />   
            <asp:HiddenField ID="hfEditId" runat="server" />
            <asp:HiddenField ID="hfEditedRowId" runat="server" />

            <asp:ScriptManager ID="ScriptManager1" runat="server" />
            <asp:UpdatePanel ID="upGrid" runat="server">
              <ContentTemplate>
                 <asp:GridView ID="GridView2" runat="server" AutoGenerateColumns="False" CssClass="table table-striped table-bordered table-hover border border-2 shadow-lg sticky-grid mt-1"
                     DataKeyNames="id" OnRowEditing="GridView2_RowEditing"
                     OnRowUpdating="GridView2_RowUpdating" OnRowDeleting="GridView2_RowDeleting" OnRowCancelingEdit="GridView2_RowCancelingEdit"
                     OnRowDataBound="GridView2_RowDataBound">
                     <Columns>
                         <asp:BoundField DataField="id" HeaderText="ID" ReadOnly="true" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1" ItemStyle-CssClass="fixed-column-1" HeaderStyle-BackColor="Gray" HeaderStyle-ForeColor="White" />
        
                         <asp:TemplateField HeaderText="Username" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1" ItemStyle-CssClass="fixed-column-1">
                             <ItemTemplate>
                                 <asp:Label ID="lblUsername" runat="server" Text='<%# Eval("username") %>'></asp:Label>
                             </ItemTemplate>
                             <EditItemTemplate>
                                 <asp:TextBox ID="txtUsername" runat="server" Text='<%# Bind("username") %>' 
                                     CssClass="form-control" />
                             </EditItemTemplate>
                              <HeaderStyle ForeColor="White" BackColor="Gray" />
                              <ItemStyle HorizontalAlign="Justify" />
                         </asp:TemplateField>
        
                         <asp:TemplateField HeaderText="Password" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1" ItemStyle-CssClass="fixed-column-1">
                             <ItemTemplate>••••••••</ItemTemplate>
                             <EditItemTemplate>
                                 <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" 
                                     CssClass="form-control" placeholder="Leave blank to keep current" />
                             </EditItemTemplate>
                               <HeaderStyle ForeColor="White" BackColor="Gray" />
                               <ItemStyle HorizontalAlign="Justify" />
                         </asp:TemplateField>
        
                         <asp:TemplateField HeaderText="Enabled" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1" ItemStyle-CssClass="fixed-column-1">
                             <ItemTemplate>
                                 <asp:CheckBox ID="chkEnabledItem" runat="server" 
                                     Checked='<%# Eval("isEnabled") %>' Enabled="false" />
                             </ItemTemplate>
                             <EditItemTemplate>
                                 <asp:CheckBox ID="chkEnabled" runat="server" 
                                     Checked='<%# Bind("isEnabled") %>' />
                             </EditItemTemplate>
                               <HeaderStyle ForeColor="White" BackColor="Gray" />
                               <ItemStyle HorizontalAlign="Justify" />
                         </asp:TemplateField>

                         <asp:TemplateField HeaderText="Stores" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1" ItemStyle-CssClass="fixed-column-1">
                             <ItemTemplate>
                                 <%# Eval("StoreNos") %>
                             </ItemTemplate>

                             <EditItemTemplate>
                                 <asp:HiddenField ID="hfStoreIds" runat="server"
                                                  Value='<%# Eval("StoreNos") %>' />
                                 <asp:ListBox ID="lstStores" runat="server"
                                              SelectionMode="Multiple"
                                              CssClass="store-select form-control me-2"
                                              Style="display:none; width:100%;" />
                                 <div id="locationPillsContainer" class="location-pills-container mb-2"></div>
                             </EditItemTemplate>
                               <HeaderStyle ForeColor="White" BackColor="Gray" />
                               <ItemStyle HorizontalAlign="Justify" />
                         </asp:TemplateField>

                             <asp:TemplateField HeaderText="Permissions" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1" ItemStyle-CssClass="fixed-column-1">
                               <ItemTemplate>
                                 <%# Eval("Permissions") %>
                               </ItemTemplate>
                               <EditItemTemplate>
                                 <div>
                                   <div class="form-check">
                                    <asp:CheckBox ID="chkExpiry_Enable" runat="server" CssClass="form-check-input perm-toggle bg-transparent" />
                                    <label class="form-check-label" for="chkExpiry_Enable">Expiry List</label>
                                  </div>
                                  <div id="permExpiry" class="permission-options ms-4 mb-2 d-inline" >
                                    <asp:RadioButton ID="rdoExpiry_View" runat="server" GroupName="ExpiryList" Text="View" CssClass="form-check-input bg-transparent me-1" /> 
                                    <asp:RadioButton ID="rdoExpiry_Edit" runat="server" GroupName="ExpiryList" Text="Edit" CssClass="form-check-input bg-transparent me-1" /> 
                                    <asp:RadioButton ID="rdoExpiry_Admin" runat="server" GroupName="ExpiryList" Text="Admin" CssClass="form-check-input bg-transparent me-1" />
                                    <asp:RadioButton ID="rdoExpiry_Super" runat="server" GroupName="ExpiryList" Text="Super" CssClass="form-check-input bg-transparent me-1" /> 
                                    <asp:RadioButton ID="rdoExpiry_Super1" runat="server" GroupName="ExpiryList" Text="Super1" CssClass="form-check-input bg-transparent me-1" /> 
                                  </div>
          
                                   <div class="form-check">
                                    <asp:CheckBox ID="chkNegative_Enable" runat="server" CssClass="form-check-input perm-toggle bg-transparent" />
                                    <label class="form-check-label" for="chkNegative_Enable">Negative Inventory</label>
                                  </div>
                                  <div runat="server" id="permNegative" class="permission-options ms-4 mb-2 d-inline">
                                    <asp:RadioButton ID="rdoNegative_View" runat="server" GroupName="NegativeInventory" Text="View" CssClass="form-check-input bg-transparent me-1" /> 
                                    <asp:RadioButton ID="rdoNegative_Edit" runat="server" GroupName="NegativeInventory" Text="Edit" CssClass="form-check-input bg-transparent me-1" />
                                    <asp:RadioButton ID="rdoNegative_Admin" runat="server" GroupName="NegativeInventory" Text="Admin" CssClass="form-check-input bg-transparent me-1" />
                                    <asp:RadioButton ID="rdoNegative_Super" runat="server" GroupName="NegativeInventory" Text="Super" CssClass="form-check-input bg-transparent me-1" /> 
                                  </div>

                                 <div class="form-check">
                                   <asp:CheckBox ID="chkSystem_Enable" runat="server" CssClass="form-check-input perm-toggle bg-transparent"  />
                                   <label class="form-check-label" for="chkSystem_Enable">System Settings</label>
                                 </div>
                                 <div runat="server" id="permSystem" class="permission-options ms-4 mb-2 d-inline">
                                   <asp:RadioButton ID="rdoSystem_View" runat="server" GroupName="SystemSettings" Text="View" CssClass="form-check-input bg-transparent me-1" /> 
                                   <asp:RadioButton ID="rdoSystem_Edit" runat="server" GroupName="SystemSettings" Text="Edit" CssClass="form-check-input bg-transparent me-1" /> 
                                   <asp:RadioButton ID="rdoSystem_Admin" runat="server" GroupName="SystemSettings" Text="Admin" CssClass="form-check-input bg-transparent me-1" /> 
                                   <asp:RadioButton ID="rdoSystem_Super" runat="server" GroupName="SystemSettings" Text="Super" CssClass="form-check-input bg-transparent me-1" /> 
                                 </div>

                                 <div class="form-check">
                                   <asp:CheckBox ID="chkCarWay_Enable" runat="server" CssClass="form-check-input perm-toggle bg-transparent" />
                                   <label class="form-check-label" for="chkCarWay_Enable">Car Way</label>
                                 </div>
                                 <div runat="server" id="permCarWay" class="permission-options ms-4 mb-2 d-inline">
                                   <asp:RadioButton ID="rdoCarWay_View" runat="server" GroupName="CarWay" Text="View" CssClass="form-check-input bg-transparent me-1" /> 
                                   <asp:RadioButton ID="rdoCarWay_Edit" runat="server" GroupName="CarWay" Text="Edit" CssClass="form-check-input bg-transparent me-1" /> 
                                   <asp:RadioButton ID="rdoCarWay_Admin" runat="server" GroupName="CarWay" Text="Admin" CssClass="form-check-input bg-transparent me-1" /> 
                                   <asp:RadioButton ID="rdoCarWay_Super" runat="server" GroupName="CarWay" Text="Super" CssClass="form-check-input bg-transparent me-1" /> 
                                 </div>

                                 <div class="form-check">
                                   <asp:CheckBox ID="chkReorderQuantity_Enable" runat="server" CssClass="form-check-input perm-toggle bg-transparent" />
                                   <label class="form-check-label" for="chkReorderQuantity_Enable">Reorder Quantity</label>
                                 </div>
                                 <div runat="server" id="permReorder" class="permission-options ms-4 mb-2 d-inline">
                                   <asp:RadioButton ID="rdoReorderQuantity_View" runat="server" GroupName="ReorderQuantity" Text="View" CssClass="form-check-input bg-transparent me-1" /> 
                                   <asp:RadioButton ID="rdoReorderQuantity_Edit" runat="server" GroupName="ReorderQuantity" Text="Edit" CssClass="form-check-input bg-transparent me-1" />
                                   <asp:RadioButton ID="rdoReorderQuantity_Admin" runat="server" GroupName="ReorderQuantity" Text="Admin" CssClass="form-check-input bg-transparent me-1" />
                                   <asp:RadioButton ID="rdoReorderQuantity_Super" runat="server" GroupName="ReorderQuantity" Text="Super" CssClass="form-check-input bg-transparent me-1" /> 
                                 </div>

                                 <div class="form-check">
                                   <asp:CheckBox ID="chkConsignList_Enable" runat="server" CssClass="form-check-input perm-toggle bg-transparent" />
                                   <label class="form-check-label" for="chkConsignList_Enable">Consignment List</label>
                                 </div>
                                 <div runat="server" id="permConsign" class="permission-options ms-4 mb-2 d-inline">
                                   <asp:RadioButton ID="rdoConsignList_View" runat="server" GroupName="ConsignmentList" Text="View" CssClass="form-check-input bg-transparent me-1" /> 
                                   <asp:RadioButton ID="rdoConsignList_Edit" runat="server" GroupName="ConsignmentList" Text="Edit" CssClass="form-check-input bg-transparent me-1" />
                                   <asp:RadioButton ID="rdoConsignList_Admin" runat="server" GroupName="ConsignmentList" Text="Admin" CssClass="form-check-input bg-transparent me-1" />
                                   <asp:RadioButton ID="rdoConsignList_Super" runat="server" GroupName="ConsignmentList" Text="Super" CssClass="form-check-input bg-transparent me-1" /> 
                                 </div>

                                <div class="form-check">
                                  <asp:CheckBox ID="chkTrainingList_Enable" runat="server" CssClass="form-check-input perm-toggle bg-transparent" />
                                  <label class="form-check-label" for="chkTrainingList_Enable">Training List</label>
                                </div>
                                <div runat="server" id="permTraining" class="permission-options ms-4 mb-2 d-inline">
                                  <asp:RadioButton ID="rdoTrainingList_View" runat="server" GroupName="TrainingList" Text="View" CssClass="form-check-input bg-transparent me-1" /> 
                                  <asp:RadioButton ID="rdoTrainingList_Edit" runat="server" GroupName="TrainingList" Text="Edit" CssClass="form-check-input bg-transparent me-1" />
                                  <asp:RadioButton ID="rdoTrainingList_Admin" runat="server" GroupName="TrainingList" Text="Admin" CssClass="form-check-input bg-transparent me-1" />
                                  <asp:RadioButton ID="rdoTrainingList_Super" runat="server" GroupName="TrainingList" Text="Super" CssClass="form-check-input bg-transparent me-1" /> 
                                </div>

                                 </div>
                               </EditItemTemplate>
                                 <HeaderStyle ForeColor="White" BackColor="Gray" />
                                 <ItemStyle HorizontalAlign="Justify" />
                             </asp:TemplateField>
        
                         <asp:CommandField ShowEditButton="true" ButtonType="Button" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1"
                             ControlStyle-CssClass="btn btn-sm m-1 text-white" ControlStyle-BackColor="#188fa6" ItemStyle-CssClass="fixed-column-1" HeaderStyle-BackColor="Gray" HeaderStyle-ForeColor="White" />
                         <asp:CommandField ShowDeleteButton="true" ButtonType="Button" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1"
                             ControlStyle-CssClass="btn btn-sm m-1 btn-danger" ItemStyle-CssClass="fixed-column-1" HeaderStyle-BackColor="Gray" HeaderStyle-ForeColor="White" />
                     </Columns>
                 </asp:GridView>
              </ContentTemplate>
            </asp:UpdatePanel>
            
        </div>

</asp:Content>
