<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" EnableEventValidation="false" CodeBehind="rege1.aspx.cs" Inherits="Expiry_list.ConsignItem.rege1" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
        <style>
            @media (min-width: 1400px) {
                .container {
                    max-width: 2000px;
                }
            }
            @media (max-width: 1870px) {
                .div-btn-rp {
                    width: 33%;
                }
                .btn-font {
                    font-size: 14px;
                }
            }
             @media (max-width: 1250px) {
                 .btn-font {
                     font-size: 10px;
                 }
             }
            .swal-text-left {
                text-align: left;
            }

            .btn-confirm {
                float: right;
                background-color: #0D330E !important;
                color: white;
                border-radius: 10px !important;
                margin-right: 10px !important;
            }

            div.dt-buttons {
              float: right;
              margin-bottom: 0px;
            }

            button.dt-button.btn-confirm:hover {
                background-color: #477023 !important; 
            }
        </style>
        <script type="text/javascript">

            $(document).ready(function () {
                const vendorNoSelector = '#<%= vendorNo.ClientID %>';
                const hiddenVendorNoSelector = '#<%= hiddenVendorNo.ClientID %>';
                const hiddenVendorTextSelector = '#<%= hiddenVendorText.ClientID %>';

                $(vendorNoSelector).select2({
                    placeholder: 'Search by Vendor No or Vendor Name',
                    minimumInputLength: 2,
                    ajax: {
                        url: '/ConsignItem/rege1.aspx/GetVendors',
                        type: 'POST',
                        contentType: "application/json; charset=utf-8",
                        dataType: 'json',
                        delay: 250,
                        data: function (params) {
                            return JSON.stringify({ searchTerm: params.term });
                        },
                        processResults: function (data) {
                            if (data && data.d && data.d.length > 0 && data.d[0].vendorNo === "ERROR") {
                                Swal.fire({
                                    icon: 'error',
                                    title: 'Server Error',
                                    text: data.d[0].vendorName
                                });
                                return { results: [] };
                            }

                            var items = data.d || [];

                            if (items.length === 0) {
                                setTimeout(function () {
                                    Swal.fire({
                                        icon: 'warning',
                                        title: 'No Vendors Found',
                                        text: 'No Vendors match your search. Please try again.'
                                    });
                                }, 300);
                            }

                            return {
                                results: items.map(function (item) {
                                    let displayText = item.vendorNo + " - " + item.vendorName;
                                    if (item.extraInfo) {
                                        displayText += " - " + item.extraInfo;
                                    }
                                    return {
                                        id: item.vendorNo,
                                        text: displayText
                                    };
                                })
                            };
                        },
                        cache: true
                    }
                });

                // Restore vendor selection from hidden fields
                var vendorId = $(hiddenVendorNoSelector).val();
                var vendorText = $(hiddenVendorTextSelector).val();

                if (vendorId && vendorText) {
                    var $vendorSelect = $(vendorNoSelector);

                    if ($vendorSelect.find('option[value="' + vendorId + '"]').length === 0) {
                        var newOption = new Option(vendorText, vendorId, true, true);
                        $vendorSelect.append(newOption);
                    }

                    $vendorSelect.val(vendorId).trigger('change');
                }

                // Update hidden fields when vendor changes
                $(vendorNoSelector).on('change', function () {
                    var selectedOption = $(this).find('option:selected');
                    $(hiddenVendorNoSelector).val($(this).val());

                    if (selectedOption.length) {
                        $(hiddenVendorTextSelector).val(selectedOption.text());
                    }
                });
                
                // Initialize Select2 for item search

                initializeDataTable();
                if (typeof (Sys) !== 'undefined') {
                    Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
                        initializeDataTable();
                    });
                }

            });


            document.addEventListener('DOMContentLoaded', function () {
                    document.getElementById("link_home").href = "../AdminDashboard.aspx";
            });

            function initializeDataTable() {
                const grid = $("#<%= GridView.ClientID %>");
     
                 if (grid.length === 0 || grid.find('tr').length === 0) {
                     return;
                 }
     
                 if ($.fn.DataTable.isDataTable(grid)) {
                     grid.DataTable().destroy();
                     grid.removeAttr('style');
                 }
     
                if (<%= GridView.EditIndex >= 0 ? "true" : "false" %> === false) {
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
                            scrollY: 497,
                            info: true,                           
                            order: [[0, 'asc']],
                            lengthMenu: [[50, 100, 150, 200], [50, 100, 150, 200]],
                            dom: 'fBltip',
                            buttons: [
                                {
                                    className: 'btn btn-success text-white fw-bold btn-confirm',
                                    text: 'Confirm',
                                    action: function () {
                                        document.getElementById('<%= btnConfirmConsign.ClientID %>').click();
                                    }
                                }
                            ],
                            columnDefs: [
                               { orderable: false, targets: [3, 4, 5, 6] }  
                            ],
                        });
                    } catch (e) {
                        console.error('DataTable initialization error:', e);
                    }                    
                }
            }

            function isNumberKey(evt) {
                var charCode = (evt.which) ? evt.which : evt.keyCode;
                if (charCode == 8 || charCode == 9 || charCode == 13 || charCode == 27 || charCode == 46)
                    return true;

                if (charCode >= 48 && charCode <= 57)
                    return true;
                return false;
            }            

            Sys.Application.addEventListener("load", function () {
                var prevVendorNo = $(hiddenVendorNoSelector).val();
                if (prevVendorNo) {

                    var optionExists = $(vendorNoSelector + ' option[value=\"' + prevVendorNo + '\"]').length > 0;
                    if (!optionExists) {
                        var option = new Option(prevVendorNo, prevVendorNo, true, true);
                        $(vendorNoSelector).append(option).trigger('change');
                    } else {
                        $(vendorNoSelector).val(prevVendorNo).trigger('change');
                    }
                }
            });

            history.pushState(null, null, location.href);

            window.addEventListener("popstate", function (event) {
                location.reload();
            });

        </script>

