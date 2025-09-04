<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="main1.aspx.cs" Inherits="Expiry_list.CarWay.main1" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <script type="text/javascript">
        document.addEventListener('DOMContentLoaded', function () {
            document.getElementById("link_home").href = "../AdminDashboard.aspx";
        });
    </script>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

      
    <div>
        <asp:Button runat="server"
       CssClass="btn btn-warning text-white rounded-pill px-4"
       OnClick="cc_Click1"
       Text="CarWay Create →" />

    <asp:Button runat="server"
       CssClass="btn btn-warning text-white rounded-pill px-4"
       OnClick="c1_Click1"
       Text="WH View →" />


</asp:Content>
