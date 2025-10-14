<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="dailyRegistration.aspx.cs" Inherits="Expiry_list.StoreDailyStatement.dailyRegistration" EnableEventValidation="false" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <style>
        .card-padding {
            padding: 0 15%;
        }

        .info-card {
            background-color: white;
            border-radius: 10px;
        }

        .dotted-bg {
            background-color: white; /* Base color */
            background-image: radial-gradient(#4682B4 1px, transparent 1px);
            background-size: 20px 20px; /* spacing between dots */
        }

        .hidden {
            display: none;
        }
        .show {
            display: block;
        }
    </style>

    <script type="text/javascript">

        document.addEventListener('DOMContentLoaded', function () {
            document.getElementById("link_home").href = "../AdminDashboard.aspx";
        });

        function allowOnlyNumbers(evt) {
            const allowedKeys = [
                    "Backspace", "Delete", "ArrowLeft", "ArrowRight", "Tab"
                    ];

                    if (allowedKeys.includes(evt.key)) {
                return; // allow control keys
            }

            // Allow digits, minus, and decimal
            if (!/[0-9.\-]/.test(evt.key)) {
                evt.preventDefault();
            }
        }

        function sanitizeInput(input) {
            let value = input.value;

            // Only one minus at the start
            value = value.replace(/(?!^)-/g, "");

            // Only one decimal point
            value = value.replace(/(\..*)\./g, "$1");

            // Trim leading zeros (except before decimal)
            value = value.replace(/^(-?)0+(?=\d)/, "$1");

            input.value = value;
        }

        document.addEventListener("DOMContentLoaded", () => {
            document.querySelectorAll(".amount").forEach(input => {
                input.addEventListener("keydown", allowOnlyNumbers);
                input.addEventListener("input", () => {
                    sanitizeInput(input);
                    calculateTotal();
                });
                    
            });
        });

        function calculateTotal() {
            let total = 0;
            const submitElem = document.getElementById('<%= submitAmt.ClientID %>');
            const netElem = document.getElementById('<%= netAmt.ClientID %>');
            document.querySelectorAll(".amount").forEach(input => {
                const cleaned = input.value.replace(/,/g, "");  // strip commas
                const val = parseFloat(cleaned);
                if (!isNaN(val) && input !== submitElem && input !== netElem) {
                    total += val;
                }
            });
            //document.getElementById('<%= netAmt.ClientID %>').value = total.toFixed(2);
            document.getElementById('<%= netAmt.ClientID %>').value = total.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        }

        document.addEventListener("input", function (e) {
            if (e.target.classList.contains("amount")) {
                let value = e.target.value.replace(/,/g, ""); // remove old commas
                if (!isNaN(value) && value.length > 0) {
                    e.target.value = Number(value).toLocaleString();
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
        <div class="offset-lg-2 col-lg-8 col-md-8 Justify-content-center mb-3">
            <div class="card shadow-sm h-100">
                <!-- Card Header -->
                <div class="card-header text-white text-center"
                    style="background-color: #4682B4; border-top-left-radius: 10px; border-top-right-radius: 10px;">
                    <h2 class="mb-0 fw-bolder">Store Daily Statement</h2>
                    <asp:Label ID="tdyDate" runat="server" CssClass="ms-2" />
                    <asp:Literal ID="sessionDataLiteral" runat="server" />
                </div>

                <!-- Card Body -->
                <div class="card-body">
                    <div class="row justify-content-center">
                        <asp:ScriptManager ID="ScriptManager2" runat="server" EnablePartialRendering="true">
                        <Scripts>
                            <asp:ScriptReference Name="MicrosoftAjax.js" />
                            <asp:ScriptReference Name="MicrosoftAjaxWebForms.js" />
                        </Scripts>
                        </asp:ScriptManager>
                        <asp:UpdatePanel class="col-12 d-block" runat="server" ID="UpdatePanel2" UpdateMode="Conditional">
                            <ContentTemplate>
                                <!-- No Field -->
                                <div class="row mb-3">
                                    <div class="row col-6 align-items-center">
                                        <label for="<%= no.ClientID %>"class="col-3 col-form-label ps-0 px-0 text-end fw-bold">Store No.</label>
                                        <div class="col-sm-9">
                                            <asp:TextBox runat="server" CssClass="form-control" ReadOnly="true" BorderStyle="None" ID="no"/>
                                        </div>
                                    </div>
                                    <div class="row col-6 align-items-center">
                                        <label for="<%= name.ClientID %>" class="col-3 col-form-label ps-0 px-0 text-end fw-bold">Store Name</label>
                                        <div class="col-sm-9">
                                            <asp:TextBox runat="server" CssClass="form-control" ID="name" ReadOnly="true" BorderStyle="None"/>
                                        </div>
                                    </div>
                                </div>
                                <div id="div_invAmt" runat="server" class="row align-items-center">
                                    <div class="row col-6 align-items-center">
                                        <label for="<%= totalSalesAmt.ClientID %>"class="col-3 col-form-label ps-0 px-0 text-end fw-bold">Total Sales</label>
                                        <div class="col-sm-9">
                                            <asp:TextBox runat="server" CssClass="form-control amount" ID="totalSalesAmt" />
                                        </div>
                                    </div>
                                    <div class="row col-6 align-items-center">
                                        <label for="<%= submitAmt.ClientID %>" class="col-3 col-form-label ps-0 px-0 text-end fw-bold">အပ်ငွေ</label>
                                        <div class="col-sm-9"> 
                                            <asp:TextBox runat="server" CssClass="form-control amount" ID="submitAmt" />
                                        </div>
                                    </div>
                                </div>
                            </ContentTemplate>
                         </asp:UpdatePanel>
                    </div>
                </div>
            </div>
        </div>

        <!-- Store Daily Details -->
        <div class="offset-lg-2 col-lg-8 col-md-8 px-10">
            <div class="card shadow-sm" style="border-radius: 10px;">

                <div class="card-body dotted-bg rounded-5">
                    <div class="row">
                        <div class="col-3 dotted-bg" style=""></div>
                        <div class="col-6 pt-3 info-card">
                            <asp:UpdatePanel runat="server" ID="UpdatePanel1" UpdateMode="Conditional">
                                <ContentTemplate>
                                    <div id="div_advpay1" class="row g-2 mb-3 hidden" runat="server">
                                        <label for="<%= advPayShweAmt.ClientID %>" class="col-sm-6 col-form-label">ယခင်နေ့လက်ကျန်ငွေ - ဦးရွှေမြင့်</label>
                                        <div class="col-sm-6">
                                            <asp:TextBox runat="server" CssClass="form-control amount" ID="advPayShweAmt" />
                                        </div>
                                    </div>
                                    <div id="div_advpay2" class="row g-2 mb-3 hidden" runat="server">
                                        <label for="<%= advPayABankAmt.ClientID %>" class="col-sm-6 col-form-label">ယခင်နေ့လက်ကျန်ငွေ - A Bank</label>
                                        <div class="col-sm-6">
                                            <asp:TextBox runat="server" CssClass="form-control amount" ID="advPayABankAmt" />
                                        </div>
                                    </div>
                                    <div id="div_advpay3" class="row g-2 mb-3 hidden" runat="server">
                                        <label for="<%= advPayKbzAmt.ClientID %>" class="col-sm-6 col-form-label">ယခင်နေ့လက်ကျန်ငွေ - KBZ</label>
                                        <div class="col-sm-6">
                                            <asp:TextBox runat="server" CssClass="form-control amount" ID="advPayKbzAmt" />
                                        </div>
                                    </div>
                                    <div id="div_advpay4" class="row g-2 mb-3 hidden" runat="server">
                                        <label for="<%= advPayUabAmt.ClientID %>" class="col-sm-6 col-form-label">ယခင်နေ့လက်ကျန်ငွေ - UAB</label>
                                        <div class="col-sm-6">
                                            <asp:TextBox runat="server" CssClass="form-control amount" ID="advPayUabAmt" />
                                        </div>
                                    </div>
                                
                                    <div id="div_dailysales1" class="row g-2 mb-3 hidden" runat="server">
                                        <label for="<%= dailySalesShweAmt.ClientID %>" class="col-sm-6 col-form-label">Daily Sales အပ်ငွေ - ဦးရွှေမြင့်</label>
                                        <div class="col-sm-6">
                                            <asp:TextBox runat="server" CssClass="form-control amount" ID="dailySalesShweAmt"/>
                                        </div>
                                    </div>
                                    <div id="div_dailysales2" class="row g-2 mb-3 hidden" runat="server">
                                        <label for="<%= dailySalesABankAmt.ClientID %>" class="col-sm-6 col-form-label">Daily Sales အပ်ငွေ - A Bank</label>
                                        <div class="col-sm-6">
                                            <asp:TextBox runat="server" CssClass="form-control amount" ID="dailySalesABankAmt"/>
                                        </div>
                                    </div>
                                    <div id="div_dailysales3" class="row g-2 mb-3 hidden" runat="server">
                                        <label for="<%= dailySalesKbzAmt.ClientID %>" class="col-sm-6 col-form-label">Daily Sales အပ်ငွေ - KBZ</label>
                                        <div class="col-sm-6">
                                            <asp:TextBox runat="server" CssClass="form-control amount" ID="dailySalesKbzAmt"/>
                                        </div>
                                    </div>
                                    <div id="div_dailysales4" class="row g-2 mb-3 hidden" runat="server">
                                        <label for="<%= dailySalesUabAmt.ClientID %>" class="col-sm-6 col-form-label">Daily Sales အပ်ငွေ - UAB</label>
                                        <div class="col-sm-6" runat="server">
                                            <asp:TextBox runat="server" CssClass="form-control amount" ID="dailySalesUabAmt"/>
                                        </div>
                                    </div>

                                    <div class="row g-2 mb-3">
                                        <label for="<%= pettyCash.ClientID %>" class="col-sm-6 col-form-label">Petty Cash</label>
                                        <div class="col-sm-6">
                                            <asp:TextBox runat="server" CssClass="form-control amount" ID="pettyCash"/>
                                        </div>
                                    </div>

                                    <div class="row g-2 mb-3">
                                        <label for="<%= extraAmt.ClientID %>" class="col-sm-6 col-form-label">ပိုငွေ/လိုငွေ</label>
                                        <div class="col-sm-6">
                                            <asp:TextBox runat="server" CssClass="form-control amount" ID="extraAmt"/>
                                        </div>
                                    </div>

                                    <div id="div_mmqr1" class="row g-2 mb-3 hidden" runat="server">
                                        <label for="<%= mmqr1Amt.ClientID %>" class="col-sm-6 col-form-label">Pay Total MMQR</label>
                                        <div class="col-sm-6">
                                            <asp:TextBox runat="server" CssClass="form-control amount" ID="mmqr1Amt"/>
                                        </div>
                                    </div>
                                    <div id="div_mmqr2" class="row g-2 mb-3 hidden" runat="server">
                                        <label for="<%= mmqr2Amt.ClientID %>" class="col-sm-6 col-form-label">Pay Total MMQR</label>
                                        <div class="col-sm-6">
                                            <asp:TextBox runat="server" CssClass="form-control amount" ID="mmqr2Amt"/>
                                        </div>
                                    </div>
                                    <div id="div_mmqr3" class="row g-2 mb-3 hidden" runat="server">
                                        <label for="<%= mmqr3Amt.ClientID %>" class="col-sm-6 col-form-label">Pay Total MMQR</label>
                                        <div class="col-sm-6">
                                            <asp:TextBox runat="server" CssClass="form-control amount" ID="mmqr3Amt"/>
                                        </div>
                                    </div>
                                    <div id="div1" class="row g-2 mb-3 hidden" runat="server">
                                        <label for="<%= mmqr4Amt.ClientID %>" class="col-sm-6 col-form-label">Pay Total MMQR4</label>
                                        <div class="col-sm-6">
                                            <asp:TextBox runat="server" CssClass="form-control amount" ID="mmqr4Amt"/>
                                        </div>
                                    </div>
                                   <%-- <div class="row g-2 mb-3">
                                        <label for="<%= mmqr4Amt.ClientID %>" class="col-sm-4 col-form-label">Pay Total MMQR 4</label>
                                        <div class="col-sm-8">
                                            <asp:TextBox runat="server" CssClass="form-control form-control-sm" ID="mmqr4Amt"/>
                                        </div>
                                    </div>--%>
                                    <div class="row g-2 mb-3">
                                        <label for="<%= payTotalAmt.ClientID %>" class="col-sm-6 col-form-label">Pay Total Kpay</label>
                                        <div class="col-sm-6">
                                            <asp:TextBox runat="server" CssClass="form-control amount" ID="payTotalAmt"/>
                                        </div>
                                    </div>
                                    <div id="div_cardpay1" class="row g-2 mb-3 hidden" runat="server">
                                        <label for="<%= cardABankAmt.ClientID %>" class="col-sm-6 col-form-label">Card Payment(A Bank)</label>
                                        <div class="col-sm-6">
                                            <asp:TextBox runat="server" CssClass="form-control amount" ID="cardABankAmt"/>
                                        </div>
                                    </div>
                                    <div id="div_cardpay2" class="row g-2 mb-3 hidden" runat="server">
                                        <label for="<%= cardAyaAmt.ClientID %>" class="col-sm-6 col-form-label">Card Payment(AYA)</label>
                                        <div class="col-sm-6">
                                            <asp:TextBox runat="server" CssClass="form-control amount" ID="cardAyaAmt"/>
                                        </div>
                                    </div>
                                    <div id="div_cardpay3" class="row g-2 mb-3 hidden" runat="server">
                                        <label for="<%= cardUabAmt.ClientID %>" class="col-sm-6 col-form-label">Card Payment(UAB)</label>
                                        <div class="col-sm-6">
                                            <asp:TextBox runat="server" CssClass="form-control amount" ID="cardUabAmt"/>
                                        </div>
                                    </div>
                                    <div class="row g-2 mb-3">
                                        <label for="<%= deliPayAmt.ClientID %>" class="col-sm-6 col-form-label">Flash Deli Pay</label>
                                        <div class="col-sm-6">
                                            <asp:TextBox runat="server" CssClass="form-control amount" ID="deliPayAmt"/>
                                        </div>
                                    </div>
                                    <div class="row g-2 mb-3">
                                        <label for="<%= deliCodAmt.ClientID %>" class="col-sm-6 col-form-label">Flash Deli COD</label>
                                        <div class="col-sm-6">
                                            <asp:TextBox runat="server" CssClass="form-control amount" ID="deliCodAmt"/>
                                        </div>
                                    </div>

                                    <div class="row g-2 mb-3">
                                        <label for="<%= netAmt.ClientID %>" class="col-sm-6 col-form-label">Total</label>
                                        <div class="col-sm-6">
                                            <asp:TextBox runat="server" CssClass="form-control amount" ID="netAmt" ReadOnly="true" />
                                        </div>
                                    </div>  
                                </ContentTemplate>
                            </asp:UpdatePanel>
                            <div class="row g-2 mb-3 justify-content-center">
                                <div class="col-auto">
                                    <asp:Button ID="btnSave" runat="server"
                                        Text="Save"
                                        CssClass="btn text-white me-2"
                                        Style="background:#4682b4;width:100px"
                                        ForeColor="White" Font-Bold="True" Font-Size="Large"
                                        OnClick="btnSave_Click"
                                        OnClientClick="calculateTotal();"/>
                                </div>
                            </div>
                        </div>
                        <div class="col-3 dotted-bg" style=""></div>
                         <asp:HiddenField ID="hfNetAmt" runat="server" />
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>

