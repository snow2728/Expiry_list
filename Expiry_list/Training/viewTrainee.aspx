<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="viewTrainee.aspx.cs" Inherits="Expiry_list.Training.viewTrainee" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">

    <%
        var permissions = Session["formPermissions"] as Dictionary<string, string>;
        string expiryPerm = permissions != null && permissions.ContainsKey("TrainingList") ? permissions["TrainingList"] : "";
    %>
     <script type="text/javascript">
         var expiryPermission = '<%= expiryPerm %>';
     </script>

    <script type="text/javascript">

        $(document).ready(function () {
            initializeDataTable();

            if (typeof (Sys) !== 'undefined') {
                Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
                    initializeDataTable();
                });
            }

            window.openTraineeDetails = function (traineeId) {
                $("#<%= hiddenTraineeId.ClientID %>").val(traineeId);
                __doPostBack('<%= btnLoadTopics.UniqueID %>', '');
                new bootstrap.Modal(document.getElementById("topicsModal")).show();
            }
        });

        function openTraineeModal() {
            $('#traineeModal').modal('show');
        }

        function closeTraineeModal() {
            $('#traineeModal').modal('hide');
        }

        function initializeDataTable() {
          const grid = $("#<%= GridView2.ClientID %>");

            if (grid.length === 0 || grid.find('tr').length === 0) {
                return;
            }

            if ($.fn.DataTable.isDataTable(grid)) {
                grid.DataTable().destroy();
                grid.removeAttr('style');
            }

          if (<%= GridView2.EditIndex >= 0 ? "true" : "false" %> === false) {
                if (grid.find('thead').length === 0) {
                    const headerRow = grid.find('tr:first').detach();
                    grid.prepend($('<thead/>').append(headerRow));
                }

                const headerCols = grid.find('thead tr:first th').length;
                const bodyCols = grid.find('tbody tr:first td').length;

                if (headerCols !== bodyCols) {
                    console.error('Header and body column count mismatch:', headerCols, 'vs', bodyCols);
                    return;
                }

                try {
                    grid.DataTable({
                        responsive: true,
                        paging: true,
                        searching: true,
                        sorting: true,
                        info: true,
                        order: [[0, 'asc']],
                        stateSave: false,
                        lengthMenu: [[10, 25, 50, 100], [10, 25, 50, 100]],
                        dom: 'f<"top-toggle">ltip',
                        initComplete: function () {
                            const toggleHtml = `
                            <div class="toggle-container">
                                <span class="toggle-label">Status:</span>
                                <label class="switch">
                                    <input type="checkbox" id="toggleSwitch">
                                    <span class="slider round"></span>
                                </label>
                                <span class="toggle-status" id="toggleStatus">Off</span>
                            </div>`;

                            $('.top-toggle').append(toggleHtml);

                            $('#toggleSwitch').on('change', function () {
                                if (this.checked) {
                                    $('#toggleStatus').text('On').css('color', '#0D330E');
                                    console.log('Toggle is ON');
                                } else {
                                    $('#toggleStatus').text('Off').css('color', '#dc3545'); 
                                    console.log('Toggle is OFF');
                                }
                            });
                        },
                        columnDefs: [
                            { orderable: false, targets: [5, 6] },
                            { targets: '_all', orderSequence: ["asc", "desc", ""] }
                        ]
                    });
                } catch (e) {
                    console.error('DataTable initialization error:', e);
                }
          }
        }

        function initializeDataTable2() {
            const table = $('#gvTraineeTopics');
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
                    //{ orderable: false, targets: [-1, -2] }
                    { targets: '_all', orderSequence: ["asc", "desc", ""] }
                ]
              
            });
        }

        document.addEventListener('DOMContentLoaded', function () {
            document.getElementById("link_home").href = "../AdminDashboard.aspx";
        });


    </script>

