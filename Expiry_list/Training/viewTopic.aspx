<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="viewTopic.aspx.cs" Inherits="Expiry_list.Training.viewTopic" %>
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

          // Reinitialize after AJAX postbacks
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
                      scrollX: true,
                      scrollY: "63vh",
                      scrollCollapse: true,
                      order: [[0, 'asc']],
                      stateSave: false,
                      lengthMenu: [[10, 25, 50, 100], [10, 25, 50, 100]],
                      columnDefs: [
                          { orderable: false, targets: [4,5] }
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
                    <div class="card-body pb-1">
                        <div class="d-flex justify-content-between align-items-center add-topic-btn ">
                            <h5 class="card-title fa-2x" style="color:#022f56;">Topic List</h5>
                            <a href="#" class="btn text-white pe-3" style="background-color:#022f56;" 
                               data-bs-toggle="modal" data-bs-target="#topicModal">
                               <i class="fa-solid fa-user-plus ps-3"></i> Add New Topic
                            </a>
                        </div>
                    </div>

                    <div class="table-responsive">
                        <asp:GridView ID="GridView2" runat="server" AutoGenerateColumns="False" 
                           CssClass="table table-striped ResizableGrid table-hover border-2 shadow-lg sticky-grid overflow-x-auto overflow-y-auto display"
                            DataKeyNames="id" AllowPaging="false" PagerSettings-Visible="false"
                            OnRowEditing="GridView2_RowEditing"
                            OnRowUpdating="GridView2_RowUpdating"
                            OnRowCancelingEdit="GridView2_RowCancelingEdit"
                            OnRowDataBound="GridView2_RowDataBound"
                            HeaderStyle-BackColor="#4486ab" HeaderStyle-ForeColor="White"
                            EmptyDataText="No records found." ShowHeaderWhenEmpty="true">

                            <Columns>

                                <asp:TemplateField ItemStyle-HorizontalAlign="Center" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1" HeaderText="No">
                                    <ItemTemplate>
                                        <asp:Label ID="lblLinesNo" runat="server" Text='<%# Container.DataItemIndex + 1 %>' />
                                    </ItemTemplate>
                                    <HeaderStyle ForeColor="White" BackColor="#488db4" Width="5%" />
                                    <ItemStyle Width="5%" />
                                </asp:TemplateField>

                                <asp:TemplateField HeaderText="Topic Name" SortExpression="topicName" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
                                    <ItemTemplate>
                                        <asp:Label ID="lblTopicName" runat="server" Text='<%# Eval("topicName") %>'></asp:Label>
                                    </ItemTemplate>
                                    <EditItemTemplate>
                                        <asp:TextBox ID="txtTopicName" runat="server" Text='<%# Bind("topicName") %>' 
                                            CssClass="form-control" />
                                    </EditItemTemplate>
                                    <HeaderStyle ForeColor="White" BackColor="#488db4" Width="35%" />
                                    <ItemStyle Width="35%" HorizontalAlign="Justify" />
                                </asp:TemplateField>

                                <asp:TemplateField HeaderText="Description" SortExpression="description" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
                                    <ItemTemplate>
                                        <asp:Label ID="lblDescription" runat="server" Text='<%# Eval("description") %>'></asp:Label>
                                    </ItemTemplate>
                                    <EditItemTemplate>
                                        <asp:TextBox ID="txtDescription" runat="server" Text='<%# Bind("description") %>' 
                                            CssClass="form-control" />
                                    </EditItemTemplate>
                                    <HeaderStyle ForeColor="White" BackColor="#488db4" Width="15%" />
                                    <ItemStyle Width="15%" HorizontalAlign="Justify" />
                                </asp:TemplateField>

                                <asp:TemplateField HeaderText="Trainer Name" SortExpression="trainerName" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
                                    <ItemTemplate>
                                        <asp:Label ID="lblTrainer" runat="server" Text='<%# Eval("trainerName") %>'></asp:Label>
                                    </ItemTemplate>
                                    <EditItemTemplate>
                                        <asp:DropDownList ID="traineDp" runat="server" 
                                            CssClass="form-control form-control-sm dropdown-icon"
                                            DataTextField="name"
                                            DataValueField="id">
                                        </asp:DropDownList>
                                    </EditItemTemplate>
                                    <HeaderStyle ForeColor="White" BackColor="#488db4" Width="20%" />
                                    <ItemStyle Width="20%" HorizontalAlign="Justify" />
                                </asp:TemplateField>

                               <asp:TemplateField HeaderText="Status" SortExpression="IsActive" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
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

                                <asp:TemplateField HeaderText="Actions" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
                                    <ItemTemplate>
                                        <div class="text-center">

                                             <%
                                                 var formPermissions = Session["formPermissions"] as Dictionary<string, string>;
                                                 string perm = formPermissions != null && formPermissions.ContainsKey("TrainingList") ? formPermissions["TrainingList"] : null;
                                             %>

                                             <% if (perm == "admin" || perm == "super") { %>
                                                 <asp:LinkButton ID="btnEdit" runat="server" CommandName="Edit" CausesValidation="False"
                                                    CssClass="btn btn-sm text-white ms-1 mb-1 me-1" BackColor="#0a61ae" ToolTip="Edit">
                                                    <i class="fas fa-pencil-alt"></i>
                                                </asp:LinkButton>
                                             <% } %>

                                             <% if (perm == "admin") { %>
                                                  <asp:LinkButton ID="btnDelete" runat="server" CommandName="Delete" CausesValidation="False"
                                                      CssClass="btn btn-sm text-white" BackColor="#453b3b" ToolTip="Delete">
                                                      <i class="fas fa-trash-alt"></i>
                                                  </asp:LinkButton>
                                             <% } %>
                                        </div>
                                    </ItemTemplate>
                                    <EditItemTemplate>
                                        <div class="text-center">
                                            <asp:LinkButton ID="btnUpdate" runat="server" CommandName="Update" CausesValidation="False"
                                                CssClass="btn btn-sm text-dark me-1" BackColor="#9ad9fe" ToolTip="Update">
                                                <i class="fas fa-save"></i>
                                            </asp:LinkButton>
                                            <asp:LinkButton ID="btnCancel" runat="server" CommandName="Cancel" CausesValidation="False"
                                                CssClass="btn btn-sm text-white btn-secondary" ToolTip="Cancel">
                                                <i class="fas fa-times"></i>
                                            </asp:LinkButton>
                                        </div>
                                    </EditItemTemplate>
                                    <HeaderStyle ForeColor="White" BackColor="#488db4" Width="15%" CssClass="text-center" />
                                    <ItemStyle Width="15%" CssClass="text-center" />
                                </asp:TemplateField>

                            </Columns>
                        </asp:GridView>
                    </div>
                 
                </div>
              </ContentTemplate>
            </asp:UpdatePanel>
            
        </div>

    <%-- New Topic Modal --%>
<div class="modal fade" id="topicModal" tabindex="-1" aria-hidden="true" ေaria-labelledby="topicModalLabel">
    <div class="modal-dialog modal-dialog-centered modal-md">
        <div class="modal-content rounded-4 shadow-lg border-0">

            <!-- Modal Header -->
            <div class="card-header fw-bolder text-center d-flex justify-content-between align-items-center p-3"
                style="background-color: #022F56; color:#c9b99f; border-top-left-radius: 10px; border-top-right-radius: 10px;">
                <h2 class="mb-0"><i class="bi bi-person-plus me-2"></i> New Topic</h2>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
            </div>

            <!-- Modal Body -->
            <div class="modal-body p-4" style="background-color: #f8f9fa;">
                <asp:Panel ID="pnlAddTopic" runat="server">
                    <div class="card-body text-white">
                        <!-- Name Field -->
                        <div class="row g-2 mb-3">
                            <label for="<%= topicName.ClientID %>" class="col-sm-3 col-form-label fa-1x fw-bolder" style="color:#076585;">Topic Name</label>
                            <div class="col-sm-9">
                                <asp:TextBox runat="server" CssClass="form-control form-control-sm fw-bolder fa-1x" ID="topicName" />
                                 <asp:RequiredFieldValidator ID="RequiredFieldValidator7" runat="server"
                                   ErrorMessage="Topic name is required!"
                                   ControlToValidate="topicName" Display="Dynamic"
                                   CssClass="text-danger d-block" SetFocusOnError="True" />
                            </div>
                        </div>

                        <!-- Desc Field -->
                        <div class="row g-2 mb-3">
                             <label for="<%= topicdesc.ClientID %>" class="col-sm-3 col-form-label fw-bolder fa-1x" style="color:#076585;">Description</label>
                             <div class="col-sm-9">
                                 <asp:TextBox runat="server" CssClass="form-control form-control-sm" ID="topicdesc" TextMode="MultiLine" />
                             </div>
                        </div>

                       <!-- Trainer Field -->
                       <div class="row g-2 mb-3">
                            <label for="<%= traineDp.ClientID %>" class="col-sm-3 col-form-label fa-1x fw-bolder" style="color:#076585;">Trainer</label>
                            <div class="col-sm-9">
                                 <asp:DropDownList ID="traineDp" runat="server" CssClass="form-control form-control-sm dropdown-icon">
                                 </asp:DropDownList>
                                 <asp:RequiredFieldValidator ID="RequiredFieldValidator8" runat="server"
                                   ErrorMessage="Trainer is required!"
                                   ControlToValidate="traineDp" Display="Dynamic"
                                   CssClass="text-danger d-block" SetFocusOnError="True" />
                            </div>
                        </div>

                    </div>
                </asp:Panel>
            </div>

            <!-- Modal Footer -->
            <div class="modal-footer" style="background-color: #f1f1f1;">
                <asp:Button Text="Add Topic" runat="server" CssClass="btn fw-bolder px-3 me-1 rounded-4"
                    Style="background-color: #022F56; color:#c9b99f;"
                    ID="addTopicBtn" OnClick="btnaddTopic_Click" />
                <button type="button" class="btn btn-secondary rounded-4" data-bs-dismiss="modal">Close</button>
            </div>

        </div>
    </div>
</div>

</asp:Content>
