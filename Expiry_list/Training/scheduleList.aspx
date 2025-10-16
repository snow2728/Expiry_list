<%@ Page Title="Schedule List" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="scheduleList.aspx.cs" Inherits="Expiry_list.Training.scheduleList" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">

    <%
        var permissions = Session["formPermissions"] as Dictionary<string, string>;
        string expiryPerm = permissions != null && permissions.ContainsKey("TrainingList") ? permissions["TrainingList"] : "";
    %>
     <script type="text/javascript">
         var expiryPermission = '<%= expiryPerm %>';
     </script>

<script type="text/javascript">
    var selectedItems = [];

    $(document).ready(function () {
        initializeDataTableMain();
        // Reinitialize DataTables
        if (typeof (Sys) !== 'undefined') {
            Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
                initializeDataTableMain();
            });
        }

        const inputBox = $("#traineeMultiSelect .multi-select-input");
        const dropdown = $("#traineeMultiSelect .multi-select-dropdown");
        const searchInput = $('<input type="text" class="dropdown-search" placeholder="Search trainees...">');
        dropdown.append(searchInput);

        function loadTrainees(searchTerm) {
            dropdown.find('div[data-value]').remove();
            $.ajax({
                url: 'scheduleList.aspx/GetTrainee',
                type: 'POST',
                contentType: 'application/json; charset=utf-8',
                dataType: 'json',
                data: JSON.stringify({
                    searchTerm: searchTerm || "",
                    positionId: window.currentSchedulePositionId || 0
                }),
                success: function (response) {
                    const list = (response && response.d) ? response.d : [];
                    dropdown.find('.no-result').remove();
                    if (list.length === 0) {
                        dropdown.append('<div class="no-result text-muted px-2 py-1">No trainees found</div>');
                    } else {
                        list.forEach(t => {
                            dropdown.append(
                                `<div data-value="${t.Id}" class="trainee-item px-2 py-1">
                                    <strong>${t.Name}</strong>
                                    <div class="small text-muted">${t.Position || '-'} · ${t.Store || '-'}</div>
                                </div>`
                            );
                        });
                    }
                },
                error: function (xhr, status, error) {
                    console.error("AJAX Error:", status, error, xhr.responseText);
                }
            });
        }

        inputBox.on("click", function (e) {
            e.stopPropagation();
            dropdown.toggle();
            if (dropdown.is(":visible")) {
                searchInput.focus();
                loadTrainees("");
            }
        });

        searchInput.on("keyup", function () { loadTrainees($(this).val()); });

        $(document).on("click", "#traineeMultiSelect .multi-select-dropdown div[data-value]", function () {
            const value = $(this).data("value");
            const text = $(this).find("strong").text();
            if (!selectedItems.find(x => x.id === value)) selectedItems.push({ id: value, text: text });
            updateInput();
            searchInput.val('');
        });

        inputBox.on("click", ".remove-pill", function (e) {
            e.stopPropagation();
            selectedItems = selectedItems.filter(v => v.id !== $(this).data("value"));
            updateInput();
            dropdown.show();
            searchInput.focus();
        });

        function updateInput() {
            inputBox.html('');
            if (selectedItems.length === 0) {
                inputBox.append('<input type="text" class="placeholder" placeholder="Select trainees..." readonly />');
            } else {
                selectedItems.forEach(item => {
                    inputBox.append(`<span class="pill">${item.text} 
                        <span class="remove-pill" data-value="${item.id}" style="cursor:pointer;">&times;</span></span>`);
                });
                inputBox.append('<input type="text" style="width:2px;border:none;outline:none;" readonly />');
            }
            $("#<%= hfSelectedTrainees.ClientID %>").val(JSON.stringify(selectedItems));
        }

        $(document).click(e => { if (!$(e.target).closest("#traineeMultiSelect").length) dropdown.hide(); });
        updateInput();

        window.openRegisterModal = function (scheduleId, topicId, positionId) {
            $("#<%= hfScheduleId.ClientID %>").val(scheduleId);
            $("#<%= hfTopicId.ClientID %>").val(topicId);
            $("#<%= hfSelectedTrainees.ClientID %>").val("[]");
            window.currentSchedulePositionId = positionId;
            selectedItems = [];
            updateInput();
            $("#traineeMultiSelect .multi-select-dropdown").hide();

            new bootstrap.Modal(document.getElementById("registerModal")).show();
        };

        window.openTraineeDetails = function (scheduleId) {
            $("#<%= hfSelectedScheduleId.ClientID %>").val(scheduleId);
            __doPostBack('<%= btnLoadTrainees.UniqueID %>', '');
            new bootstrap.Modal(document.getElementById("traineeDetailsModal")).show();
        };

        $('#traineeDetailsModal').on('shown.bs.modal', function () {
            initializeDataTableTrainees();
        });
    });


    //For Note Preview
    $(document).on('click', '.truncated-note', function (e) {
        e.preventDefault();

        var fullNote = $(this).data('fullnote');
        $('#noteModal .modal-body').text(fullNote);

        var modal = new bootstrap.Modal(document.getElementById('noteModal'));
        modal.show();
    });

    document.addEventListener('DOMContentLoaded', function () {
        document.getElementById("link_home").href = "../AdminDashboard.aspx";
    });

    function getRemarkValue(cell) {
        // Check for input first
        const input = cell.find('input');
        if (input.length) return input.val().trim();

        // Check for span or inner content from DataTables render
        const span = cell.find('span.truncated-note');
        if (span.length) return span.data('fullnote') || span.text().trim();

        // fallback
        return cell.text().trim();
    }

    function showCancelSweetAlert(linkBtn) {
        const row = $(linkBtn).closest("tr");
        const dataTable = $('#<%= GridView2.ClientID %>').DataTable();

        const dtRow = dataTable.row(row);
        const rowIndex = dtRow.index();

        const remarkColIndex = dataTable.columns().header().toArray().findIndex(
            th => $(th).text().trim().toLowerCase() === 'reason'
        );

        const remarkCell = dataTable.cell(rowIndex, remarkColIndex);
        const remark = $(remarkCell.node()).text().trim();

        //console.log('Rendered remark:', remark);

        if (!remark) {
            Swal.fire({
                icon: 'error',
                title: 'Cancel Reason Required',
                text: 'Please fill in the cancel reason before cancelling this schedule.',
                confirmButtonColor: '#d33'
            });
            return false;
        }

        Swal.fire({
            title: 'Cancel Schedule?',
            text: 'Are you sure you want to cancel this schedule? </br> This schedule might have registered trainees.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Yes, Cancel it!',
            cancelButtonText: 'No, Keep it',
            confirmButtonColor: '#d33',
            cancelButtonColor: '#3085d6',
            reverseButtons: true,
            focusCancel: true
        }).then((result) => {
            if (result.isConfirmed) {
                const currentRemark = $(dataTable.cell(rowIndex, remarkColIndex).node()).text().trim();
                if (!currentRemark) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Cannot Cancel',
                        text: 'Schedule cannot be cancelled without reason. Please enter reason in the cancel reason column.',
                        confirmButtonColor: '#d33'
                    });
                    return false;
                }

                linkBtn.onclick = null;
                linkBtn.click();
            }
        });

        return false;
    }

    function initializeDataTableMain() {
        const grid = $("#<%= GridView2.ClientID %>");
        if (!grid.length) return;

        if ($.fn.DataTable.isDataTable(grid)) grid.DataTable().destroy();

        if (grid.find('thead').length === 0) {
            const headerRow = grid.find('tr:first').detach();
            grid.prepend($('<thead/>').append(headerRow));
        }

        const headerCols = grid.find("thead tr th").length;
        const bodyCols = grid.find("tbody tr:first td").length;
        if (headerCols !== bodyCols) {
            console.warn(`Column mismatch: header=${headerCols}, body=${bodyCols}`);
            return;
        }

        const dataTable = grid.DataTable({
            responsive: false,
            paging: true,
            searching: true,
            info: true,
            autoWidth: false,
            stateSave: true,
            scrollX: true,
            scrollY: '57vh',
            scrollCollapse: true,
            order: [[6, 'desc']],
            lengthMenu: [[25, 50, 100], [25, 50, 100]],
            columnDefs: [
                { orderable: false, targets: [6, 7, 8, 9, 10].filter(i => i < headerCols) },
                { targets: [1, 2], visible: false },
                { targets: '_all', orderSequence: ["asc", "desc", ""] }
            ],
            initComplete: function () {
                this.api().columns.adjust();
            }
        });

        enableRemarkEditing(dataTable);
    }

    function enableRemarkEditing(dataTable) {
        const gridSelector = '#<%= GridView2.ClientID %>';

        $(gridSelector + ' tbody').off('dblclick').on('dblclick', 'td', function () {
            const cell = dataTable.cell(this);
            if (!cell.any()) return;

            const colIndex = cell.index().column;
            const columnHeader = $(dataTable.column(colIndex).header()).text().trim().toLowerCase();

            const isRemark = columnHeader.includes('remark');
            const isReason = columnHeader.includes('reason');
            if (!isRemark && !isReason) return;

            const targetColumn = isRemark ? 'remark' : 'reason';

            const oldValue = $(cell.node()).text().trim();
            if ($(this).find('input').length > 0) return;

            const editor = $('<input type="text" class="form-control form-control-sm" style="width:100%;">')
                .val(oldValue);

            $(cell.node()).empty().append(editor);
            editor.focus().select();

            const saveEdit = function () {
                const newValue = editor.val().trim();

                if (newValue === oldValue) {
                    $(cell.node()).text(oldValue);
                    return;
                }

                const row = $(cell.node()).closest('tr');
                const rowId = row.data('id');

                if (!rowId) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Error',
                        text: 'Could not identify the record. Please refresh and try again.',
                        confirmButtonColor: '#d33'
                    });
                    $(cell.node()).text(oldValue);
                    return;
                }

                $.ajax({
                    type: "POST",
                    url: "scheduleList.aspx/UpdateColumnValue",
                    contentType: "application/json; charset=utf-8",
                    data: JSON.stringify({
                        id: parseInt(rowId),
                        column: targetColumn,  
                        value: newValue
                    }),
                    success: function (response) {
                        if (response.d === "Success") {
                            cell.data(newValue).draw();
                            Swal.fire({
                                icon: 'success',
                                title: 'Updated!',
                                text: `${targetColumn.charAt(0).toUpperCase() + targetColumn.slice(1)} has been updated successfully.`,
                                timer: 1200,
                                showConfirmButton: false
                            });
                        } else {
                            $(cell.node()).text(oldValue);
                            Swal.fire({
                                icon: 'error',
                                title: 'Error',
                                text: 'Failed to update: ' + response.d,
                                confirmButtonColor: '#d33'
                            });
                        }
                    },
                    error: function (xhr, status, error) {
                        $(cell.node()).text(oldValue);
                        Swal.fire({
                            icon: 'error',
                            title: 'Error',
                            text: 'Failed to update. Please try again. Error: ' + error,
                            confirmButtonColor: '#d33'
                        });
                    }
                });
            };

            editor.on('blur', saveEdit);
            editor.on('keydown', function (e) {
                if (e.key === 'Enter') saveEdit();
                else if (e.key === 'Escape') $(cell.node()).text(oldValue);
            });
        });
    }

    function initializeDataTableTrainees() {
        const table = $('#gvScheduleTrainees');
        if (table.length === 0 || table.find('tr').length === 0) return;

        if ($.fn.DataTable.isDataTable(table)) {
            table.DataTable().destroy();
            table.find('thead').remove();
        }

        if (table.find('thead').length === 0) {
            const headerRow = table.find('tr:first').detach();
            table.prepend($('<thead/>').append(headerRow));
        }

        table.DataTable({
            responsive: false,
            paging: true,
            searching: true,
            ordering: true,
            autoWidth: false,
            scrollX: true,
            scrollY: '50vh',
            scrollCollapse: true,
            columnDefs: [
                { orderable: false, targets: [-1] },
                { targets: '_all', orderSequence: ["asc", "desc", ""] }
            ],
            initComplete: function () {
                this.api().columns.adjust();
            }
        });
    }

    $('#traineeDetailsModal').on('shown.bs.modal', function () {
        initializeDataTableTrainees();
    });

    function highlightRow(btn) {
        $('#<%= GridView2.ClientID %> tbody tr').removeClass('selected-row');

           var row = $(btn).closest('tr');
           row.addClass('selected-row');
    }