</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server" />
    <div class="container py-4"> 
        <asp:UpdatePanel ID="upGrid" runat="server" UpdateMode="Conditional">
            <ContentTemplate>
                <div class="card shadow-lg p-3 rounded-4">
                    <div class="card-body pb-1">
                        <div class="d-flex justify-content-between align-items-center mb-0">
                            <h2>Trainee List</h2>
                            <button type="button" class="btn text-white" style="background-color:#022f56;" onclick="openTraineeModal();">
                                <i class="fa-solid fa-user-plus"></i> Add New Trainee
                            </button>
                        </div>
                    </div>
               
                    <div class="table-responsive">
                        <asp:GridView ID="GridView2" runat="server" AutoGenerateColumns="False" CssClass="table table-striped table-bordered table-hover border border-2 shadow-lg sticky-grid mt-1 overflow-scroll"
                            DataKeyNames="id,name,storeId,positionId,IsActive"
                             OnRowEditing="GridView2_RowEditing"
                             OnRowUpdating="GridView2_RowUpdating"
                             OnRowCancelingEdit="GridView2_RowCancelingEdit"
                             OnRowDeleting="GridView2_RowDeleting"
                             OnRowDataBound="GridView2_RowDataBound" >
                           <Columns>

                                <asp:TemplateField ItemStyle-HorizontalAlign="Justify" HeaderText="No">
                                     <ItemTemplate>
                                         <asp:Label ID="lblLinesNo" runat="server" Text='<%# Container.DataItemIndex + 1 %>' />
                                     </ItemTemplate>
                                     <ControlStyle Width="50px" />
                                     <HeaderStyle ForeColor="White" BackColor="#488db4" />
                                     <ItemStyle HorizontalAlign="Justify" />
                                 </asp:TemplateField>

                                 <asp:TemplateField HeaderText="Name" ItemStyle-HorizontalAlign="Justify" SortExpression="name" >
                                     <ItemTemplate>
                                         <asp:Label ID="lblName" runat="server" Text='<%# Eval("name") %>'></asp:Label>
                                     </ItemTemplate>
                                     <EditItemTemplate>
                                         <asp:TextBox ID="txtName" runat="server" Text='<%# Bind("name") %>'
                                             CssClass="form-control" />
                                     </EditItemTemplate>
                                     <HeaderStyle ForeColor="White" BackColor="#488db4" />
                                     <ItemStyle HorizontalAlign="Justify" />
                                 </asp:TemplateField>

                                 <asp:TemplateField HeaderText="Store" SortExpression="store" HeaderStyle-VerticalAlign="Middle" >
                                     <ItemTemplate>
                                         <asp:Label ID="lblStore" runat="server" Text='<%# Eval("storeName") %>' />
                                     </ItemTemplate>
                                     <EditItemTemplate>
                                        <asp:DropDownList ID="storeDp" runat="server" 
                                             CssClass="form-control form-control-sm dropdown-icon"
                                             DataTextField="storeNo"
                                             DataValueField="id">
                                         </asp:DropDownList>
                                     </EditItemTemplate>
                                     <ItemStyle HorizontalAlign="Justify" />
                                     <HeaderStyle ForeColor="White" BackColor="#488db4" />
                                 </asp:TemplateField>

                                 <asp:TemplateField HeaderText="Level" SortExpression="level" HeaderStyle-VerticalAlign="Middle" >
                                     <ItemTemplate>
                                         <asp:Label ID="lblPosition" runat="server" Text='<%# Eval("positionName") %>'></asp:Label>
                                     </ItemTemplate>
                                     <EditItemTemplate>
                                         <asp:DropDownList ID="PositionDb" runat="server" CssClass="form-control form-control-sm dropdown-icon" DataTextField="name" DataValueField="id" >
                                         </asp:DropDownList>
                                     </EditItemTemplate>
                                     <HeaderStyle ForeColor="White" BackColor="#488db4" />
                                     <ItemStyle HorizontalAlign="Justify" />
                                 </asp:TemplateField>

                                 <asp:TemplateField HeaderText="Status" SortExpression="IsActive">
                                    <ItemTemplate>
                                        <div style="text-align:left;">
                                            <%# Convert.ToBoolean(Eval("IsActive")) ? "Active" : "Inactive" %>
                                        </div>
                                    </ItemTemplate>
                                    <EditItemTemplate>
                                        <div style="text-align:left;">
                                            <asp:CheckBox ID="chkTopic_Enable" runat="server" 
                                                Checked='<%# Convert.ToBoolean(Eval("IsActive")) %>' 
                                                Text="Active" CssClass="chk-status" />
                                        </div>
                                    </EditItemTemplate>
                                    <HeaderStyle ForeColor="White" BackColor="#488db4" HorizontalAlign="Left" Width="10%" />
                                    <ItemStyle HorizontalAlign="Left" Width="10%" />
                                </asp:TemplateField>

                               <asp:TemplateField HeaderText="Topics" SortExpression="topics" HeaderStyle-VerticalAlign="Middle" >
                                   <ItemTemplate>
                                     <button type="button" class="btn btn-sm btn-outline-info fw-bold"
                                         onclick="openTraineeDetails(<%# Eval("id") %>)">
                                       Details
                                     </button>
                                   </ItemTemplate>
                                     <HeaderStyle ForeColor="White" BackColor="#488db4" />
                                     <ItemStyle HorizontalAlign="Justify" />
                              </asp:TemplateField>

                              <asp:TemplateField HeaderText="Actions">
                                <ItemTemplate>

                                    <div class="text-center">
                                        <%
                                            var formPermissions = Session["formPermissions"] as Dictionary<string, string>;
                                            string perm = formPermissions != null && formPermissions.ContainsKey("TrainingList") ? formPermissions["TrainingList"] : null;
                                        %>

                                        <% if (perm == "admin" || perm == "edit") { %>
                                            <asp:LinkButton ID="btnEdit" runat="server" CommandName="Edit" CausesValidation="False"
                                                CssClass="btn btn-sm text-white mt-1 ms-1 me-2" BackColor="#0a61ae" ToolTip="Edit">
                                                <i class="fas fa-pencil-alt"></i>
                                            </asp:LinkButton>
                                        <% } %>

                                        <% if (perm == "admin") { %>
                                            <asp:LinkButton ID="btnDelete" runat="server" CommandName="Delete" CausesValidation="False"
                                                CssClass="btn btn-sm mt-1 ms-1 me-2 text-white" BackColor="#453b3b" ToolTip="Delete">
                                                <i class="fas fa-trash-alt"></i>
                                            </asp:LinkButton>
                                        <% } %>
                                    </div>
                                    
                                </ItemTemplate>

                                <EditItemTemplate>
                                    <asp:LinkButton ID="btnUpdate" runat="server" CommandName="Update" CausesValidation="False"
                                        CssClass="btn btn-sm ms-1 text-white" BackColor="#9ad9fe" ToolTip="Update">
                                        <i class="fas fa-save"></i>
                                    </asp:LinkButton>
                                    <asp:LinkButton ID="btnCancel" runat="server" CommandName="Cancel" CausesValidation="False"
                                        CssClass="btn btn-sm ms-2 text-white btn-secondary" ToolTip="Cancel">
                                        <i class="fas fa-times"></i>
                                    </asp:LinkButton>
                                </EditItemTemplate>

                                <HeaderStyle ForeColor="White" BackColor="#488db4" CssClass="text-center" />
                                <ItemStyle HorizontalAlign="Justify" />
                            </asp:TemplateField>

                             </Columns>
                         </asp:GridView>
                    </div>
               
             </div>
            </ContentTemplate>
             <Triggers>
                <asp:AsyncPostBackTrigger ControlID="GridView2" EventName="RowEditing" />
                <asp:AsyncPostBackTrigger ControlID="GridView2" EventName="RowUpdating" />
                <asp:AsyncPostBackTrigger ControlID="GridView2" EventName="RowDeleting" />
                <asp:AsyncPostBackTrigger ControlID="GridView2" EventName="RowCancelingEdit" />
            </Triggers>
        </asp:UpdatePanel>
    </div>

    <!-- Add New Trainee Modal -->
