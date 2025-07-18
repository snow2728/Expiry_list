<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" EnableViewState="true"  Inherits="Expiry_list.CarWay.dash2" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <script type="text/javascript">
        function getAvailableToteBoxes() {
            return JSON.parse('<%= new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(GetAvailableToteBoxes()) %>');
        }

        const storeOptions = <%= new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(
            lstStoreFilter1.Items.Cast<ListItem>().Select(i => new { Value = i.Value, Text = i.Text }).ToList()
        ) %>;

        let availableToteBoxes = [];
        let committedToteBoxes = {};

        // active tote boxes
        let activeToteBoxes = new Set();

        $(document).ready(function () {
            
            availableToteBoxes = JSON.parse('<%= new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(GetAvailableToteBoxes()) %>');

            // Initialize Select2 controls
            $('.store-select').select2({
                placeholder: "Select destination stores",
                closeOnSelect: false,
                width: '100%',
                allowClear: true
            });

            $('.tote-box-select').select2({
                placeholder: "Select Tote box No",
                closeOnSelect: false,
                width: '100%',
                allowClear: true
            });

            $('<%= btnSave.ClientID %>').click(function (e) {
                if (!validateToteBoxCommitment()) {
                    e.preventDefault();
                    return false;
                }
                return true;
            });

            // Handle packing type change
            $(document).on('change', '.packing-type', function () {
                const $row = $(this).closest('tr');
                const selectedValue = $(this).val();
                const $toteBox = $row.find('.tote-box-select');
                const $setBtn = $row.find('.btn-set');

                if (selectedValue === "1") {
                    $toteBox.prop("disabled", false);
                    $setBtn.prop("disabled", false);
                    $toteBox.select2();
                } else {
                    $toteBox.val(null).trigger('change');
                    $toteBox.prop("disabled", true);
                    $setBtn.prop("disabled", true);
                }
            });

            // Set initial state
            $('.packing-type').each(function () {
                const $row = $(this).closest('tr');
                const $toteBox = $row.find('.tote-box-select');
                $toteBox.prop("disabled", $(this).val() !== "1");
            });

            // Handle Set button clicks
            $(document).on('click', '.btn-set', function () {
                const $row = $(this).closest('tr');
                const rowIndex = $row.index();
                const selectedToteIds = $row.find('.tote-box-select').val() || [];

                if (selectedToteIds.length === 0) {
                    Swal.fire({
                        icon: 'warning',
                        title: 'Validation Required',
                        text: 'Please select at least one tote box'
                    });
                    return;
                }

                const $button = $(this);
                $button.html('<i class="fas fa-spinner fa-spin"></i> Set');

                $.ajax({
                    type: "POST",
                    url: "dash2.aspx/UpdateToteBoxStatus",
                    data: JSON.stringify({ toteBoxIds: selectedToteIds }),
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    success: function (response) {
                        // FIX: Use response.d directly or define result properly
                        const result = response.d; // Add this line

                        if (result.success) {
                            // Add commitment indicator
                            $row.addClass('row-disabled');
                            $row.attr('data-committed', 'true');

                            committedToteBoxes[rowIndex] = selectedToteIds;

                            selectedToteIds.forEach(id => activeToteBoxes.add(id));

                            availableToteBoxes = availableToteBoxes.filter(tb =>
                                !selectedToteIds.includes(tb.Value)
                            );

                            Swal.fire({
                                icon: 'success',
                                title: 'Tote Boxes Committed',
                                text: 'Tote boxes have been successfully reserved'
                            });

                            // Disable the row inputs
                            disableRow($row);

                            refreshToteBoxDropdowns();
                        } else {
                            Swal.fire({
                                icon: 'error',
                                title: 'Update Failed',
                                text: result.message
                            });
                            $button.html('Set');
                        }
                    },
                    error: function (xhr, status, error) {
                        console.error("AJAX Error:", status, error);
                        Swal.fire({
                            icon: 'error',
                            title: 'Server Error',
                            text: 'Error updating tote boxes. Please try again.'
                        });
                        $button.html('Set');
                    }
                });
            });
        
            // Handle browser close/logout
            window.addEventListener('beforeunload', function(e) {
                if (activeToteBoxes.size > 0) {

                    const data = JSON.stringify(Array.from(activeToteBoxes));
                    const blob = new Blob([data], {type: 'application/json; charset=UTF-8'});
                    navigator.sendBeacon('dash2.aspx/RevertAllActiveToteBoxes', blob);
                }
            });
        });

        function disableRow($row) {
            // Disable inputs
            $row.find('.packing-type').prop('disabled', true);
            $row.find('.store-select').prop('disabled', true);
            $row.find('.quantity-input').prop('readonly', true);

            $row.find('.tote-box-select').prop('disabled', true);

            $row.addClass('row-disabled');
            $row.attr('data-committed', 'true'); 
        }

        function enableRow($row) {
            // Enable inputs
            $row.find('.packing-type').prop('disabled', false);
            $row.find('.store-select').prop('disabled', false);
            $row.find('.quantity-input').prop('readonly', false);

            const packingType = $row.find('.packing-type').val();
            if (packingType === "1") {
                $row.find('.tote-box-select').prop('disabled', false);
            }

            $row.removeClass('row-disabled');
            $row.removeAttr('data-committed');
        }

        function refreshToteBoxDropdowns() {
            $('.tote-box-select').each(function (rowIndex) {
                const $select = $(this);
                const currentValue = $select.val() || [];
                const isDisabled = $select.prop('disabled');

                $select.empty();

                const committedForRow = committedToteBoxes[rowIndex] || [];
                const combinedToteBoxes = [...availableToteBoxes];

                committedForRow.forEach(id => {
                    if (!combinedToteBoxes.some(tb => tb.Value === id)) {
                        combinedToteBoxes.push({ Value: id, Text: "Tote " + id });
                    }
                });

                combinedToteBoxes.forEach(item => {
                    $select.append(new Option(item.Text, item.Value));
                });

                $select.select2({
                    placeholder: "Select Tote Box No.",
                    closeOnSelect: false,
                    width: '100%',
                    allowClear: true
                });

                $select.prop('disabled', isDisabled);
                if (currentValue.length > 0) {
                    $select.val(currentValue).trigger('change');
                }
            });
        }

        function addNewRow(event) {
                event.preventDefault();

                if (!validateHeader()) {
                    return;
                }


                const gridBody = document.getElementById('gridBody');
                const rowCount = gridBody.querySelectorAll('tr').length;
                const newRowIndex = rowCount;

                const newRow = document.createElement('tr');
                newRow.className = 'grid-row text-center';
                newRow.innerHTML = `
                    <td class="text-center">
                        <input type="text" class="form-control" value="${rowCount + 1}" style="width:47px;" readonly>
                    </td>
                    <td>
                        <select class="form-select packing-type">
                            <option value="0" selected>Choose One Packing Type.</option>
                            <option value="1">Tote Box</option>
                            <option value="2">Plastic Bag</option>
                            <option value="3">Carton</option>
                            <option value="4">Other</option>
                        </select>
                    </td>
                    <td>
                        <div class="store-dropdown-wrapper">
                            <select class="store-select" multiple style="width: 100%;">
                                ${getStoreOptions()}
                            </select>
                        </div>
                    </td>
                    <td class="text-center">
                        <input type="text" class="form-control text-center quantity-input" style="width: 90px;" inputmode="numeric" oninput="this.value = this.value.replace(/[^0-9]/g, '')">
                    </td>
                    <td>
                        <div class="tote-box-dropdown-wrapper">
                            <select class="tote-box-select" multiple style="width: 100%;" disabled>
                                ${getToteBoxOptions()}
                            </select>
                        </div>
                    </td>
                    <td>
                        <button type="button" class="btn btn-sm btn-info text-white btn-set">Set</button>
                        <button type="button" class="btn btn-sm btn-danger text-white ms-1" onclick="deleteRow(this)">X</button>
                    </td>
                `;

                gridBody.appendChild(newRow);

                // Initialize Select2
                $(newRow).find('.store-select').select2({
                    placeholder: "Select destination stores",
                    closeOnSelect: false,
                    width: '100%',
                    allowClear: true
                });

                $(newRow).find('.tote-box-select').select2({
                    placeholder: "Select Tote Box No.",
                    closeOnSelect: false,
                    width: '100%',
                    allowClear: true
                });

                // Set initial disabled state
                $(newRow).find('.tote-box-select').prop('disabled', true);
            }

        function getToteBoxOptions() {
            let options = '';
            availableToteBoxes.forEach(item => {
                options += `<option value="${item.Value}">${item.Text}</option>`;
            });
            return options;
        }

        function collectGridData() {
            const gridData = [];
            $('#gridBody tr').each(function (index) {
                const $row = $(this);
                const stores = $row.find('.store-select').val() || [];
                const toteBoxes = $row.find('.tote-box-select').val() || [];

                gridData.push({
                    LineNumber: index + 1,
                    PackingType: $row.find('select.packing-type').val() || "",
                    Stores: stores,
                    Quantity: parseInt($row.find('input.quantity-input').val()) || 0,
                    ToteBoxNo: toteBoxes,
                    IsCommitted: $row.hasClass('row-disabled') // Track commitment status
                });
            });

            $('#<%= hdnGridData.ClientID %>').val(JSON.stringify(gridData));
            return true;
        }

        function getStoreOptions() {
            let options = '';
            storeOptions.forEach(item => {
                options += `<option value="${item.Value}">${item.Text}</option>`;
            });
            return options;
        }

        function validateDetailLines() {
            let hasLines = false;
            let hasUncommittedTote = false;
            let invalidRows = [];

            $('#gridBody tr').each(function (index) {
                const $row = $(this);

                // Check if row has any data
                const packingType = $row.find('.packing-type').val();
                const stores = $row.find('.store-select').val() || [];
                const quantity = $row.find('.quantity-input').val();
                const buttonText = $row.find('.btn-set').text().trim();

                if (packingType !== "0" || stores.length > 0 || quantity) {
                    hasLines = true;

                    // Check for uncommitted Tote Box
                    if (packingType === "1" && buttonText === "Set") {
                        hasUncommittedTote = true;
                        invalidRows.push(index + 1);
                    }
                }
            });

            if (!hasLines) {
                Swal.fire({
                    icon: 'error',
                    title: 'No Detail Lines',
                    text: 'Please add at least one detail line before saving.',
                    confirmButtonText: 'OK'
                });
                return false;
            }

            if (hasUncommittedTote) {
                Swal.fire({
                    icon: 'error',
                    title: 'Unconfirmed Tote Boxes',
                    html: `Rows with Tote Box packing type must be confirm first:<br>• Row ${invalidRows.join('<br>• Row ')}<br>Please click "Set" before saving.`,
                    confirmButtonText: 'OK'
                });
                return false;
            }

            return true;
        }

        function validateForSave() {
            if (!validateHeader()) {
                return false;
            }

            collectGridData(); // Ensure data is collected

            // Check if any detail lines exist
            const gridDataJson = $('#<%= hdnGridData.ClientID %>').val();
            if (!gridDataJson || gridDataJson === "[]") {
                Swal.fire({
                    icon: 'error',
                    title: 'No Detail Lines',
                    text: 'Please add at least one detail line before saving.',
                    confirmButtonText: 'OK'
                });
                return false;
            }

            try {
                const gridData = JSON.parse(gridDataJson);
                const hasData = gridData.some(row =>
                    row.PackingType !== "0" ||
                    (row.Stores && row.Stores.length > 0) ||
                    row.Quantity > 0);

                if (!hasData) {
                    Swal.fire({
                        icon: 'error',
                        title: 'No Valid Detail Lines',
                        text: 'Please add valid data to at least one detail line.',
                        confirmButtonText: 'OK'
                    });
                    return false;
                }

                // Check for uncommitted tote boxes
                const uncommittedToteRows = gridData
                    .filter(row => row.PackingType === "1" && !row.IsCommitted)
                    .map(row => row.LineNumber);

                if (uncommittedToteRows.length > 0) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Uncommitted Tote Boxes',
                        html: `Rows with Tote Box must be committed:<br>• ${uncommittedToteRows.join('<br>• ')}`,
                        confirmButtonText: 'OK'
                    });
                    return false;
                }
            } catch (e) {
                console.error("Validation error:", e);
                return false;
            }

            return true;
        }

        function deleteRow(btn) {
                event.preventDefault();
                const row = btn.closest('tr');
                const rowIndex = $(row).index();
                const committedTotes = committedToteBoxes[rowIndex] || [];

                // Revert ToteBox status if there are committed totes
                if (committedTotes.length > 0) {
                    $.ajax({
                        type: "POST",
                        url: "dash2.aspx/RevertToteBoxStatus",
                        data: JSON.stringify({ toteBoxIds: committedTotes }),
                        contentType: "application/json; charset=utf-8",
                        dataType: "json",
                        success: function (response) {
                            const result = response.d;
                            if (result.success) {
                                const toteItems = committedTotes.map(id => {
                                    return { Value: id, Text: "Tote " + id };
                                });

                                availableToteBoxes = [...availableToteBoxes, ...toteItems];

                                committedTotes.forEach(id => activeToteBoxes.delete(id));

                                refreshToteBoxDropdowns();

                                delete committedToteBoxes[rowIndex];

                                removeRow(row);
                            } else {
                                Swal.fire({
                                    icon: 'error',
                                    title: 'Revert Failed',
                                    text: result.message
                                });
                            }
                        },
                        error: function () {
                            Swal.fire({
                                icon: 'error',
                                title: 'Server Error',
                                text: 'Failed to revert tote box status.'
                            });
                        }
                    });
                } else {
                    removeRow(row);
                }
        }

        function validateHeader() {
            const wayType = document.getElementById('<%= ddlWayType.ClientID %>');
             const carNo = document.getElementById('<%= ddlCarNo.ClientID %>');
            const departureDate = document.getElementById('<%= txtDepDate.ClientID %>');
            const departureTime = document.getElementById('<%= txtDepTime.ClientID %>');
            const fromLocation = document.getElementById('<%= txtFLocation.ClientID %>');

            const errors = [];

            if (!wayType || wayType.value === "0") errors.push("Way Type");
            if (!carNo || carNo.value === "0") errors.push("Car Number");
            if (!departureDate || !departureDate.value.trim()) errors.push("Departure Date");
            if (!departureTime || !departureTime.value.trim()) errors.push("Departure Time");
            if (!fromLocation || !fromLocation.value.trim()) errors.push("From Location");

            if (errors.length > 0) {
                Swal.fire({
                    icon: 'error',
                    title: 'Missing Required Information',
                    html: `Please complete these header fields first:<br>• ${errors.join('<br>• ')}`
                });
                return false;
            }
            return true;
        }


        function removeRow(row) {
                if ($('#gridBody tr').length > 1) {
                    $(row).remove();

                    // Update row numbers and reindex committedToteBoxes
                    const newCommitted = {};
                    $('#gridBody tr').each(function (newIndex) {
                        const oldIndex = $(this).index();
                        if (committedToteBoxes[oldIndex]) {
                            newCommitted[newIndex] = committedToteBoxes[oldIndex];
                        }
                    });
                    committedToteBoxes = newCommitted;

                    // Update row numbers
                    $('#gridBody tr').each(function (index) {
                        $(this).find('td:first-child input').val(index + 1);
                    });
                } else {
                    Swal.fire({
                        icon: 'info',
                        title: 'Notice',
                        text: 'You need at least one row in the configuration.'
                    });
                }
        }

        function validateMultiSelect(sender, args) {
            const listbox = document.getElementById(sender.controltovalidate || sender.controlToValidate);
            if (listbox) {
                args.IsValid = Array.from(listbox.options).some(opt => opt.selected);
            } else {
                args.IsValid = false;
            }
        }

        function validateToteBoxCommitment() {
            let hasUncommittedTote = false;
            let invalidRows = [];

            $('#gridBody tr').each(function (index) {
                const $row = $(this);
                const packingType = $row.find('.packing-type').val();
                const buttonText = $row.find('.btn-set').text();

                // Check if it's a Tote Box row with uncommitted changes
                if (packingType === "1" && buttonText === "Set") {
                    hasUncommittedTote = true;
                    invalidRows.push(index + 1);
                }
            });

            if (hasUncommittedTote) {
                Swal.fire({
                    icon: 'error',
                    title: 'Uncommitted Tote Boxes',
                    html: `Rows with Tote Box packing type must be committed first:<br>• Row ${invalidRows.join('<br>• Row ')}<br>Please click "Set" before saving.`,
                    confirmButtonText: 'OK'
                });
                return false;
            }
            return true;
        }

    </script>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <asp:HiddenField ID="hdnGridData" runat="server" />
    
      <a href="../AdminDashboard.aspx" class="btn text-white ms-2" style="background-color: #996FD6;"><i class="fa-solid fa-left-long"></i> Home</a>
    <div class="container py-4">
        <a href="main1.aspx" class="btn text-white mb-2" style="background-color: #158396;">
            <i class="fa-solid fa-left-long"></i> Home
        </a>

        <!-- Header Section -->
        <div class="header-section p-4 mb-4 rounded-2" style="background: gray;">
            <div class="d-flex flex-column flex-md-row justify-content-between align-items-md-center mb-4">
                <div>
                  <asp:Button ID="btnSave" runat="server" Text="Save" 
                    OnClientClick="return validateForSave();" 
                    OnClick="btnSave_Click" CssClass="btn btn-primary" />
                </div>
            </div>

            <div class="row g-3">
                <div class="col-md-3">
                    <label class="form-label text-white">Way No.</label>
                    <asp:TextBox ID="txtWayNo" runat="server" CssClass="form-control bg-white text-black border-light" 
                        Text="" ReadOnly="true" />
                </div>
                <div class="col-md-3">
                    <label class="form-label text-white">Way Type</label>
                    <asp:DropDownList ID="ddlWayType" runat="server" CssClass="form-select bg-white text-black border-light">
                        <asp:ListItem Text="Choose One Way Type" Value="0" Selected="True" />
                        <asp:ListItem Text="DC To Store" Value="DC To Store" />
                        <asp:ListItem Text="Store To Store" Value="Store To Store" />
                        <asp:ListItem Text="Department To Department" Value="Department To Department" />
                    </asp:DropDownList>
                    <asp:RequiredFieldValidator ID="rfvWayType" runat="server" ControlToValidate="ddlWayType"
                         ErrorMessage="Way Type is required." Display="Dynamic" 
                        CssClass="text-danger" SetFocusOnError="True" />
                </div>
                <div class="col-md-3">
                    <label class="form-label text-white">Car No.</label>
                    <asp:DropDownList ID="ddlCarNo" runat="server" CssClass="form-select bg-white text-black border-light" Style="width: 100%;">
                    </asp:DropDownList>
                     <asp:RequiredFieldValidator ID="rfvCarNo" runat="server" ControlToValidate="ddlCarNo"
                         ErrorMessage="Car No is required." Display="Dynamic" CssClass="text-danger" SetFocusOnError="True" />
                </div>
                <div class="col-md-3">
                    <label class="form-label text-white">Created Date</label>
                   <asp:TextBox ID="txtCreatedDate" runat="server" TextMode="Date" CssClass="form-control bg-white text-black border-light" />
                </div>
                <div class="col-md-3">
                    <label class="form-label text-white">Driver Name</label>
                    <asp:TextBox ID="txtDriverName" runat="server" CssClass="form-control bg-white text-black border-light" />
                    <asp:RequiredFieldValidator ID="rfvDriverName" runat="server" ControlToValidate="txtDriverName"
                        ErrorMessage="Driver Name is required." Display="Dynamic" CssClass="text-danger" SetFocusOnError="True" />
                </div>
                <div class="col-md-3">
                    <label class="form-label text-white">Departure Date</label>
                    <asp:TextBox ID="txtDepDate" runat="server" TextMode="Date" CssClass="form-control bg-white text-black border-light" />
                     <asp:RequiredFieldValidator ID="rvfDepDate" runat="server" ControlToValidate="txtDepDate"
                         ErrorMessage="Departure Date is required." Display="Dynamic" CssClass="text-danger" SetFocusOnError="True" />
                </div>
                <div class="col-md-3">
                    <label class="form-label text-white">Departure Time</label>
                    <asp:TextBox ID="txtDepTime" runat="server" TextMode="Time" CssClass="form-control bg-white text-black border-light" />
                    <asp:RequiredFieldValidator ID="rvfDepTime" runat="server" ControlToValidate="txtDepTime"
                       ErrorMessage="Departure Time is required." Display="Dynamic" CssClass="text-danger" SetFocusOnError="True" />
                </div>
                <div class="col-md-3">
                    <label class="form-label text-white">From Location</label>
                    <asp:TextBox ID="txtFLocation" runat="server" CssClass="form-control bg-white text-black border-light" />
                    <asp:RequiredFieldValidator ID="rvfFlocation" runat="server" ControlToValidate="txtFLocation"
                       ErrorMessage="Location is required." Display="Dynamic" CssClass="text-danger" SetFocusOnError="True" />
                </div>
                <div class="col-md-1">
                    <label class="form-label text-white">Transit.</label>
                    <asp:CheckBox ID="chkTransit" runat="server" CssClass="form-check-input" />
                </div>
            </div>
        </div>

        <!-- Detail Section -->
        <div class="card border-0 shadow mb-4">
            <div class="card-header bg-white py-3 border-0">
                <div class="d-flex justify-content-between align-items-center">
                    <h3 class="mb-0 text-info"><i class="fas fa-list me-2"></i> Lines</h3>
                    <button type="button" class="btn px-4 action-btn text-white" 
                        style="background: #1995AD;" onclick="addNewRow(event)">
                        <i class="fas fa-plus me-2"></i>Add Line
                    </button>
                </div>
            </div>
            <div class="card-body p-0">
                <div class="table-responsive">
                    <table class="table table-hover align-middle mb-0">
                        <thead class="table-light">
                            <tr>
                                <th style="width: 47px;" class="text-center">No.</th>
                                <th style="width: 250px;">Packing Type</th>
                                <th style="width: 290px;">Destination Store.</th>
                                <th style="width: 90px;" class="text-center">Qty.</th>
                                <th style="width:190px">ToteBox No.</th>
                                <th style="width: 100px;" class="text-center">Actions</th> 
                            </tr>
                        </thead>
                        <tbody id="gridBody" runat="server" ClientIDMode="Static">
                            <tr class="grid-row text-center">
                                <td class="text-center">
                                    <input type="text" class="form-control" style="width:47px;" value="1" readonly />
                                </td>
                                <td>
                                    <asp:DropDownList ID="ddlPackingType" runat="server" CssClass="form-select packing-type">
                                        <asp:ListItem Value="0" Text="Choose One Packing Type." Selected="True" />
                                        <asp:ListItem Value="1" Text="Tote Box" />
                                        <asp:ListItem Value="2" Text="Plastic Bag" />
                                        <asp:ListItem Value="3" Text="Carton" />
                                        <asp:ListItem Value="4" Text="Other" />
                                    </asp:DropDownList>
                                    <asp:RequiredFieldValidator ID="rfvPackingType" runat="server" ControlToValidate="ddlPackingType"
                                        InitialValue="0" ErrorMessage="Packing type is required." Display="Dynamic" CssClass="text-danger" />
                                </td>
                              <td>
                                    <div class="store-dropdown-wrapper">
                                        <asp:ListBox ID="lstStoreFilter1" runat="server" CssClass="form-control store-select"
                                            SelectionMode="Multiple" Style="width: 100%;">
                                        </asp:ListBox>
                                        <asp:CustomValidator ID="cvStoreFilter1" runat="server"
                                            ClientValidationFunction="validateMultiSelect"
                                            ErrorMessage="At least one store must be selected." Display="Dynamic" CssClass="text-danger" />
                                    </div>
                                </td>
                               <td class="text-center">
                                    <asp:TextBox ID="txtQuantity" runat="server" CssClass="form-control text-center quantity-input"
                                        TextMode="Number" onkeydown="return event.key.match(/[0-9]/) || ['Backspace','Delete','ArrowLeft','ArrowRight','Tab'].includes(event.key)"
                                        oninput="this.value = this.value.replace(/[^0-9]/g, '')"
                                        />
                                    <asp:RangeValidator ID="rvQuantity" runat="server" ControlToValidate="txtQuantity"
                                        MinimumValue="1" MaximumValue="10000" Type="Integer"
                                        ErrorMessage="Quantity must be between 1 and 10000." Display="Dynamic" CssClass="text-danger" />
                                </td>
                               <td>
                                    <div class="tote-box-dropdown-wrapper">
                                        <asp:ListBox ID="ddlToteBox1" runat="server" SelectionMode="Multiple"
                                            ClientIDMode="Static" CssClass="tote-box-select"
                                            Enabled="false" Width="100%">
                                        </asp:ListBox>
                                    </div>
                                </td>
                                <td>
                                    <button type="button" class="btn btn-sm btn-info text-white btn-set">Set</button>
                                    <button type="button" class="btn btn-sm btn-danger text-white ms-1" onclick="deleteRow(this)">X</button>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
        
        <!-- Status Bar -->
        <div class="d-flex flex-column flex-md-row justify-content-between align-items-center bg-white p-3 rounded shadow-sm">
            <div class="d-flex align-items-center mb-3 mb-md-0">
                <button class="btn btn-outline-primary btn-sm rounded-circle action-btn me-2">
                    <i class="fas fa-arrow-left"></i>
                </button>
                <span class="me-3 fw-medium">Record 1 of 1</span>
                <button class="btn btn-outline-primary btn-sm rounded-circle action-btn me-3">
                    <i class="fas fa-arrow-right"></i>
                </button>
            </div>
            <div class="d-flex align-items-center">
                <button class="btn btn-outline-secondary btn-sm rounded-circle action-btn me-2">
                    <i class="fas fa-chevron-left"></i>
                </button>
                <span class="mx-2 fw-medium">Page 1</span>
                <button class="btn btn-outline-secondary btn-sm rounded-circle action-btn">
                    <i class="fas fa-chevron-right"></i>
                </button>
            </div>
        </div>
        
        <!-- Status Label -->
        <div id="lblStatus" runat="server" class="alert alert-info mt-3"></div>
    </div>
</asp:Content>