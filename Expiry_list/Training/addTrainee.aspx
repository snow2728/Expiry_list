<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="addTrainee.aspx.cs" Inherits="Expiry_list.Training.addTrainee" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">



</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

     <a href="../AdminDashboard.aspx" class="btn text-white ms-2" style="background-color: #022F56;"><i class="fa-solid fa-left-long"></i>Home</a>

  <div class="container-fluid" >
      <div class="row justify-content-center g-2">
          <div class="card shadow rounded-4 p-2 col-12 col-lg-5 col-md-4">
              <!-- New Topic Card -->
                <asp:Panel ID="pnlAddTrainee" runat="server" style="background-color: #CCDEE4;">
                   <!-- Card Header -->
                   <div class="card-header fw-bolder text-center"
                       style="background-color: #022F56; color:#c9b99f; border-top-left-radius: 10px; border-top-right-radius: 10px;">
                       <h2 class="mb-0">New Trainee</h2>
                   </div>
                    <div class="card-body text-white text-center">
                        <!-- Trainee Field -->
                        <div class="row g-2 mb-3">
                            <label for="<%= traineeName.ClientID %>" class="col-sm-4 col-form-label fa-1x fw-bolder" style="color:#076585;">Trainee Name</label>
                            <div class="col-sm-8">
                                <asp:TextBox runat="server" CssClass="form-control form-control-sm" ID="traineeName" ReadOnly="true" />
                                <%--<asp:DropDownList ID="traineeName" runat="server" CssClass="form-control form-control-sm dropdown-icon" > 
                                </asp:DropDownList>--%>
                                 <asp:RequiredFieldValidator ID="RequiredFieldValidator7" runat="server"
                                   ErrorMessage="Name is required!"
                                   ControlToValidate="traineeName" Display="Dynamic"
                                   CssClass="text-danger d-block" SetFocusOnError="True" />
                            </div>
                        </div>

                       <!-- Level Field -->
                          <div class="row g-2 mb-3">
                            <label for="<%= levelDb.ClientID %>" class="col-sm-4 col-form-label fa-1x fw-bolder" style="color:#076585;">Level</label>
                            <div class="col-sm-8">
                               <asp:DropDownList ID="levelDb" runat="server" CssClass="form-control form-control-sm dropdown-icon">
                                    <asp:ListItem Text="Select Level" Value="" />
                                    <asp:ListItem Value="trainee" Text="Trainee"></asp:ListItem>
                                    <asp:ListItem Value="juniorsale" Text="Junior Sale"></asp:ListItem>
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
                          <%--  <asp:TextBox runat="server" CssClass="form-control form-control-sm" ID="storeNo" ReadOnly="true" />--%>
                              <asp:DropDownList ID="trainerDp" runat="server" CssClass="form-control form-control-sm">
                              </asp:DropDownList>
                              <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server"
                                  ErrorMessage="Trainer is required!"
                                  ControlToValidate="trainerDp" Display="Dynamic"
                                  CssClass="text-danger d-block" SetFocusOnError="True" />
                          </div>
                      </div>

                        <!-- Add Btn - Trainee -->
                        <div class="row g-2 mb-3">
                            <div class="col-sm-8 text-center">
                                <asp:Button Text="Add Trainee" runat="server" CssClass="btn fw-bolder px-4 me-2"
                                    Style="background-color: #022F56; color:#c9b99f; border-radius: 20px;"
                                    ID="addTraineeBtn1" />
                            </div>
                      </div>

                    </div>
                </asp:Panel>
          </div>
      </div>
  </div>

</asp:Content>