<div class="modal fade" id="traineeModal" tabindex="-1" aria-labelledby="traineeModalLabel" aria-hidden="true">
    <div class="modal-dialog modal-lg modal-dialog-centered">
        <div class="modal-content rounded-3 shadow-lg border-0">
            
            <!-- Modal Header -->
            <div class="modal-header text-white" style="background-color:#022f56;">
                <h5 class="modal-title fw-bold" id="traineeModalLabel">
                    <i class="bi bi-person-plus me-2"></i> New Trainee
                </h5>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
            </div>

            <!-- Hidden Field -->
            <asp:HiddenField ID="hfTopicStatuses" runat="server" />

            <!-- Modal Body -->
            <div class="modal-body">
                <asp:UpdatePanel ID="upModal" runat="server">
                    <ContentTemplate>
                        <div class="container-fluid">
                            <div class="row g-3">

                                <!-- Name -->
                                <div class="col-12 col-md-6">
                                    <label for="traineeName" class="form-label fw-semibold">Name</label>
                                    <asp:TextBox runat="server" CssClass="form-control form-control-sm" ID="traineeName" placeholder="Enter trainee name" />
                                </div>

                                <!-- Store -->
                                <div class="col-12 col-md-6">
                                    <label for="storeDp" class="form-label fw-semibold">Store</label>
                                    <asp:DropDownList ID="storeDp" runat="server" CssClass="form-select form-select-sm">
                                        <asp:ListItem Text="Select Store" Value="" />
                                    </asp:DropDownList>
                                </div>

                                <!-- Level -->
                                <div class="col-12 col-md-6">
                                    <label for="levelDb" class="form-label fw-semibold">Level</label>
                                    <asp:DropDownList ID="levelDb" runat="server" CssClass="form-select form-select-sm" onchange="loadTopics(this.value)">
                                    </asp:DropDownList>
                                </div>

                            </div>
                        </div>
                    </ContentTemplate>
                </asp:UpdatePanel>
            </div>

            <!-- Modal Footer -->
            <div class="modal-footer d-flex justify-content-end">
                <asp:Button Text="Add Trainee" runat="server" CssClass="btn fw-bold px-4"
                    Style="background-color: #022F56; color:#c9b99f; border-radius: 30px;"
                    ID="addTopicBtn1" OnClick="btnaddTrainee_Click" />
                <button type="button" class="btn btn-secondary rounded-4" data-bs-dismiss="modal">Close</button>
            </div>
        </div>
    </div>
