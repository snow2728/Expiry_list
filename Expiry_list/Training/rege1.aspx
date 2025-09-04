<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="rege1.aspx.cs" Inherits="Expiry_list.Training.rege1" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    
       <script type="text/javascript">

           function updateTrainerAndLevel() {
               var ddl = document.getElementById('<%= topicDP.ClientID %>');
               var selectedOption = ddl.options[ddl.selectedIndex];

               if (selectedOption.value === "") {
                   document.getElementById('<%= trainerDp.ClientID %>').value = "";
                    document.getElementById('<%= position.ClientID %>').value = "";
                    return;
               }
      
                var trainer = selectedOption.getAttribute("data-trainer") || '';
                var level = selectedOption.getAttribute("data-level") || '';
      
                document.getElementById('<%= trainerDp.ClientID %>').value = trainer;
                document.getElementById('<%= position.ClientID %>').value = level;
           }

            window.onload = function () {
               document.getElementById('<%= topicDP.ClientID %>').addEventListener("change", updateTrainerAndLevel);
           };

       </script>

</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

<div class="d-flex justify-content-between align-items-center mb-3 flex-wrap">
    <!-- Home Button -->
    <div class="mb-2">
        <a href="../AdminDashboard.aspx" class="btn text-white ms-2" style="background-color:#488DB4;">
            <i class="fa-solid fa-left-long"></i> Home
        </a>
    </div>

    <!-- Excel Import Section -->
    <div class="me-3" style="max-width: 400px;">
        <!-- File Upload + Button -->
        <div class="input-group input-group-sm">
            <asp:FileUpload ID="fuExcel" runat="server" CssClass="form-control me-2 rounded-3 border shadow-md" BorderColor="Gray" />
            <asp:Button ID="btnUploadExcel" runat="server" Text="Upload File"
                CssClass="btn fw-semibold rounded-3 text-white" BackColor="#488DB4"
                OnClick="btnUploadExcel_Click" CausesValidation="false" />
        </div>
    </div>

