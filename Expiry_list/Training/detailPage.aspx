<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="detailPage.aspx.cs" Inherits="Expiry_list.Training.detailPage" %>
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
    });

    document.addEventListener('DOMContentLoaded', function () {
        document.getElementById("link_home").href = "../AdminDashboard.aspx";
    });


    function initializeDataTableMain() {
        const grid = $("#<%= GridView2.ClientID %>");
        if (!grid.length) return;

        if ($.fn.DataTable.isDataTable(grid)) grid.DataTable().destroy();

        // ensure thead exists
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

        grid.DataTable({
            responsive: false,
            paging: true,
            searching: true,
            info: true,
            autoWidth: false,
            stateSave: true,
            ordering: true,
            order: [[1, 'asc']],
            scrollX: true,
            scrollY: '57vh',
            scrollCollapse: true,
            lengthMenu: [[10, 25, 50, 100], [10, 25, 50, 100]],
            columnDefs: [
                { targets: [1], visible: false },
                { targets: '_all', orderSequence: ["asc", "desc", ""] }
            ],
            initComplete: function () {
                this.api().columns.adjust();
            }
        });
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
                   CssClass="table table-striped table-hover border-2 shadow-lg sticky-grid overflow-x-auto display"
                    AutoGenerateColumns="False"
                    DataKeyNames="id"
                    AllowPaging="false"
                    ShowHeaderWhenEmpty="true"
                    HeaderStyle-BackColor="#4486ab"
                    HeaderStyle-ForeColor="White"
                    GridLines="None"
                    EmptyDataText="No records found.">

                    <HeaderStyle CssClass="text-left text-white" />

                    <Columns>

                       <asp:TemplateField HeaderText="Topic Name" ItemStyle-CssClass="text-left" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
                            <ItemTemplate>
                                <asp:Label ID="lblTopicName" runat="server" Text='<%# Eval("topicName") %>' />
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Position ID" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
                             <ItemTemplate>
                                 <asp:HiddenField ID="htid" runat="server" Value='<%# Eval("id") %>' />
                             </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Room" ItemStyle-CssClass="text-left" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
                            <ItemTemplate>
                                <asp:Label ID="lblRoom" runat="server" Text='<%# Eval("room") %>' CssClass="d-block text-left" />
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Trainer Name" ItemStyle-CssClass="text-left" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
                            <ItemTemplate>
                                <asp:Label ID="lblTrainerName" runat="server" Text='<%# Eval("trainerName") %>' CssClass="d-block text-left" />
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Training Date" ItemStyle-CssClass="text-left" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
                            <ItemTemplate>
                                <asp:Label ID="lblDate" runat="server"
                                    Text='<%# Eval("date", "{0:dd-MM-yyyy}") %>'
                                    data-order='<%# Eval("date", "{0:yyyy-MM-dd}") %>'
                                    CssClass="d-block text-left" />
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Training Time" ItemStyle-CssClass="text-left" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
                            <ItemTemplate>
                                <asp:Label ID="lblTime" runat="server" Text='<%# Eval("time") %>' CssClass="d-block text-left" />
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Trainee Name" ItemStyle-CssClass="text-left" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
                          <ItemTemplate>
                              <asp:Label ID="lblTraineeLevel" runat="server" Text='<%# Eval("name") %>' CssClass="d-block text-left" />
                          </ItemTemplate>
                      </asp:TemplateField>

                     <asp:TemplateField HeaderText="Trainee Level" ItemStyle-CssClass="text-left" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
                        <ItemTemplate>
                            <asp:Label ID="lblTraineeLevel" runat="server" Text='<%# Eval("position") %>' CssClass="d-block text-left" />
                        </ItemTemplate>
                    </asp:TemplateField>

                    <asp:TemplateField HeaderText="Status" ItemStyle-CssClass="text-left" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
                        <ItemTemplate>
                            <asp:Label ID="lblTraineeLevel" runat="server" Text='<%# Eval("status") %>' CssClass="d-block text-left" />
                        </ItemTemplate>
                    </asp:TemplateField>

                  <asp:TemplateField HeaderText="Exam" ItemStyle-CssClass="text-left" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
                      <ItemTemplate>
                          <asp:Label ID="lblTraineeLevel" runat="server" Text='<%# Eval("exam") %>' CssClass="d-block text-left" />
                      </ItemTemplate>
                  </asp:TemplateField>

               <asp:TemplateField HeaderText="Schedule Status" ItemStyle-CssClass="text-left" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
                    <ItemTemplate>
                        <asp:Label 
                            ID="lblScheduleStatus" 
                            runat="server" 
                            Text='<%# (Convert.ToBoolean(Eval("IsCancel")) ? "Cancelled" : "Available") %>' 
                            CssClass="d-block text-left" />
                    </ItemTemplate>
                </asp:TemplateField>

                    </Columns>
                </asp:GridView>
            </div>
            </div>
        </div>
    </div>

</asp:Content>
