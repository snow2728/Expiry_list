<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="viewTopic.aspx.cs" Inherits="Expiry_list.Training.viewTopic" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">

</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
     <a href="../AdminDashboard.aspx" class="btn text-white ms-2" style="background-color : #022f56;"><i class="fa-solid fa-left-long"></i> Home</a>
            <div class="container py-4">
            <div class="d-flex justify-content-between align-items-center mb-4">
                <h2>Topic List</h2>
               <a href="#" class="btn text-white" style="background-color:#022f56;" 
                    data-bs-toggle="modal" data-bs-target="#traineeModal">
                    <i class="fa-solid fa-user-plus"></i> Add New Topic
                 </a>
            </div>

            <asp:HiddenField ID="hfSelectedRows" runat="server" />
            <asp:HiddenField ID="hfSelectedIDs" runat="server" />
            <asp:HiddenField ID="hflength" runat="server" />   
            <asp:HiddenField ID="hfEditId" runat="server" />
            <asp:HiddenField ID="hfEditedRowId" runat="server" />

            <asp:ScriptManager ID="ScriptManager1" runat="server" />
            <asp:UpdatePanel ID="upGrid" runat="server">
              <ContentTemplate>
                 <asp:GridView ID="GridView2" runat="server" AutoGenerateColumns="False" CssClass="table table-striped table-bordered table-hover border border-2 shadow-lg sticky-grid mt-1 overflow-scroll"
                     DataKeyNames="id" OnRowEditing="GridView2_RowEditing"
                     OnRowUpdating="GridView2_RowUpdating" OnRowDeleting="GridView2_RowDeleting" OnRowCancelingEdit="GridView2_RowCancelingEdit" OnRowDataBound="GridView2_RowDataBound" >
                     <Columns>

                       <asp:TemplateField ItemStyle-HorizontalAlign="Justify" HeaderText="No" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header2" ItemStyle-CssClass="fixed-column-2">
                          <ItemTemplate>
                              <asp:Label ID="lblLinesNo" runat="server" Text='<%# Container.DataItemIndex + 1 %>' />
                          </ItemTemplate>
                          <ControlStyle Width="50px" />
                          <HeaderStyle ForeColor="White" BackColor="#488db4" />
                          <ItemStyle HorizontalAlign="Justify" />
                      </asp:TemplateField>

                         <asp:TemplateField HeaderText="Topic Name" SortExpression="topicName" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1" ItemStyle-CssClass="fixed-column-1">
                             <ItemTemplate>
                                 <asp:Label ID="lblTopicName" runat="server" Text='<%# Eval("topicName") %>'></asp:Label>
                             </ItemTemplate>
                             <EditItemTemplate>
                                 <asp:TextBox ID="txtTopicName" runat="server" Text='<%# Bind("topicName") %>' 
                                     CssClass="form-control" />
                             </EditItemTemplate>
                              <HeaderStyle ForeColor="White" BackColor="#488db4" />
                              <ItemStyle HorizontalAlign="Justify" />
                         </asp:TemplateField>

                         <asp:TemplateField HeaderText="Description" SortExpression="description" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1" ItemStyle-CssClass="fixed-column-1">
                             <ItemTemplate>
                                 <asp:Label ID="lblDescription" runat="server" Text='<%# Eval("description") %>'></asp:Label>
                             </ItemTemplate>
                             <EditItemTemplate>
                                 <asp:TextBox ID="txtDescription" runat="server" Text='<%# Bind("description") %>' 
                                     CssClass="form-control" />
                             </EditItemTemplate>
                              <HeaderStyle ForeColor="White" BackColor="#488db4" />
                              <ItemStyle HorizontalAlign="Justify" />
                         </asp:TemplateField>

                           <asp:TemplateField HeaderText="Trainer Name" SortExpression="trainerName" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1" ItemStyle-CssClass="fixed-column-1">
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
                                <HeaderStyle ForeColor="White" BackColor="#488db4" />
                                <ItemStyle HorizontalAlign="Justify" />
                            </asp:TemplateField>
        
                         <asp:CommandField ShowEditButton="true" ButtonType="Button" 
                             ControlStyle-CssClass="btn btn-sm m-1 text-white" ControlStyle-BackColor="#022f56" ItemStyle-CssClass="fixed-column-1" HeaderStyle-BackColor="#488db4" HeaderStyle-ForeColor="White" />
                         <asp:CommandField ShowDeleteButton="true" ButtonType="Button" 
                             ControlStyle-CssClass="btn btn-sm m-1 btn-danger" ItemStyle-CssClass="fixed-column-1" HeaderStyle-BackColor="#488db4" HeaderStyle-ForeColor="White" />
                     </Columns>
                 </asp:GridView>
              </ContentTemplate>
            </asp:UpdatePanel>
            
        </div>

<div class="modal fade" id="traineeModal" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog modal-dialog-centered modal-lg">
        <div class="modal-content rounded-4 shadow-lg border-0">

            <!-- Modal Header -->
            <div class="modal-header" style="background-color: #022F56; color: #c9b99f;">
                <h5 class="modal-title fw-bold">Add New Trainee</h5>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>

            <!-- Modal Body -->
            <div class="modal-body p-4" style="background-color: #f8f9fa;">
                <asp:Panel ID="pnlAddTopic" runat="server" style="background-color: #CCDEE4;">
                   <!-- Card Header -->
                   <div class="card-header fw-bolder text-center d-flex justify-content-between align-items-center p-3"
                       style="background-color: #022F56; color:#c9b99f; border-top-left-radius: 10px; border-top-right-radius: 10px;">
                       <h2 class="mb-0">New Topic</h2>
                       <a href="viewTopic.aspx" class="btn text-white" style="background-color:#488db4;"><i class="fa-solid fa-user-plus"></i>View Topic List</a>
                   </div>
                    <div class="card-body text-white text-center">
                        <!-- Name Field -->
                        <div class="row g-2 mb-3">
                            <label for="<%= topicName.ClientID %>" class="col-sm-4 col-form-label fa-1x fw-bolder" style="color:#076585;">Topic Name</label>
                            <div class="col-sm-8">
                                <asp:TextBox runat="server" CssClass="form-control form-control-sm fw-bolder fa-1x" ID="topicName" />
                                 <asp:RequiredFieldValidator ID="RequiredFieldValidator7" runat="server"
                                   ErrorMessage="Topic name is required!"
                                   ControlToValidate="topicName" Display="Dynamic"
                                   CssClass="text-danger d-block" SetFocusOnError="True" />
                            </div>
                        </div>

                        <!-- Desc Field -->
                        <div class="row g-2 mb-3">
                             <label for="<%= topicdesc.ClientID %>" class="col-sm-4 col-form-label fw-bolder fa-1x" style="color:#076585;">Description</label>
                             <div class="col-sm-8">
                                 <asp:TextBox runat="server" CssClass="form-control form-control-sm" ID="topicdesc" TextMode="MultiLine" />
                             </div>
                        </div>

                       <!-- Trainer Field -->
                       <div class="row g-2 mb-3">
                            <label for="<%= traineDp.ClientID %>" class="col-sm-4 col-form-label fa-1x fw-bolder" style="color:#076585;">Trainer</label>
                            <div class="col-sm-8">
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
                <asp:Button Text="Add Topic" runat="server" CssClass="btn fw-bolder px-4 me-2"
                    Style="background-color: #022F56; color:#c9b99f; border-radius: 20px;"
                    ID="addTopicBtn" OnClick="btnaddTopic_Click" />
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
            </div>

        </div>
    </div>
</div>
</asp:Content>
