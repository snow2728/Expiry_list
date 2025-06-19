<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="regeForm.aspx.cs" Inherits="Expiry_list.regeForm" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <script type="text/javascript">
        $(document).ready(function () {
            InitializeStoreFilter();
            InitializeGridEditStores();
            setupPermissionToggles();

            if (typeof (Sys) !== 'undefined') {
                Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
                    InitializeStoreFilter();
                    InitializeGridEditStores();
                    setupPermissionToggles();
                });
            }
        });

        document.addEventListener('DOMContentLoaded', function () {
            updateLocationPillsDisplay();

            const listBox = document.getElementById('<%= lstStoreFilter.ClientID %>');
            if (listBox) {
                 listBox.addEventListener('change', updateLocationPillsDisplay);
            }
        });

        function setupPermissionToggles() {
            const pairs = [
                { checkboxId: '<%= chkExpiryList_Enable.ClientID %>', sectionId: 'permissionExpiryList' },
                { checkboxId: '<%= chkNegativeInventory_Enable.ClientID %>', sectionId: 'permissionNegativeInventory' },
                { checkboxId: '<%= chkSystemSettings_Enable.ClientID %>', sectionId: 'permissionSystemSettings' },
                { checkboxId: '<%= chkCarWayPlan_Enable.ClientID %>', sectionId: 'permissionCarWayPlan' },
                { checkboxId: '<%= chkReorderQuantity_Enable.ClientID %>', sectionId: 'permissionReorderQuantity' }
            ];

            pairs.forEach(({ checkboxId, sectionId }) => {
                // Initialize visibility
                togglePermissionsById(checkboxId, sectionId);

                // Add change event
                const cb = document.getElementById(checkboxId);
                if (cb) {
                    cb.addEventListener("change", () => togglePermissionsById(checkboxId, sectionId));
                }
            });
        }

        // Updated toggle function
        function togglePermissionsById(checkboxId, targetDivId) {
            const checkbox = document.getElementById(checkboxId);
            const section = document.getElementById(targetDivId);

            if (checkbox && section) {
                // Toggle based on checkbox state
                section.style.display = checkbox.checked ? "flex" : "none";

                // Clear selections when hiding
                if (!checkbox.checked) {
                    clearPermissionRadios(section);
                }
            }
        }

        // Helper to clear radio selections
        function clearPermissionRadios(container) {
            const radios = container.querySelectorAll('input[type="radio"]');
            radios.forEach(radio => radio.checked = false);
        }

        function InitializeStoreFilter() {
            var $select = $('#<%= lstStoreFilter.ClientID %>');
            var allOptionId = "all";

            if (!$select.length) return;

            // Remove duplicate "All" options if exists
            $select.find('option').filter(function () {
                return $(this).val() === allOptionId;
            }).slice(1).remove();

            // Destroy any existing Select2 instance before reinitializing
            if ($select.hasClass("select2-hidden-accessible")) {
                $select.select2('destroy');
            }

            $select.select2({
                placeholder: "-- Select stores --",
                closeOnSelect: false,
                width: '80%',
                allowClear: true,
                dropdownParent: $(document.body),
                minimumResultsForSearch: 1,
                dropdownAutoWidth: true,
                escapeMarkup: function (m) { return m; },
                language: {
                    noResults: function () {
                        return "No stores found";
                    }
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

                    var selectedValues = $select.val() || [];
                    var isAll = data.id === allOptionId;
                    var hasAll = selectedValues.includes(allOptionId);
                    var isChecked = isAll ? hasAll : selectedValues.includes(data.id);
                    var isDisabled = hasAll && !isAll;

                    return $(
                        '<div class="select2-checkbox-option d-flex align-items-center">' +
                        '<input type="checkbox" class="select2-checkbox me-2" ' +
                        (isChecked ? 'checked ' : '') +
                        (isAll ? ' data-is-all="true"' : '') +
                        (isDisabled ? ' disabled' : '') + '>' +
                        '<div class="select2-text text-truncate' + (isAll ? ' fw-bold"' : '"') + '>' + data.text + '</div>' +
                        '</div>'
                    );
                },
                templateSelection: function () {
                    return ''; // Hide default selected text – handled by pills
                }
            });

            // Handle checkbox clicks
            $select.data('select2').$dropdown.off('click.select2Checkbox').on('click.select2Checkbox', '.select2-checkbox', function (e) {
                var $option = $(this).closest('.select2-results__option');
                var data = $option.data('data');
                if (data) {
                    var isAll = data.id === allOptionId;
                    var selected = $select.val() || [];

                    if (isAll) {
                        $select.val(this.checked ? [allOptionId] : []).trigger('change');
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

            // On selection change
            $select.off('change.select2Update').on('change.select2Update', function () {
                var values = $select.val() || [];
                var hasAll = values.includes(allOptionId);

                if (hasAll && values.length > 1) {
                    $select.val([allOptionId]).trigger('change');
                    return;
                } else {
                    var filtered = values.filter(v => v !== allOptionId);
                    if (filtered.length !== values.length) {
                        $select.val(filtered).trigger('change');
                    }
                }

                updateSelect2Checkboxes($select);
                updateLocationPillsDisplay();
            });

            // Prevent text selection clicks
            $select.on('select2:selecting select2:unselecting', function (e) {
                if (e.params?.args?.originalEvent &&
                    !$(e.params.args.originalEvent.target).hasClass('select2-checkbox')) {
                    e.preventDefault();
                }
            });

            // Clear all event
            $select.on('select2:clearing', function () {
                $select.data('select2').$dropdown.find('.select2-checkbox')
                    .prop('checked', false).prop('disabled', false);
                $select.trigger('input.select2');
            });

            // Initial render
            updateSelect2Checkboxes($select);
            updateLocationPillsDisplay();
        }

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
                    if (!data || !data.id) return;  // 🚫 Fix null 'id' access

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

                        // 🚫 Avoid triggering change if nothing changed
                        const newVal = selected.filter(v => v);
                        if (JSON.stringify(newVal.sort()) !== JSON.stringify((selected || []).sort())) {
                            $select.val(newVal).trigger('change');
                        } else {
                            $select.val(newVal); // silently set
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
            const listBox = document.getElementById('<%= lstStoreFilter.ClientID %>');
            if (listBox) {
                Array.from(listBox.options).forEach(option => {
                    option.selected = false;
                });
                updateLocationPillsDisplay();
            }
        }

        Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {

            InitializeStoreFilter();

        });

        history.pushState(null, null, location.href);
        window.addEventListener("popstate", function (event) {
            location.reload();
        });

    </script>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <a href="AdminDashboard.aspx" class="btn text-white ms-2" style="background-color : #158396;"><i class="fa-solid fa-left-long"></i> Home</a>

    <div class="container-fluid">
    <div class="row justify-content-center">
        <div class="col-lg-8">
            <div class="card shadow rounded-4 p-4">
                <div class="row g-3">
                    <!-- Username -->
                    <div class="col-md-6">
                        <label for="<%= usernameTextBox.ClientID %>" class="form-label">Username</label>
                        <asp:TextBox ID="usernameTextBox" runat="server"
                            CssClass="form-control border-info shadow-sm"
                            AutoFocus="true" AutoComplete="username" />
                        <asp:RequiredFieldValidator ID="usernameRequired" runat="server"
                            ControlToValidate="usernameTextBox"
                            ErrorMessage="Username is required!"
                            CssClass="text-danger small d-block mt-1"
                            Display="Dynamic" />
                    </div>

                    <!-- Password -->
                    <div class="col-md-6">
                        <label for="<%= passwordTextBox.ClientID %>" class="form-label">Password</label>
                        <asp:TextBox ID="passwordTextBox" runat="server"
                            TextMode="Password"
                            CssClass="form-control border-info shadow-sm"
                            AutoComplete="current-password" />
                        <asp:RequiredFieldValidator ID="passwordRequired" runat="server"
                            ControlToValidate="passwordTextBox"
                            ErrorMessage="Password is required!"
                            CssClass="text-danger small d-block mt-1"
                            Display="Dynamic" />
                    </div>
                </div>

                 <div class="row g-3">

                      <!-- Store Selector -->
                     <div class="col-md-6 mt-4">
                         <label for="<%= lstStoreFilter.ClientID %>" class="form-label me-3">Store(s)</label>
                         <asp:ListBox ID="lstStoreFilter" runat="server" CssClass="form-control d-none" SelectionMode="Multiple"></asp:ListBox>
                         <div id="locationPillsContainer" class="location-pills-container mb-2"></div>
                         <asp:RequiredFieldValidator ID="storeNoRequired" runat="server"
                             ControlToValidate="lstStoreFilter"
                             ErrorMessage="Store is required!"
                             CssClass="text-danger small d-block mt-1"
                             Display="Dynamic" />
                     </div>

                     <div class="col-md-6 mt-4">
                         <asp:CheckBox ID="chkEnable" runat="server" Text=" Enabled" />
                     </div>

                 </div>

                <!-- Form Permissions -->
                <div class="mt-4">
                    <label class="form-label fw-bold">Form Permissions</label>

                    <%-- Expiry List --%>
                    <div class="border border-info rounded p-3 mb-3">
                        <div class="form-check">
                            <asp:CheckBox ID="chkExpiryList_Enable" runat="server" CssClass="form-check-input" />
                            <label class="form-check-label" for="<%= chkExpiryList_Enable.ClientID %>">Expiry List</label>
                        </div>
                        <div id="permissionExpiryList" class="row g-3 ms-3 mt-2" style="display: none;">
                            <div class="col-auto form-check">
                                <asp:RadioButton ID="rdoExpiryList_View" GroupName="ExpiryList" runat="server" CssClass="form-check-input" />
                                <label class="form-check-label" for="<%= rdoExpiryList_View.ClientID %>">View</label>
                            </div>
                            <div class="col-auto form-check">
                                <asp:RadioButton ID="rdoExpiryList_Edit" GroupName="ExpiryList" runat="server" CssClass="form-check-input" />
                                <label class="form-check-label" for="<%= rdoExpiryList_Edit.ClientID %>">Edit</label>
                            </div>
                            <div class="col-auto form-check">
                                <asp:RadioButton ID="rdoExpiryList_Admin" GroupName="ExpiryList" runat="server" CssClass="form-check-input" />
                                <label class="form-check-label" for="<%= rdoExpiryList_Admin.ClientID %>">Admin</label>
                            </div>
                             <div class="col-auto form-check">
                                 <asp:RadioButton ID="rdoExpiryList_Super" GroupName="ExpiryList" runat="server" CssClass="form-check-input" />
                                 <label class="form-check-label" for="<%= rdoExpiryList_Super.ClientID %>">Super</label>
                             </div>
                        </div>
                    </div>

                    <!-- Negative Inventory -->
                    <div class="border border-info rounded p-3 mb-3">
                        <div class="form-check">
                            <asp:CheckBox ID="chkNegativeInventory_Enable" runat="server" CssClass="form-check-input" />
                            <label class="form-check-label" for="<%= chkNegativeInventory_Enable.ClientID %>"> Negative Inventory</label>
                        </div>
                        <div id="permissionNegativeInventory" class="row g-3 ms-3 mt-2" style="display: none;">
                            <div class="col-auto form-check">
                                <asp:RadioButton ID="rdoNegativeInventory_View" GroupName="NegativeInventory" runat="server" CssClass="form-check-input" />
                                <label class="form-check-label" for="<%= rdoNegativeInventory_View.ClientID %>">View</label>
                            </div>
                            <div class="col-auto form-check">
                                <asp:RadioButton ID="rdoNegativeInventory_Edit" GroupName="NegativeInventory" runat="server" CssClass="form-check-input" />
                                <label class="form-check-label" for="<%= rdoNegativeInventory_Edit.ClientID %>">Edit</label>
                            </div>
                            <div class="col-auto form-check">
                                <asp:RadioButton ID="rdoNegativeInventory_Admin" GroupName="NegativeInventory" runat="server" CssClass="form-check-input" />
                                <label class="form-check-label" for="<%= rdoNegativeInventory_Admin.ClientID %>">Admin</label>
                            </div>
                              <div class="col-auto form-check">
                                  <asp:RadioButton ID="rdoNegativeInventory_Super" GroupName="NegativeInventory" runat="server" CssClass="form-check-input" />
                                  <label class="form-check-label" for="<%= rdoNegativeInventory_Super.ClientID %>">Super</label>
                              </div>
                        </div>
                    </div>

                    <%-- System Settings --%>
                    <div class="border border-info rounded p-3 mb-3">
                        <div class="form-check">
                            <asp:CheckBox ID="chkSystemSettings_Enable" runat="server" CssClass="form-check-input" />
                            <label class="form-check-label" for="<%= chkSystemSettings_Enable.ClientID %>">System Setting</label>
                        </div>
                        <div id="permissionSystemSettings" class="row g-3 ms-3 mt-2" style="display: none;">
                            <div class="col-auto form-check">
                                <asp:RadioButton ID="rdoSystemSettings_View" GroupName="SystemSettings" runat="server" CssClass="form-check-input" />
                                <label class="form-check-label" for="<%= rdoSystemSettings_View.ClientID %>">View</label>
                            </div>
                            <div class="col-auto form-check">
                                <asp:RadioButton ID="rdoSystemSettings_Edit" GroupName="SystemSettings" runat="server" CssClass="form-check-input" />
                                <label class="form-check-label" for="<%= rdoSystemSettings_Edit.ClientID %>">Edit</label>
                            </div>
                            <div class="col-auto form-check">
                                <asp:RadioButton ID="rdoSystemSettings_Admin" GroupName="SystemSettings" runat="server" CssClass="form-check-input" />
                                <label class="form-check-label" for="<%= rdoSystemSettings_Admin.ClientID %>">Admin</label>
                            </div>
                             <div class="col-auto form-check">
                                 <asp:RadioButton ID="rdoSystemSettings_Super" GroupName="SystemSettings" runat="server" CssClass="form-check-input" />
                                 <label class="form-check-label" for="<%= rdoSystemSettings_Super.ClientID %>">Super</label>
                             </div>
                        </div>
                    </div>

                    <%-- Car Way --%>
                     <div class="border border-info rounded p-3 mb-3">
                         <div class="form-check">
                             <asp:CheckBox ID="chkCarWayPlan_Enable" runat="server" CssClass="form-check-input" />
                             <label class="form-check-label" for="<%= chkCarWayPlan_Enable.ClientID %>">Car Way</label>
                         </div>

                         <div id="permissionCarWayPlan" class="row g-3 ms-3 mt-2" style="display: none;">
                             <div class="col-auto form-check">
                                 <asp:RadioButton ID="rdoCarWayPlan_View" GroupName="CarWayPlan" runat="server" CssClass="form-check-input" />
                                 <label class="form-check-label" for="<%= rdoCarWayPlan_View.ClientID %>">View</label>
                             </div>
                             <div class="col-auto form-check">
                                 <asp:RadioButton ID="rdoCarWayPlan_Edit" GroupName="CarWayPlan" runat="server" CssClass="form-check-input" />
                                 <label class="form-check-label" for="<%= rdoCarWayPlan_Edit.ClientID %>">Edit</label>
                             </div>
                             <div class="col-auto form-check">
                                 <asp:RadioButton ID="rdoCarWayPlan_Admin" GroupName="CarWayPlan" runat="server" CssClass="form-check-input" />
                                 <label class="form-check-label" for="<%= rdoCarWayPlan_Admin.ClientID %>">Admin</label>
                             </div>
                               <div class="col-auto form-check">
                                  <asp:RadioButton ID="rdoCarWayPlan_Super" GroupName="CarWayPlan" runat="server" CssClass="form-check-input" />
                                  <label class="form-check-label" for="<%= rdoCarWayPlan_Super.ClientID %>">Super</label>
                              </div>
                         </div>
                     </div>

                    <%-- Reorder Quantity --%>
                     <div class="border border-info rounded p-3 mb-3">
                             <div class="form-check">
                                 <asp:CheckBox ID="chkReorderQuantity_Enable" runat="server" CssClass="form-check-input" />
                                 <label class="form-check-label" for="<%= chkReorderQuantity_Enable.ClientID %>">Reorder Quantity</label>
                             </div>

                             <div id="permissionReorderQuantity" class="row g-3 ms-3 mt-2" style="display: none;">
                                 <div class="col-auto form-check">
                                     <asp:RadioButton ID="rdoReorderQuantity_View" GroupName="ReorderQuantity" runat="server" CssClass="form-check-input" />
                                     <label class="form-check-label" for="<%= rdoReorderQuantity_View.ClientID %>">View</label>
                                 </div>
                                 <div class="col-auto form-check">
                                     <asp:RadioButton ID="rdoReorderQuantity_Edit" GroupName="ReorderQuantity" runat="server" CssClass="form-check-input" />
                                     <label class="form-check-label" for="<%= rdoReorderQuantity_Edit.ClientID %>">Edit</label>
                                 </div>
                                 <div class="col-auto form-check">
                                     <asp:RadioButton ID="rdoReorderQuantity_Admin" GroupName="ReorderQuantity" runat="server" CssClass="form-check-input" />
                                     <label class="form-check-label" for="<%= rdoReorderQuantity_Admin.ClientID %>">Admin</label>
                                 </div>
                                 <div class="col-auto form-check">
                                    <asp:RadioButton ID="rdoReorderQuantity_Super" GroupName="ReorderQuantity" runat="server" CssClass="form-check-input" />
                                    <label class="form-check-label" for="<%= rdoReorderQuantity_Super.ClientID %>">Super</label>
                                </div>
                             </div>
                         </div>

                        <!-- Register Button -->
                        <div>
                            <asp:Button ID="Button1" runat="server" Text="Save" 
                                OnClick="btnRegister_Click" OnClientClick="return validateStoreSelection();" 
                                CssClass="btn btn-primary btn-md fw-bold shadow-sm" 
                                style="background-color: #158396; border-color: #127485;" />
                        </div>
                    </div>
                </div>
             </div>
        </div>
    </div>

</asp:Content>
