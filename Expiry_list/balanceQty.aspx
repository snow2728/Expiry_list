<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" EnableEventValidation="false" CodeBehind="balanceQty.aspx.cs" Inherits="Expiry_list.balanceQty" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <script src="js/customJS.js"></script>
<script type="text/javascript">

    $(document).ready(function () {

        initializeComponents();
        $(document).on('search.dt', function (e, settings) {
            var searchVal = settings.oPreviousSearch.sSearch || "";
            $('#<%= hfLastSearch.ClientID %>').val(searchVal);
        });
    });

    let isDataTableInitialized = false;

    function initializeComponents() {
        const grid = $("#<%= GridView2.ClientID %>");

        if (!$.fn.DataTable.isDataTable(grid)) {
            if (grid.find('thead').length === 0) {
                grid.prepend($('<thead/>').append(grid.find('tr:first').detach()));
            }

            grid.DataTable({
                responsive: true,
                ordering: true,
                serverSide: true,
                paging: true,
                filter: true,
                scrollY: 589,
                scrollCollapse: true,
                autoWidth: false,
                stateSave: true,
                processing: true,
                ajax: {
                    url: 'balanceQty.aspx', 
                    type: 'POST',
                    data: function (d) {
                        return {
                            draw: d.draw,
                            start: d.start,
                            length: d.length,
                            order: d.order,
                            search: d.search.value
                        };
                    }
                },
                columns: [
                    {
                        data: null,
                        width: "50px",
                        render: function (data, type, row, meta) {
                            return meta.row + 1;
                        }
                    },
                    /*{ data: 'srno' },*/
                    { data: 'ItemNo', width: "150px", },
                    { data: 'Description', width: "470px", },
                    { data: 'LocationCode', width: "110px", },
                    { data: 'BalanceQty', width: "70px", },
                    {
                        data: 'TakenDate',
                        width: "130px",
                        render: function (data) {
                            return new Date(data).toLocaleDateString('en-GB');
                        }
                    },
                    {
                        data: 'TakenTime',
                        width: "150px",
                        render: function (data) {
                            const date = new Date(data);
                            return date.toLocaleString('en-GB', {
                                year: 'numeric',
                                month: '2-digit',
                                day: '2-digit',
                                hour: '2-digit',
                                minute: '2-digit',
                                second: '2-digit'
                            });
                        }
                    },
                    { data: 'UnitofMeasure', width: "130px", },
                    { data: 'ItemFamily', width: "130px", } 
                ],
                order: [[1, 'asc']],
                columnDefs: [
                    { targets: '_all', orderSequence: ["asc", "desc", ""] }
                ],
                select: { style: 'multi', selector: 'td:first-child' },
                lengthMenu: [[100, 500, 1000], [100, 500, 1000]],
                initComplete: function (settings) {
                    var api = this.api();
                    setTimeout(function () {
                        api.columns.adjust();
                    }, 50);
                },
                "error": function (xhr, error, thrown) {
                    console.error("DataTables error:", error, thrown);
                    alert("Error loading data. Check console for details.");
                }
              
          });
        }
    }

      Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
         
          initializeComponents();
      });

    function refreshDataTable() {
        const grid = $("#<%= GridView2.ClientID %>");
        if ($.fn.DataTable.isDataTable(grid)) {
            grid.DataTable().ajax.reload();
        }
    }


    history.pushState(null, null, location.href);
    window.addEventListener("popstate", function (event) {
        location.reload();
    });

