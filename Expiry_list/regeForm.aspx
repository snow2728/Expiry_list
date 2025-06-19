<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="regeForm.aspx.cs" Inherits="Expiry_list.regeForm" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">

</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <a href="AdminDashboard.aspx" class="btn text-white" style="background-color : #158396;"><i class="fa-solid fa-left-long"></i> Home</a>

    <div class="container-fluid">
    <div class="row gy-4">
        <!-- Registration Form -->
        <div class="col-12 col-lg-5 col-xxl-4 mb-4">
            <div class="card shadow rounded-4 p-3">
                <!-- Username -->
                <div class="mb-3">
                    <label for="<%= usernameTextBox.ClientID %>" class="form-label">Username</label>
                    <asp:TextBox ID="usernameTextBox" runat="server" 
                        CssClass="form-control border-info shadow-sm" 
                        AutoFocus="true" AutoComplete="username"/>
                    <asp:RequiredFieldValidator ID="usernameRequired" runat="server"
                        ControlToValidate="usernameTextBox"
                        ErrorMessage="Username is required"
                        CssClass="text-danger small d-block mt-1"
                        Display="Dynamic" />
                </div>

                <!-- Password -->
                <div class="mb-3">
                    <label for="<%= passwordTextBox.ClientID %>" class="form-label">Password</label>
                    <asp:TextBox ID="passwordTextBox" runat="server" 
                        TextMode="Password" 
                        CssClass="form-control border-info shadow-sm" 
                        AutoComplete="current-password" />
                    <asp:RequiredFieldValidator ID="passwordRequired" runat="server"
                        ControlToValidate="passwordTextBox"
                        ErrorMessage="Password is required"
                        CssClass="text-danger small d-block mt-1"
                        Display="Dynamic" />
                </div>

                <!-- Role & Store -->
                <div class="row g-3 mb-4">
                    <div class="col-12 col-md-6">
                        <label for="<%= roleTextBox.ClientID %>" class="form-label">Role</label>
                        <asp:DropDownList ID="roleTextBox" 
                            CssClass="form-select border-info shadow-sm" 
                            runat="server">
                            <asp:ListItem Text="Choose Role..." Value="" />
                            <asp:ListItem Text="Admin" Value="admin" />
                            <asp:ListItem Text="User" Value="user" />
                            <asp:ListItem Text="Viewer" Value="viewer" />
                        </asp:DropDownList>
                        <asp:RequiredFieldValidator ID="roleRequired" runat="server"
                            ControlToValidate="roleTextBox"
                            ErrorMessage="Role is required"
                            CssClass="text-danger small d-block mt-1"
                            Display="Dynamic" />
                    </div>

                    <div class="col-12 col-md-6" id="storeDrop" runat="server">
                        <label for="<%= storeTextBox.ClientID %>" class="form-label">Store</label>
                        <asp:DropDownList ID="storeTextBox" runat="server" 
                            CssClass="form-select border-info shadow-sm">
                            <asp:ListItem Text="" Value="" />
                        </asp:DropDownList>
                        <asp:RequiredFieldValidator ID="storeNoRequired" runat="server"
                            ControlToValidate="storeTextBox"
                            ErrorMessage="Store is required"
                            CssClass="text-danger small d-block mt-1"
                            Display="Dynamic" />
                    </div>
                </div>

                <!-- Register Button -->
                <div class="">
                    <asp:Button ID="btnRegister" runat="server" Text="Register" 
                        OnClick="btnRegister_Click" 
                        CssClass="btn btn-primary btn-md fw-bold shadow-sm" 
                        style="background-color: #158396; border-color: #127485;" />
                </div>
            </div>
        </div>

           <div class="col-12 col-lg-7 col-xxl-8">
            <asp:ScriptManager ID="ScriptManager1" EnablePartialRendering="true" runat="server" />
            <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                <ContentTemplate>
                    <div class="card shadow rounded-4 p-3 mb-5">
                        <!-- Scrollable Table Container -->
                        <div class="table-responsive rounded-4" style="max-height: 515px;">
                            <asp:GridView ID="userGridView" runat="server" 
                                CssClass="table table-striped table-hover align-middle mb-0"  
                                AutoGenerateColumns="false"
                                OnRowEditing="userGridView_RowEditing" 
                                OnRowUpdating="userGridView_RowUpdating" 
                                OnRowCancelingEdit="userGridView_RowCancelingEdit" 
                                OnRowDeleting="userGridView_RowDeleting"
                                OnPageIndexChanging="GridView2_PageIndexChanging"  
                                OnRowDataBound="userGridView_RowDataBound"
                                OnDataBound="userGridView_DataBound"
                                OnSorting="userGridView_Sorting"
                                AllowPaging="True" HeaderStyle-CssClass="sticky-header" AllowSorting="True" PageSize="25" CellPadding="4" ForeColor="#333333" GridLines="None"
                                DataKeyNames="id"> 
                            
                                    <HeaderStyle CssClass="sticky-top text-white" BackColor="#1995AD" />
                                    <RowStyle CssClass="cursor-pointer" />

                                <%--<HeaderStyle BackColor="#7cd2dd" ForeColor="White" Font-Bold="True" />--%>
 
                            <Columns>
                                <asp:TemplateField HeaderText="Username" SortExpression="Username" ItemStyle-CssClass="text-nowrap">
                                     <ItemTemplate>
                                         <asp:Label ID="lblUsername" runat="server" Text='<%# Eval("Username") %>' />
                                     </ItemTemplate>
                                     <EditItemTemplate>
                                         <asp:TextBox ID="txtUsername" runat="server" Text='<%# Eval("Username") %>' />
                                     </EditItemTemplate>
                                     <HeaderStyle ForeColor="#ffffff" />
                                 </asp:TemplateField>
        
                                <asp:TemplateField HeaderText="Password" SortExpression="Password" ItemStyle-CssClass="text-truncate">
                                    <ItemTemplate>
                                        <asp:Label ID="lblPassword" runat="server" Text='<%# MaskPassword(Eval("Password")) %>' />
                                    </ItemTemplate>
                                    <EditItemTemplate>
                                        <asp:TextBox ID="txtPassword" TextMode="Password" runat="server" Text='<%# Eval("Password") %>' CssClass="form-control" />
                                    </EditItemTemplate>
                                    <HeaderStyle ForeColor="#ffffff" />
                                </asp:TemplateField>

                                <asp:TemplateField HeaderText="Role" SortExpression="Role" ItemStyle-CssClass="priority-2">
                                    <ItemTemplate>
                                        <asp:Label ID="lblRole" runat="server" Text='<%# Eval("Role") %>' />
                                    </ItemTemplate>
                                    <EditItemTemplate>
                                        <asp:DropDownList ID="ddlEditRole" runat="server">
                                            <asp:ListItem Text="Admin" Value="admin" />
                                            <asp:ListItem Text="User" Value="user" />
                                            <asp:ListItem Text="Viewer" Value="viewer" />
                                        </asp:DropDownList>
                                    </EditItemTemplate>
                                     <HeaderStyle ForeColor="#ffffff" />
                                </asp:TemplateField>

                               <asp:TemplateField HeaderText="Store" SortExpression="StoreName" ItemStyle-CssClass="priority-3">
                                    <ItemTemplate>
                                        <asp:Label ID="lblStore" runat="server" Text='<%# Eval("StoreName") %>' />
                                    </ItemTemplate>
                                    <EditItemTemplate>
                                        <asp:DropDownList ID="ddlEditStore" runat="server">
                                            <asp:ListItem Text="" />
                                        </asp:DropDownList>
                                    </EditItemTemplate>
                                    <HeaderStyle ForeColor="#ffffff" />
                                </asp:TemplateField>

                              <asp:TemplateField HeaderText="Enabled" ItemStyle-CssClass="priority-4">
                                    <ItemTemplate>
                                        <asp:CheckBox ID="chkIsEnabled" runat="server" Checked='<%# Eval("IsEnabled") %>' Enabled="false" />
                                    </ItemTemplate>
                                    <EditItemTemplate>
                                        <asp:CheckBox ID="chkEditIsEnabled" runat="server" Checked='<%# Eval("IsEnabled") %>' />
                                    </EditItemTemplate>
                                    <HeaderStyle ForeColor="#ffffff" />
                                </asp:TemplateField>

                                <asp:CommandField ShowEditButton="true" CausesValidation="false" ControlStyle-CssClass="btn text-white m-1" ControlStyle-BackColor="#166f7c" />
                                <asp:CommandField ShowDeleteButton="true" CausesValidation="false" ControlStyle-CssClass="btn text-white m-1" ControlStyle-BackColor="#166f7c" />

                            </Columns>

                              <PagerTemplate>
                                <div class="fixed-bottom">
                                    <div class="container-fluid py-2  mt-5">
                                        <div class="d-flex flex-column flex-md-row justify-content-center align-items-center gap-2">
                                            <div class="d-flex gap-2">
                                                <asp:LinkButton ID="lnkPrev" runat="server" CausesValidation="false" CommandName="Page" CommandArgument="Prev" CssClass="btn m-1" BackColor="#158396">
                                                    Previous
                                                </asp:LinkButton>

                                                <asp:LinkButton ID="lnkNext" runat="server" CommandName="Page" CausesValidation="false" CommandArgument="Next" CssClass="btn m-1" BackColor="#158396">
                                                    Next
                                                </asp:LinkButton>
                                            </div>
                                            <asp:Label ID="lblShowing" runat="server" CssClass="mt-1 d-block fw-bold"></asp:Label>
                                        </div>
                                    </div>
                                </div>
                            </PagerTemplate>

                           <PagerStyle CssClass="pagination-container" />

                        </asp:GridView>
                        </div>
                    </ContentTemplate>
               </asp:UpdatePanel>
            </div>
        </div>
    </div>

</asp:Content>
