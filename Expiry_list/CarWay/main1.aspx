<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" Inherits="Expiry_list.CarWay.main1" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
      <a href="../AdminDashboard.aspx" class="btn text-white ms-2" style="background-color: #996FD6;"><i class="fa-solid fa-left-long"></i> Home</a>
    <div>
        <asp:Button runat="server"
       CssClass="btn btn-warning text-white rounded-pill px-4"
       OnClick="cc_Click1"
       Text="CarWay Create →" />

    <asp:Button runat="server"
       CssClass="btn btn-warning text-white rounded-pill px-4"
       OnClick="c1_Click1"
       Text="WH View →" />
</div>

</asp:Content>