</div>

    <div class="container-fluid" style="background-color: #e6efff;">
        <div class="row justify-content-center">
            <div class="col-lg-8">
               <%-- <div class=" text-end mb-4">
                   <a href="addTrainer.aspx" class="btn text-white" style="background-color:#022F56;"><i class="fa-solid fa-user-plus"></i> New Trainer</a>
                   <a href="addTopic.aspx" class="btn text-white" style="background-color:#022F56;"><i class="fa-solid fa-folder-plus"></i> New Topic</a>
               </div>--%>

                <div class="card shadow rounded-4 p-4" style="background-color: #CCDEE4;">
                    <!-- Card Header -->
                    <div class="card-header text-center align-item-center"
                        style="background-color: #022F56; color: #c9b99f; border-top-left-radius: 10px; border-top-right-radius: 10px;">
                        <h2 class="">Schedule Registration Form</h2>
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
                                    <label for="<%= no.ClientID %>" class="col-sm-4 col-form-label fw-bolder fa-1x" style="color: #076585;">No</label>
                                    <div class="col-sm-8">
                                        <asp:TextBox runat="server" CssClass="form-control form-control-sm" ID="no" ReadOnly="true" />
                                    </div>
                                </div>

                                <!-- Topic Field -->
                                <div class="row g-2 mb-3">
                                    <label for="<%= topicDP.ClientID %>" class="col-sm-4 col-form-label fw-bolder fa-1x" style="color: #076585;">Topic</label>
                                    <div class="col-sm-8">
                                        <asp:DropDownList ID="topicDP" runat="server" CssClass="form-control form-control-sm dropdown-icon" AppendDataBoundItems="true" >
                                        </asp:DropDownList>
                                        <asp:RequiredFieldValidator ID="RequiredFieldValidator3" runat="server"
                                            ErrorMessage="Topic must be selected!"
                                            ControlToValidate="topicDP" Display="Dynamic"
                                            CssClass="text-danger d-block mt-1" SetFocusOnError="True" />
                                    </div>
                                </div>

                                <!-- Description Field -->
                                <div class="row g-2 mb-3">
                                    <label for="<%= desc.ClientID %>" class="col-sm-4 col-form-label fw-bolder fa-1x" style="color: #076585;">Description</label>
                                    <div class="col-sm-8">
                                        <asp:TextBox runat="server" CssClass="form-control form-control-sm"
                                            TextMode="MultiLine" ID="desc" />
                                    </div>
                                </div>

                                <!-- Location Field -->
                                <div class="row g-2 mb-3">
                                    <label for="<%= locationDp.ClientID %>" class="col-sm-4 col-form-label fw-bolder fa-1x" style="color: #076585;">Training Room</label>
                                    <div class="col-sm-8">
                                        <asp:DropDownList ID="locationDp" runat="server" CssClass="form-control form-control-sm dropdown-icon">
                                        </asp:DropDownList>
                                        <asp:RequiredFieldValidator ID="storeNoRequired" runat="server"
                                            ControlToValidate="locationDp"
                                            ErrorMessage="Location is required!"
                                            CssClass="text-danger small d-block mt-1"
                                            Display="Dynamic" />
                                    </div>
                                </div>

                                <!-- Trainer Field -->
                                <div class="row g-2 mb-3">
                                    <label for="<%= trainerDp.ClientID %>" class="col-sm-4 col-form-label fw-bolder fa-1x" style="color: #076585;">Trainer</label>
                                    <div class="col-sm-8">
                                        <asp:TextBox runat="server" CssClass="form-control form-control-sm" ID="trainerDp" ReadOnly="true" />
                                    </div>
                                </div>

                                <!-- Position Field -->
                                <div class="row g-2 mb-3">
                                    <label for="<%= position.ClientID %>" class="col-sm-4 col-form-label fw-bolder fa-1x" style="color: #076585;">Position</label>
                                    <div class="col-sm-8">
                                        <asp:TextBox ID="position" runat="server" CssClass="form-control form-control-sm" ReadOnly="true" />
                                    </div>
                                </div>

                                <!-- Date Field -->
                                <div class="row g-2 mb-3">
                                    <label for="<%= date.ClientID %>" class="col-sm-4 col-form-label fw-bolder fa-1x" style="color: #076585;">Date</label>
                                    <div class="col-sm-8">
                                        <asp:TextBox runat="server" CssClass="form-control form-control-sm" TextMode="Date"
                                            ID="date" name="date" />
                                        <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server"
                                            ErrorMessage="Date must be selected!"
                                            ControlToValidate="date" Display="Dynamic"
                                            CssClass="text-danger" SetFocusOnError="True" />
                                    </div>
                                </div>

                               <!-- Time Field -->
                                <div class="row g-2 mb-3">
                                    <label for="<%= timeDp.ClientID %>" class="col-sm-4 col-form-label fw-bolder fa-1x" style="color: #076585;">Time</label>
                                    <div class="col-sm-8">
                                        <asp:DropDownList ID="timeDp" runat="server" CssClass="form-control form-control-sm dropdown-icon">
                                            <asp:ListItem Text="Select Time Range" Value="" />
                                            <asp:ListItem Text="08:30 AM - 11:30 AM" Value="08:30 AM - 11:30 AM" />
                                            <asp:ListItem Text="12:30 PM - 03:30 PM" Value="12:30 PM - 03:30 PM" />
                                            <asp:ListItem Text="09:00 AM - 12:00 PM" Value="09:00 AM - 12:00 PM" />
                                            <asp:ListItem Text="09:00 AM - 03:00 PM" Value="09:00 AM - 03:00 PM" />
                                        </asp:DropDownList>
                                        <asp:RequiredFieldValidator ID="RequiredFieldValidator2" runat="server"
                                            ErrorMessage="Time must be selected!"
                                            ControlToValidate="timeDp" Display="Dynamic"
                                            CssClass="text-danger" SetFocusOnError="True" />
                                    </div>
                                </div>

                                <!-- Create Button -->
                                <div class="text-center ">
                                    <asp:Button Text="Create" runat="server" CssClass="btn px-4 me-2 fa-1x fw-bolder"
                                        Style="background-color: #022F56; color: #c9b99f; border-radius: 20px;"
                                        ID="createBtn" OnClick="createBtn_Click" />
                                </div>
                            </div>
                        </div>
                    </div>
                            </ContentTemplate>
                        </asp:UpdatePanel>
                    </div>
                </div>
            </div>
        </div>
    </div>

  <!-- Excel Preview Modal -->
<%--<div class="modal fade" id="excelPreviewModal" tabindex="-1" aria-labelledby="excelPreviewLabel" aria-hidden="true">
    <div class="modal-dialog modal-xl modal-dialog-scrollable">
        <div class="modal-content">
            
            <!-- Header -->
            <div class="modal-header bg-primary text-white">
                <h5 class="modal-title" id="excelPreviewLabel">Excel Preview</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>

            <!-- Body -->
            <div class="modal-body">
                <asp:GridView ID="gvExcelPreview" runat="server" AutoGenerateColumns="true" 
                    CssClass="table table-bordered table-sm nowrap"
                    OnRowDataBound="gvExcelPreview_RowDataBound" />
            </div>

            <!-- Footer -->
            <div class="modal-footer">
                <asp:UpdatePanel runat="server" ID="UpdatePanelExcelPreview" UpdateMode="Conditional">
                    <ContentTemplate>
                        <asp:Button ID="btnConfirmInsert" runat="server" Text="Confirm & Insert" 
                            CssClass="btn btn-primary px-4" 
                            OnClick="btnConfirmInsert_Click" CausesValidation="false" />
                    </ContentTemplate>
                </asp:UpdatePanel>
            </div>
            
        </div>
    </div>
</div>--%>

</asp:Content>