</div>


<!-- View / Edit Topics Modal -->
<div class="modal fade" id="topicsModal" tabindex="-1" aria-labelledby="topicsModalLabel" aria-hidden="true">
    <div class="modal-dialog modal-dialog-centered modal-xl modal-dialog-scrollable">
        <div class="modal-content rounded-3 shadow-lg h-100">

            <!-- Modal Header -->
            <div class="modal-header text-white" style="background-color:#022f56;">
                <h5 class="modal-title fw-bold" id="topicsModalLabel">
                    <i class="bi bi-journal-text me-2"></i> Trainee Topics
                </h5>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
            </div>

            <!-- Modal Body -->
            <div class="modal-body p-2" style="max-height:75vh; overflow-y:auto;">

                <asp:UpdatePanel ID="upTopics" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>

                        <!-- Hidden trigger for async load -->
                        <asp:Button ID="btnLoadTopics" runat="server" OnClick="btnLoadTopics_Click" Style="display:none;" />
                        <asp:HiddenField ID="hiddenTraineeId" runat="server" />

                        <!-- Topics Grid -->
                        <asp:GridView ID="gvTraineeTopics" runat="server" AutoGenerateColumns="False"
                            DataKeyNames="id"
                            OnRowDataBound="gvTraineeTopics_RowDataBound"
                            CssClass="table table-bordered table-striped dataTable w-100"
                            ClientIDMode="Static"
                            ShowHeader="true" HeaderStyle-BackColor="#4486ab" HeaderStyle-ForeColor="White"
                            EmptyDataText="No topics found."
                            UseAccessibleHeader="true">

                            <Columns>
                                <asp:BoundField DataField="topicName" HeaderText="Topic Name" />
                                <asp:BoundField DataField="status" HeaderText="Status" />

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

            <!-- Modal Footer -->
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
            </div>

        </div>
    </div>
</div>

</asp:Content>