<%@ Page Title="Schedule List" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="scheduleList.aspx.cs" Inherits="Expiry_list.Training.scheduleList" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">

    <%
        var permissions = Session["formPermissions"] as Dictionary<string, string>;
        string expiryPerm = permissions != null && permissions.ContainsKey("TrainingList") ? permissions["TrainingList"] : "";
    %>
     <script type="text/javascript">
         var expiryPermission = '<%= expiryPerm %>';
     </script>

<script>
        var selectedItems = [];

        $(document).ready(function () {
            initializeDataTable();

            if (typeof (Sys) !== 'undefined') {
                Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
                    initializeDataTable();
                    //initializeDataTable2()
                });
            }

            // Multi-select trainee search & selection
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

            // Search trainees
            searchInput.on("keyup", function () {
                loadTrainees($(this).val());
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

            // For Register Trainee Modal
            window.openRegisterModal = function (scheduleId, topicId, positionId) {
                $("#<%= hfScheduleId.ClientID %>").val(scheduleId);
                $("#<%= hfTopicId.ClientID %>").val(topicId);
                $("#<%= hfSelectedTrainees.ClientID %>").val("[]");
                window.currentSchedulePositionId = positionId;

                selectedItems = [];
                $("#traineeMultiSelect .multi-select-input").html('<input type="text" class="placeholder" placeholder="Select trainees..." readonly />');
                $("#traineeMultiSelect .multi-select-dropdown").hide();

                new bootstrap.Modal(document.getElementById("registerModal")).show();
            };

            //For TraineeTopic Details Modal
            window.openTraineeDetails = function (scheduleId) {
                $("#<%= hfSelectedScheduleId.ClientID %>").val(scheduleId);
                __doPostBack('<%= btnLoadTrainees.UniqueID %>', ''); 
                new bootstrap.Modal(document.getElementById("traineeDetailsModal")).show();
            }

            $('#traineeDetailsModal, #registerModal').on('hidden.bs.modal', function () {
                $('#<%= GridView2.ClientID %> tbody tr').removeClass('selected-row');
           });

        });

        document.addEventListener('DOMContentLoaded', function () {
            document.getElementById("link_home").href = "../AdminDashboard.aspx";
        });

        function initializeDataTable() {
            const grid = $("#<%= GridView2.ClientID %>");
            if (grid.length === 0 || grid.find('tr').length === 0) return;

            if ($.fn.DataTable.isDataTable(grid)) grid.DataTable().destroy();

            if (grid.find('thead').length === 0) {
                const headerRow = grid.find('tr:first').detach();
                grid.prepend($('<thead/>').append(headerRow));
            }

            grid.DataTable({
                responsive: true,
                paging: true,
                searching: true,
                info: true,
                autoWidth: false,
                scrollX: true,
                scrollY: 407,
                scrollCollapse: true,
                order: [[0, 'asc']],
                lengthMenu: [[10, 25, 50, 100], [10, 25, 50, 100]],
                columnDefs: [{ orderable: false, targets: [ 10, 11 ] }, { targets: [4, 5], visible: false }]
            });
        }

        function initializeDataTable2() {
            const table = $('#gvScheduleTrainees');
            if (table.length === 0 || table.find('tr').length === 0) return;

            if ($.fn.DataTable.isDataTable(table)) {
                table.DataTable().destroy();
                table.find('thead').remove();
            }

            if (table.find('thead').length === 0) {
                const headerRow = table.find('tr:first');
                if (headerRow.length > 0) {
                    headerRow.detach();
                    table.prepend($('<thead/>').append(headerRow));
                }
            }

            table.DataTable({
                paging: true,
                searching: true,
                ordering: true,
                responsive: true,
                autoWidth: false,
                columnDefs: [
                    { orderable: false, targets: [-1, -2] }
                ]
            });
        }

        function highlightRow(btn) {
            // clear previous selection
            $('#<%= GridView2.ClientID %> tbody tr').removeClass('selected-row');

            // highlight clicked row
            var row = $(btn).closest('tr');
            row.addClass('selected-row');
        }

