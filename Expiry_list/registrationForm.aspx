<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="registrationForm.aspx.cs" Inherits="Expiry_list.registrationForm" EnableEventValidation="false" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">

    <script type="text/javascript">

        $(document).ready(function () {
            var staffName = $('#<%= hiddenStaffName.ClientID %>').val();
              $('#<%= staffName.ClientID %>').val(staffName);

              // Initialize Select2 for item search
              $("#<%= itemNo.ClientID %>").select2({
                  placeholder: 'Search by Item No, Description, or Barcode',
                  minimumInputLength: 2,
                  allowClear: true,
                  //tags: true,
                  ajax: {
                      url: 'registrationForm.aspx/GetItems',
                      type: 'POST',
                      contentType: "application/json; charset=utf-8",
                      dataType: 'json',
                      delay: 250,
                      data: function (params) {
                          return JSON.stringify({ searchTerm: params.term });
                      },
                      processResults: function (data) {
                          setTimeout(function () {

                          }

                          return {
                              results: data.d.map(function (item) {
                                  return {
                                      id: item.ItemNo,
                                      text: item.ItemNo + " - " + item.ItemDescription,
                                      description: item.ItemDescription,
                                      barcode: item.Barcode,
                                      uom: item.UOM,
                                      packing: item.PackingInfo
                                  };
                              })
                          };
                      },
                      cache: true
                  }
              });

              var selectedItemId = $('#<%= hiddenSelectedItem.ClientID %>').val();
              var selectedItemText = $('#<%= hiddenDescription.ClientID %>').val();

              if (selectedItemId) {
                  // Manually add the selected option to Select2
                  var $itemNo = $('#<%= itemNo.ClientID %>');
                  var option = new Option(selectedItemId + " - " + selectedItemText, selectedItemId, true, true);
                  $itemNo.append(option).trigger('change');
              }

              $('#<%= itemNo.ClientID %>').on('select2:select', function (e) {
                  clearFields();

                  var selectedData = e.params.data;

                  // Set the new values after clearing
                  $('#<%= desc.ClientID %>').val(selectedData.description);
                    $('#<%= uom.ClientID %>').val(selectedData.uom);
                    $('#<%= packingInfo.ClientID %>').val(selectedData.packing);

                    var barcode = selectedData.barcode.length > 0 ? selectedData.barcode[0] : '';
                    $('#<%= barcodeNo.ClientID %>').val(barcode);

                    // Update hidden fields
                    $('#<%= hiddenSelectedItem.ClientID %>').val(selectedData.id);
                    $('#<%= hiddenBarcodeNo.ClientID %>').val(barcode);
                    $('#<%= hiddenDescription.ClientID %>').val(selectedData.description);
                    $('#<%= hiddenUOM.ClientID %>').val(selectedData.uom);
                    $('#<%= hiddenPackingInfo.ClientID %>').val(selectedData.packing);
                });

              $('#<%= itemNo.ClientID %>').on('select2:clear', function (e) {
                  clearFields();
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

        // Clear textboxes when a new item is selected
        function clearFields() {
            $('#<%= desc.ClientID %>').val('');
               $('#<%= uom.ClientID %>').val('');
               $('#<%= packingInfo.ClientID %>').val('');
               $('#<%= barcodeNo.ClientID %>').val('');
               $('#<%= qty.ClientID %>').val('');
               $('#<%= expiryDate.ClientID %>').val('');
               $('#<%= batchNo.ClientID %>').val('');
               $('#<%= note.ClientID %>').val('');

               // Clear hidden fields as well
               $('#<%= hiddenSelectedItem.ClientID %>').val('');
               $('#<%= hiddenBarcodeNo.ClientID %>').val('');
               $('#<%= hiddenDescription.ClientID %>').val('');
               $('#<%= hiddenUOM.ClientID %>').val('');
               $('#<%= hiddenPackingInfo.ClientID %>').val('');

               $('#<%= hiddenQty.ClientID %>').val('');
               $('#<%= hiddenBatchNo.ClientID %>').val('');
        }

        function restoreFields(itemNoValue, itemDescription) {
            if (itemNoValue) {
                // Clear existing options and add the selected item
                var $itemNo = $('#<%= itemNo.ClientID %>');
                   $itemNo.empty(); // Clear existing options
                   var option = new Option(itemNoValue + " - " + itemDescription, itemNoValue, true, true);
                   $itemNo.append(option).trigger('change');
               }

               // Restore other fields from hidden values
               $('#<%= desc.ClientID %>').val($('#<%= hiddenDescription.ClientID %>').val());
               $('#<%= uom.ClientID %>').val($('#<%= hiddenUOM.ClientID %>').val());
               $('#<%= packingInfo.ClientID %>').val($('#<%= hiddenPackingInfo.ClientID %>').val());
               $('#<%= barcodeNo.ClientID %>').val($('#<%= hiddenBarcodeNo.ClientID %>').val());
        }

        history.pushState(null, null, location.href);

        window.addEventListener("popstate", function (event) {
            location.reload();
        });

    </script>

</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">


    <div class="container-fluid" style="background-color: #f1f1f2;">
        <div class="row g-2">

            <div class="col-12 col-lg-5 col-md-4">
                <div class="card shadow-sm h-100">
                    <!-- Card Header -->
                    <div class="card-header text-white text-center"
                        style="background-color: #1995ad; border-top-left-radius: 10px; border-top-right-radius: 10px;">
                        <h2 class="mb-0">Registration Form</h2>
                        <asp:Label ID="tdyDate" runat="server" CssClass="ms-2" />
                        <asp:Literal ID="sessionDataLiteral" runat="server" />
                    </div>

                    <!-- Card Body -->
                    <div class="card-body">

                        <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
                        <asp:UpdatePanel runat="server" ID="UpdatePanel1" UpdateMode="Conditional">
                            <ContentTemplate>
                                <!-- No Field -->
                                <div class="row g-2 mb-3">
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
                        <asp:HiddenField ID="hiddenBatchNo" runat="server" />
                        <asp:HiddenField ID="hiddenNote" runat="server" />
                        <asp:HiddenField ID="hiddenQty" runat="server" />
                        <asp:HiddenField ID="hiddenStaffName" runat="server" />

                        <!-- Item No Field -->
                        <div class="row g-2 mb-3">
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
                        <div class="row g-2 mb-3">
                            <label for="<%= desc.ClientID %>" class="col-sm-4 col-form-label">Description</label>
                            <div class="col-sm-8">
                                <asp:TextBox runat="server" CssClass="form-control form-control-sm"
                                    TextMode="MultiLine" ID="desc"
                                    Enabled="false" ReadOnly="true" />
                            </div>
                        </div>

                        <!-- Expiry Date Field -->
                        <div class="row g-2 mb-3">
                            <label for="<%= expiryDate.ClientID %>" class="col-sm-4 col-form-label">Expiry Date</label>
                            <div class="col-sm-8">
                                <asp:TextBox runat="server" CssClass="form-control form-control-sm" TextMode="Month"
                                    ID="expiryDate" name="expiry_date" />
                                <asp:RequiredFieldValidator ID="RequiredFieldValidator3" runat="server"
                                    ErrorMessage="Date must be selected!"
                                    ControlToValidate="expiryDate" Display="Dynamic"
                                    CssClass="text-danger" SetFocusOnError="True" />
                            </div>
                        </div>

                        <!-- Quantity Field -->
                        <div class="row g-2 mb-3">
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

                        <!-- Base UOM Field -->
                        <div class="row g-2 mb-3">
                            <label for="<%= uom.ClientID %>" class="col-sm-4 col-form-label">Base UOM</label>
                            <div class="col-sm-8">
                                <asp:TextBox runat="server" CssClass="form-control form-control-sm" ID="uom"
                                    Enabled="false" ReadOnly="true" />
                            </div>
                        </div>

                        <!-- Packing Info Field -->
                        <div class="row g-2 mb-3">
                            <label for="<%= packingInfo.ClientID %>" class="col-sm-4 col-form-label">Packing Info</label>
                            <div class="col-sm-8">
                                <asp:TextBox runat="server" CssClass="form-control form-control-sm" ID="packingInfo"
                                    Enabled="false" ReadOnly="true" />
                            </div>
                        </div>

                        <!-- Barcode No Field -->
                        <div class="row g-2 mb-3">
                            <label for="<%= barcodeNo.ClientID %>" class="col-sm-4 col-form-label">Barcode No</label>
                            <div class="col-sm-8">
                                <asp:TextBox runat="server" CssClass="form-control form-control-sm" ID="barcodeNo"
                                    Enabled="false" ReadOnly="true" />
                            </div>
                        </div>

                        <!-- Batch No Field -->
                        <div class="row g-2 mb-3">
                            <label for="<%= batchNo.ClientID %>" class="col-sm-4 col-form-label">Batch No</label>
                            <div class="col-sm-8">
                                <asp:TextBox runat="server" CssClass="form-control form-control-sm"
                                    ID="batchNo" name="batch_no" />
                                <asp:RequiredFieldValidator ID="RequiredFieldValidator4" runat="server"
                                    ErrorMessage="Batch No must be chosen!"
                                    ControlToValidate="batchNo" Display="Dynamic"
                                    CssClass="text-danger" SetFocusOnError="True" />
                            </div>
                        </div>

                        <!-- Store No Field -->
                        <div class="row g-2 mb-3">
                            <label for="<%= storeNo.ClientID %>" class="col-sm-4 col-form-label">Location</label>
                            <div class="col-sm-8">
                                <asp:TextBox runat="server" CssClass="form-control form-control-sm"
                                    ID="storeNo" name="store_no"
                                    Enabled="false" ReadOnly="true" />
                            </div>
                        </div>

                        <!-- StaffName Field -->
                        <div class="row g-2 mb-3">
                            <label for="<%= staffName.ClientID %>" class="col-sm-4 col-form-label">Staff Name</label>
                            <div class="col-sm-8">
                                <asp:TextBox ID="staffName" runat="server" CssClass="form-control form-control-sm" Enabled="false" ReadOnly="true"></asp:TextBox>
                            </div>
                        </div>

                        <!-- Note Field -->
                        <div class="row g-2 mb-3">
                            <label for="<%= note.ClientID %>" class="col-sm-4 col-form-label">Note</label>
                            <div class="col-sm-8">
                                <asp:TextBox runat="server" TextMode="MultiLine"
                                    CssClass="form-control form-control-sm"
                                    ID="note" name="note" Rows="2"
                                    placeholder="Enter here..." />
                            </div>
                        </div>

                        <!-- Add Button -->
                        <div class="text-center ">
                            <asp:Button Text="Add" runat="server" CssClass="btn px-4 me-2 text-white"
                                Style="background-color: #158396; border-radius: 20px;"
                                ID="addBtn" OnClick="addBtn_Click1" />

                            <%--<asp:Button Text="Clear" runat="server" CssClass="btn px-4"
                                Style="background-color: #1995ad; color: #f1f1f2; border-radius: 20px;"
                                ID="clearBtn" OnClick="clearBtn_Click" />--%>
                        </div>

                    </div>
                </div>
            </div>

            <!-- Expiry List Column -->
            <div class="col-lg-7 col-md-8 p-0">
                <!-- Remove padding -->
                <div class="card shadow-sm" style="border-radius: 10px;">

                    <!-- Card Header -->
                    <div class="card-header text-white text-center"
                        style="background-color: #1995ad; border-top-left-radius: 10px; border-top-right-radius: 10px;">
                        <h2 class="mb-0">Expiry List</h2>
                    </div>
 
                        <div class="card-body p-2 overflow-scroll">
                            <asp:Button Text="Confirm" runat="server" ID="btnConfirmAll"
                                CssClass="btn btn-dark m-1 fa-1x text-black"
                                CausesValidation="false" Style="background-color: #a1d6e2;" OnClick="btnConfirmAll_Click1" />
                            <!-- Reduced padding -->
                            <div class="table-responsive">
                                <!-- Bootstrap responsive table wrapper -->
                                <asp:GridView runat="server"
                                    ID="gridTable"
                                    AutoGenerateColumns="false"
                                    ShowFooter="true" DataKeyNames="No"
                                    CssClass="table table-striped table-hover border-black border-2 shadow-lg mt-2 border-top"
                                    GridLines="None" Width="100%"
                                    OnRowEditing="GridView_RowEditing"
                                    OnRowDeleting="GridView_RowDeleting"
                                    OnRowCancelingEdit="GridView_RowCancelingEdit"
                                    OnRowUpdating="GridView_RowUpdating"
                                    EnableViewState="true">

                                    <EmptyDataTemplate>
                                        <div class="alert alert-info">No items to confirm!</div>
                                    </EmptyDataTemplate>

                                    <Columns>
                                        <asp:BoundField DataField="ItemNo" HeaderText="Item No" ReadOnly="True" />
                                        <asp:BoundField DataField="Description" HeaderText="Description" ReadOnly="True" />

                                        <asp:TemplateField HeaderText="Qty">
                                            <ItemTemplate>
                                                <asp:Label ID="lblQuantity" runat="server" Text='<%# Eval("Qty") %>'></asp:Label>
                                            </ItemTemplate>
                                            <EditItemTemplate>
                                                <asp:TextBox ID="txtQuantity" runat="server"
                                                    Text='<%# Bind("Qty") %>' Width="59px"
                                                    onkeypress="return blockInvalidChars(event)" onpaste="return false"></asp:TextBox>
                                            </EditItemTemplate>
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="UOM">
                                            <ItemTemplate>
                                                <asp:Label ID="lblUom" runat="server" Text='<%# Eval("UOM") %>'></asp:Label>
                                            </ItemTemplate>
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Packing Info">
                                            <ItemTemplate>
                                                <asp:Label ID="lblPack" runat="server" Text='<%# Eval("PackingInfo") %>'></asp:Label>
                                            </ItemTemplate>
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Expiry Date">
                                            <ItemTemplate>
                                                <asp:Label ID="lblExpiryDate" runat="server" Text='<%# Eval("ExpiryDate", "{0:MMM-yyyy}") %>'></asp:Label>
                                            </ItemTemplate>
                                            <EditItemTemplate>
                                                <asp:TextBox ID="txtExpiryDate" runat="server"
                                                    Text='<%# Bind("ExpiryDate", "{0:yyyy-MM}") %>'
                                                    type="month" Width="109px"></asp:TextBox>
                                            </EditItemTemplate>
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Batch">
                                            <ItemTemplate>
                                                <asp:Label ID="lblBatch" runat="server" Text='<%# Eval("BatchNo") %>'></asp:Label>
                                            </ItemTemplate>
                                            <EditItemTemplate>
                                                <asp:TextBox ID="txtBatch" runat="server" Text='<%# Bind("BatchNo") %>' Width="127px"></asp:TextBox>
                                            </EditItemTemplate>
                                        </asp:TemplateField>

                                        <asp:TemplateField HeaderText="Note">
                                            <ItemTemplate>
                                                <asp:Label ID="lblNote" runat="server" Text='<%# Eval("Note") %>'></asp:Label>
                                            </ItemTemplate>
                                            <EditItemTemplate>
                                                <asp:TextBox ID="txtNote" runat="server" Text='<%# Bind("Note") %>' Width="157px"></asp:TextBox>
                                            </EditItemTemplate>
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
                                        </asp:TemplateField>

                                        <asp:CommandField ShowEditButton="True" ShowDeleteButton="True"
                                            ControlStyle-CssClass="btn btn-outline-primary m-1 text-white" ControlStyle-BackColor="#158396" />
                                    </Columns>
                                </asp:GridView>
                            </div>
                        </div>
                    </div>
            </div>
        </div>
    </div>
</asp:Content>
