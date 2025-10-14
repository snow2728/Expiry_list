<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="viewTrainer.aspx.cs" Inherits="Expiry_list.Training.viewTrainer" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">

       <%
           var permissions = Session["formPermissions"] as Dictionary<string, string>;
           string expiryPerm = permissions != null && permissions.ContainsKey("TrainingList") ? permissions["TrainingList"] : "";
       %>
        <script type="text/javascript">
            var expiryPermission = '<%= expiryPerm %>';
        </script>

  <script>
      $(document).ready(function () {
          initializeDataTable();

          if (typeof (Sys) !== 'undefined') {
              Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
                  initializeDataTable();
              });
          }
      });

      document.addEventListener('DOMContentLoaded', function () {
          document.getElementById("link_home").href = "../AdminDashboard.aspx";
      });

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
                      scrollX: true,
                      scrollY: "63vh",
                      scrollCollapse: true,
                      autoWidth: false,
                      order: [[0, 'asc']],
                      stateSave: true,
                      lengthMenu: [[10, 25, 50, 100], [10, 25, 50, 100]],
                      columnDefs: [
                          { orderable: false, targets: [3] },
                          { targets: '_all', orderSequence: ["asc", "desc", ""] }
                      ]
                  });
              } catch (e) {
                  console.error('DataTable initialization error:', e);
              }
          }
      }
  </script>

    <style>
        .small-col {
            width: 60px !important;
            max-width: 60px !important;
            text-align: center;
        }

    </style>

