<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" EnableEventValidation="false" CodeBehind="dailyStatementList.aspx.cs" Inherits="Expiry_list.dailyStatementList" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        table.dataTable th,
        table.dataTable td {
            white-space: nowrap; /* prevent wrapping */
            border: 1px solid grey;
        }

        table.dataTable thead th {
            min-width: 100px;
        }

        .sticky-grid th {
            z-index: 10;
        }

        .select2-container .select2-selection--multiple {
            min-height: 34px;
            height: auto;
            display: flex !important;
            flex-wrap: wrap !important;
            align-items: center;
            width: 350px;
            column-gap: 3px;
        }

        .location-pill {
            width: 32%;
        }

    </style>
    <script type="text/javascript">

        $(document).ready(function () {
            
            InitializeStoreFilter();  
            if (typeof (Sys) !== 'undefined') {
                Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
                    Object.keys(filterMap).forEach(key => {
                        const checkbox = document.getElementById(filterMap[key].checkboxId);
                        if (checkbox) {
                            checkbox.addEventListener('change', updateFilterVisibility);
                        }
                    });
                });
            }
            initializeComponents();
        });

        let isDataTableInitialized = false;
        var table; 

        function initializeComponents() {
            const grid = $("#<%= GridView2.ClientID %>");

            if (<%= GridView2.EditIndex >= 0 ? "true" : "false" %>) {
                if ($.fn.DataTable.isDataTable(grid)) {
                    grid.DataTable().destroy();
                    grid.removeAttr('style');
                }
                return;
            }
            table = $('#<%= GridView2.ClientID %>').DataTable({
                scrollX: true,
                scrollY: "70vh",
                scrollCollapse: true,
                paging: true,
                ordering: true,
                fixedHeader: true,
                orderCellsTop: true,
                lengthMenu: [[100, 500, 1000], [100, 500, 1000]],
            });            

             // Add a custom month filter
            $.fn.dataTable.ext.search.push(function (settings, data, dataIndex) {
                var selectedMonth = $('#monthFilter').val(); 
                var matchesMonth = true;

                var rowDate = data[0]; 
                if (!rowDate) return false;

                var parts = rowDate.split('/');

                var d = new Date(parts[2], parts[1] - 1, parts[0]);

                // Compare by "YYYY-MM"
                var rowMonth = d.getFullYear() + '-' + ('0' + (d.getMonth() + 1)).slice(-2);
                if (selectedMonth) {
                    matchesMonth = (rowMonth === selectedMonth);
                }                
                var selectedStores = $('#<%= hdnStoreFilter.ClientID %>').val();
                var rowStore = data[1]; 
                var matchesStore = true;
                var storeFilterGroup = document.getElementById('<%= storeFilterGroup.ClientID %>'); 
                if (selectedStores && (storeFilterGroup != null)) {                    
                    var storeList = selectedStores.split(',');
                    matchesStore = (selectedStores === 'all' || storeList.includes(rowStore));
                }
                
                return matchesMonth && matchesStore;
            });

            $('#monthFilter,#<%= lstStoreFilter.ClientID %>').on('change', function () {
                table.draw();
            });

            table.draw();
        }

        document.addEventListener("DOMContentLoaded", function () {

            const monthInput = document.getElementById("monthFilter");
            const now = new Date();
            const year = now.getFullYear();
            const month = String(now.getMonth() + 1).padStart(2, '0');
            monthInput.value = `${year}-${month}`;

            const filterPane = document.getElementById("filterPane");
            if (filterPane) {
                filterPane.style.display = "<%= ViewState["FilterPanelVisible"] != null ? (bool)ViewState["FilterPanelVisible"] ? "block" : "none" : "none" %>";
            }
            document.getElementById("link_home").href = "../AdminDashboard.aspx";

            updateLocationPillsDisplay();

            const listBox = document.getElementById('<%= lstStoreFilter.ClientID %>');
            if (listBox) {
                listBox.addEventListener('change', updateLocationPillsDisplay);
            }          

        });

        function InitializeStoreFilter() {
            var $select = $('#<%= lstStoreFilter.ClientID %>');
            var allOptionId = "all";

             // Remove duplicate "All" option if exists
             $select.find('option').filter(function () {
                 return $(this).val() === allOptionId;
             }).slice(1).remove();

             $select.select2({
                 placeholder: "-- Select stores --",
                 closeOnSelect: false,
                 width: 'auto',
                 allowClear: true,
                 dropdownParent: $select.closest('.filter-group'),
                 minimumResultsForSearch: 1,
                 escapeMarkup: function (m) { return m; },

                 // Add search configuration
                 language: {
                     noResults: function () {
                         return "No stores found";
                     }
                 },
                 matcher: function (params, data) {
                     if ($.trim(params.term) === '') {
                         return data;
                     }

                     // Check search term
                     if (data.text.toLowerCase().indexOf(params.term.toLowerCase()) > -1) {
                         return data;
                     }

                     return null;
                 },

                 templateResult: function (data) {
                     if (!data.id) return data.text;
                     var isAll = data.id === allOptionId;
                     var selectedValues = $select.val() || [];
                     var hasAll = $select.val()?.includes(allOptionId);
                     var isChecked = isAll ? hasAll : selectedValues.includes(data.id);
                     var isDisabled = hasAll && !isAll;

                     return $(
                         '<div class="select2-checkbox-option d-flex align-items-center">' +
                         '  <input type="checkbox" class="select2-checkbox me-2" ' +
                         (isChecked ? 'checked' : '') +
                         (isAll ? ' data-is-all="true"' : '') +
                         (isDisabled ? ' disabled' : '') + '>' +
                         '  <div class="select2-text' + (isAll ? ' fw-bold"' : '"') + '>' + data.text + '</div>' +
                         '</div>'
                     );
                 },

                 templateSelection: function (data, container) {
                     return '';
                 }
            });

            // checkbox
            var storeFilterGroup = document.getElementById('<%= storeFilterGroup.ClientID %>');   
            if (storeFilterGroup != null) {
                $select.data('select2').$dropdown.on('click', '.select2-checkbox', function (e) {
                    var $option = $(this).closest('.select2-results__option');
                    var data = $option.data('data');
                    if (data) {
                        var isAll = data.id === allOptionId;
                        var selected = $select.val() || [];

                        if (isAll) {
                            if (this.checked) {
                                $select.val([allOptionId]).trigger('change');
                            } else {
                                $select.val([]).trigger('change');
                            }
                        } else {
                            var index = selected.indexOf(data.id);
                            if (this.checked && index === -1) {
                                selected.push(data.id);
                            } else if (!this.checked && index !== -1) {
                                selected.splice(index, 1);
                            }
                            $select.val(selected).trigger('change');
                        }
                    }
                });
            }             

            $('#<%= lstStoreFilter.ClientID %>').on('change', function () {
                const $select = $(this);
                const $container = $select.next('.select2-container').find('.select2-selection');

                // Remove old pills
                $container.find('.location-pill').remove();

                // Add new pills
                $select.find('option:selected').each(function () {
                    const pill = $('<span class="location-pill my-2">' +
                        '<span class="pill-text">' + $(this).text() + '</span>' +
                        '<span class="pill-remove" data-value="' + $(this).val() + '">×</span>' +
                        '</span>');
                    pill.find('.pill-remove').on('click', function (e) {
                        e.stopPropagation();
                        const value = $(this).data('value');
                        $select.find('option[value="' + value + '"]').prop('selected', false);
                        $select.trigger('change');
                    });
                    $container.append(pill);
                });
                const selectedValues = $select.val() || [];

                $("#<%= hdnStoreFilter.ClientID %>").val(selectedValues.join(','));
            });

            $select.on('change', function () {
                var values = $select.val() || [];
                var hasAll = values.includes(allOptionId);

                if (hasAll) {
                    if (values.length > 1) {
                        $select.val([allOptionId]).trigger('change');
                        return;
                    }
                } else {
                    values = values.filter(v => v !== allOptionId);
                    if (values.length !== $select.val().length) {
                        $select.val(values).trigger('change');
                    }
                }

                setTimeout(() => {
                    $select.select2('close');
                    $select.select2('open');
                }, 50);

            });
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
          <div class="card-header" style="background-color:#4682B4;">
              <h4 class="text-center text-white">Daily Statement List</h4>
          </div>
            <div class="card-body">

                <div class="d-flex align-items-center flex-wrap mb-2">
                    
                   <div class="me-2 shadow-md">
                        <input type="month" id="monthFilter" class="form-control me-2" />
                    </div>  
                    <div class="d-flex filter-group align-items-center col-2" id="storeFilterGroup" runat="server">
                        <label class="me-2 fw-bold" for="<%=lstStoreFilter.ClientID %>">Location</label>
            
                        <asp:ListBox ID="lstStoreFilter" runat="server" CssClass="form-control select2-multi-check" SelectionMode="Multiple" style="display:block"></asp:ListBox>
                        <asp:HiddenField ID="hdnStoreFilter" runat="server" />
                    </div>
                </div>                

                <div class="d-flex py-2 col-lg-12 col-md-12 overflow-x-auto overflow-y-auto">                    
                    <!-- Table -->
                    <div class="col-md-12" id="gridCol">
                        <ContentTemplate>

                            <asp:Panel ID="pnlNoData" runat="server" Visible="false">
                                  <div class="alert alert-info">No items to Filter</div>
                            </asp:Panel>

                             <div class="table-responsive gridview-container">
                                 <asp:GridView ID="GridView2" runat="server"
                                    AutoGenerateColumns="False"
                                    CssClass="table table-striped table-hover shadow-lg sticky-grid overflow-x-auto overflow-y-auto"
                                    ShowHeader="False"
                                    DataKeyNames="sdsId"
                                    OnPreRender="GridView2_PreRender"
                                    OnRowCreated="GridView2_RowCreated"
                                    OnSorting="GridView2_Sorting">
                                    <PagerStyle CssClass="pagination-wrapper" HorizontalAlign="Center" VerticalAlign="Middle" />
                                    <Columns>
                                        <asp:BoundField DataField="createdDate" HeaderText="Date"  DataFormatString="{0:dd'/'MM'/'yyyy}" HtmlEncode="false"/>
                                        <asp:BoundField DataField="storeNo" HeaderText="Store No" />
                                        <asp:BoundField DataField="storeName" HeaderText="Store Name" />
                                        <asp:BoundField DataField="totalSalesAmt" HeaderText="Total Sales" />
                                        <asp:BoundField DataField="pettyCash" HeaderText="Petty Cash" />

                                        <asp:BoundField DataField="advPayShweAmt" HeaderText="Shwe" />
                                        <asp:BoundField DataField="advPayABankAmt" HeaderText="ABank" />
                                        <asp:BoundField DataField="advPayKbzAmt" HeaderText="KBZ" />
                                        <asp:BoundField DataField="advPayUabAmt" HeaderText="UAB" />

                                        <asp:BoundField DataField="dailySalesShweAmt" HeaderText="Shwe" />
                                        <asp:BoundField DataField="dailySalesABankAmt" HeaderText="ABank" />
                                        <asp:BoundField DataField="dailySalesKbzAmt" HeaderText="KBZ" />
                                        <asp:BoundField DataField="dailySalesUabAmt" HeaderText="UAB" />

                                        <asp:BoundField DataField="mmqr1Amt" HeaderText="A+" />
                                        <asp:BoundField DataField="mmqr2Amt" HeaderText="MMQR-86" />
                                        <asp:BoundField DataField="mmqr3Amt" HeaderText="MMQR-62" />

                                        <asp:BoundField DataField="payTotalAmt" HeaderText="Pay Total Kpay" />

                                        <asp:BoundField DataField="cardABankAmt" HeaderText="ABank" />
                                        <asp:BoundField DataField="cardAyaAmt" HeaderText="AYA" />
                                        <asp:BoundField DataField="cardUabAmt" HeaderText="UAB" />

                                        <asp:BoundField DataField="extraAmt" HeaderText="ပိုငွေ/လိုငွေ" />
                                        <asp:BoundField DataField="deliPayAmt" HeaderText="Delivery Pay" />
                                        <asp:BoundField DataField="deliCodAmt" HeaderText="Delivery COD" />
                                        <asp:BoundField DataField="NetAmt" HeaderText="Net Amount" />
                                    </Columns>
                                </asp:GridView>
                            </div>
                        </ContentTemplate>                            
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
