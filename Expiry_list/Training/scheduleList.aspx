<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="scheduleList.aspx.cs" Inherits="Expiry_list.Training.scheduleList" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">

    <script type="text/javascript">

        $(document).ready(function () {

            initializeComponents();
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
                        url: 'scheduleList.aspx', 
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
                        { data: 'tranNo', width: "150px", },
                        { data: 'topicName', width: "470px", },
                        { data: 'description', width: "110px", },
                        { data: 'room', width: "70px", },
                        { data: 'trainerName', width: "130px", },
                        { data: 'position', width: "130px", },
                        {
                            data: 'date',
                            width: "130px",
                            render: function (data) {
                                if (!data) return '';
                                const date = new Date(data);
                                return date.toLocaleDateString('en-GB', {
                                    day: '2-digit',
                                    month: 'short',
                                    year: 'numeric'
                                });
                            }
                        },
                        {
                            data: 'time',
                            width: "150px",
                            render: function (data) {
                                if (!data) return '';

                                if (data.includes(':')) {
                                    const parts = data.split(':');
                                    const hours = parseInt(parts[0]);
                                    const minutes = parts[1];
                                    const ampm = hours >= 12 ? 'PM' : 'AM';
                                    const hours12 = hours % 12 || 12; 
                                    return `${hours12}:${minutes} ${ampm}`;
                                }
                                return data;
                            }
                        }
                    ],
                    order: [[1, 'asc']],
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
    
    <div class="container-fluid">
        <div class="row card">
            <div class="card-header d-flex justify-content-around col-8 p-2 offset-2">
                <div class="col-2">
                    <asp:TextBox ID="dateTb" runat="server" TextMode="Date"></asp:TextBox>
                </div>
                 <div class="col-2">
                     <asp:TextBox ID="timeTb" runat="server" TextMode="Time"></asp:TextBox>
                 </div>
                 <div class="col-2">
                     <asp:TextBox ID="topicDp" runat="server" placeholder="Topic dropdown..." ></asp:TextBox>
                 </div>
                 <div class="col-2">
                        <asp:DropDownList ID="locationDp" runat="server" CssClass="form-control form-control-sm dropdown-icon">
                            <asp:ListItem Text="Select Loction" Value="" />
                            <asp:ListItem Value="aungthapyay" Text="Aung Tha Pyay"></asp:ListItem>
                            <asp:ListItem Value="yankin" Text="Yan Kin"></asp:ListItem>
                        </asp:DropDownList>
                        <asp:RequiredFieldValidator ID="storeNoRequired" runat="server"
                            ControlToValidate="locationDp"
                            ErrorMessage="Location is required!"
                            CssClass="text-danger small d-block mt-1"
                            Display="Dynamic" />
                 </div>
                 <div class="col-2">
                     <asp:Button ID="showBtn" runat="server" Text="Show Avaliable Schedule" />
                 </div>
            </div>

            <div class="card-body" id="gridCol">
                  <asp:UpdatePanel ID="UpdatePanel2" runat="server" UpdateMode="Conditional">
                     <ContentTemplate>

                        <asp:Panel ID="pnlNoData" runat="server" Visible="false">
                              <div class="alert alert-info">No items to Filter</div>
                        </asp:Panel>

                        <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePartialRendering="true">
                           <Scripts>
                               <asp:ScriptReference Name="MicrosoftAjax.js" />
                               <asp:ScriptReference Name="MicrosoftAjaxWebForms.js" />
                           </Scripts>
                       </asp:ScriptManager>

                         <div class="table-responsive gridview-container " style="height: 673px">
                             <asp:GridView ID="GridView2" runat="server"
                                 CssClass="table table-striped table-bordered table-hover border border-2 shadow-lg sticky-grid mt-1 overflow-x-auto overflow-y-auto"
                                 AutoGenerateColumns="False"
                                 DataKeyNames="id"
                                 UseAccessibleHeader="true"
                                
                                 AllowPaging="false"
                                 PageSize="100"
                                 CellPadding="4"
                                 ForeColor="#333333"
                                 GridLines="None"
                                 AutoGenerateEditButton="false" ShowHeaderWhenEmpty="true"  >

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

                                     <asp:TemplateField HeaderText="Trans No" ItemStyle-Width="100px" SortExpression="tranNo" HeaderStyle-ForeColor="White" ItemStyle-HorizontalAlign="Justify" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" >
                                         <ItemTemplate>
                                             <asp:Label ID="lblNo" runat="server" Text='<%# Eval("tranNo") %>' />
                                         </ItemTemplate><HeaderStyle ForeColor="White" BackColor="Gray" />
                                         <ControlStyle Width="100px" />
                                         <ItemStyle HorizontalAlign="Justify" />
                                     </asp:TemplateField>

                                     <asp:TemplateField HeaderText="Topic Name" SortExpression="topicName" HeaderStyle-ForeColor="White" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" >
                                         <ItemTemplate>
                                             <asp:Label ID="lblTopicName" runat="server" Text='<%# Eval("topicName") %>' />
                                         </ItemTemplate>
                                         <ControlStyle Width="117px" />
                                         <HeaderStyle ForeColor="White" BackColor="Gray" />
                                         <ItemStyle HorizontalAlign="Justify" />
                                     </asp:TemplateField>

                                     <asp:TemplateField HeaderText="Description" SortExpression="description" HeaderStyle-ForeColor="Black" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" >
                                         <ItemTemplate>
                                             <asp:Label ID="lblDesc" runat="server" Text='<%# Eval("description") %>' />
                                         </ItemTemplate>
                                         <ControlStyle Width="257px" />
                                         <HeaderStyle ForeColor="White" BackColor="Gray" />
                                         <ItemStyle HorizontalAlign="Justify" />
                                     </asp:TemplateField>

                                   <asp:TemplateField HeaderText="Room" SortExpression="room" HeaderStyle-ForeColor="Black" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" >
                                      <ItemTemplate>
                                          <asp:Label ID="lblRoom" runat="server" Text='<%# Eval("room") %>' />
                                      </ItemTemplate>
                                      <ControlStyle Width="100px" />
                                      <HeaderStyle ForeColor="White" BackColor="Gray" />
                                      <ItemStyle HorizontalAlign="Justify" />
                                  </asp:TemplateField>

                                   <asp:TemplateField HeaderText="Trainer Name" SortExpression="trainerName" HeaderStyle-ForeColor="Black" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" >
                                      <ItemTemplate>
                                          <asp:Label ID="lblTrainerName" runat="server" Text='<%# Eval("trainerName") %>' />
                                      </ItemTemplate>
                                      <ControlStyle Width="100px" />
                                      <HeaderStyle ForeColor="White" BackColor="Gray" />
                                      <ItemStyle HorizontalAlign="Justify" />
                                  </asp:TemplateField>

                                 <asp:TemplateField HeaderText="Trainee Level" SortExpression="position" HeaderStyle-ForeColor="Black" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" >
                                    <ItemTemplate>
                                        <asp:Label ID="lblTraineeLevel" runat="server" Text='<%# Eval("position") %>' />
                                    </ItemTemplate>
                                    <ControlStyle Width="100px" />
                                    <HeaderStyle ForeColor="White" BackColor="Gray" />
                                    <ItemStyle HorizontalAlign="Justify" />
                                </asp:TemplateField>

                                <asp:TemplateField HeaderText="Training Date" SortExpression="date" HeaderStyle-ForeColor="Black" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" >
                                    <ItemTemplate>
                                        <asp:Label ID="lblDate" runat="server" 
                                            Text='<%# FormatDisplayDate(Eval("date")) %>' />
                                    </ItemTemplate>
                                    <ControlStyle Width="100px" />
                                    <HeaderStyle ForeColor="White" BackColor="Gray" />
                                    <ItemStyle HorizontalAlign="Justify" />
                                </asp:TemplateField>

                               <asp:TemplateField HeaderText="Training Time" SortExpression="time" HeaderStyle-ForeColor="Black" HeaderStyle-HorizontalAlign="Center" HeaderStyle-VerticalAlign="Middle" ItemStyle-HorizontalAlign="Justify" >
                                    <ItemTemplate>
                                        <asp:Label ID="lblTime" runat="server" 
                                            Text='<%# FormatDisplayTime(Eval("time")) %>' />
                                    </ItemTemplate>
                                    <ControlStyle Width="100px" />
                                    <HeaderStyle ForeColor="White" BackColor="Gray" />
                                    <ItemStyle HorizontalAlign="Justify" />
                                </asp:TemplateField>

                              <%--       <asp:CommandField ShowEditButton="true" ShowCancelButton="true" ControlStyle-CssClass="btn btn-outline-primary m-1 text-white"
                                         EditText="-" UpdateText="<i class='fa-solid fa-file-arrow-up'></i> Update"
                                         CancelText="<i class='fa-solid fa-xmark'></i> Cancel">
                                         <ControlStyle CssClass="btn btn-outline-primary m-1 text-white" Width="105px" BackColor="#158396" />
                                         <HeaderStyle ForeColor="White" BackColor="Gray" />
                                         <ItemStyle HorizontalAlign="Justify" />
                                     </asp:CommandField>--%>

                                 </Columns>

                                 <SelectedRowStyle BackColor="#E2DED6" Font-Bold="True" ForeColor="#333333" />
                                 <SortedAscendingCellStyle BackColor="#E9E7E2" />
                                 <SortedAscendingHeaderStyle BackColor="#506C8C" />
                                 <SortedDescendingCellStyle BackColor="#FFFDF8" />
                                 <SortedDescendingHeaderStyle BackColor="#6F8DAE" />
                             </asp:GridView>
                         </div>
                     </ContentTemplate>
<%--                     <Triggers>
                          <asp:PostBackTrigger ControlID="excel" />
                         <asp:AsyncPostBackTrigger ControlID="btnApplyFilter" EventName="Click" />
                         <asp:AsyncPostBackTrigger ControlID="btnResetFilter" EventName="Click" />
                     </Triggers>--%>
                </asp:UpdatePanel>
            </div>

        </div>

     </div>

</asp:Content>
