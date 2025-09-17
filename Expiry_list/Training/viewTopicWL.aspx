<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="viewTopicWL.aspx.cs" Inherits="Expiry_list.Training.viewTopicWL" %>
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
             initTrainerMultiSelect("trainerDpMultiSelect", "<%= hfTrainerDp.ClientID %>");
             
             initAllGridTrainers();

             if (typeof (Sys) !== 'undefined') {
                 Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
                     initializeDataTable();
                     initTrainerMultiSelect("trainerDpMultiSelect", "<%= hfTrainerDp.ClientID %>");
                    initAllGridTrainers();
                });
            }
         });

        function initAllGridTrainers() {
            $('.trainer-multi-select').each(function () {
                const hiddenId = $(this).find('input[type=hidden]').attr('id');
                const topicId = $(this).data('topic');
                initTrainerMultiSelect($(this).attr('id'), hiddenId, topicId);
            });
        }

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

        function initTrainerMultiSelect(containerId, hiddenFieldId, topicId) {
            const container = $("#" + containerId);
            const inputBox = container.find(".multi-select-input");
            const dropdown = container.find(".multi-select-dropdown");
            const hiddenField = $("#" + hiddenFieldId);
            const hiddenNamesField = container.find("input[id$='hfTrainerNames']");

            dropdown.empty();
            const searchInput = $('<input type="text" class="dropdown-search" placeholder="Search trainers...">');
            dropdown.append(searchInput);

            let selectedTrainers = [];

            function loadExistingTrainers() {
                const existingIds = hiddenField.val() || "";
                const existingNames = hiddenNamesField.val() || "";
                const ids = existingIds.split(",").map(x => x.trim()).filter(x => x);
                const names = existingNames.split(",").map(x => x.trim());

                selectedTrainers = [];

                for (let i = 0; i < ids.length; i++) {
                    let trainerName = names[i];
                    if (!trainerName || trainerName === ids[i]) {
                        trainerName = ids[i];
                    }
                    selectedTrainers.push({ id: ids[i], text: trainerName });
                }

                if (selectedTrainers.some(t => t.text === t.id)) {
                    $.ajax({
                        url: 'viewTopicWL.aspx/GetTrainers',
                        type: 'POST',
                        contentType: 'application/json; charset=utf-8',
                        dataType: 'json',
                        data: JSON.stringify({ searchTerm: "" }),
                        success: function (response) {
                            const list = response.d || [];
                            selectedTrainers.forEach(t => {
                                const match = list.find(x => x.Id == t.id);
                                if (match) t.text = match.Name;
                            });
                            renderSelectedTrainers(); 
                        }
                    });
                }
            }

            function renderSelectedTrainers() {
                inputBox.html('');
                if (selectedTrainers.length === 0) {
                    inputBox.append('<input type="text" class="placeholder" placeholder="Select trainers..." readonly />');
                } else {
                    selectedTrainers.forEach(t => {
                        inputBox.append(`<span class="pill">${t.text} <span class="remove-pill" data-value="${t.id}" style="cursor:pointer;">&times;</span></span>`);
                    });
                    inputBox.append('<input type="text" style="width:2px;border:none;outline:none;" readonly />');
                }
                hiddenField.val(selectedTrainers.map(x => x.id).join(","));
                hiddenNamesField.val(selectedTrainers.map(x => x.text).join(", "));
            }

            function loadTrainers(searchTerm) {
                dropdown.find('div.trainer-item').remove();
                $.ajax({
                    url: 'viewTopicWL.aspx/GetTrainers',
                    type: 'POST',
                    contentType: 'application/json; charset=utf-8',
                    dataType: 'json',
                    data: JSON.stringify({ searchTerm: searchTerm || "" }),
                    success: function (response) {
                        const list = response.d || [];
                        if (list.length === 0) {
                            dropdown.append('<div class="no-result text-muted px-2 py-1">No trainers found</div>');
                        } else {
                            list.forEach(t => {
                                const isSelected = selectedTrainers.find(x => x.id == t.Id);
                                const activeClass = isSelected ? 'active' : '';
                                dropdown.append(`<div data-value="${t.Id}" class="trainer-item px-2 py-1 ${activeClass}">${t.Name}</div>`);
                                if (isSelected) isSelected.text = t.Name; // sync name
                            });
                        }
                    }
                });
            }

            // --- Event handlers ---
            inputBox.on("click", function (e) {
                e.stopPropagation();
                dropdown.toggle();
                if (dropdown.is(":visible")) searchInput.focus();
                loadTrainers("");
            });

            searchInput.on("keyup", function () { loadTrainers($(this).val()); });

            dropdown.on("click", ".trainer-item", function () {
                const id = $(this).data("value");
                const name = $(this).text().trim();
                if (!selectedTrainers.find(x => x.id == id)) selectedTrainers.push({ id, text: name });
                renderSelectedTrainers();
                searchInput.val('').focus();
            });

            inputBox.on("click", ".remove-pill", function (e) {
                e.stopPropagation();
                const id = $(this).data("value");
                selectedTrainers = selectedTrainers.filter(x => x.id != id);
                renderSelectedTrainers();
                dropdown.show();
                searchInput.focus();
            });

            $(document).click(e => { if (!$(e.target).closest(container).length) dropdown.hide(); });

            // --- Initial render ---
            loadExistingTrainers();
            renderSelectedTrainers();
        }

        // Initialize GridView rows on edit
        function initAllGridTrainers() {
            $('.trainer-multi-select').each(function () {
                const hiddenId = $(this).find('input[type=hidden]').attr('id');
                const topicId = $(this).data('topic');
                initTrainerMultiSelect($(this).attr('id'), hiddenId, topicId);
            });
        }

        function updateTrainer() {
            var ddl = document.getElementById('<%= topicName.ClientID %>');
             var selectedOption = ddl.options[ddl.selectedIndex];
             var trainerName = selectedOption.getAttribute("data-trainer");
            document.getElementById('<%= hfTrainerDp.ClientID %>').value = trainerName || '';
        }

         window.onload = function () {
             document.getElementById('<%= topicName.ClientID %>').addEventListener("change", updateTrainer);
         };
    </script>