</script>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">     
 <div class="container-fluid col-lg-12">
 <div class="card shadow-md border-dark-subtle">
     <div class="card-header" style="background-color:#1995ad;">
         <h4 class="text-center text-white">Negative Inventory Report</h4>
     </div>
       <div class="card-body">
             <asp:Button ID="btnExport" runat="server" CssClass="btn btn-info text-white me-2" Text="Export to Excel" ForeColor="White" Font-Bold="True" Font-Size="Medium" style="background:#1995ad;" OnClick="btnExport_Click" />
           <div class="d-flex p-2">

               <asp:HiddenField ID="hfSelectedIDs" runat="server" />
               <asp:HiddenField ID="hflength" runat="server" />   
               <asp:HiddenField ID="hfLastSearch" runat="server" />

               <!-- Table -->
                 <div class="col-md-12" id="gridCol">
                      <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePartialRendering="true"></asp:ScriptManager>
                     <asp:UpdatePanel ID="UpdatePanel2" runat="server" UpdateMode="Conditional">
                          <ContentTemplate>
                              <div class="table-responsive gridview-container">
                                <asp:GridView ID="GridView2" CssClass="table table-striped table-bordered table-responsive table-hover shadow-lg sticky-grid overflow-x-auto overflow-y-auto small mt-2"
                                     runat="server" AutoGenerateColumns="False" DataKeyNames="srno" UseAccessibleHeader="true" OnSorting="GridView1_Sorting" CellPadding="4" ForeColor="#333333" GridLines="None" AutoGenerateEditButton="false" >

                                      <EditRowStyle BackColor="#999999" />
                                      <FooterStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />

                                      <HeaderStyle Wrap="true" BackColor="#808080" Font-Bold="True" ForeColor="White" ></HeaderStyle>
                                      <PagerStyle CssClass="pagination-wrapper" HorizontalAlign="Center" VerticalAlign="Middle" />
                                      <RowStyle CssClass="table-row data-row" BackColor="#F7F6F3" ForeColor="#333333"></RowStyle>
                                      <AlternatingRowStyle CssClass="table-alternating-row" BackColor="White" ForeColor="#284775"></AlternatingRowStyle>

                                       <EmptyDataTemplate>
                                           <div class="alert alert-info">No items to Filter</div>
                                       </EmptyDataTemplate>

                                      <Columns>

                                          <asp:TemplateField ItemStyle-HorizontalAlign="Justify" HeaderText="No" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle">
                                              <ItemTemplate>
                                                  <asp:Label ID="lblLinesNo" runat="server" Text='<%# Container.DataItemIndex + 1 %>' />
                                              </ItemTemplate>
                                              <ControlStyle Width="50px" />
                                              <HeaderStyle ForeColor="White" BackColor="Gray" />
                                              <ItemStyle HorizontalAlign="Justify" />
                                          </asp:TemplateField>

                                      <%--<asp:TemplateField HeaderText="Serial No" SortExpression="srno" HeaderStyle-ForeColor="White" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify">
                                          <ItemTemplate>
                                              <asp:Label ID="srno" runat="server" Text='<%# Eval("srno") %>' />
                                          </ItemTemplate>
                                          <ControlStyle Width="120px" />
                                          <HeaderStyle ForeColor="White" BackColor="Gray" />
                                          <ItemStyle HorizontalAlign="Justify" />
                                      </asp:TemplateField>--%>

                                     <asp:TemplateField HeaderText="Item No" SortExpression="ItemNo" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify">
                                      <ItemTemplate>
                                       <asp:Label ID="itemNo" runat="server" Text='<%# Eval("ItemNo") %>' />
                                      </ItemTemplate>
                                         <ControlStyle Width="170px" />
                                    </asp:TemplateField>

                                      <asp:TemplateField HeaderText="Description" SortExpression="Description" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" >
                                          <ItemTemplate>
                                              <asp:Label ID="Description" runat="server" Text='<%# Eval("Description") %>' />
                                          </ItemTemplate>
                                          <ControlStyle Width="470px" />
                                           <HeaderStyle ForeColor="White" BackColor="Gray" />
                                           <ItemStyle HorizontalAlign="Justify" />
                                      </asp:TemplateField>

                                      <asp:TemplateField HeaderText="Location Code" SortExpression="LocationCode" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify">
                                      <ItemTemplate>
                                        <asp:Label ID="locationCode" runat="server" Text='<%# Eval("LocationCode") %>' />
                                      </ItemTemplate>
                                          <ControlStyle Width="110px" />
                                    </asp:TemplateField>

                                      <asp:TemplateField HeaderText="Balance Qty" SortExpression="balanceQty" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" >
                                          <ItemTemplate>
                                              <asp:Label ID="balanceQty" runat="server" Text='<%# Eval("BalanceQty") %>' />
                                          </ItemTemplate>
                                          <ControlStyle Width="70px" />
                                           <HeaderStyle ForeColor="White" BackColor="Gray" />
                                           <ItemStyle HorizontalAlign="Justify" />
                                      </asp:TemplateField>

                                           <asp:TemplateField HeaderText="Taken Date" SortExpression="TakenDate" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify">
                                              <ItemTemplate>
                                                  <asp:Label ID="TakenDate" runat="server" 
                                                      Text='<%# Eval("TakenDate", "{0:d}") %>' />
                                              </ItemTemplate>
                                                 <ControlStyle Width="130px" />
                                          </asp:TemplateField>

                                           <asp:TemplateField HeaderText="Taken Time" SortExpression="TakenTime" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify">
                                              <ItemTemplate>
                                                  <asp:Label ID="TakenTime" runat="server" 
                                                      Text='<%# Eval("TakenTime", "{0:HH:mm:ss}") %>' />
                                              </ItemTemplate>
                                                 <ControlStyle Width="150px" />
                                          </asp:TemplateField>

                                      <asp:TemplateField HeaderText="Unit Of Measure" SortExpression="unitofMeasure" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" >
                                          <ItemTemplate>
                                              <asp:Label ID="UnitofMeasure" runat="server" Text='<%# Eval("UnitofMeasure") %>' />
                                          </ItemTemplate>
                                          <ControlStyle Width="130px" />
                                           <HeaderStyle ForeColor="White" BackColor="Gray" />
                                           <ItemStyle HorizontalAlign="Justify" />
                                      </asp:TemplateField>

                                     <asp:TemplateField HeaderText="Item Family Code" SortExpression="ItemFamily" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify">
                                        <ItemTemplate>
                                           <asp:Label ID="ItemFamily" runat="server" Text='<%# Eval("ItemFamily") %>' />
                                        </ItemTemplate>
                                            <ControlStyle Width="130px" />
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
                     </asp:UpdatePanel>
                 </div>
           </div>
         </div>
       </div>
   </div>
</asp:Content>