</script>

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
                <div class="col-md-2"><asp:TextBox ID="dateTb" runat="server" CssClass="form-control form-control-sm" TextMode="Date" /></div>
                <div class="col-md-2"><asp:DropDownList ID="timeDp" runat="server" CssClass="form-select form-select-sm">
                    <asp:ListItem Text="Select Time" Value="" />
                    <asp:ListItem Text="08:30 AM - 11:30 AM"  Value="08:30 AM - 11:30 AM" />
                    <asp:ListItem Text="12:30 PM - 03:30 PM" Value="12:30 PM - 03:30 PM" />
                    <asp:ListItem Text="09:00 AM - 12:00 PM" Value="09:00 AM - 12:00 PM" />
                    <asp:ListItem Text="09:00 AM - 03:00 PM" Value="09:00 AM - 03:00 PM" />
                </asp:DropDownList></div>
                <div class="col-md-2"><asp:DropDownList ID="topicName" runat="server" CssClass="form-select form-select-sm" AppendDataBoundItems="True" /></div>
                <div class="col-md-2"><asp:DropDownList ID="locationDp" runat="server" CssClass="form-select form-select-sm">
                    <asp:ListItem Text="Select Location" Value="" />
                </asp:DropDownList></div>
                <div class="col-md-2"><asp:DropDownList ID="trainerDp" runat="server" CssClass="form-select form-select-sm" AppendDataBoundItems="True">
                </asp:DropDownList></div>
                <div class="col-md-2 d-flex gap-2">
                    <asp:Button ID="showBtn" runat="server" Text="Show" CausesValidation="false" OnClick="showBtn_Click" CssClass="btn btn-info btn-sm text-white flex-fill" />
                    <asp:Button ID="resetBtn" runat="server" Text="Reset" OnClick="resetBtn_Click"  CausesValidation="false" CssClass="btn btn-danger btn-sm flex-fill" />
                </div>
            </div>
        </div>
    </div>

    <div class="card shadow-sm">
        <div class="card-body">
            <div class="table-responsive" style="max-height:70vh; overflow:auto;">
                <asp:GridView ID="GridView2" runat="server"
                   CssClass="table table-striped table-hover table-sm table-bordered align-middle mb-0 mt-1"
                    AutoGenerateColumns="False"
                    DataKeyNames="id"
                    AllowPaging="false"
                    ShowHeaderWhenEmpty="true"
                    HeaderStyle-BackColor="#4486ab"
                    HeaderStyle-ForeColor="White"
                    GridLines="None"
                    AllowSorting="false"
                    EmptyDataText="No records found."> 
                    <HeaderStyle CssClass="text-center text-white" />
                    <RowStyle CssClass="align-middle" />
                    <EmptyDataTemplate>No Data!!</EmptyDataTemplate>

                     <Columns>

                        <asp:TemplateField HeaderText="No" ItemStyle-Width="5%" HeaderStyle-CssClass="text-center">
                            <ItemTemplate>
                                <asp:Label ID="lblLinesNo" runat="server" Text='<%# Container.DataItemIndex + 1 %>' />
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Trans No" ItemStyle-Width="5%">
                            <ItemTemplate>
                                <asp:Label ID="lblNo" runat="server" Text='<%# Eval("tranNo") %>' />
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Topic Name" ItemStyle-Width="18%">
                            <ItemTemplate>
                                <asp:Label ID="lblTopicName" runat="server" Text='<%# Eval("topicName") %>' />
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Description" ItemStyle-Width="15%">
                            <ItemTemplate>
                                <asp:Label ID="lblDesc" runat="server" Text='<%# Eval("description") %>' CssClass="description" />
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Topic ID" ItemStyle-CssClass="d-none">
                            <ItemTemplate>
                                <asp:HiddenField ID="hfTopicId" runat="server" Value='<%# Eval("topicId") %>' />
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Position ID" ItemStyle-CssClass="d-none">
                            <ItemTemplate>
                                <asp:HiddenField ID="hfPositionId" runat="server" Value='<%# Eval("positionId") %>' />
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Room" ItemStyle-Width="10%">
                            <ItemTemplate>
                                <asp:Label ID="lblRoom" runat="server" Text='<%# Eval("room") %>' />
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Trainer Name" ItemStyle-Width="10%">
                            <ItemTemplate>
                                <asp:Label ID="lblTrainerName" runat="server" Text='<%# Eval("trainerName") %>' />
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Trainee Level" ItemStyle-Width="10%">
                            <ItemTemplate>
                                <asp:Label ID="lblTraineeLevel" runat="server" Text='<%# Eval("position") %>' />
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Training Date" ItemStyle-Width="10%">
                            <ItemTemplate>
                                <asp:Label ID="lblDate" runat="server" Text='<%# FormatDisplayDate(Eval("date")) %>' />
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Training Time" ItemStyle-Width="10%">
                            <ItemTemplate>
                                <asp:Label ID="lblTime" runat="server" Text='<%# Eval("time") %>' />
                            </ItemTemplate>
                        </asp:TemplateField>

                    <asp:TemplateField HeaderText="Actions" ItemStyle-Width="7%" HeaderStyle-CssClass="text-center">
                        <ItemTemplate>
                            <div class="text-center">
                                <%
                                    var formPermissions = Session["formPermissions"] as Dictionary<string, string>;
                                    string perm = formPermissions != null && formPermissions.ContainsKey("TrainingList") ? formPermissions["TrainingList"] : null;
                                %>

                                <% if (perm == "admin" || perm == "super") { %>
                                    <a href="javascript:void(0);" class="btn btn-sm text-white mb-2" style="background-color:#022f56;"
                                       onclick='highlightRow(this); openTraineeDetails(<%# Eval("id") %>)'>
                                       <i class="fa fa-eye"></i> Details
                                    </a>
                                <% } %>

                                <% if (perm == "admin" || perm=="edit") { %>
                                    <a href="javascript:void(0);" class="btn btn-sm btn-info text-white" 
                                       onclick='highlightRow(this); openRegisterModal(<%# Eval("id") %>, "<%# Eval("topicId") %>", <%# Eval("positionId") %>)'>
                                       <i class="fa fa-user-plus"></i> Register
                                    </a>
                                <% } %>
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
                                <asp:BoundField DataField="name" HeaderText="Trainee Name" />
                                <asp:BoundField DataField="store" HeaderText="Store" />
                                <asp:BoundField DataField="position" HeaderText="Position" />

                                <asp:TemplateField HeaderText="Status">
                                    <ItemTemplate>
                                        <asp:DropDownList ID="ddlStatus" runat="server" CssClass="form-select form-select-sm"
                                            AutoPostBack="true"
                                            OnSelectedIndexChanged="ddlStatus_SelectedIndexChanged"
                                            SelectedValue='<%# Eval("status") %>'>
                                            <asp:ListItem Text="Registered" Value="Registered"></asp:ListItem>
                                            <asp:ListItem Text="Waitlisted" Value="Waitlisted"></asp:ListItem>
                                            <asp:ListItem Text="Removed" Value="Removed"></asp:ListItem>
                                        </asp:DropDownList>
                                    </ItemTemplate>
                                </asp:TemplateField>

                                <asp:TemplateField HeaderText="Exam">
                                    <ItemTemplate>
                                        <asp:DropDownList ID="ddlExam" runat="server" CssClass="form-select form-select-sm"
                                            AutoPostBack="true"
                                            OnSelectedIndexChanged="ddlExam_SelectedIndexChanged"
                                            SelectedValue='<%# Eval("exam") %>'>
                                            <asp:ListItem Text="Not Taken" Value="Not Taken"></asp:ListItem>
                                            <asp:ListItem Text="Passed" Value="Passed"></asp:ListItem>
                                            <asp:ListItem Text="Failed" Value="Failed"></asp:ListItem>
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

</asp:Content>