</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
           <div class="container py-4">
           <asp:HiddenField ID="hfSelectedRows" runat="server" />
           <asp:HiddenField ID="hfSelectedIDs" runat="server" />
           <asp:HiddenField ID="hflength" runat="server" />   
           <asp:HiddenField ID="hfEditId" runat="server" />
           <asp:HiddenField ID="hfEditedRowId" runat="server" />
           <asp:HiddenField ID="hfSelectedTrainers" runat="server" />
                 
         <asp:ScriptManager ID="ScriptManager1" runat="server" />
           <asp:UpdatePanel ID="upGrid" runat="server">
             <ContentTemplate>

                <div class="card shadow-lg p-3 rounded-4">
                    <div class="card-body pb-1">
                        <div class="d-flex justify-content-between align-items-center add-topic-btn mb-2">
                            <h5 class="card-title fa-2x" style="color:#022f56;">Topic Via Level</h5>
                            <a href="#" class="btn text-white pe-3" 
                               style="background-color:#022f56;" 
                               data-bs-toggle="modal" 
                               data-bs-target="#topicModal">
                               <i class="fa-solid fa-user-plus ps-3"></i> Add New Topic With Level
                            </a>
                        </div>
                    </div>
                  
                     <div class="table-responsive">
                         <asp:GridView ID="GridView2" runat="server" AutoGenerateColumns="False" CssClass="table table-striped table-hover table-bordered"
                            DataKeyNames="id,topic,traineeLevel" AllowPaging="false" PagerSettings-Visible="false" OnRowEditing="GridView2_RowEditing"
                            OnRowUpdating="GridView2_RowUpdating" OnRowDeleting="GridView2_RowDeleting" 
                            OnRowCancelingEdit="GridView2_RowCancelingEdit" OnRowDataBound="GridView2_RowDataBound" HeaderStyle-BackColor="#4486ab" HeaderStyle-ForeColor="White"
                            EmptyDataText="No records found." ShowHeaderWhenEmpty="true" >

                            <Columns>
                                <asp:TemplateField ItemStyle-HorizontalAlign="Left" HeaderText="No" >
                                    <ItemTemplate>
                                        <asp:Label ID="lblLinesNo" runat="server" Text='<%# Container.DataItemIndex + 1 %>' />
                                    </ItemTemplate>
                                    <HeaderStyle ForeColor="White" BackColor="#488db4" Width="5%" />
                                    <ItemStyle Width="5%" HorizontalAlign="Left" />
                                </asp:TemplateField>

                                <asp:TemplateField HeaderText="Topic Name" SortExpression="topic" >
                                    <ItemTemplate>
                                        <asp:Label ID="lblTopicName" runat="server" Text='<%# Eval("topic") %>'></asp:Label>
                                    </ItemTemplate>
                                    <EditItemTemplate>
                                       <asp:TextBox runat="server" CssClass="form-control form-control-sm" ID="txtTopic"  Text='<%# Bind("topic") %>' />
                                    </EditItemTemplate>
                                    <HeaderStyle ForeColor="White" BackColor="#488db4" Width="45%" />
                                    <ItemStyle Width="45%" HorizontalAlign="Left" />
                                </asp:TemplateField>

                                <asp:TemplateField HeaderText="Trainee Level" SortExpression="traineeLevel" >
                                    <ItemTemplate>
                                        <asp:Label ID="lblTrainee" runat="server" Text='<%# Eval("traineeLevel") %>'></asp:Label>
                                    </ItemTemplate>
                                    <EditItemTemplate>
                                        <asp:DropDownList ID="ddlTraineeLevel" runat="server" CssClass="form-control" DataTextField="name" DataValueField="id">
                                        </asp:DropDownList>
                                    </EditItemTemplate>
                                    <HeaderStyle ForeColor="White" BackColor="#488db4" Width="15%" />
                                    <ItemStyle Width="15%" HorizontalAlign="Left" />
                                </asp:TemplateField>

                                <asp:TemplateField HeaderText="Trainer Name" SortExpression="trainerName">
                                    <ItemTemplate>
                                        <asp:Label ID="lblTrainer" runat="server" Text='<%# Eval("trainerNamesCsv") %>'></asp:Label>
                                    </ItemTemplate>
                                    <EditItemTemplate>
                                        <div id="trainerMultiSelect_<%# Eval("id") %>" 
                                             class="trainer-multi-select multi-select-container" 
                                             data-topic='<%# Eval("id") %>'>
                                            <div class="multi-select-input"></div>
                                            <div class="multi-select-dropdown"></div>
                                           <asp:HiddenField ID="hfTrainerIds" runat="server" Value='<%# Eval("trainerIdsCsv") %>' />
                                            <asp:HiddenField ID="hfTrainerNames" runat="server" Value='<%# Eval("trainerNamesCsv") %>' />
                                        </div>
                                    </EditItemTemplate>
                                    <HeaderStyle ForeColor="White" BackColor="#488db4" Width="20%" />
                                    <ItemStyle Width="20%" HorizontalAlign="Left" />
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
       
                               <asp:TemplateField HeaderText="Actions" ItemStyle-HorizontalAlign="Center" HeaderStyle-HorizontalAlign="Center">
                                    <ItemTemplate>
                                        <div style="text-align:center;">
                                            <%
                                                var formPermissions = Session["formPermissions"] as Dictionary<string, string>;
                                                string perm = formPermissions != null && formPermissions.ContainsKey("TrainingList") ? formPermissions["TrainingList"] : null;
                                            %>

                                            <% if (perm == "admin") { %>
                                                 <asp:LinkButton ID="btnEdit" runat="server" CommandName="Edit" CausesValidation="False"
                                                     CssClass="btn btn-sm text-white mt-1 ms-1 me-2" style="background-color:#0a61ae;" ToolTip="Edit">
                                                     <i class="fas fa-pencil-alt"></i>
                                                 </asp:LinkButton>
                                                 <asp:LinkButton ID="btnDelete" runat="server" CommandName="Delete" CausesValidation="False"
                                                     CssClass="btn btn-sm mt-1 ms-1 me-2 text-white" style="background-color:#453b3b;" ToolTip="Delete" >
                                                     <i class="fas fa-trash-alt"></i>
                                                 </asp:LinkButton>
                                            <% } %>
                                           
                                        </div>
                                    </ItemTemplate>
                                    <EditItemTemplate>
                                        <div style="text-align:center;">
                                            <asp:LinkButton ID="btnUpdate" runat="server" CommandName="Update" CausesValidation="False"
                                                CssClass="btn btn-sm text-white" style="background-color:#9ad9fe;" ToolTip="Update">
                                                <i class="fas fa-save"></i>
                                            </asp:LinkButton>
                                            <asp:LinkButton ID="btnCancel" runat="server" CommandName="Cancel" CausesValidation="False"
                                                CssClass="btn btn-sm ms-2 text-white btn-secondary" ToolTip="Cancel">
                                                <i class="fas fa-times"></i>
                                            </asp:LinkButton>
                                        </div>
                                    </EditItemTemplate>
                                    <HeaderStyle ForeColor="White" BackColor="#488db4" CssClass="text-center" Width="15%" />
                                    <ItemStyle HorizontalAlign="Center" />
                                </asp:TemplateField>
                            </Columns>

                        </asp:GridView>
                     </div>
                
                </div>
             </ContentTemplate>
           </asp:UpdatePanel>  
       </div>

