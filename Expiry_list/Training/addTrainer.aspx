<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="addTrainer.aspx.cs" Inherits="Expiry_list.Training.addTrainer" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">



</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <a href="../AdminDashboard.aspx" class="btn text-white ms-2" style="background-color: #022F56;"><i class="fa-solid fa-left-long"></i>Home</a>

   <div class="container-fluid" >
       <div class="row justify-content-center g-2">
           <div class="card shadow rounded-4 p-2 col-12 col-lg-5 col-md-4">
                  <!-- New Trainer Card -->
                   <asp:Panel ID="pnlAddTrainer" runat="server" style="background-color:#CCDEE4;">
                      <!-- Card Header -->
                      <div class="card-header fw-bolder text-center d-flex justify-content-between align-items-center p-3"
                          style="background-color: #022F56; color:#c9b99f; border-top-left-radius: 10px; border-top-right-radius: 10px;">
                          <h2 class="mb-0">New Trainer</h2>
                          <a href="viewTrainer.aspx" class="btn text-white" style="background-color:#488db4;"><i class="fa-solid fa-user-plus"></i>View Trainer List</a>
                      </div>
                       <div class="card-body text-white text-center">
                           <!-- Name Field -->
                           <div class="row g-2 mb-3">
                               <label for="<%= trainerName.ClientID %>" class="col-sm-4 col-form-label fa-1x fw-bolder" style="color:#076585;">Trainer Name</label>
                               <div class="col-sm-8">
                                   <asp:TextBox runat="server" CssClass="form-control form-control-sm" ID="trainerName" />
                                    <asp:RequiredFieldValidator ID="RequiredFieldValidator5" runat="server"
                                      ErrorMessage="Name is required!"
                                      ControlToValidate="trainerName" Display="Dynamic"
                                      CssClass="text-danger d-block" SetFocusOnError="True" />
                               </div>
                           </div>

                          <!-- Position Field -->
                             <div class="row g-2 mb-3">
                               <label for="<%= trainerPosition.ClientID %>" class="col-sm-4 col-form-label fa-1x fw-bolder" style="color:#076585;">Position</label>
                               <div class="col-sm-8">
                                   <asp:DropDownList ID="trainerPosition" runat="server" CssClass="form-control form-control-sm dropdown-icon" AppendDataBoundItems="true">
                                  </asp:DropDownList>
                                    <asp:RequiredFieldValidator ID="RequiredFieldValidator6" runat="server"
                                      ErrorMessage="Position is required!"
                                      ControlToValidate="trainerPosition" Display="Dynamic"
                                      CssClass="text-danger d-block" SetFocusOnError="True" />
                               </div>
                           </div>

                        <!-- Add Btn - Trainer -->
                        <div class="row g-2 mb-3">
                            <div class="col-sm-8 text-center">
                                <asp:Button Text="Add Trainer" runat="server" CssClass="btn fw-bolder px-4 me-2"
                                    Style="background-color: #022F56; color:#c9b99f; border-radius: 20px;"
                                    ID="addTrainerBtn" OnClick="btnaddTrainer_Click" />
                            </div>
                      </div>

                       </div>
                   </asp:Panel>
            </div>
        </div>
    </div>

</asp:Content>
