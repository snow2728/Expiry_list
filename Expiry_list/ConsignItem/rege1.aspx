<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" EnableEventValidation="false" CodeBehind="rege1.aspx.cs" Inherits="Expiry_list.ConsignItem.rege1" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">

        <script type="text/javascript">

            $(document).ready(function () {
                var staffName = $('#<%= hiddenStaffName.ClientID %>').val();
                $('#<%= staffName.ClientID %>').val(staffName);
                const vendorNoSelector = '#<%= vendorNo.ClientID %>';
                const hiddenVendorNoSelector = '#<%= hiddenVendorNo.ClientID %>';
                const hiddenVendorTextSelector = '#<%= hiddenVendorText.ClientID %>';

                $(vendorNoSelector).select2({
                    placeholder: 'Search by Vendor No or Vendor Name',
                    minimumInputLength: 2,
                    ajax: {
                        url: 'rege1.aspx/GetVendors',
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

                    $('#<%= itemNo.ClientID %>').val(null).trigger('change');
                    clearFields();
                });

                // Initialize Select2 for item search
                $("#<%= itemNo.ClientID %>").select2({
                    placeholder: 'Search by Item No, Description, or Barcode',
                    minimumInputLength: 0,
                    allowClear: true,
                    ajax: {
                        url: 'rege1.aspx/GetItems',
                        type: 'POST',
                        contentType: "application/json; charset=utf-8",
                        dataType: 'json',
                        delay: 250,
                        data: function (params) {
                            var vendorNo = $('#<%= vendorNo.ClientID %>').val();
                            if (!vendorNo) {
                                Swal.fire({
                                    icon: 'error',
                                    title: 'Vendor Required',
                                    text: 'Please select a vendor first'
                                });
                                return JSON.stringify({
                                    request: {
                                        vendorNo: '',
                                        searchTerm: params.term || ''
                                    }
                                });
                            }
                            return JSON.stringify({
                                request: {
                                    vendorNo: vendorNo,
                                    searchTerm: params.term || ''
                                }
                            });
                        },
                        processResults: function (data) {
                            // Handle server errors
                            if (data && data.d && data.d.length > 0 && data.d[0].ItemNo === "ERROR") {
                                Swal.fire({
                                    icon: 'error',
                                    title: 'Server Error',
                                    text: data.d[0].ItemDescription
                                });
                                return { results: [] };
                            }

                            // Handle null response
                            if (!data || !data.d) {
                                return { results: [] };
                            }

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
                                results: items.map(function (item) {
                                    return {
                                        id: item.ItemNo,
                                        text: item.ItemNo + " - " + item.ItemDescription,
                                        description: item.ItemDescription,
                                        barcode: item.Barcode || [],
                                        uom: item.UOM || '',
                                        packing: item.PackingInfo || ''
                                    };
                                })
                            };
                        },
                        error: function (xhr, status, error) {
                            try {
                                var jsonResponse = JSON.parse(xhr.responseText);
                                var errorMessage = jsonResponse.Message || error;
                                Swal.fire({
                                    icon: 'error',
                                    title: 'Request Failed',
                                    text: 'Server error: ' + errorMessage
                                });
                            } catch (e) {
                                Swal.fire({
                                    icon: 'error',
                                    title: 'Request Failed',
                                    text: 'Server error: ' + error
                                });
                            }
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

                $("#<%= itemNo.ClientID %>").on('focus', function () {
                    $(this).select2('open');
                });

                $('#<%= itemNo.ClientID %>').on('select2:select', function (e) {
                    clearFields();

                    var selectedData = e.params.data;

                    $('#<%= desc.ClientID %>').val(selectedData.description);
                    $('#<%= uom.ClientID %>').val(selectedData.uom);
                    $('#<%= packingInfo.ClientID %>').val(selectedData.packing);

                    var barcode = selectedData.barcode.length > 0 ? selectedData.barcode[0] : '';
                    $('#<%= barcodeNo.ClientID %>').val(barcode);
                    $('#<%= hiddenSelectedItem.ClientID %>').val(selectedData.id);
                    $('#<%= hiddenBarcodeNo.ClientID %>').val(barcode);
                    $('#<%= hiddenDescription.ClientID %>').val(selectedData.description);
                    $('#<%= hiddenUOM.ClientID %>').val(selectedData.uom);
                    $('#<%= hiddenPackingInfo.ClientID %>').val(selectedData.packing);
                    $('#<%= hiddenVendorNo.ClientID %>').val($('#<%= vendorNo.ClientID %>').val());
                });

                $('#<%= itemNo.ClientID %>').on('select2:clear', function (e) {
                    clearFields();
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

            // Clear textboxes when a new item is selected
            function clearFields() {
                $('#<%= desc.ClientID %>').val('');
                $('#<%= uom.ClientID %>').val('');
                $('#<%= packingInfo.ClientID %>').val('');
                $('#<%= barcodeNo.ClientID %>').val('');
                $('#<%= qty.ClientID %>').val('');
                $('#<%= note.ClientID %>').val('');

                // Clear hidden fields as well
                $('#<%= hiddenSelectedItem.ClientID %>').val('');
                $('#<%= hiddenBarcodeNo.ClientID %>').val('');
                $('#<%= hiddenDescription.ClientID %>').val('');
                $('#<%= hiddenUOM.ClientID %>').val('');
                $('#<%= hiddenPackingInfo.ClientID %>').val('');
                $('#<%= hiddenQty.ClientID %>').val('');
            }

            function restoreFields(itemNoValue, itemDescription) {
                if (itemNoValue) {
                    var $itemNo = $('#<%= itemNo.ClientID %>');
                    $itemNo.empty(); 
                    var option = new Option(itemNoValue + " - " + itemDescription, itemNoValue, true, true);
                    $itemNo.append(option).trigger('change');
                }

                // Restore other fields from hidden values
                $('#<%= desc.ClientID %>').val($('#<%= hiddenDescription.ClientID %>').val());
                $('#<%= uom.ClientID %>').val($('#<%= hiddenUOM.ClientID %>').val());
                $('#<%= packingInfo.ClientID %>').val($('#<%= hiddenPackingInfo.ClientID %>').val());
                $('#<%= barcodeNo.ClientID %>').val($('#<%= hiddenBarcodeNo.ClientID %>').val());
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

    <div class="container-fluid" style="background-color: #f1f1f2;">
        <div class="row g-2">

            <div class="col-12 col-lg-5 col-md-4">
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
                        <asp:HiddenField ID="hiddenNote" runat="server" />
                        <asp:HiddenField ID="hiddenQty" runat="server" />
                        <asp:HiddenField ID="hiddenStaffName" runat="server" />
                        <asp:HiddenField ID="hiddenVendorNo" runat="server" />
                         <asp:HiddenField ID="hiddenVendorText" runat="server" />

                     <!-- Vendor No Field -->
                     <div class="row g-2 mb-3">
                        <label for="<%= vendorNo.ClientID %>" class="col-sm-4 col-form-label">Vendor</label>
                        <div class="col-sm-8">
                             <asp:DropDownList ID="vendorNo" runat="server" 
                                CssClass="form-control form-control-sm select2"
                                AppendDataBoundItems="true">
                                <asp:ListItem Text="" Value="" />
                            </asp:DropDownList>
                            <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server"
                                ErrorMessage="Vendor must be selected!"
                                ControlToValidate="vendorNo" Display="Dynamic"
                                CssClass="text-danger" SetFocusOnError="True" />
                        </div>
                    </div>

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
                                    MinimumValue="0" MaximumValue="1000000" Type="Integer"
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
                            <asp:Button Text="Save" runat="server" CssClass="btn px-4 me-2 fw-bolder text-white"
                                Style="background-color: #0D330E; border-radius: 20px;"
                                ID="addBtn" OnClick="addBtn_Click1" />

                        </div>

                    </div>
                </div>
            </div>

            <!-- Consignment List Column -->
            <div class="col-lg-7 col-md-8 p-0">
                <!-- Remove padding -->
                <div class="card shadow-sm" style="border-radius: 10px;">

                    <!-- Card Header -->
                    <div class="card-header text-white text-center"
                        style="background-color: #477023; border-top-left-radius: 10px; border-top-right-radius: 10px;">
                        <h2 class="mb-0 fw-bolder">Consignment List</h2>
                    </div>

                    <div class="card-body p-2 overflow-scroll">
                        <asp:Button Text="Confirm" runat="server" ID="btnConfirmAll"
                            CssClass="btn btn-whitw m-1 fa-1x text-white fw-bold"
                            CausesValidation="false" Style="background-color: #0D330E;" OnClick="btnConfirmAll_Click1" />

                        <div class="table-responsive">
                            <asp:GridView runat="server" BackColor="#E6DED1" ်ForeColor="#B1C095"
                                ID="gridTable"
                                AutoGenerateColumns="false"
                                ShowFooter="true" DataKeyNames="No"
                                CssClass="table table-striped table-hover border-black border-2 shadow-lg mt-2 border-top fw-bold"
                                GridLines="None" Width="100%"
                                OnRowEditing="GridView_RowEditing"
                                OnRowDeleting="GridView_RowDeleting"
                                OnRowCancelingEdit="GridView_RowCancelingEdit"
                                OnRowUpdating="GridView_RowUpdating"
                                EnableViewState="true">

                                <HeaderStyle Wrap="true" BackColor="#6A7D4F" Font-Bold="True" ForeColor="#B1C095"></HeaderStyle>
                                <EditRowStyle BackColor="#999999" />
                                <FooterStyle BackColor="#6A7D4F" Font-Bold="True" ForeColor="#B1C095" />
                                <PagerStyle CssClass="pagination-wrapper" HorizontalAlign="Center" VerticalAlign="Middle" />
                                <RowStyle CssClass="table-row data-row" BackColor="#E6DED1" ForeColor="#333333"></RowStyle>
                                <AlternatingRowStyle CssClass="table-alternating-row" BackColor="#B1C095" ForeColor="#284775"></AlternatingRowStyle>

                                <EmptyDataTemplate>
                                    <div class="alert" style="background-color:#B1C095;">No items to confirm!</div>
                                </EmptyDataTemplate>

                                <Columns>
                                    <asp:BoundField DataField="ItemNo" HeaderText="Item No" HeaderStyle-Font-Bold="true" HeaderStyle-ForeColor="#B1C095" ReadOnly="True" />
                                    <asp:BoundField DataField="Description" HeaderText="Description" ReadOnly="True" HeaderStyle-ForeColor="#B1C095" />

                                    <asp:TemplateField HeaderText="Qty" HeaderStyle-ForeColor="#B1C095"  HeaderStyle-Font-Bold="true">
                                        <ItemTemplate>
                                            <asp:Label ID="lblQuantity" runat="server" Text='<%# Eval("Qty") %>'></asp:Label>
                                        </ItemTemplate>
                                        <EditItemTemplate>
                                            <asp:TextBox ID="txtQuantity" runat="server"
                                                Text='<%# Bind("Qty") %>' Width="59px"
                                                onkeypress="return blockInvalidChars(event)" onpaste="return false"></asp:TextBox>
                                        </EditItemTemplate>
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="UOM" HeaderStyle-ForeColor="#B1C095" HeaderStyle-Font-Bold="true">
                                        <ItemTemplate>
                                            <asp:Label ID="lblUom" runat="server" Text='<%# Eval("UOM") %>'></asp:Label>
                                        </ItemTemplate>
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Packing Info" HeaderStyle-ForeColor="#B1C095" HeaderStyle-Font-Bold="true">
                                        <ItemTemplate>
                                            <asp:Label ID="lblPack" runat="server" Text='<%# Eval("PackingInfo") %>'></asp:Label>
                                        </ItemTemplate>
                                    </asp:TemplateField>

                                    <asp:TemplateField HeaderText="Note" HeaderStyle-ForeColor="#B1C095" HeaderStyle-Font-Bold="true" >
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
                                            <asp:HiddenField ID="hfCompleted" runat="server" Value='<%# Eval("completedDate") %>' />
                                        </ItemTemplate>
                                        <EditItemTemplate>
                                            <asp:HiddenField ID="hfBarcodeNo" runat="server" Value='<%# Bind("BarcodeNo") %>' />
                                            <asp:HiddenField ID="hfCompleted" runat="server" Value='<%# Bind("completedDate") %>' />
                                        </EditItemTemplate>
                                    </asp:TemplateField>

                                    <asp:CommandField ShowEditButton="True" ShowDeleteButton="True"
                                        CausesValidation="false" EditText="<i class='fa-solid fa-pen-to-square'></i>"
                                        DeleteText="<i class='fa-solid fa-trash'></i>"
                                        UpdateText="<i class='fa-solid fa-file-arrow-up'></i>"
                                        CancelText="<i class='fa-solid fa-xmark'></i>"
                                        ControlStyle-CssClass="btn m-1 text-white" ControlStyle-BackColor="#477023" />
                                </Columns>

                                  <SelectedRowStyle BackColor="#E2DED6" Font-Bold="True" ForeColor="#333333" />
                                  <SortedAscendingCellStyle BackColor="#E9E7E2" />
                                  <SortedAscendingHeaderStyle BackColor="#506C8C" />
                                  <SortedDescendingCellStyle BackColor="#FFFDF8" />
                                  <SortedDescendingHeaderStyle BackColor="#6F8DAE" />
                            </asp:GridView>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

</asp:Content>