</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="container" style="background-color: #f1f1f2;">
        <%--<div class="g-2">--%>
            <div class="offset-lg-1 col-lg-10 col-md-8 Justify-content-center mb-3">
                <div class="card shadow-sm h-100">
                    <!-- Card Header -->
                    <div class="card-header text-white text-center"
                        style="background-color: #477023; border-top-left-radius: 10px; border-top-right-radius: 10px;">
                        <h2 class="mb-0 fw-bolder">Consignment Register Form</h2>
                        <asp:Label ID="tdyDate" runat="server" CssClass="ms-2" />
                        <asp:Literal ID="sessionDataLiteral" runat="server" />
                    </div>

                    <!-- Card Body -->
                    <div class="card-body">
                        <div class="row justify-content-center">
                            <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePartialRendering="true">
                            <Scripts>
                                <asp:ScriptReference Name="MicrosoftAjax.js" />
                                <asp:ScriptReference Name="MicrosoftAjaxWebForms.js" />
                            </Scripts>
                            </asp:ScriptManager>
                                    <!-- No Field -->
                            <div class="row">
                                <div class="col-2 justify-content-center">
                                    <div class="row">
                                        <label for="<%= no.ClientID %>" class="col-sm-5 col-form-label ps-0 px-0 text-end fw-bold">No</label>
                                        <div class="col-4 col-sm-7">
                                            <asp:TextBox runat="server" CssClass="form-control text-center" ID="no" ReadOnly="true" />
                                        </div>
                                    </div>
                                </div>

                                    <!-- Hidden Fields -->
                                    <asp:HiddenField ID="hiddenVendorNo" runat="server" />
                                    <asp:HiddenField ID="hiddenVendorText" runat="server" />
                                <!-- Vendor No Field -->
                                <div class="col-5 col-sm-5">
                                    <div class="row">
                                        <label for="<%= vendorNo.ClientID %>" class="col-sm-2 col-form-label px-0 text-end fw-bold">Vendor</label>
                                        <div class="col-sm-9">
                                                <asp:DropDownList ID="vendorNo" runat="server" 
                                                CssClass="form-control select2 ps-0 pe-0"
                                                AppendDataBoundItems="true">
                                                <asp:ListItem Text="" Value="" />
                                            </asp:DropDownList>
                                            <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server"
                                                ErrorMessage="Vendor must be selected!"
                                                ControlToValidate="vendorNo" Display="Dynamic"
                                                CssClass="text-danger" SetFocusOnError="True" />
                                        </div>
                                    </div>
                                </div>
                                <div class="col-4 col-sm-4 align-item-center">
                                    <div class="d-flex align-items-center justify-content-center">
                                        <div class="text-center me-1">  
                                            <%--col-4 col-lg-4 col-md-4 col-sm-3  div-btn-rp--%>
                                            <asp:Button Text="Show Items" runat="server" CssClass="btn fw-bold text-white btn-font"
                                                Style="background-color: #0D330E; border-radius: 10px;"
                                                ID="getItemBtn" onClick="btnGetItems_Click"/>
                                        </div>

                                        <div class="text-center me-lg-1">
                                           <%-- col-4 col-lg-4 col-md-4 col-sm-3 --%>
                                            <asp:Button Text="Export Excel" runat="server" CssClass="btn fw-bold text-white btn-font"
                                                Style="background-color: #0D330E; border-radius: 10px;"
                                                ID="Button1" onClick="btnExportExcel_Click"/>
                                        </div>
                                        <div class="text-center">
                                             <%--col-4 col-lg-3 col-md-4 col-sm-3 --%>
                                            <asp:Button Text="Clear Vendor" runat="server" CssClass="btn fw-bold text-white btn-font"
                                                Style="background-color: #0D330E; border-radius: 10px;"
                                                ID="Button2" onClick="btnClearVendor_Click"/>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Consignment List Column -->
            <div class="offset-lg-1 col-lg-10 col-md-8 p-0">
                <!-- Remove padding -->
                <div class="card shadow-sm" style="border-radius: 10px;">

                    <!-- Card Header -->
                    <div class="card-header text-white text-center"
                        style="background-color: #477023; border-top-left-radius: 10px; border-top-right-radius: 10px;">
                    </div>

                    <div class="card-body p-2">

                        <div class="col-md-12" id="gridCol">
                            <asp:UpdatePanel ID="UpdatePanel2" runat="server" UpdateMode="Conditional">
                            <ContentTemplate>

                               <asp:Panel ID="pnlNoData" runat="server" Visible="false">
                                     <div class="alert alert-info">No items to Filter</div>
                               </asp:Panel>

                                <div class="table-responsive gridview-container " style="min-height: 665px">
                                    <asp:GridView id="GridView" runat="server"
                                        CssClass="table table-striped table-bordered table-hover border border-2 shadow-lg sticky-grid mt-1 overflow-x-auto overflow-y-auto"
                                        AutoGenerateColumns="False"
                                        DataKeyNames="No"
                                        UseAccessibleHeader="true"
                                        OnRowEditing="GridView_RowEditing"
                                        OnRowDeleting="GridView_RowDeleting"
                                        OnRowCancelingEdit="GridView_RowCancelingEdit"
                                        OnRowUpdating="GridView_RowUpdating"
                                        AllowPaging="false"
                                        PageSize="100"
                                        CellPadding="4"
                                        ForeColor="#333333"
                                        GridLines="None"
                                        ShowHeaderWhenEmpty="true"  >

                                        <EditRowStyle BackColor="#999999" />
                                        <FooterStyle BackColor="#5D7B9D" Font-Bold="True" ForeColor="White" />

                                        <HeaderStyle Wrap="true" BackColor="#6A7D4F" Font-Bold="True" ForeColor="#B1C095"></HeaderStyle>
                                        <EditRowStyle BackColor="#999999" />
                                        <FooterStyle BackColor="#6A7D4F" Font-Bold="True" ForeColor="#B1C095" />
                                        <PagerStyle CssClass="pagination-wrapper" HorizontalAlign="Center" VerticalAlign="Middle" />
                                        <RowStyle CssClass="table-row data-row" BackColor="#E6DED1" ForeColor="#333333"></RowStyle>

                                        <EmptyDataTemplate>
                                            <div class="alert" style="background-color:#B1C095;">No Item Found!</div>
                                        </EmptyDataTemplate>

                                        <Columns>
                                            <asp:BoundField DataField="No" Visible="false" />
                                            <asp:BoundField DataField="ItemNo" HeaderText="Item No" HeaderStyle-Font-Bold="true" HeaderStyle-ForeColor="#B1C095" HeaderStyle-BackColor="#6A7D4F" ReadOnly="True" HeaderStyle-CssClass="position-sticky top-0 z-3" />
                                            <asp:BoundField DataField="Description" HeaderText="Description" ReadOnly="True" HeaderStyle-ForeColor="#B1C095" HeaderStyle-BackColor="#6A7D4F" HeaderStyle-Font-Bold="true" HeaderStyle-CssClass="position-sticky top-0 z-3"/>                                            
                                            
                                            <asp:TemplateField HeaderText="Qty" HeaderStyle-ForeColor="#B1C095" HeaderStyle-BackColor="#6A7D4F" HeaderStyle-Font-Bold="true" HeaderStyle-CssClass="position-sticky top-0 z-3">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblQuantity" runat="server" Text='<%# Eval("Qty") %>'></asp:Label>
                                                </ItemTemplate>
                                                <EditItemTemplate>
                                                    <asp:TextBox ID="txtQuantity" runat="server"
                                                        Text='<%# Bind("Qty") %>' Width="59px"
                                                        onkeypress="return blockInvalidChars(event)" onpaste="return false"></asp:TextBox>
                                                </EditItemTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="UOM" HeaderStyle-ForeColor="#B1C095" HeaderStyle-BackColor="#6A7D4F" HeaderStyle-Font-Bold="true" HeaderStyle-CssClass="position-sticky top-0 z-3">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblUom" runat="server" Text='<%# Eval("UOM") %>'></asp:Label>
                                                </ItemTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Packing Info" HeaderStyle-ForeColor="#B1C095" HeaderStyle-BackColor="#6A7D4F" HeaderStyle-Font-Bold="true" HeaderStyle-CssClass="position-sticky top-0 z-3">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblPack" runat="server" Text='<%# Eval("PackingInfo") %>'></asp:Label>
                                                </ItemTemplate>
                                            </asp:TemplateField>

                                            <asp:TemplateField HeaderText="Note" HeaderStyle-ForeColor="#B1C095" HeaderStyle-BackColor="#6A7D4F" HeaderStyle-Font-Bold="true" HeaderStyle-CssClass="position-sticky top-0 z-3">
                                                <ItemTemplate>
                                                    <asp:Label ID="lblNote" runat="server" Text='<%# Eval("Note") %>'></asp:Label>
                                                </ItemTemplate>
                                                <EditItemTemplate>
                                                    <asp:TextBox ID="txtNote" runat="server" Text='<%# Bind("Note") %>' Width="99%"></asp:TextBox>
                                                </EditItemTemplate>
                                            </asp:TemplateField>

                                            <asp:CommandField ShowEditButton="True"
                                                CausesValidation="false" EditText="<i class='fa-solid fa-pen-to-square'></i>"
                                                UpdateText="<i class='fa-solid fa-file-arrow-up'></i>"
                                                CancelText="<i class='fa-solid fa-xmark'></i>"
                                                ControlStyle-CssClass="btn m-1 text-white" ControlStyle-BackColor="#477023" HeaderStyle-BackColor="#6A7D4F" HeaderStyle-CssClass="position-sticky top-0 z-3">
                                            <ItemStyle HorizontalAlign="Center" CssClass="text-center" />
                                            </asp:CommandField>
                                        </Columns>

                                            <SelectedRowStyle BackColor="#E2DED6" Font-Bold="True" ForeColor="#333333" />
                                            <SortedAscendingCellStyle BackColor="#E9E7E2" />
                                            <SortedAscendingHeaderStyle BackColor="#506C8C" />
                                            <SortedDescendingCellStyle BackColor="#FFFDF8" />
                                            <SortedDescendingHeaderStyle BackColor="#6F8DAE" />
                                    </asp:GridView>
                                </div>
                                </ContentTemplate>
                                <Triggers>
                                    <asp:AsyncPostBackTrigger ControlID="GridView" EventName="RowEditing" />
                                    <asp:AsyncPostBackTrigger ControlID="GridView" EventName="RowUpdating" />
                                    <asp:AsyncPostBackTrigger ControlID="GridView" EventName="RowDeleting" />
                                    <asp:AsyncPostBackTrigger ControlID="GridView" EventName="RowCancelingEdit" />
                                </Triggers>
                            </asp:UpdatePanel>
                        </div>
                       
                    </div>
                </div>
          </div>
    </div>
    <asp:Button ID="btnHiddenOk" runat="server" Text="HiddenPostback" 
    OnClick="btnOk_Click" Style="display:none;" />
    <asp:Button ID="btnConfirmConsign" runat="server" Text="HiddenConfirm" 
    OnClick="btnConfirmConsign_Click" Style="display:none;" />
</asp:Content>