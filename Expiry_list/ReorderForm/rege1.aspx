<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" EnableEventValidation="false" CodeBehind="rege1.aspx.cs" Inherits="Expiry_list.ReorderForm.rege1" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
  <script type="text/javascript">
      $(document).ready(function () {
            var staffName = $('#<%= hiddenStaffName.ClientID %>').val();
            $('#<%= staffName.ClientID %>').val(staffName);

            // Initialize Select2
            $("#<%= itemNo.ClientID %>").select2({
                placeholder: 'Search by Item No, Description, or Barcode',
                minimumInputLength: 2,
                allowClear: true,
                ajax: {
                    url: 'rege1.aspx/GetItems',
                    type: 'POST',
                    contentType: "application/json; charset=utf-8",
                    dataType: 'json',
                    delay: 250,
                    data: function (params) {
                        return JSON.stringify({ searchTerm: params.term });
                    },
                    processResults: function (data) {
                        var items = data.d || [];

                        if (items.length === 0) {
                            setTimeout(function () {
                                Swal.fire({
                                    icon: 'warning',
                                    title: 'No Items Found',
                                    text: 'No items match your search. Please try again.'
                                });
                            }, 300); 
                        }
                        return {
                            results: data.d.map(function (item) {
                                return {
                                    id: item.ItemNo,
                                    text: item.ItemNo + " - " + item.ItemDescription,
                                    description: item.ItemDescription,
                                    barcode: item.Barcode,
                                    uoms: item.UOMList,
                                    packing: item.PackingInfo,
                                    divisionCode: item.DivisionCode,
                                    vendorNo: item.VendorNo || '',
                                    vendorName: item.VendorName || '' 
                                };
                            })
                        };
                    },
                    cache: true
                }
            });

            $('#<%= itemNo.ClientID %>').on('select2:select', function (e) {
                clearFields();

                var selectedData = e.params.data;
                console.log("Selected Item:", selectedData);

                $('#<%= desc.ClientID %>').val(selectedData.description);
                $('#<%= packingInfo.ClientID %>').val(selectedData.packing);

                var barcode = selectedData.barcode.length > 0 ? selectedData.barcode[0] : '';
                $('#<%= barcodeNo.ClientID %>').val(barcode);

                var $uomSelect = $('#<%= uom.ClientID %>');
                $uomSelect.empty();

                if (selectedData.uoms && selectedData.uoms.length > 0) {
                    selectedData.uoms.forEach(function (uomValue) {
                        $uomSelect.append($('<option>', {
                            value: uomValue,
                            text: uomValue
                        }));
                    });

                    $uomSelect.val(selectedData.uoms[0]);
                    $('#<%= hiddenUOM.ClientID %>').val(selectedData.uoms[0]);
                    console.log("Default UOM selected:", selectedData.uoms[0]);
                }

                $uomSelect.off('change').on('change', function () {
                    var selectedUom = $(this).val();
                    $('#<%= hiddenUOM.ClientID %>').val(selectedUom);
                    console.log("UOM changed to:", selectedUom);
                });

                $('#<%= hiddenSelectedItem.ClientID %>').val(selectedData.id);
                $('#<%= hiddenBarcodeNo.ClientID %>').val(barcode);
                $('#<%= hiddenDescription.ClientID %>').val(selectedData.description);
                $('#<%= hiddenPackingInfo.ClientID %>').val(selectedData.packing);
                $('#<%= hiddenDivisionCode.ClientID %>').val(selectedData.divisionCode);
                $('#<%= hiddenVendorNo.ClientID %>').val(selectedData.vendorNo || '');
                $('#<%= hiddenVendorName.ClientID %>').val(selectedData.vendorName || '');
            });

            $('#<%= itemNo.ClientID %>').on('select2:clear', function (e) {
                clearFields();
                //console.log("Item selection cleared.");
            });
      });

      document.addEventListener('DOMContentLoaded', function () {
          document.getElementById("link_home").href = "../AdminDashboard.aspx";
      });

      function isNumberKey(evt) {
          var charCode = (evt.which) ? evt.which : evt.keyCode;
          if (charCode == 8 || charCode == 9 || charCode == 13 || charCode == 27 || charCode == 46)
              return true;
          if (charCode >= 48 && charCode <= 57)
              return true;
          return false;
      }

    function clearFields() {
        $('#<%= desc.ClientID %>').val('');
        $('#<%= packingInfo.ClientID %>').val('');
        $('#<%= barcodeNo.ClientID %>').val('');
        $('#<%= qty.ClientID %>').val('');
        $('#<%= note.ClientID %>').val('');

        $('#<%= uom.ClientID %>').empty();
        $('#<%= hiddenSelectedItem.ClientID %>').val('');
        $('#<%= hiddenBarcodeNo.ClientID %>').val('');
        $('#<%= hiddenDescription.ClientID %>').val('');
        $('#<%= hiddenUOM.ClientID %>').val('');
        $('#<%= hiddenPackingInfo.ClientID %>').val('');
        $('#<%= hiddenQty.ClientID %>').val('');
        $('#<%= hiddenDivisionCode.ClientID %>').val('');
        $('#<%= hiddenVendorNo.ClientID %>').val('');
        $('#<%= hiddenVendorName.ClientID %>').val('');
    }
  </script>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

