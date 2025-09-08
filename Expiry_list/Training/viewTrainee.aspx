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
        $(document).on('change', '.status-select, .exam-select', function () {
            const traineeId = $("#hiddenTraineeId").val();
            const topicId = $(this).data('id');
            const status = $(this).hasClass('status-select') ? $(this).val() : null;
            const exam = $(this).hasClass('exam-select') ? $(this).val() : null;

            $.ajax({
                url: 'viewTrainee.aspx/UpdateTraineeTopicStatusExam',
                type: 'POST',
                contentType: 'application/json; charset=utf-8',
                data: JSON.stringify({
                    traineeId: traineeId,
                    topicId: topicId,
                    status: status,
                    exam: exam
                }),
                success: function (res) {
                    if (res.d !== "success") {
                        alert("Failed to update: " + res.d);
                    } else {
                        loadTraineeTopics(traineeId);
                    }
                },
                error: function (xhr, status, error) {
                    console.error(error);
                }
            });
        });

        $(document).ready(function () {
            initializeDataTable();

            if (typeof (Sys) !== 'undefined') {
                Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
                    initializeDataTable();
                });
            }
        });

        function openTraineeModal() {
            $('#traineeModal').modal('show');
        }

        function closeTraineeModal() {
            $('#traineeModal').modal('hide');
        }

        function handleUpdate(btn) {
            if (typeof (Page_ClientValidate) == 'function') {
                Page_ClientValidate();
            }
            return true;
        }

        function loadTraineeTopics(traineeId) {
            const table = $("#topicsTable");

            // Destroy existing DataTable (if already initialized)
            if ($.fn.DataTable.isDataTable(table)) {
                table.DataTable().destroy();
                $("#topicsTableBody").empty();
            }

            $("#topicsTableBody").html(
                '<tr><td colspan="3" class="text-center text-muted py-4">Loading topics...</td></tr>'
            );

            $.ajax({
                url: 'viewTrainee.aspx/GetTraineeTopics',
                type: 'POST',
                contentType: 'application/json; charset=utf-8',
                dataType: 'json',
                data: JSON.stringify({ traineeId: traineeId }),
                success: function (response) {
                    const list = response.d || [];
                    if (list.length === 0) {
                        $("#topicsTableBody").html(
                            '<tr><td colspan="3" class="text-center text-muted py-4">No topics found</td></tr>'
                        );
                        return;
                    }

                    let html = '';
                    list.forEach(t => {
                        html += `
                    <tr>
                        <td>${t.name}</td>
                        <td class="text-center">${t.status}</td>
                        <td class="text-center">${t.exam}</td>
                    </tr>`;
                    });

                    $("#topicsTableBody").html(html);

                    table.DataTable({
                        responsive: true,
                        paging: true,
                        searching: true,
                        ordering: true,
                        info: true,
                        pageLength: 5,
                        lengthMenu: [[5, 10, 25, 50], [5, 10, 25, 50]]
                    });
                },
                error: function (xhr, status, error) {
                    console.error(error);
                }
            });
        }

        function openTopicsModal(traineeId) {
            $("#hiddenTraineeId").val(traineeId);
            loadTraineeTopics(traineeId);
            new bootstrap.Modal(document.getElementById("topicsModal")).show();
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
              // Ensure table has proper structure
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
                      columnDefs: [
                          { orderable: false, targets: [4] }
                      ]
                  });
              } catch (e) {
                  console.error('DataTable initialization error:', e);
              }
          }
      }

    </script>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server" />
    <a href="../AdminDashboard.aspx" class="btn text-white ms-2" style="background-color: #022f56;">
        <i class="fa-solid fa-left-long"></i>Home
    </a>
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
                             DataKeyNames="id"
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
                                         <asp:Label ID="lblStore" runat="server" Text='<%# Eval("store") %>' />
                                     </ItemTemplate>
                                     <EditItemTemplate>
                                        <asp:DropDownList ID="storeDp" runat="server" 
                                             CssClass="form-control form-control-sm dropdown-icon"
                                             DataTextField="store"
                                             DataValueField="id">
                                         </asp:DropDownList>
                                     </EditItemTemplate>
                                     <ItemStyle HorizontalAlign="Justify" />
                                     <HeaderStyle ForeColor="White" BackColor="#488db4" />
                                 </asp:TemplateField>

                                 <asp:TemplateField HeaderText="Level" SortExpression="level" HeaderStyle-VerticalAlign="Middle" >
                                     <ItemTemplate>
                                         <asp:Label ID="lblPosition" runat="server" Text='<%# Eval("position") %>'></asp:Label>
                                     </ItemTemplate>
                                     <EditItemTemplate>
                                         <asp:DropDownList ID="PositionDb" runat="server" CssClass="form-control form-control-sm dropdown-icon">
                                         </asp:DropDownList>
                                     </EditItemTemplate>
                                     <HeaderStyle ForeColor="White" BackColor="#488db4" />
                                     <ItemStyle HorizontalAlign="Justify" />
                                 </asp:TemplateField>

                               <asp:TemplateField HeaderText="Topics" SortExpression="topics" HeaderStyle-VerticalAlign="Middle" >
                                   <ItemTemplate>
                                     <button type="button" class="btn btn-sm btn-outline-info fw-bold"
                                         onclick="openTopicsModal(<%# Eval("id") %>)">
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
<div class="modal fade" id="topicsModal" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog modal-lg modal-dialog-centered modal-dialog-scrollable">
        <div class="modal-content rounded-3 shadow-lg border-0">

            <!-- Hidden Field -->
            <asp:HiddenField ID="hiddenTraineeId" runat="server" />

            <!-- Modal Header -->
            <div class="modal-header text-white" style="background-color: #022f56; ">
                <h5 class="modal-title fw-bold">
                    <i class="bi bi-journal-text me-2"></i> Trainee Topics
                </h5>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
            </div>

            <!-- Modal Body -->
            <div class="modal-body p-2" style="max-height: 70vh; overflow-y: auto;">
                <div class="table-responsive">
                    <table id="topicsTable" class="table table-striped table-hover table-bordered mb-0">
                        <thead class="">
                            <tr class="text-start" style="background-color:#488db4;" > 
                                <th style="width: 55%;">Topic Name</th>
                                <th style="width: 20%;">Status</th>
                                <th style="width: 25%;">Exam Result</th>
                            </tr>
                        </thead>
                        <tbody id="topicsTableBody" class="text-start" > 
                            <tr>
                                <td colspan="3" class="text-muted py-4">Loading topics...</td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>

            <!-- Modal Footer -->
            <div class="modal-footer justify-content-end">
                <button type="button" class="btn btn-secondary px-4" data-bs-dismiss="modal">
                    Close
                </button>
            </div>

        </div>
    </div>
</div>

</asp:Content>