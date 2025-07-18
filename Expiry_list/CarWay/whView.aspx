<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="whView.aspx.cs" Inherits="Expiry_list.CarWay.whView" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">

</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
<<<<<<< HEAD

      <a href="../AdminDashboard.aspx" class="btn text-white ms-2" style="background-color: #996FD6;"><i class="fa-solid fa-left-long"></i> Home</a>

  <%--  <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePartialRendering="true">
         <Scripts>
             <asp:ScriptReference Name="MicrosoftAjax.js" />
             <asp:ScriptReference Name="MicrosoftAjaxWebForms.js" />
         </Scripts>
     </asp:ScriptManager>--%>

    <div class="col-md-12 ms-3" id="gridCol">
   <%--<asp:UpdatePanel ID="UpdatePanel2" runat="server" UpdateMode="Conditional">
=======
    <div class="col-md-12 ms-3" id="gridCol">
   <asp:UpdatePanel ID="UpdatePanel2" runat="server" UpdateMode="Conditional">
>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b
     <ContentTemplate>

        <asp:Panel ID="pnlNoData" runat="server" Visible="false">
              <div class="alert alert-info">No items to Filter</div>
<<<<<<< HEAD
        </asp:Panel>--%>
=======
        </asp:Panel>
>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b

         <div class="table-responsive gridview-container " style="height: 673px">
             <asp:GridView ID="GridView2" runat="server"
                 CssClass="table table-striped table-bordered table-hover shadow-lg sticky-grid mt-1 overflow-x-auto overflow-y-auto"
                 AutoGenerateColumns="False"
                 DataKeyNames="id"
                 UseAccessibleHeader="true"
                 AllowPaging="false"
                 PageSize="100"
                 CellPadding="4"
                 ForeColor="#333333"
                 GridLines="None"
                 AutoGenerateEditButton="false" ShowHeaderWhenEmpty="true" >

                      <EditRowStyle BackColor="#999999" />
                      <FooterStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />

                      <HeaderStyle Wrap="true" BackColor="#808080" Font-Bold="True" ForeColor="White"></HeaderStyle>
                      <PagerStyle CssClass="pagination-wrapper" HorizontalAlign="Center" VerticalAlign="Middle" />
                      <RowStyle CssClass="table-row data-row" BackColor="#F7F6F3" ForeColor="#333333"></RowStyle>
                      <AlternatingRowStyle CssClass="table-alternating-row" BackColor="White" ForeColor="#284775"></AlternatingRowStyle>

                      <EmptyDataTemplate>
                          <div class="alert alert-info">No items to Filter</div>
                      </EmptyDataTemplate>

                   <Columns>

                         <asp:TemplateField ItemStyle-HorizontalAlign="Justify" HeaderText="No" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" >
                             <ItemTemplate>
                                 <asp:Label ID="lblLinesNo" runat="server" Text='<%# Container.DataItemIndex + 1 %>' />
                             </ItemTemplate>
                             <ControlStyle Width="50px" />
                             <HeaderStyle ForeColor="White" BackColor="Gray" />
                             <ItemStyle HorizontalAlign="Justify" />
                         </asp:TemplateField>

                       <asp:TemplateField HeaderText="Way No" ItemStyle-Width="100px" SortExpression="wayno" HeaderStyle-ForeColor="White" ItemStyle-HorizontalAlign="Justify" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" >
                          <ItemTemplate>
                              <asp:Label ID="lblWayNo" runat="server" Text='<%# Eval("wayNo") %>' />
                          </ItemTemplate><HeaderStyle ForeColor="White" BackColor="Gray" />
                          <ControlStyle Width="100px" />
                          <ItemStyle HorizontalAlign="Justify" />
                      </asp:TemplateField>

                      <asp:TemplateField HeaderText="Car No" SortExpression="carNo" HeaderStyle-ForeColor="Black" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" >
                          <ItemTemplate>
                              <asp:Label ID="lblCarNo" runat="server" Text='<%# Eval("carNo") %>' />
                          </ItemTemplate>
                          <ControlStyle Width="257px" />
                          <HeaderStyle ForeColor="White" BackColor="Gray" />
                          <ItemStyle HorizontalAlign="Justify" />
                      </asp:TemplateField>

                      <asp:TemplateField HeaderText="Driver Name" SortExpression="driverName" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" >
                          <ItemTemplate>
                              <asp:Label ID="lblDriverName" runat="server" Text='<%# Eval("driverName") %>' />
                          </ItemTemplate>
                         <ControlStyle Width="127px" />
                          <HeaderStyle ForeColor="White" BackColor="Gray" />
                          <ItemStyle HorizontalAlign="Justify" />
                      </asp:TemplateField>

                      <asp:TemplateField HeaderText="Departure Date" SortExpression="depDate" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" >
                          <ItemTemplate>
                              <asp:Label ID="lblDepDate" runat="server" Text='<%# Eval("depDate") %>' />
                          </ItemTemplate>
                          <ControlStyle Width="97px" />
                           <HeaderStyle ForeColor="White" BackColor="Gray" />
                           <ItemStyle HorizontalAlign="Justify" />
                      </asp:TemplateField>

                      <asp:TemplateField HeaderText="Departure Time" SortExpression="depTime" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" >
                          <ItemTemplate>
                              <asp:Label ID="lblDepTime" runat="server" Text='<%# Eval("depTime") %>' />
                          </ItemTemplate>
                          <ControlStyle Width="97px" />
                           <HeaderStyle ForeColor="White" BackColor="Gray" />
                           <ItemStyle HorizontalAlign="Justify" />
                      </asp:TemplateField>
                     
                 </Columns>

                         <SelectedRowStyle BackColor="#E2DED6" Font-Bold="True" ForeColor="#333333" />
                         <SortedAscendingCellStyle BackColor="#E9E7E2" />
                         <SortedAscendingHeaderStyle BackColor="#506C8C" />
                         <SortedDescendingCellStyle BackColor="#FFFDF8" />
                         <SortedDescendingHeaderStyle BackColor="#6F8DAE" />
                     </asp:GridView>
             </div>
         </ContentTemplate>
<<<<<<< HEAD
        <%-- <Triggers>
             <asp:AsyncPostBackTrigger ControlID="" EventName="Click" />
             <asp:AsyncPostBackTrigger ControlID="" EventName="Click" />
         </Triggers>
     </asp:UpdatePanel>--%>
=======
         <Triggers>
             <asp:AsyncPostBackTrigger ControlID="btnApplyFilter" EventName="Click" />
             <asp:AsyncPostBackTrigger ControlID="btnResetFilter" EventName="Click" />
         </Triggers>
     </asp:UpdatePanel>
>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b
 </div>
</asp:Content>