<div class="container-fluid" style="color:white; font-weight:bold;">
    <div class="row g-2">

        <div class="col-12 col-lg-5 col-md-4">
            <div class="card shadow-sm h-100">
                <!-- Card Header -->
                <div class="card-header text-white text-center"
                    style="background-color: #BD467F; border-top-left-radius: 10px; border-top-right-radius: 10px;">
                    <h2 class="mb-0">Reorder Quantity Form</h2>
                    <asp:Label ID="tdyDate" runat="server" CssClass="ms-2" />
                    <asp:Literal ID="sessionDataLiteral" runat="server" />
                </div>

                <!-- Card Body -->
                  <div class="card-body" style="background-color: #F1B4D1;">

                    <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
                    <asp:UpdatePanel runat="server" ID="UpdatePanel1" UpdateMode="Conditional">
                        <ContentTemplate>
                            <!-- No Field -->
                            <div class="row g-2 mb-3" style="color: #BD467F;">
                                <label for="<%= no.ClientID %>" class="col-sm-4 col-form-label">No</label>
                                <div class="col-sm-8">
                                    <asp:TextBox runat="server" CssClass="form-control form-control-sm" ID="no" ReadOnly="true" />
                                </div>
                            </div>
                        </ContentTemplate>
                    </asp:UpdatePanel>

                  <!-- Hidden Fields -->
                    <asp:HiddenField ID="hiddenItemNo" runat="server" />
                    <asp:HiddenField ID="hiddenSelectedItem" runat="server" />
                    <asp:HiddenField ID="hiddenDescription" runat="server" />
                    <asp:HiddenField ID="hiddenBarcodeNo" runat="server" />
                    <asp:HiddenField ID="hiddenUOM" runat="server" />
                    <asp:HiddenField ID="hiddenPackingInfo" runat="server" />
                    <asp:HiddenField ID="hiddenNote" runat="server" />
                    <asp:HiddenField ID="hiddenQty" runat="server" />
                    <asp:HiddenField ID="hiddenStaffName" runat="server" />
                    <asp:HiddenField ID="hiddenDivisionCode" runat="server" />
                    <asp:HiddenField ID="hiddenVendorNo" runat="server" />
                    <asp:HiddenField ID="hiddenVendorName" runat="server" />

                    <!-- Item No Field -->
                    <div class="row g-2 mb-3 " style="color: #BD467F;">
                        <label for="<%= itemNo.ClientID %>" class="col-sm-4 col-form-label">Item No</label>
                        <div class="col-sm-8">
                            <asp:DropDownList ID="itemNo" runat="server" CssClass="form-control form-control-sm select2" OnSelectedIndexChanged="itemNo_SelectedIndexChanged1">
                                <asp:ListItem Text="" Value="" />
                            </asp:DropDownList>
                            <asp:RequiredFieldValidator ID="RequiredFieldValidator2" runat="server"
                                ErrorMessage="Item must be selected!"
                                ControlToValidate="itemNo" Display="Dynamic"
                                CssClass="text-danger" SetFocusOnError="True" />
                        </div>
                    </div>

                    <!-- Description Field -->
                    <div class="row g-2 mb-3 " style="color: #BD467F;">
                        <label for="<%= desc.ClientID %>" class="col-sm-4 col-form-label">Description</label>
                        <div class="col-sm-8">
                            <asp:TextBox runat="server" CssClass="form-control form-control-sm"
                                TextMode="MultiLine" ID="desc"
                                Enabled="false" ReadOnly="true" />
                        </div>
                    </div>

                    <!-- Quantity Field -->
                    <div class="row g-2 mb-3" style="color: #BD467F;">
                        <label for="<%= qty.ClientID %>" class="col-sm-4 col-form-label">Quantity</label>
                        <div class="col-sm-8">
                            <asp:TextBox runat="server" CssClass="form-control form-control-sm no-spinners"
                                ID="qty" name="qty" TextMode="Number"
                                onkeypress="return isNumberKey(event)" onpaste="return false" />
                            <asp:RequiredFieldValidator ID="rfvQty" runat="server"
                                ErrorMessage="Quantity is required!"
                                ControlToValidate="qty" Display="Dynamic"
                                CssClass="text-danger" SetFocusOnError="True" />
                            <asp:RangeValidator ID="rvQty" runat="server"
                                ErrorMessage="Please enter a valid whole number."
                                ControlToValidate="qty" Display="Dynamic"
                                MinimumValue="1" MaximumValue="1000000" Type="Integer"
                                CssClass="text-danger" SetFocusOnError="True" />
                        </div>
                    </div>

                    <!-- UOM Field -->
                    <div class="row g-2 mb-3" style="color: #BD467F;">
                        <label for="<%= uom.ClientID %>" class="col-sm-4 col-form-label">UOM</label>
                        <div class="col-sm-8">
                           <asp:DropDownList runat="server" CssClass="form-select form-select-sm" ID="uom" AppendDataBoundItems="true" />
                        </div>
                    </div>

                    <!-- Packing Info Field -->
                    <div class="row g-2 mb-3 " style="color: #BD467F;">
                        <label for="<%= packingInfo.ClientID %>" class="col-sm-4 col-form-label">Packing Info</label>
                        <div class="col-sm-8">
                            <asp:TextBox runat="server" CssClass="form-control form-control-sm" ID="packingInfo"
                                Enabled="false" ReadOnly="true" />
                        </div>
                    </div>

                    <!-- Barcode No Field -->
                    <div class="row g-2 mb-3" style="color: #BD467F;" >
                        <label for="<%= barcodeNo.ClientID %>" class="col-sm-4 col-form-label">Barcode No</label>
                        <div class="col-sm-8">
                            <asp:TextBox runat="server" CssClass="form-control form-control-sm" ID="barcodeNo"
                                Enabled="false" ReadOnly="true" />
                        </div>
                    </div>

                    <!-- Store No Field -->
                    <div class="row g-2 mb-3 " style="color: #BD467F;">
                        <label for="<%= storeNo.ClientID %>" class="col-sm-4 col-form-label">Location</label>
                        <div class="col-sm-8">
                            <asp:TextBox runat="server" CssClass="form-control form-control-sm"
                                ID="storeNo" name="store_no"
                                Enabled="false" ReadOnly="true" />
                        </div>
                    </div>

                    <!-- StaffName Field -->
                    <div class="row g-2 mb-3" style="color: #BD467F;">
                        <label for="<%= staffName.ClientID %>" class="col-sm-4 col-form-label">Staff Name</label>
                        <div class="col-sm-8">
                            <asp:TextBox ID="staffName" runat="server" CssClass="form-control form-control-sm" Enabled="false" ReadOnly="true"></asp:TextBox>
                        </div>
                    </div>

                    <!-- Note Field -->
                    <div class="row g-2 mb-3 " style="color: #BD467F;">
                        <label for="<%= note.ClientID %>" class="col-sm-4 col-form-label">Note</label>
                        <div class="col-sm-8">
                            <asp:TextBox runat="server" TextMode="MultiLine"
                                CssClass="form-control form-control-sm"
                                ID="note" name="note" Rows="2"
                                placeholder="Enter here..." />
                        </div>
                    </div>

                   <div class="text-center">
                        <asp:Button Text="Save" runat="server" CssClass="btn px-4 me-2 text-white"
                            style="background: #a10d54; border-radius: 20px;"
                            ID="addBtn" OnClick="addBtn_Click1" />
                    </div>

                </div>
            </div>
        </div>

        <!-- Expiry List Column -->
         <div class="col-lg-7 col-md-8 p-0">
            <div class="card shadow-sm" style="border-radius: 10px;">

                <!-- Card Header -->
                <div class="card-header text-white text-center"
                    style="background-color: #BD467F; border-top-left-radius: 10px; border-top-right-radius: 10px;">
                    <h2 class="mb-0">Reorder Quantity List</h2>
                </div>

                <div class="card-body p-2 overflow-scroll" style="background-color: #F1B4D1;">
                    <asp:Button Text="Sent Request" runat="server" ID="btnConfirmAll"
                        CssClass="btn text-white m-1 fw-bold"
                        Style="background-color: #a10d54; border-radius: 10px;"
                        CausesValidation="false" OnClick="btnConfirmAll_Click1" />

                        <!-- Reduced padding -->
                       <div class="table-responsive">
                        <asp:GridView runat="server" 
                                ID="gridTable"
                                AutoGenerateColumns="false"
                                ShowFooter="true" DataKeyNames="No"
                                CssClass="table table-striped table-hover border-black border-2 shadow-lg mt-2 border-top"
                                GridLines="None" Width="100%"
                                OnRowEditing="GridView_RowEditing"
                                OnRowDeleting="GridView_RowDeleting" OnRowDataBound="gridTable_RowDataBound"
                                OnRowCancelingEdit="GridView_RowCancelingEdit" 
                                OnRowUpdating="GridView_RowUpdating"
                                EnableViewState="true">

                             <HeaderStyle Wrap="true" BackColor="#bd467f" Font-Bold="True" ForeColor="#f1b4d1"></HeaderStyle>
                             <EditRowStyle BackColor="#999999" />
                             <FooterStyle BackColor="#bd467f" Font-Bold="True" ForeColor="#f1b4d1" />
                             <PagerStyle CssClass="pagination-wrapper" HorizontalAlign="Center" VerticalAlign="Middle" />
                             <RowStyle CssClass="table-row data-row" BackColor="#E6DED1" ForeColor="#333333"></RowStyle>
                             <AlternatingRowStyle CssClass="table-alternating-row" BackColor="#eda7cc" ForeColor="#e6ded1"></AlternatingRowStyle>


                                <EmptyDataTemplate>
                                    <div class="alert text-white" style="background: #BD467F;">No items to confirm!</div>
                                </EmptyDataTemplate>

                                <Columns>
                                   <asp:BoundField DataField="ItemNo" HeaderText="Item No" ReadOnly="True" 
                                        HeaderStyle-ForeColor="#f1b4d1" HeaderStyle-BackColor="#bd467f" 
                                        ItemStyle-ForeColor="#bd467f" /> 
                                    <asp:BoundField DataField="Description" HeaderText="Description" 
                                        ControlStyle-Width="151px" ReadOnly="True" 
                                        HeaderStyle-ForeColor="#f1b4d1" HeaderStyle-BackColor="#bd467f" 
                                        ItemStyle-ForeColor="#bd467f" /> 

                                    <asp:TemplateField HeaderText="Qty">
                                        <ItemTemplate>
                                            <asp:Label ID="lblQuantity" runat="server" Text='<%# Eval("Qty") %>'></asp:Label>
                                        </ItemTemplate>
                                        <EditItemTemplate>
                                            <asp:TextBox ID="txtQuantity" runat="server"
                                                Text='<%# Bind("Qty") %>' Width="59px"
                                                onkeypress="return blockInvalidChars(event)" onpaste="return false"></asp:TextBox>
                                        </EditItemTemplate>
                                        <HeaderStyle ForeColor="#f1b4d1" BackColor="#bd467f" />
                                        <ControlStyle ForeColor="#bd467f" />
                                    </asp:TemplateField>

                                <asp:TemplateField HeaderText="UOM" ItemStyle-CssClass="priority-3">
                                    <ItemTemplate>
                                        <asp:Label ID="lblUom" runat="server" Text='<%# Eval("UOM") %>'></asp:Label>
                                    </ItemTemplate>
                                    <EditItemTemplate>
                                        <asp:DropDownList runat="server" CssClass="form-select form-select-sm" Width="111px" ID="ddlUom" />
                                    </EditItemTemplate>
                                    <HeaderStyle ForeColor="#f1b4d1" BackColor="#bd467f" />
                                    <ControlStyle ForeColor="#bd467f" />
                                </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Packing Info">
                                        <ItemTemplate>
                                            <asp:Label ID="lblPack" runat="server" Text='<%# Eval("PackingInfo") %>'></asp:Label>
                                        </ItemTemplate>
                                        <HeaderStyle ForeColor="#f1b4d1" BackColor="#bd467f" />
                                        <ControlStyle ForeColor="#bd467f" />
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Note">
                                        <ItemTemplate>
                                            <asp:Label ID="lblNote" runat="server" Text='<%# Eval("Note") %>'></asp:Label>
                                        </ItemTemplate>
                                        <EditItemTemplate>
                                            <asp:TextBox ID="txtNote" runat="server" Text='<%# Bind("Note") %>' Width="151px"></asp:TextBox>
                                        </EditItemTemplate>
                                        <HeaderStyle ForeColor="#f1b4d1" BackColor="#bd467f" />
                                        <ControlStyle ForeColor="#bd467f" />
                                    </asp:TemplateField>

                                    <asp:TemplateField Visible="false">
                                        <ItemTemplate>
                                            <asp:HiddenField ID="hfBarcodeNo" runat="server" Value='<%# Eval("BarcodeNo") %>' />
                                            <asp:HiddenField ID="hfRemark" runat="server" Value='<%# Eval("Remark") %>' />
                                            <asp:HiddenField ID="hfCompleted" runat="server" Value='<%# Eval("completedDate") %>' />
                                            <asp:HiddenField ID="hfDivisionCode" runat="server" Value='<%# Eval("divisionCode") %>' />
                                            <asp:HiddenField ID="hfVendorNo" runat="server" Value='<%# Eval("vendorNo") %>' />
                                            <asp:HiddenField ID="hfVendorName" runat="server" Value='<%# Eval("vendorName") %>' />
                                        </ItemTemplate>
                                        <EditItemTemplate>
                                            <asp:HiddenField ID="hfBarcodeNo" runat="server" Value='<%# Bind("BarcodeNo") %>' />
                                            <asp:HiddenField ID="hfRemark" runat="server" Value='<%# Bind("Remark") %>' />
                                            <asp:HiddenField ID="hfCompleted" runat="server" Value='<%# Bind("completedDate") %>' />
                                            <asp:HiddenField ID="hfDivisionCode" runat="server" Value='<%# Eval("divisionCode") %>' />
                                            <asp:HiddenField ID="hfVendorNo" runat="server" Value='<%# Eval("vendorNo") %>' />
                                            <asp:HiddenField ID="hfVendorName" runat="server" Value='<%# Eval("vendorName") %>' />
                                        </EditItemTemplate>
                                        <HeaderStyle ForeColor="#f1b4d1" BackColor="#bd467f" />
                                        <FooterStyle ForeColor="#f1b4d1" BackColor="#bd467f" />
                                    </asp:TemplateField>

                                    <asp:CommandField ShowEditButton="True" ShowDeleteButton="True" HeaderStyle-BackColor="#bd467f"
                                        CausesValidation="false" EditText="<i class='fa-solid fa-pen-to-square'></i>"
                                        DeleteText="<i class='fa-solid fa-trash'></i>"
                                        UpdateText="<i class='fa-solid fa-file-arrow-up'></i>"
                                        CancelText="<i class='fa-solid fa-xmark'></i>"
                                        ControlStyle-CssClass="btn btn-outline-primary m-1 text-white" ControlStyle-BackColor="#a10d54" />
                                </Columns>
                            </asp:GridView>
                        </div>
                    </div>
                </div>
        </div>
    </div>
</div>
</asp:Content>
