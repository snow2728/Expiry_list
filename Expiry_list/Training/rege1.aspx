<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="rege1.aspx.cs" Inherits="Expiry_list.Training.rege1" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">

 <script type="text/javascript">
    function updateTrainerAndLevel() {
         var ddl = document.getElementById('<%= topicDP.ClientID %>');
        if (!ddl) return;

        var selectedOption = ddl.options[ddl.selectedIndex];
        if (!selectedOption || selectedOption.value === "") {
            document.getElementById('<%= trainerDp.ClientID %>').value = "";
            document.getElementById('<%= position.ClientID %>').value = "";
            return;
        }

        document.getElementById('<%= trainerDp.ClientID %>').value = selectedOption.getAttribute("data-trainer-name") || '';
        document.getElementById('<%= position.ClientID %>').value = selectedOption.getAttribute("data-level-name") || '';
    }

     document.addEventListener("DOMContentLoaded", function () {
         updateTrainerAndLevel();
         document.getElementById("link_home").href = "../AdminDashboard.aspx";
     });

     function attachTopicHandler() {
        var ddl = document.getElementById('<%= topicDP.ClientID %>');
         if (ddl) {
             ddl.onchange = updateTrainerAndLevel;
         }
     }

     if (typeof (Sys) !== "undefined" && Sys.WebForms && Sys.WebForms.PageRequestManager) {
         Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
             attachTopicHandler();
             updateTrainerAndLevel();
         });
    }

 </script>

</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

<div class="d-flex justify-content-end align-items-center m-2 flex-wrap">

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

                        <asp:ScriptManager ID="ScriptManager1" runat="server" EnablePartialRendering="true" ></asp:ScriptManager>
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

                                        <script type="text/javascript">
                                            var topicMap = {};
                                            <% 
                                               var dt = ViewState["TopicMapping"] as System.Data.DataTable;
                                               if (dt != null) {
                                                   foreach(System.Data.DataRow row in dt.Rows) {
                                            %>
                                                                                    topicMap["<%= row["id"] %>"] = { trainer: "<%= row["trainerName"] %>", level: "<%= row["levelName"] %>" };
                                            <% 
                                                   } 
                                               } 
                                            %>

                                            function updateTrainerAndLevel() {
                                                var ddl = document.getElementById('<%= topicDP.ClientID %>');
                                                if (!ddl) return;

                                                var selectedValue = ddl.value;
                                                var trainerInput = document.getElementById('<%= trainerDp.ClientID %>');
                                                var levelInput = document.getElementById('<%= position.ClientID %>');

                                                if (!selectedValue || !topicMap[selectedValue]) {
                                                    trainerInput.value = "";
                                                    levelInput.value = "";
                                                    return;
                                                }

                                                trainerInput.value = topicMap[selectedValue].trainer;
                                                levelInput.value = topicMap[selectedValue].level;
                                            }

                                            document.addEventListener("DOMContentLoaded", function () {
                                                var ddl = document.getElementById('<%= topicDP.ClientID %>');
                                                if (ddl) ddl.onchange = updateTrainerAndLevel;
                                                updateTrainerAndLevel(); // update on first load
                                            });

                                            if (typeof (Sys) !== "undefined" && Sys.WebForms && Sys.WebForms.PageRequestManager) {
                                                Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
                                                    var ddl = document.getElementById('<%= topicDP.ClientID %>');
                                                    if (ddl) ddl.onchange = updateTrainerAndLevel;
                                                    updateTrainerAndLevel(); // update after partial postback
                                                });
                                                                                    }
                                                                                </script>


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

</asp:Content>
