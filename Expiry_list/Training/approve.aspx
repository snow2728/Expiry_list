<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="approve.aspx.cs" Inherits="Expiry_list.Training.approve" %>
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
        // Reinitialize DataTables after partial postback
        if (typeof (Sys) !== 'undefined') {
            Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
                initializeDataTableMain();
            });
        }
    });

    document.addEventListener('DOMContentLoaded', function () {
        document.getElementById("link_home").href = "../AdminDashboard.aspx";
    });

    function showCancelSweetAlert(linkBtn) {
        Swal.fire({
            title: 'Cancel Schedule?',
            text: 'Are you sure you want to cancel this schedule? This action cannot be undone.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Yes, cancel it',
            cancelButtonText: 'No, keep it',
            confirmButtonColor: '#d33',
            cancelButtonColor: '#3085d6',
            reverseButtons: true,
            focusCancel: true
        }).then((result) => {
            if (result.isConfirmed) {
                linkBtn.onclick = null; 
                linkBtn.click();  
            }
        });

        // Prevent immediate postback
        return false;
    }

    function toggleAllCheckboxes(headerCb) {
        const isChecked = headerCb.checked;
        $('.rowCheckbox').prop('checked', isChecked);
        updateSelectedIDs();
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
            lengthMenu: [[25, 50, 100], [25, 50, 100]],
            columnDefs: [
                { orderable: false, targets: [0, 9], width: "50px", className: "text-center align-middle" },
                { targets: [2, 3], visible: false }
            ],
            initComplete: function () {
                const api = this.api();
                const headerCheckbox = $('#chkAll1');
                headerCheckbox.off('click').on('click', function () {
                    toggleAllCheckboxes(this);
                });
            },
            drawCallback: function () {
                const totalRows = this.api().rows().count();
                const checkedRows = $('.rowCheckbox:checked').length;
                const headerCheckbox = $('#chkAll1');

                if (headerCheckbox.length) {
                    headerCheckbox.prop('checked', totalRows > 0 && checkedRows === totalRows);
                    headerCheckbox.prop('indeterminate', checkedRows > 0 && checkedRows < totalRows);
                }
            }
        });

        // Function to update selected rows
        function updateSelectedRows() {
            const selectedIds = [];
            $('.rowCh7ckbox:checked').each(function() {
                selectedIds.push($(this).data('id'));
            });
        
               $('#<%= hfSelectedRows.ClientID %>').val(selectedIds.join(','));
               console.log('Selected IDs:', selectedIds);
           }

           // Initialize selected rows
           updateSelectedRows();
    }

  <%--  function updateSelectedIDs() {
        var selectedIDs = [];
        $('.rowCheckbox:checked').each(function () {
            var id = $(this).data('id');
            if (id) {
                selectedIDs.push(id);
            }
        });
        $('#<%= hfSelectedIDs.ClientID %>').val(selectedIDs.join(','));
          //console.log('Selected IDs:', selectedIDs);
      }--%>

    function highlightRow(btn) {
        $('#<%= GridView2.ClientID %> tbody tr').removeClass('selected-row');

           var row = $(btn).closest('tr');
           row.addClass('selected-row');
    }
</script>

    <style>
        table.dataTable td:first-child,
        table.dataTable th:first-child {
            width: 50px !important;
            text-align: center !important;
            vertical-align: middle !important;
        }

        .dataTables_scrollHead thead th {
            position: sticky;
            top: 0;
            background-color: #4486ab !important;
            color: white !important;
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

  <asp:HiddenField ID="hfSelectedRows" runat="server" />
  <asp:HiddenField ID="hfSelectedIDs" runat="server" />

      <%-- Gridview --%>
      <div class="card shadow-sm  gridview-container rounded-1" style="height:auto">
          <div class="card-body">
            <div class="table-responsive gridview-container pt-2 pe-2 rounded-1">
               <asp:GridView ID="GridView2" runat="server"
                    CssClass="table table-striped ResizableGrid table-hover border-2 shadow-lg sticky-grid overflow-x-auto display"
                    AutoGenerateColumns="False"
                    DataKeyNames="id"
                    OnRowDeleting="GridView2_RowDeleting"
                    OnRowCommand="GridView2_RowCommand"
                    AllowPaging="false"
                    ShowHeaderWhenEmpty="true"
                    HeaderStyle-BackColor="#4486ab"
                    HeaderStyle-ForeColor="White"
                    GridLines="None"
                    AllowSorting="false"
                    EmptyDataText="No records found.">
    
                    <Columns>
                        <asp:TemplateField HeaderText="" ItemStyle-Width="10%" HeaderStyle-Width="10%" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center" HeaderStyle-CssClass="sticky-header1">
                            <HeaderTemplate>
                                <input type="checkbox" id="chkAll1" onclick="toggleAllCheckboxes(this)" />
                            </HeaderTemplate>
                            <ItemTemplate>
                                <input type="checkbox" class="rowCheckbox" data-id='<%# Eval("id") %>' />
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Topic Name" ItemStyle-Width="25%" ItemStyle-CssClass="text-left" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
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

                        <asp:TemplateField HeaderText="Trainer Name" ItemStyle-Width="15%" ItemStyle-CssClass="text-left" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
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

                        <asp:TemplateField HeaderText="Training Time" ItemStyle-Width="15%" ItemStyle-CssClass="text-left" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
                            <ItemTemplate>
                                <asp:Label ID="lblTime" runat="server" Text='<%# Eval("time") %>' CssClass="d-block text-left" />
                            </ItemTemplate>
                        </asp:TemplateField>

                        <asp:TemplateField HeaderText="Actions" ItemStyle-Width="15%" ItemStyle-CssClass="text-center" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1">
                            <ItemTemplate>
                                <div class="text-center">
                                    <%
                                        var formPermissions = Session["formPermissions"] as Dictionary<string, string>;
                                        string perm = formPermissions != null && formPermissions.ContainsKey("TrainingList") ? formPermissions["TrainingList"] : null;
                                    %>
                                    <div class="btn-group" role="group">
                                        <% if (perm == "admin" || perm == "approve") { %>
                                            <asp:LinkButton ID="btnApprove" runat="server"
                                                CssClass="btn btn-sm btn-danger me-1 btnApproveSchedule"
                                                CommandName="CancelSchedule"
                                                CommandArgument='<%# Eval("id") %>'
                                                ToolTip="Approve Schedule"
                                                OnClientClick="return showCancelSweetAlert(this);">
                                                <i class="fa fa-ban"></i>
                                            </asp:LinkButton>
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
</asp:Content>