</script>

    <style>
        table.dataTable tbody td:first-child,
        table.dataTable thead th:first-child {
            text-align: left !important;
            vertical-align: middle !important; 
        }

        .dataTables_scrollHead {
            overflow: visible !important;
        }

        table.dataTable thead th {
            background-color: #4486ab !important;
            color: white !important;
            position: sticky;
            top: 0;
            z-index: 10;
        }

        .dataTables_wrapper .dataTables_scrollBody {
            border-bottom: 1px solid #ddd;
        }

        .selected-row {
            background-color: #dfebf1 !important;
        }

    </style>

</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePageMethods="true" />
    <div class="container-fluid mt-3">

        <div class="card mb-3 shadow-sm">
            <div class="card-body">
                <div class="d-flex justify-content-between align-items-center mb-3">
                    <h4 class="fw-bold mb-0">Schedule List</h4>
                </div>
                <div class="row g-2">
                    <div class="col-md-2">
                        <asp:TextBox ID="dateTb" runat="server" CssClass="form-control form-control-sm" TextMode="Month" />
                    </div>
                    <div class="col-md-2">
                        <asp:DropDownList ID="levelDp" runat="server" CssClass="form-select form-select-sm" AppendDataBoundItems="True">
                            <asp:ListItem Text="Select Level" Value="" />
                        </asp:DropDownList>
                    </div>
                    <div class="col-md-2">
                        <asp:DropDownList ID="topicName" runat="server" CssClass="form-select form-select-sm" AppendDataBoundItems="True" />

                    </div>
                    <div class="col-md-2">
                        <asp:DropDownList ID="locationDp" runat="server" CssClass="form-select form-select-sm">
                        <asp:ListItem Text="Select Location" Value="" />
                        </asp:DropDownList>
                    </div>
                    <div class="col-md-2">
                        <asp:DropDownList ID="trainerDp" runat="server" CssClass="form-select form-select-sm" AppendDataBoundItems="True">
                        </asp:DropDownList>
                    </div>
                    <div class="col-md-2 d-flex gap-2">
                        <asp:Button ID="showBtn" runat="server" Text="Show" CausesValidation="false" OnClick="showBtn_Click" CssClass="btn btn-info btn-sm text-white flex-fill" />
                        <asp:Button ID="resetBtn" runat="server" Text="Reset" OnClick="resetBtn_Click"  CausesValidation="false" CssClass="btn btn-danger btn-sm flex-fill" />
                    </div>
                </div>
            </div>
        </div>

        <%-- Gridview --%>
        <div class="card shadow-sm  gridview-container rounded-1" style="height:auto">
            <div class="card-body">
               <div class=" table-responsive gridview-container pt-2 pe-2 rounded-1">
                    <asp:GridView ID="GridView2" runat="server"
                       CssClass="table table-striped ResizableGrid table-hover border-2 shadow-lg sticky-grid overflow-x-auto display"
                        AutoGenerateColumns="False"
                        DataKeyNames="id"
                        OnRowDeleting="GridView2_RowDeleting"
                        OnRowCommand="GridView2_RowCommand"
                        OnRowDataBound="GridView2_RowDataBound"
                        AllowPaging="false"
                        ShowHeaderWhenEmpty="true"
                        HeaderStyle-BackColor="#4486ab"
                        HeaderStyle-ForeColor="White"
                        GridLines="None"
                        AllowSorting="false"
                        EmptyDataText="No records found.">

                        <HeaderStyle CssClass="text-left text-white" />

                        <Columns>

                           <asp:TemplateField Visible="false">
                                <ItemTemplate>
                                    <asp:HiddenField ID="hfId" runat="server" Value='<%# Eval("id") %>' />
                                </ItemTemplate>
                            </asp:TemplateField>

                           <asp:TemplateField HeaderText="Topic Name" ItemStyle-Width="18%" ItemStyle-CssClass="text-left" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
                                <ItemTemplate>
                                    <asp:Label ID="lblTopicName" runat="server" Text='<%# Eval("topicName") %>' />
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="Topic ID" ItemStyle-CssClass="d-none" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
                                <ItemTemplate>
                                    <asp:HiddenField ID="hfTopicId" runat="server" Value='<%# Eval("topicId") %>' />
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="Position ID" ItemStyle-CssClass="d-none" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
                                <ItemTemplate>
                                    <asp:HiddenField ID="hfPositionId" runat="server" Value='<%# Eval("positionId") %>' />
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="Room" ItemStyle-Width="10%" ItemStyle-CssClass="text-left" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
                                <ItemTemplate>
                                    <asp:Label ID="lblRoom" runat="server" Text='<%# Eval("room") %>' CssClass="d-block text-left" />
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="Trainer Name" ItemStyle-Width="10%" ItemStyle-CssClass="text-left" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
                                <ItemTemplate>
                                    <asp:Label ID="lblTrainerName" runat="server" Text='<%# Eval("trainerName") %>' CssClass="d-block text-left" />
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="Trainee Level" ItemStyle-Width="10%" ItemStyle-CssClass="text-left" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
                                <ItemTemplate>
                                    <asp:Label ID="lblTraineeLevel" runat="server" Text='<%# Eval("position") %>' CssClass="d-block text-left" />
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="Training Date" ItemStyle-Width="10%" ItemStyle-CssClass="text-left" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
                                <ItemTemplate>
                                    <asp:Label ID="lblDate" runat="server"
                                        Text='<%# Eval("date", "{0:dd-MM-yyyy}") %>'
                                        data-order='<%# Eval("date", "{0:yyyy-MM-dd}") %>'
                                        CssClass="d-block text-left" />
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="Training Time" ItemStyle-Width="5%" ItemStyle-CssClass="text-left" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
                                <ItemTemplate>
                                    <asp:Label ID="lblTime" runat="server" Text='<%# Eval("time") %>' CssClass="d-block text-left" />
                                </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="Remark" ItemStyle-Width="5%" ItemStyle-CssClass="text-left" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
                                <ItemTemplate>
                                    <asp:Label ID="lblRemark" runat="server" Text='<%# Eval("remark") %>' CssClass="d-block text-left" />
                                </ItemTemplate>
                           </asp:TemplateField>

                           <asp:TemplateField HeaderText="Cancel Reason" ItemStyle-Width="5%" ItemStyle-CssClass="text-left" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
                                 <ItemTemplate>
                                     <asp:Label ID="lblReason" runat="server" Text='<%# Eval("reason") %>' CssClass="d-block text-left" />
                                 </ItemTemplate>
                            </asp:TemplateField>

                            <asp:TemplateField HeaderText="Actions" ItemStyle-Width="7%" ItemStyle-CssClass="text-center" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
                                <ItemTemplate>
                                    <div class="text-center">
                                        <%
                                            var formPermissions = Session["formPermissions"] as Dictionary<string, string>;
                                            string perm = formPermissions != null && formPermissions.ContainsKey("TrainingList") ? formPermissions["TrainingList"] : null;
                                        %>

                                       <div class="btn-group" role="group">

                                        <% if (perm == "admin" || perm == "super") { %>
                                            <!-- Details -->
                                           <%-- <a href="javascript:void(0);" 
                                               class="btn btn-sm me-1 text-white" style="background-color:#022f56;"
                                               onclick='highlightRow(this); openTraineeDetails(<%# Eval("id") %>)'>
                                                <i class="fa fa-eye"></i> Details
                                            </a>--%>

                                            <button type="button" class="btn btn-sm me-1 text-white" style="background-color:#022f56;"
                                                 onclick="highlightRow(this); openTraineeDetails(<%# Eval("id") %>)">
                                                <i class="fa fa-eye"></i> Details
                                            </button>

                                            <!-- Cancel -->
                                             <asp:LinkButton ID="btnCancel" runat="server"
                                                CssClass="btn btn-sm btn-danger me-1 btnCancelSchedule"
                                                CommandName="CancelSchedule"
                                                CommandArgument='<%# Eval("id") %>'
                                                ToolTip="Cancel Schedule"
                                                OnClientClick="return showCancelSweetAlert(this);">
                                                <i class="fa fa-ban"></i>
                                            </asp:LinkButton>
                                        <% } %>

                                        <% if (perm == "admin" || perm == "edit") { %>
                                            <!-- Register -->
                                          <%--  <a href="javascript:void(0);" 
                                               class="btn btn-sm btn-info me-1 text-white"
                                               onclick='highlightRow(this); openRegisterModal(<%# Eval("id") %>, "<%# Eval("topicWLTId") %>", <%# Eval("positionId") %>)'>
                                                <i class="fa fa-user-plus"></i> Register
                                            </a>--%>

                                           <button type="button" class="btn btn-sm btn-info me-1 text-white"
                                                 onclick='highlightRow(this); openRegisterModal(<%# Eval("id") %>, "<%# Eval("topicWLTId") %>", <%# Eval("positionId") %>)'>
                                                 <i class="fa fa-user-plus"></i> Register
                                            </button>
                                        <% } %>

                                    </div>

                                    </div>
                                </ItemTemplate>
                            </asp:TemplateField>

                        </Columns>

                    </asp:GridView>
                </div>
                </div>
            </div>
        </div>

    <!-- Schedule Trainees Modal -->
    <div class="modal fade" id="traineeDetailsModal" tabindex="-1" aria-labelledby="traineeDetailsLabel" aria-hidden="true">
      <div class="modal-dialog modal-dialog-centered modal-xl modal-dialog-scrollable">
             <div class="modal-content rounded-3 shadow-lg h-100">

                <div class="modal-header text-white" style="background-color:#022f56;">
                    <h5 class="modal-title fw-bold" id="traineeDetailsLabel">Schedule Trainees</h5>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                </div>

                <div class="modal-body p-2" style="max-height:75vh; overflow-y:auto;">
                    <asp:UpdatePanel ID="upTrainees" runat="server" UpdateMode="Conditional">
                        <ContentTemplate>
                            <!-- Hidden trigger -->
                            <asp:Button ID="btnLoadTrainees" runat="server" OnClick="btnLoadTrainees_Click" Style="display:none;" />
                            <asp:HiddenField ID="hfSelectedScheduleId" runat="server" />

                            <!-- Trainee Grid -->
                            <asp:GridView ID="gvScheduleTrainees" runat="server" AutoGenerateColumns="False"
                                DataKeyNames="id"
                                OnRowDataBound="gvScheduleTrainees_RowDataBound"
                                CssClass="table table-bordered table-striped dataTable w-100"
                                ClientIDMode="Static"
                                ShowHeader="true" HeaderStyle-BackColor="#4486ab" HeaderStyle-ForeColor="White" EmptyDataText="No records found."
                                UseAccessibleHeader="true">

                                <Columns>
                                    <asp:BoundField DataField="name" HeaderText="Trainee Name" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1" />
                                    <asp:BoundField DataField="store" HeaderText="Store" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1" />
                                    <asp:BoundField DataField="position" HeaderText="Position" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1" />

                                    <asp:TemplateField HeaderText="Status" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
                                        <ItemTemplate>
                                            <asp:DropDownList ID="ddlStatus" runat="server" CssClass="form-select form-select-sm"
                                                AutoPostBack="true"
                                                OnSelectedIndexChanged="ddlStatus_SelectedIndexChanged"
                                                SelectedValue='<%# Eval("status") %>'>
                                                <asp:ListItem Text="Registered" Value="Registered"></asp:ListItem>
                                                <asp:ListItem Text="Attend" Value="Attend"></asp:ListItem>
                                                <asp:ListItem Text="Absent" Value="Absent"></asp:ListItem>
                                            </asp:DropDownList>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                </Columns>
                            </asp:GridView>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                </div>

                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                </div>
            </div>
        </div>
    </div>


  <!-- Register Trainee Modal -->
    <div class="modal fade" id="registerModal" tabindex="-1" aria-labelledby="registerModalLabel" aria-hidden="true">
        <div class="modal-dialog modal-lg">
            <div class="modal-content rounded-3 shadow-lg">
                <div class="modal-header text-white" style="background-color:#022f56;">
                    <h5 class="modal-title fw-bold">Register Trainees</h5>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                </div>
                <div class="modal-body">
                    <asp:HiddenField ID="hfScheduleId" runat="server" />
                    <asp:HiddenField ID="hfSelectedTrainees" runat="server" />
                    <asp:HiddenField ID="hfTopicId" runat="server" />
                    <div id="traineeMultiSelect" class="multi-select-container">
                        <div class="multi-select-input"></div>
                        <div class="multi-select-dropdown"></div>
                    </div>
                    <div class="alert alert-info mt-3">
                        <strong>Status = Registered</strong> | <strong>Exam = Not Taken</strong>.
                    </div>
                </div>
                <div class="modal-footer">
                    <asp:Button ID="btnSaveRegister" runat="server" Text="Save" CssClass="btn text-white" BackColor="#022f56" OnClick="btnSaveRegister_Click" />
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                </div>
            </div>
        </div>
    </div>

<!-- Note Modal -->
 <div class="modal fade" id="noteModal" tabindex="-1" aria-hidden="true">
     <div class="modal-dialog modal-dialog-centered">
         <div class="modal-content">
             <div class="modal-header">
                 <h5 class="modal-title">Full Remark</h5>
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
