<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="addTopicWL.aspx.cs" Inherits="Expiry_list.Training.addTopicWL" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">

    <script type="text/javascript">
        function updateTrainer() {
            var ddl = document.getElementById('<%= topicName.ClientID %>');
            var selectedOption = ddl.options[ddl.selectedIndex];
            var trainerName = selectedOption.getAttribute("data-trainer");
            document.getElementById('<%= trainerDp.ClientID %>').value = trainerName || '';
        }

        window.onload = function () {
            document.getElementById('<%= topicName.ClientID %>').addEventListener("change", updateTrainer);
        };
    </script>

</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

 <a href="../AdminDashboard.aspx" class="btn text-white ms-2" style="background-color: #022F56;"><i class="fa-solid fa-left-long"></i>Home</a>

  <div class="container-fluid" >
      <div class="row justify-content-center g-2">
          <div class="card shadow rounded-4 p-2 col-12 col-lg-5 col-md-4">
              <!-- New Topic Card -->
                <asp:Panel ID="pnlAddTopicWL" runat="server" style="background-color: #CCDEE4;">
                   <!-- Card Header -->
                     <div class="card-header fw-bolder text-center d-flex justify-content-between align-items-center p-3"
                         style="background-color: #022F56; color:#c9b99f; border-top-left-radius: 10px; border-top-right-radius: 10px;">
                         <h2 class="mb-0">New Topic With Level</h2>
                         <a href="viewTopicWL.aspx" class="btn text-white" style="background-color:#488db4;"><i class="fa-solid fa-user-plus"></i>View Topic With Level List</a>
                     </div>
                    <div class="card-body text-white text-center">
                        <!-- Topic Field -->
                        <div class="row g-2 mb-3">
                            <label for="<%= topicName.ClientID %>" class="col-sm-4 col-form-label fa-1x fw-bolder" style="color:#076585;">Topic</label>
                            <div class="col-sm-8">
                                <asp:DropDownList ID="topicName" runat="server" CssClass="form-control form-control-sm dropdown-icon" AppendDataBoundItems="True"> 
                                </asp:DropDownList>
                                <asp:RequiredFieldValidator ID="RequiredFieldValidator7" runat="server"
                                   ErrorMessage="Topic is required!"
                                   ControlToValidate="topicName" Display="Dynamic"
                                   CssClass="text-danger d-block" SetFocusOnError="True" />
                            </div>
                        </div>

                       <!-- Level Field -->
                          <div class="row g-2 mb-3">
                            <label for="<%= levelDb.ClientID %>" class="col-sm-4 col-form-label fa-1x fw-bolder" style="color:#076585;">Level</label>
                            <div class="col-sm-8">
                               <asp:DropDownList ID="levelDb" runat="server" CssClass="form-control form-control-sm dropdown-icon">
                               </asp:DropDownList>
                               <asp:RequiredFieldValidator ID="RequiredFieldValidator8" runat="server"
                                   ErrorMessage="Level is required!"
                                   ControlToValidate="levelDb" Display="Dynamic"
                                   CssClass="text-danger d-block" SetFocusOnError="True" />
                            </div>
                        </div>

                         <!-- Trainer Field -->
                        <div class="row g-2 mb-3">
                          <label for="<%= trainerDp.ClientID %>" class="col-sm-4 col-form-label fa-1x fw-bolder" style="color:#076585;">Trainer</label>
                          <div class="col-sm-8">
                            <asp:TextBox runat="server" CssClass="form-control form-control-sm" ID="trainerDp" ReadOnly="true" />
                            <%--<asp:DropDownList ID="trainerDp" runat="server" CssClass="form-control form-control-sm"></asp:DropDownList>--%>
                          </div>
                      </div>

                        <!-- Add Btn - Topic -->
                        <div class="row g-2 mb-3">
                            <div class="col-sm-8 text-center">
                                <asp:Button Text="Add Topic" runat="server" CssClass="btn fw-bolder px-4 me-2"
                                    Style="background-color: #022F56; color:#c9b99f; border-radius: 20px;"
                                    ID="addTopicBtn1" OnClick="btnaddTopic_Click" />
                            </div>
                      </div>

                    </div>
                </asp:Panel>
          </div>
      </div>
  </div>

</asp:Content>
