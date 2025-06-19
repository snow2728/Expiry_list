<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" EnableEventValidation="false" CodeBehind="rege1.aspx.cs" Inherits="Expiry_list.ReorderForm.rege1" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
  <script type="text/javascript">
      $(document).ready(function () {
          var staffName = $('#<%= hiddenStaffName.ClientID %>').val();
        $('#<%= staffName.ClientID %>').val(staffName);

        //console.log("Page Ready. Staff Name:", staffName);

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
                                packing: item.PackingInfo
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
        });

        $('#<%= itemNo.ClientID %>').on('select2:clear', function (e) {
            clearFields();
            console.log("Item selection cleared.");
        });
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

    }
  </script>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <a href="../AdminDashboard.aspx" class="btn text-white ms-2" style="background: #996FD6;"><i class="fa-solid fa-left-long"></i>Home</a>

<div class="container-fluid" style="color:white; font-weight:bold;">
    <div class="row g-2">

        <div class="col-12 col-lg-5 col-md-4">
            <div class="card shadow-sm h-100">
                <!-- Card Header -->
                <div class="card-header text-white text-center"
                    style="background-color: #996FD6; border-top-left-radius: 10px; border-top-right-radius: 10px;">
                    <h2 class="mb-0">Reorder Quantity Form</h2>
                    <asp:Label ID="tdyDate" runat="server" CssClass="ms-2" />
                    <asp:Literal ID="sessionDataLiteral" runat="server" />
                </div>

                <!-- Card Body -->
                  <div class="card-body" style="background-color: #D0C9EA;">

                    <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
                    <asp:UpdatePanel runat="server" ID="UpdatePanel1" UpdateMode="Conditional">
                        <ContentTemplate>
                            <!-- No Field -->
                            <div class="row g-2 mb-3" style="color: #996FD6;">
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

                    <!-- Item No Field -->
                    <div class="row g-2 mb-3 " style="color: #996FD6;">
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
                    <div class="row g-2 mb-3 " style="color: #996FD6;">
                        <label for="<%= desc.ClientID %>" class="col-sm-4 col-form-label">Description</label>
                        <div class="col-sm-8">
                            <asp:TextBox runat="server" CssClass="form-control form-control-sm"
                                TextMode="MultiLine" ID="desc"
                                Enabled="false" ReadOnly="true" />
                        </div>
                    </div>

                    <!-- Quantity Field -->
                    <div class="row g-2 mb-3" style="color: #996FD6;">
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
                    <div class="row g-2 mb-3" style="color: #996FD6;">
                        <label for="<%= uom.ClientID %>" class="col-sm-4 col-form-label">UOM</label>
                        <div class="col-sm-8">
                           <asp:DropDownList runat="server" CssClass="form-select form-select-sm" ID="uom" AppendDataBoundItems="true" />
                        </div>
                    </div>

                    <!-- Packing Info Field -->
                    <div class="row g-2 mb-3 " style="color: #996FD6;">
                        <label for="<%= packingInfo.ClientID %>" class="col-sm-4 col-form-label">Packing Info</label>
                        <div class="col-sm-8">
                            <asp:TextBox runat="server" CssClass="form-control form-control-sm" ID="packingInfo"
                                Enabled="false" ReadOnly="true" />
                        </div>
                    </div>

                    <!-- Barcode No Field -->
                    <div class="row g-2 mb-3" style="color: #996FD6;" >
                        <label for="<%= barcodeNo.ClientID %>" class="col-sm-4 col-form-label">Barcode No</label>
                        <div class="col-sm-8">
                            <asp:TextBox runat="server" CssClass="form-control form-control-sm" ID="barcodeNo"
                                Enabled="false" ReadOnly="true" />
                        </div>
                    </div>

                    <!-- Store No Field -->
                    <div class="row g-2 mb-3 " style="color: #996FD6;">
                        <label for="<%= storeNo.ClientID %>" class="col-sm-4 col-form-label">Location</label>
                        <div class="col-sm-8">
                            <asp:TextBox runat="server" CssClass="form-control form-control-sm"
                                ID="storeNo" name="store_no"
                                Enabled="false" ReadOnly="true" />
                        </div>
                    </div>

                    <!-- StaffName Field -->
                    <div class="row g-2 mb-3" style="color: #996FD6;">
                        <label for="<%= staffName.ClientID %>" class="col-sm-4 col-form-label">Staff Name</label>
                        <div class="col-sm-8">
                            <asp:TextBox ID="staffName" runat="server" CssClass="form-control form-control-sm" Enabled="false" ReadOnly="true"></asp:TextBox>
                        </div>
                    </div>

                    <!-- Note Field -->
                    <div class="row g-2 mb-3 " style="color: #996FD6;">
                        <label for="<%= note.ClientID %>" class="col-sm-4 col-form-label">Note</label>
                        <div class="col-sm-8">
                            <asp:TextBox runat="server" TextMode="MultiLine"
                                CssClass="form-control form-control-sm"
                                ID="note" name="note" Rows="2"
                                placeholder="Enter here..." />
                        </div>
                    </div>

                   <div class="text-center">
                        <asp:Button Text="Add" runat="server" CssClass="btn px-4 me-2 text-white"
                            style="background: #996FD6; border-radius: 20px;"
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
                    style="background-color: #996FD6; border-top-left-radius: 10px; border-top-right-radius: 10px;">
                    <h2 class="mb-0">Reorder Quantity List</h2>
                </div>

                <div class="card-body p-2 overflow-scroll" style="background-color: #D0C9EA;">
                    <asp:Button Text="Sent Request" runat="server" ID="btnConfirmAll"
                        CssClass="btn text-white m-1 fw-bold"
                        Style="background-color: #996FD6; border-radius: 10px;"
                        CausesValidation="false" OnClick="btnConfirmAll_Click1" />

                        <!-- Reduced padding -->
                       <div class="table-responsive" style="color: #D0C9EA;">
                        <asp:GridView runat="server"
                                ID="gridTable"
                                RowStyle-CssClass="grid-purple-text"
                                AutoGenerateColumns="false"
                                ShowFooter="true" DataKeyNames="No"
                                CssClass="table table-striped table-hover border-black border-2 shadow-lg mt-2 border-top"
                                GridLines="None" Width="100%"
                                OnRowEditing="GridView_RowEditing"
                                OnRowDeleting="GridView_RowDeleting" OnRowDataBound="gridTable_RowDataBound"
                                OnRowCancelingEdit="GridView_RowCancelingEdit" 
                                OnRowUpdating="GridView_RowUpdating"
                                EnableViewState="true">

                                <EmptyDataTemplate>
                                    <div class="alert text-white" style="background: #996FD6;">No items to confirm!</div>
                                </EmptyDataTemplate>

                                <Columns>
                                    <asp:BoundField DataField="ItemNo" HeaderText="Item No" ReadOnly="True" HeaderStyle-ForeColor="#9E7CD7" HeaderStyle-BackColor="#C2B4E2" />
                                    <asp:BoundField DataField="Description" HeaderText="Description" ControlStyle-Width="151px" ReadOnly="True" HeaderStyle-ForeColor="#9E7CD7" HeaderStyle-BackColor="#C2B4E2" />

                                    <asp:TemplateField HeaderText="Qty">
                                        <ItemTemplate>
                                            <asp:Label ID="lblQuantity" runat="server" Text='<%# Eval("Qty") %>'></asp:Label>
                                        </ItemTemplate>
                                        <EditItemTemplate>
                                            <asp:TextBox ID="txtQuantity" runat="server"
                                                Text='<%# Bind("Qty") %>' Width="59px"
                                                onkeypress="return blockInvalidChars(event)" onpaste="return false"></asp:TextBox>
                                        </EditItemTemplate>
                                          <HeaderStyle ForeColor="#9E7CD7" BackColor="#C2B4E2" />
                                    </asp:TemplateField>

                                <asp:TemplateField HeaderText="UOM" ItemStyle-CssClass="priority-3">
                                    <ItemTemplate>
                                        <asp:Label ID="lblUom" runat="server" Text='<%# Eval("UOM") %>'></asp:Label>
                                    </ItemTemplate>
                                    <EditItemTemplate>
                                        <asp:DropDownList runat="server" CssClass="form-select form-select-sm" Width="111px" ID="ddlUom" />
                                    </EditItemTemplate>
                                    <HeaderStyle ForeColor="#9E7CD7" BackColor="#C2B4E2" />
                                </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Packing Info">
                                        <ItemTemplate>
                                            <asp:Label ID="lblPack" runat="server" Text='<%# Eval("PackingInfo") %>'></asp:Label>
                                        </ItemTemplate>
                                        <HeaderStyle ForeColor="#9E7CD7" BackColor="#C2B4E2" />
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Note">
                                        <ItemTemplate>
                                            <asp:Label ID="lblNote" runat="server" Text='<%# Eval("Note") %>'></asp:Label>
                                        </ItemTemplate>
                                        <EditItemTemplate>
                                            <asp:TextBox ID="txtNote" runat="server" Text='<%# Bind("Note") %>' Width="151px"></asp:TextBox>
                                        </EditItemTemplate>
                                        <HeaderStyle ForeColor="#9E7CD7" BackColor="#C2B4E2" />
                                    </asp:TemplateField>

                                    <asp:TemplateField Visible="false">
                                        <ItemTemplate>
                                            <asp:HiddenField ID="hfBarcodeNo" runat="server" Value='<%# Eval("BarcodeNo") %>' />
                                            <asp:HiddenField ID="hfRemark" runat="server" Value='<%# Eval("Remark") %>' />
                                            <asp:HiddenField ID="hfCompleted" runat="server" Value='<%# Eval("completedDate") %>' />
                                        </ItemTemplate>
                                        <EditItemTemplate>
                                            <asp:HiddenField ID="hfBarcodeNo" runat="server" Value='<%# Bind("BarcodeNo") %>' />
                                            <asp:HiddenField ID="hfRemark" runat="server" Value='<%# Bind("Remark") %>' />
                                            <asp:HiddenField ID="hfCompleted" runat="server" Value='<%# Bind("completedDate") %>' />
                                        </EditItemTemplate>
                                        <HeaderStyle ForeColor="#9E7CD7" BackColor="#B399DD" />
                                        <FooterStyle ForeColor="#9E7CD7" BackColor="#B399DD" />
                                    </asp:TemplateField>

                                    <asp:CommandField ShowEditButton="True" ShowDeleteButton="True" HeaderStyle-BackColor="#C2B4E2"
                                        CausesValidation="false" EditText="<i class='fa-solid fa-pen-to-square'></i>"
                                        DeleteText="<i class='fa-solid fa-trash'></i>"
                                        UpdateText="<i class='fa-solid fa-file-arrow-up'></i>"
                                        CancelText="<i class='fa-solid fa-xmark'></i>"
                                        ControlStyle-CssClass="btn btn-outline-primary m-1 text-white" ControlStyle-BackColor="#9E7CD7" />
                                </Columns>
                            </asp:GridView>
                        </div>
                    </div>
                </div>
        </div>
    </div>
</div>
</asp:Content>