<%-- New Topic Via Level Modal --%>
<div class="modal fade" id="topicModal" tabindex="-1" aria-hidden="true" aria-labelledby="topicModalLabel">
    <div class="modal-dialog modal-dialog-centered modal-lg">
        <div class="modal-content rounded-4 shadow-lg border-0">

            <!-- Modal Header -->
            <div class="card-header fw-bolder d-flex justify-content-between align-items-center p-3"
                 style="background-color: #022F56; color:#c9b99f; border-top-left-radius: 12px; border-top-right-radius: 12px;">
                <h4 class="mb-0">New Topic With Level</h4>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
            </div>

            <!-- Modal Body -->
            <div class="modal-body p-4" style="background-color: #f8f9fa;">
                <asp:Panel ID="pnlAddTopic" runat="server">
                    <div class="row g-3">
                        
                        <!-- Topic -->
                        <div class="col-12 col-md-6">
                            <label for="<%= topicName.ClientID %>" class="form-label fw-bold text-dark">Topic</label>
                            <asp:TextBox runat="server" CssClass="form-control form-control-sm" ID="topicName" />
                            <asp:RequiredFieldValidator ID="RequiredFieldValidator7" runat="server"
                                ErrorMessage="Topic is required!" ControlToValidate="topicName" Display="Dynamic"
                                CssClass="text-danger small d-block" SetFocusOnError="True" />
                        </div>

                        <!-- Level -->
                        <div class="col-12 col-md-6">
                            <label for="<%= levelDb.ClientID %>" class="form-label fw-bold text-dark">Level</label>
                            <asp:DropDownList ID="levelDb" runat="server" CssClass="form-select form-select-sm"></asp:DropDownList>
                            <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server"
                                ErrorMessage="Level is required!" ControlToValidate="levelDb" Display="Dynamic"
                                CssClass="text-danger small d-block" SetFocusOnError="True" />
                        </div>

                        <!-- Trainer -->
                        <div class="col-12">
                            <label class="form-label fw-bold text-dark">Trainer</label>
                            <div id="trainerDpMultiSelect" class="trainer-multi-select multi-select-container">
                                <div class="multi-select-input"></div>
                                <div class="multi-select-dropdown"></div>
                            </div>
                            <asp:HiddenField ID="hfTrainerDp" runat="server" />
                            <asp:HiddenField ID="HiddenField1" runat="server" />
                        </div>
                    </div>
                </asp:Panel>
            </div>

            <!-- Modal Footer -->
            <div class="modal-footer d-flex justify-content-end">
                <asp:Button Text="Add" runat="server" CssClass="btn fw-bold px-4"
                    Style="background-color: #022F56; color:#c9b99f; border-radius: 30px;"
                    ID="addTopicBtn" OnClick="btnaddTopic_Click" />
                <button type="button" class="btn btn-secondary rounded-4" data-bs-dismiss="modal">Close</button>
            </div>
        </div>
    </div>
</div>

</asp:Content>