</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    
    <div class="container py-4">
        <asp:HiddenField ID="hfSelectedRows" runat="server" />
        <asp:HiddenField ID="hfSelectedIDs" runat="server" />
        <asp:HiddenField ID="hflength" runat="server" />   
        <asp:HiddenField ID="hfEditId" runat="server" />
        <asp:HiddenField ID="hfEditedRowId" runat="server" />

        <asp:ScriptManager ID="ScriptManager1" runat="server" />
        <asp:UpdatePanel ID="upGrid" runat="server">
            <ContentTemplate>
                <div class="card shadow-lg p-3 rounded-4">
                    <div class="card-body">
                        <div class="d-flex justify-content-between align-items-center">
                            <h5 class="card-title fa-2x" style="color:#022f56;">Trainer List</h5>
                            <a href="#" class="btn text-white" style="background-color:#022f56;"
                                data-bs-toggle="modal" data-bs-target="#trainerModal">
                                <i class="fa-solid fa-user-plus"></i> Add New Trainer
                            </a>
                        </div>
                        
                    <div class="table-responsive gridview-container pt-2 pe-2 rounded-1">
                          <asp:GridView ID="GridView2" runat="server" AutoGenerateColumns="false"
                              CssClass="table table-striped table-hover border-2 shadow-lg sticky-grid overflow-x-auto overflow-y-auto display"
                                DataKeyNames="id"
                                AllowPaging="false"
                                PagerSettings-Visible="False"
                                OnRowEditing="GridView2_RowEditing"
                                OnRowUpdating="GridView2_RowUpdating"
                                OnRowDeleting="GridView2_RowDeleting"
                                OnRowCancelingEdit="GridView2_RowCancelingEdit"
                                OnRowDataBound="GridView2_RowDataBound"
                                HeaderStyle-BackColor="#4486ab" HeaderStyle-ForeColor="White"
                                EmptyDataText="No records found." ShowHeaderWhenEmpty="true">
                                <Columns>
                                    <asp:TemplateField HeaderText="No." HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1 small-col">
                                        <HeaderStyle CssClass="text-white small-col" BackColor="#4486ab" />
                                        <ItemStyle CssClass="small-col" />
                                        <ItemTemplate>
                                            <asp:Label ID="lblLinesNo" runat="server"
                                                Text='<%# Container.DataItemIndex + 1 %>' />
                                        </ItemTemplate>
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="<i class='fa-solid fa-user me-2'></i>Name" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1"  SortExpression="name">
                                        <ItemTemplate>
                                            <asp:Label ID="lblName" runat="server" Text='<%# Eval("name") %>'></asp:Label>
                                        </ItemTemplate>
                                        <EditItemTemplate>
                                            <asp:TextBox ID="txtName" runat="server" Text='<%# Bind("name") %>' CssClass="form-control form-control-sm" />
                                        </EditItemTemplate>
                                        <HeaderStyle CssClass="text-white" BackColor="#4486ab"  />
                                        <ItemStyle HorizontalAlign="Left" />
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="<i class='fa-solid fa-briefcase me-2'></i>Position" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1"  SortExpression="positionName">
                                        <ItemTemplate>
                                            <asp:Label ID="lblPosition" runat="server" Text='<%# Eval("positionName") %>'></asp:Label>
                                        </ItemTemplate>
                                        <EditItemTemplate>
                                            <asp:DropDownList ID="txtPosition" runat="server" CssClass="form-control form-control-sm">
                                            </asp:DropDownList>
                                        </EditItemTemplate>
                                        <HeaderStyle CssClass="text-white" BackColor="#4486ab" />
                                        <ItemStyle HorizontalAlign="Left" />
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Actions" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1"  >
                                        <ItemTemplate>

                                             <%
                                                 var formPermissions = Session["formPermissions"] as Dictionary<string, string>;
                                                 string perm = formPermissions != null && formPermissions.ContainsKey("TrainingList") ? formPermissions["TrainingList"] : null;
                                             %>

                                             <% if (perm == "admin" || perm == "super") { %>
                                                 <asp:LinkButton ID="btnEdit" runat="server" CommandName="Edit" CausesValidation="False"
                                                    CssClass="btn btn-sm text-white ms-2 me-2" BackColor="#0a61ae" ToolTip="Edit">
                                                    <i class="fas fa-pencil-alt"></i>
                                                </asp:LinkButton>
                                             <% } %>

                                             <% if (perm == "admin") { %>
                                                    <asp:LinkButton ID="btnDelete" runat="server" CommandName="Delete" CausesValidation="False"
                                                        CssClass="btn btn-sm me-2 text-white" BackColor="#453b3b" ToolTip="Delete" >
                                                        <i class="fas fa-trash-alt"></i>
                                                    </asp:LinkButton>
                                             <% } %>

                                        </ItemTemplate>
                                        <EditItemTemplate>
                                            <asp:LinkButton ID="btnUpdate" runat="server" CommandName="Update" CausesValidation="False"
                                                CssClass="btn btn-sm text-white" BackColor="#9ad9fe" ToolTip="Update">
                                                <i class="fas fa-save"></i>
                                            </asp:LinkButton>
                                            <asp:LinkButton ID="btnCancel" runat="server" CommandName="Cancel" CausesValidation="False"
                                                CssClass="btn btn-sm ms-2 text-white btn-secondary" ToolTip="Cancel">
                                                <i class="fas fa-times"></i>
                                            </asp:LinkButton>
                                        </EditItemTemplate>
                                        <HeaderStyle CssClass="text-white text-center" BackColor="#4486ab" HorizontalAlign="Center" />
                                        <ItemStyle HorizontalAlign="Center" CssClass="text-center" />
                                    </asp:TemplateField>
                                </Columns>
                            </asp:GridView>
                        </div>
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

    <!-- Add Trainer Modal -->
    <div class="modal fade" id="trainerModal" tabindex="-1" aria-hidden="true" aria-labelledby="trainerModalLabel">
        <div class="modal-dialog modal-dialog-centered modal-md">
            <div class="modal-content rounded-4 shadow-lg border-0">
                <!-- Modal Header -->
                <div class="card-header fw-bolder text-center d-flex justify-content-between align-items-center p-3"
                    style="background-color: #022F56; color:#c9b99f; border-top-left-radius: 10px; border-top-right-radius: 10px;">
                    <h2 class="mb-0"><i class="bi bi-person-plus me-2"></i> New Trainer</h2>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                </div>

                <!-- Modal Body -->
                <div class="modal-body p-4" style="background-color: #f8f9fa;">
                    <asp:Panel ID="pnlAddTopic" runat="server">
                        <div class="card-body text-white">
                            <!-- Name Field -->
                            <div class="row g-2 mb-3">
                                <label for="<%= trainerName.ClientID %>" class="col-sm-3 col-form-label fa-1x fw-bolder" style="color:#076585;">Name</label>
                                <div class="col-sm-9">
                                    <asp:TextBox runat="server" CssClass="form-control form-control-sm fw-bolder fa-1x" ID="trainerName" />
                                     <asp:RequiredFieldValidator ID="RequiredFieldValidator7" runat="server"
                                       ErrorMessage="Name is required!"
                                       ControlToValidate="trainerName" Display="Dynamic"
                                       CssClass="text-danger d-block" SetFocusOnError="True" />
                                </div>
                            </div>

                           <!-- Trainer Field -->
                           <div class="row g-2 mb-3">
                                <label for="<%= trainerPosition.ClientID %>" class="col-sm-3 col-form-label fa-1x fw-bolder" style="color:#076585;">Position</label>
                                <div class="col-sm-9">
                                     <asp:DropDownList ID="trainerPosition" runat="server" CssClass="form-control form-control-sm dropdown-icon">
                                     </asp:DropDownList>
                                     <asp:RequiredFieldValidator ID="RequiredFieldValidator8" runat="server"
                                       ErrorMessage="Position is required!"
                                       ControlToValidate="trainerPosition" Display="Dynamic"
                                       CssClass="text-danger d-block" SetFocusOnError="True" />
                                </div>
                            </div>
                        </div>
                    </asp:Panel>
                </div>

                <!-- Modal Footer -->
                <div class="modal-footer" style="background-color: #f1f1f1;">
                    <asp:Button Text="Add Trainer" runat="server" CssClass="btn fw-bolder px-3 me-1 rounded-4"
                        Style="background-color: #022F56; color:#c9b99f;"
                        ID="addTopicBtn" OnClick="btnaddTrainer_Click" />
                    <button type="button" class="btn btn-secondary rounded-4" data-bs-dismiss="modal">Close</button>
                </div>
            </div>
        </div>
    </div>
</asp:Content>