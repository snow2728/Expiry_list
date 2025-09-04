<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="loginPage.aspx.cs" Inherits="Expiry_list.loginPage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <script type="text/javascript">
        history.pushState(null, null, location.href);

        window.addEventListener("popstate", function (event) {
            location.reload();
        });       

    </script>

</asp:Content>
    
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
     
    <div class="container d-flex justify-content-center align-items-center vh-100" >
        <div class="card shadow-lg" style="border-radius: 20px; width: 100%; max-width: 400px; background-color: #a1d6e2;">
            <div class="card-body p-5 text-center">
                <h2 class="mb-5 text-white py-3" style="background-color: #1995ad; border-top-left-radius: 20px; border-top-right-radius: 20px;">
                    Sign In
                </h2>

                <!-- Username Field -->
                <div class="form-group mb-4">
                    <asp:TextBox ID="usernameTextBox" runat="server" CssClass="form-control rounded-pill p-3" placeholder="Username" AutoFocus="true" AutoComplete="username" />
                    <asp:RequiredFieldValidator ID="usernameRequired" runat="server" ControlToValidate="usernameTextBox" ErrorMessage="Username is required" CssClass="text-danger" Display="Dynamic" />
                </div>

                <!-- Password Field -->
                <div class="form-group mb-4">
                    <asp:TextBox ID="passwordTextBox" runat="server" TextMode="Password" CssClass="form-control rounded-pill p-3" placeholder="Password" AutoComplete="current_password" />
                    <asp:RequiredFieldValidator ID="passwordRequired" runat="server" ControlToValidate="passwordTextBox" ErrorMessage="Password is required" CssClass="text-danger" Display="Dynamic" />
                </div>

                <!-- Sign In Button -->
                <div class="form-group">
                    <asp:Button runat="server" Text="Sign in" CssClass="btn btn-block py-2" style="background-color: #1995ad; color: #f1f1f2; border-radius: 50px;" OnClick="Unnamed1_Click" />
                </div>
            </div>
        </div>
    </div>

</asp:Content>
