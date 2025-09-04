<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="viewTrainee.aspx.cs" Inherits="Expiry_list.Training.viewTrainee" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <script type="text/javascript">
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

        function openTopicsModal(traineeId) {
            $("#<%= hiddenTraineeId.ClientID %>").val(traineeId);

            var body = $("#topicsTableBody");
            body.html("<tr><td colspan='3' class='text-center text-muted py-4'>Loading topics...</td></tr>");

            $.ajax({
                type: "POST",
                url: "viewTrainee.aspx/GetTraineeTopics",
                data: JSON.stringify({ traineeId: traineeId }),
                contentType: "application/json; charset=utf-8",
                dataType: "json",
                success: function (response) {
                    var topics = response.d;
                    body.empty();

                    if (!topics || topics.length === 0) {
                        body.html("<tr><td colspan='3' class='text-center text-muted py-4'>No topics available</td></tr>");
                        $('#topicsModal').modal('show');
                        return;
                    }

                    $.each(topics, function (i, topic) {
                        body.append(`
                    <tr class="topic-row" data-topicid="${topic.id}">
                        <td class="text-truncate" style="max-width: 400px;" title="${topic.name}">${topic.name}</td>
                        <td class="text-center">
                            <input type="text" class="form-control form-control-sm text-center" 
                                   value="${topic.status}" readonly 
                                   style="background-color: transparent; border: none; padding: 0.25rem;"/>
                        </td>
                        <td class="text-center">
                            <input type="text" class="form-control form-control-sm text-center" 
                                   value="${topic.exam}" readonly 
                                   style="background-color: transparent; border: none; padding: 0.25rem;"/>
                        </td>
                    </tr>
                `);
                    });

                    $('#topicsModal').modal('show');
                },
                error: function () {
                    body.html("<tr><td colspan='3' class='text-center text-danger py-4'>Error loading topics</td></tr>");
                    $('#topicsModal').modal('show');
                }
            });
        }

         <%--   $(document).on('click', '#btnSaveTopics', function () {
                var changes = [];
                var traineeId = $("#<%= hiddenTraineeId.ClientID %>").val();

                $('.topic-row').each(function () {
                    var topicId = $(this).data('topicid');
                    var status = $(this).find('.status-dropdown').val();
                    var exam = $(this).find('.exam-dropdown').val();

                    changes.push({
                        TopicId: topicId,
                        Status: status,
                        Exam: exam
                    });
                });

                if (!traineeId || changes.length === 0) {
                    alert("No changes detected or trainee not selected.");
                    return;
                }

                $.ajax({
                    type: "POST",
                    url: "viewTrainee.aspx/SaveTraineeTopics",
                    data: JSON.stringify({ traineeId: parseInt(traineeId), topicStatuses: changes }),
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    success: function (response) {
                        if (response.d) {
                            alert("Changes saved successfully!");
                            $('#topicsModal').modal('hide');
                        } else {
                            alert("Error saving changes!");
                        }
                    },
                    error: function (xhr, status, error) {
                        console.error("Error saving topics:", error);
                        alert("Error saving changes!");
                    }
                });
            });--%>

    </script>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <asp:ScriptManager ID="ScriptManager1" runat="server" />
    <a href="../AdminDashboard.aspx" class="btn text-white ms-2" style="background-color: #022f56;">
        <i class="fa-solid fa-left-long"></i>Home
    </a>
    <div class="container py-4">
        <div class="d-flex justify-content-between align-items-center mb-4">
            <h2>Trainee List</h2>
            <asp:Button ID="btnOpenModal" runat="server" Text="Add New Trainee"
                CssClass="btn text-white" Style="background-color:#022f56;"
                OnClientClick="openTraineeModal(); return false;" />
        </div>
        
        <!-- Status message area -->
        <asp:Panel ID="pnlMessage" runat="server" CssClass="alert alert-info" Visible="false">
            <asp:Label ID="lblMessage" runat="server" Text=""></asp:Label>
        </asp:Panel>
        
        <asp:UpdatePanel ID="upGrid" runat="server" UpdateMode="Conditional">
            <ContentTemplate>
               <asp:GridView ID="GridView2" runat="server" AutoGenerateColumns="False" CssClass="table table-striped table-bordered table-hover border border-2 shadow-lg sticky-grid mt-1 overflow-scroll"
                    DataKeyNames="id"
                    OnRowEditing="GridView2_RowEditing"
                    OnRowUpdating="GridView2_RowUpdating"
                    OnRowCancelingEdit="GridView2_RowCancelingEdit"
                    OnRowDeleting="GridView2_RowDeleting"
                    OnRowDataBound="GridView2_RowDataBound" >
                  <Columns>

                       <asp:TemplateField ItemStyle-HorizontalAlign="Justify" HeaderText="No" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header2" ItemStyle-CssClass="fixed-column-2">
                            <ItemTemplate>
                                <asp:Label ID="lblLinesNo" runat="server" Text='<%# Container.DataItemIndex + 1 %>' />
                            </ItemTemplate>
                            <ControlStyle Width="50px" />
                            <HeaderStyle ForeColor="White" BackColor="#488db4" />
                            <ItemStyle HorizontalAlign="Justify" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Name" ItemStyle-HorizontalAlign="Justify" SortExpression="name" 
                            HeaderStyle-VerticalAlign="Middle" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header2" ItemStyle-CssClass="fixed-column-2">
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

                        <asp:TemplateField HeaderText="Store" SortExpression="store" HeaderStyle-VerticalAlign="Middle" 
                            HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header2" ItemStyle-CssClass="fixed-column-2">
                            <ItemTemplate>
                                <asp:Label ID="lblStore" runat="server" Text='<%# Eval("store") %>' />
                            </ItemTemplate>
                            <EditItemTemplate>
                               <asp:DropDownList ID="storeDp" runat="server" 
                                    CssClass="form-control form-control-sm dropdown-icon"
                                    DataTextField="name"
                                    DataValueField="id">
                                </asp:DropDownList>
                            </EditItemTemplate>
                            <ItemStyle HorizontalAlign="Justify" />
                            <HeaderStyle ForeColor="White" BackColor="#488db4" />
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Level" SortExpression="level" HeaderStyle-VerticalAlign="Middle" 
                            HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header2" ItemStyle-CssClass="fixed-column-2">
                            <ItemTemplate>
                                <asp:Label ID="lblPosition" runat="server" Text='<%# Eval("position") %>'></asp:Label>
                            </ItemTemplate>
                            <EditItemTemplate>
                                <asp:DropDownList ID="PositionDb" runat="server" CssClass="form-control form-control-sm dropdown-icon">
                                    <asp:ListItem Text="Select Level" Value="" />
                                    <asp:ListItem Value="trainee" Text="Trainee"></asp:ListItem>
                                    <asp:ListItem Value="juniorsale" Text="Junior Sale"></asp:ListItem>
                                </asp:DropDownList>
                            </EditItemTemplate>
                            <HeaderStyle ForeColor="White" BackColor="#488db4" />
                            <ItemStyle HorizontalAlign="Justify" />
                        </asp:TemplateField>

                      <asp:TemplateField HeaderText="Topics" SortExpression="topics" HeaderStyle-VerticalAlign="Middle" 
                            HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header2" ItemStyle-CssClass="fixed-column-2">
                          <ItemTemplate>
                            <button type="button" class="btn btn-sm btn-outline-info fw-bold"
                                onclick="openTopicsModal(<%# Eval("id") %>)">
                              Details
                            </button>
                          </ItemTemplate>
                            <HeaderStyle ForeColor="White" BackColor="#488db4" />
                            <ItemStyle HorizontalAlign="Justify" />
                     </asp:TemplateField>

                        <asp:TemplateField HeaderText="Actions" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header2" ItemStyle-CssClass="fixed-column-2" HeaderStyle-VerticalAlign="Middle" >
                            <ItemTemplate>
                                <asp:LinkButton ID="btnEdit" runat="server" CommandName="Edit"
                                     Text="Edit" CssClass="btn btn-sm btn-primary text-white "
                                    CausesValidation="false" />
                                <asp:LinkButton ID="btnDelete" runat="server" CommandName="Delete"
                                    CssClass="btn btn-sm btn-danger " Text="Delete"
                                    CausesValidation="false"
                                    OnClientClick="return confirm('Are you sure you want to delete this record?');" />
                            </ItemTemplate>

                            <EditItemTemplate>
                                <asp:LinkButton ID="btnUpdate" runat="server" CommandName="Update"
                                    CssClass="btn btn-sm btn-info" Text="Update"
                                    CausesValidation="false" />
                                <asp:LinkButton ID="btnCancel" runat="server" CommandName="Cancel"
                                    CssClass="btn btn-sm btn-secondary" Text="Cancel"
                                    CausesValidation="false" />
                            </EditItemTemplate>
                                <HeaderStyle ForeColor="White" BackColor="#488db4" />
                                <ItemStyle HorizontalAlign="Justify" />
                        </asp:TemplateField>

                    </Columns>
                </asp:GridView>
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
            <div class="modal-header bg-primary text-white">
                <h5 class="modal-title fw-bold" id="traineeModalLabel">
                    <i class="bi bi-person-plus me-2"></i> Register Trainee
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
                                        <asp:ListItem Text="Select Level" Value="" />
                                        <asp:ListItem Value="trainee" Text="Trainee" />
                                        <asp:ListItem Value="juniorsale" Text="Junior Sale" />
                                    </asp:DropDownList>
                                </div>

                            </div>
                        </div>
                    </ContentTemplate>
                </asp:UpdatePanel>
            </div>

            <!-- Modal Footer -->
            <div class="modal-footer d-flex justify-content-between">
                <button type="button" class="btn btn-outline-secondary px-4" data-bs-dismiss="modal">
                    <i class="bi bi-x-circle me-1"></i> Cancel
                </button>
                <asp:Button ID="btnaddTrainee" runat="server" Text="Save Trainee" CssClass="btn btn-success px-4" OnClick="btnaddTrainee_Click" />
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
            <div class="modal-header text-white" style="background-color:#488db4;">
                <h5 class="modal-title fw-bold">
                    <i class="bi bi-journal-text me-2"></i> Trainee Topics
                </h5>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
            </div>

            <!-- Modal Body -->
            <div class="modal-body p-2" style="max-height: 70vh; overflow-y: auto;">
                <div class="table-responsive">
                    <table class="table table-striped table-hover table-bordered align-middle mb-0">
                        <thead class="table-primary sticky-top">
                            <tr class="text-center">
                                <th style="width: 55%;">Topic Name</th>
                                <th style="width: 20%;">Status</th>
                                <th style="width: 25%;">Exam Result</th>
                            </tr>
                        </thead>
                        <tbody id="topicsTableBody">
                            <tr>
                                <td colspan="3" class="text-center text-muted py-4">Loading topics...</td>
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