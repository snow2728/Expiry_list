<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="viewTrainee.aspx.cs" Inherits="Expiry_list.Training.viewTrainee" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
      <a href="../AdminDashboard.aspx" class="btn text-white ms-2" style="background-color : #022f56;"><i class="fa-solid fa-left-long"></i> Home</a>
         <div class="container py-4">
         <div class="d-flex justify-content-between align-items-center mb-4">
             <h2>Trainer List</h2>
             <a href="addTrainee.aspx" class="btn text-white" style="background-color:#022f56;"><i class="fa-solid fa-user-plus"></i> Add New Trainer</a>
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

                      <asp:TemplateField HeaderText="Name" SortExpression="name" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1" ItemStyle-CssClass="fixed-column-1">
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

                      <asp:TemplateField HeaderText="Position" SortExpression="position" HeaderStyle-CssClass="position-sticky top-0 z-3 sticky-header1" ItemStyle-CssClass="fixed-column-1">
                          <ItemTemplate>
                              <asp:Label ID="lblPosition" runat="server" Text='<%# Eval("position") %>'></asp:Label>
                          </ItemTemplate>
                          <EditItemTemplate>
                              <asp:TextBox ID="txtPosition" runat="server" Text='<%# Bind("position") %>' 
                                  CssClass="form-control" />
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
</asp:Content>
