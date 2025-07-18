<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="AdminDashboard.aspx.cs" Inherits="Expiry_list.WebForm1" %>
<<<<<<< HEAD

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">

</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <div class=" container border-light card text-white shadow-lg w-100 mb-5"
        style="background: url('imgs/logo.jpg') center/cover no-repeat; min-height: 85vh; margin-top: -3px; position: relative; border: 15px white solid;">

        <i class="fas fa-star position-absolute text-warning" style="top: 10px; left: 10px; font-size: 2rem;"></i>
        <i class="fas fa-heart position-absolute text-danger" style="top: 10px; right: 10px; font-size: 2rem;"></i>
        <i class="fas fa-leaf position-absolute text-success" style="bottom: 10px; left: 10px; font-size: 2rem;"></i>
        <i class="fas fa-moon position-absolute text-light" style="bottom: 10px; right: 10px; font-size: 2rem;"></i>

    </div>

    <hr class="container mb-5" />

    <!-- Functional Cards -->
    <div class="container" id="functional">
        <div class="row g-4 justify-content-center row-cols-1 row-cols-md-2 row-cols-lg-3" id="cardForm">

            <!-- Expiry List Card -->
            <asp:Panel ID="pnlExpiryList" runat="server" style="display:none;">
                <div class="col">
                    <div class="card h-100 border-0 shadow-sm rounded-4 transition">
                        <div class="card-body text-center p-4">
                            <div class="icon-wrapper bg-primary rounded-circle shadow d-flex align-items-center justify-content-center mb-3 mx-auto"
                                style="width: 80px; height: 80px; margin-top: -40px;">
                                <i class="fas fa-clipboard-list fa-2x text-white"></i>
                            </div>
                            <h4 class="card-title fw-semibold mb-3">Expiry List</h4>
                            <p class="card-text text-muted mb-4">Register and track items with expiry alerts</p>
                            <asp:Button runat="server"
                                CssClass="btn btn-primary rounded-pill px-4"
                                OnClick="el_Click1"
                                Text="View More →" />
                        </div>
                    </div>
                </div>
           </asp:Panel>

            <asp:Panel ID="pnlNegativeInventory" runat="server" style="display:none;">
                <div class="col">
                    <div class="card h-100 border-0 shadow-sm rounded-4 transition">
                        <div class="card-body text-center p-4">
                            <div class="icon-wrapper bg-warning rounded-circle shadow d-flex align-items-center justify-content-center mb-3 mx-auto"
                                style="width: 80px; height: 80px; margin-top: -40px;">
                                <i class="fa-solid fa-folder-minus fa-2x text-white"></i>
                            </div>
                            <h4 class="card-title fw-semibold mb-3">Negative Inventory</h4>
                            <p class="card-text text-muted mb-4">Real-time tracking of negative inventory levels</p>
                            <asp:Button runat="server"
                                CssClass="btn btn-warning text-white rounded-pill px-4"
                                OnClick="ni_Click1"
                                Text="View More →" />
                        </div>
                    </div>
                </div>
            </asp:Panel>

            <asp:Panel ID="pnlSystemSettings" runat="server" style="display:none;">
                <div class="col">
                    <div class="card h-100 border-0 shadow-sm rounded-4 transition">
                        <div class="card-body text-center p-4">
                            <div class="icon-wrapper bg-success rounded-circle shadow d-flex align-items-center justify-content-center mb-3 mx-auto"
                                style="width: 80px; height: 80px; margin-top: -40px;">
                                <i class="fas fa-sliders fa-2x text-white"></i>
                            </div>
                            <h4 class="card-title fw-semibold mb-3">System Settings</h4>
                            <p class="card-text text-muted mb-4">Configure applications and user permissions</p>
                            <asp:Button runat="server"
                                CssClass="btn btn-success rounded-pill px-4"
                                OnClick="ss_Click1"
                                Text="View More →" />
                        </div>
                    </div>
                </div>
            </asp:Panel>

             <asp:Panel ID="pnlCarWayPlan" runat="server" style="display:none;">
                 <div class="col">
                    <div class="card h-100 border-0 shadow-sm rounded-4 transition">
                        <div class="card-body text-center p-4">
                            <div class="icon-wrapper bg-info rounded-circle shadow d-flex align-items-center justify-content-center mb-3 mx-auto"
                                style="width: 80px; height: 80px; margin-top: -40px;">
                                <i class="fa-solid fa-truck-fast fa-2x text-white"></i>
                            </div>
                            <h4 class="card-title fw-semibold mb-3">Car Way Plan</h4>
                            <p class="card-text text-muted mb-4">Manage car way plan for store and warehouse</p>
                            <asp:Button runat="server"
                                CssClass="btn btn-info text-white rounded-pill px-4"
                                OnClick="cw_Click1"
                                Text="View More →" />
                        </div>
                    </div>
                </div>
             </asp:Panel>

              <asp:Panel ID="pnlReorderQuantity" runat="server" style="display:none;">
                  <div class="col">
                     <div class="card h-100 border-0 shadow-sm rounded-4 transition">
                         <div class="card-body text-center p-4">
                             <div class="icon-wrapper rounded-circle shadow d-flex align-items-center justify-content-center mb-3 mx-auto"
                                 style="width: 80px; height: 80px; margin-top: -40px; background-color: #996FD6;">
                                 <i class="fa-solid fa-boxes-stacked fa-2x text-white"></i>
                             </div>
                             <h4 class="card-title fw-semibold mb-3">Reorder Quantity</h4>
                             <p class="card-text text-muted mb-4">Manage reorder for items</p>
                             <asp:Button runat="server"
                                 CssClass="btn text-white rounded-pill px-4"
                                 OnClick="rq_Click1" BackColor="#996FD6"
                                 Text="View More →" />
                         </div>
                     </div>
                </div>
              </asp:Panel>
        </div>
    </div>
</asp:Content>
=======
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
  <asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <div class=" container border-light card text-white shadow-md w-100 mb-5"
        style="background: url('imgs/logo.jpg');
               background-position:center; 
               background-size:cover;
               background-repeat:no-repeat;
               min-height: 85vh;
               margin-top: -3px;
               position: relative; border: 15px white solid;">
        <div class="card-body d-flex flex-column justify-content-center align-items-center text-center "> 
             <div class="overlay position-absolute top-0 start-0 w-100 h-100" 
                 style="background-color: rgba(0, 0, 0, 0.4);">
            </div>
            <div class=" position-relative z-1 p-5">
              <h1 class="display-4 fw-bold">Welcome</h1>
              <p class="lead">Shwe Ohh Pharmacy File Management System</p>
              <a href="#functional" class="btn btn-light btn-lg rounded-pill mt-4 shadow">Operational Buttons <i class="fa fa-arrow-down"></i></a>
            </div>
            <i class="fas fa-star position-absolute text-warning" style="top: 10px; left: 10px; font-size: 2.7rem;"></i>
            <i class="fas fa-heart position-absolute text-danger" style="top: 10px; right: 10px; font-size: 2.7rem;"></i>
            <i class="fas fa-leaf position-absolute text-info" style="bottom: 10px; left: 10px; font-size: 2.7rem;"></i>
            <i class="fas fa-moon position-absolute text-light" style="bottom: 10px; right: 10px; font-size: 2.7rem;"></i>
        </div>
   </div>

     <hr class="container mb-5" />

      <%-- <div class=" container border-light card text-white shadow-lg w-100 mb-5"
             style="background: url('imgs/logo.jpg') center/cover no-repeat;
                    min-height: 85vh;
                    margin-top: -3px;
                    position: relative; border: 15px white solid;">
     
            <i class="fas fa-star position-absolute text-warning" style="top: 10px; left: 10px; font-size: 2rem;"></i>
            <i class="fas fa-heart position-absolute text-danger" style="top: 10px; right: 10px; font-size: 2rem;"></i>
            <i class="fas fa-leaf position-absolute text-success" style="bottom: 10px; left: 10px; font-size: 2rem;"></i>
            <i class="fas fa-moon position-absolute text-light" style="bottom: 10px; right: 10px; font-size: 2rem;"></i>

        </div>--%>

    <!-- Functional Cards -->
    <div class="container" id="functional">
      <div class="row g-4 justify-content-center row-cols-1 row-cols-md-2 row-cols-lg-3" id="cardForm">

        <!-- Expiry List Card -->
        <div class="col">
          <div class="card h-100 border-0 shadow-sm rounded-4 transition">
            <div class="card-body text-center p-4">
              <div class="icon-wrapper bg-primary rounded-circle shadow d-flex align-items-center justify-content-center mb-3 mx-auto"
                   style="width: 80px; height: 80px; margin-top: -40px;">
                <i class="fas fa-clipboard-list fa-2x text-white"></i>
              </div>
              <h4 class="card-title fw-semibold mb-3">Expiry List</h4>
              <p class="card-text text-muted mb-4">Register and track items with automated expiry alerts</p>
              <asp:Button runat="server"
                          CssClass="btn btn-primary rounded-pill px-4"
                          OnClick="el_Click1"
                          Text="View More →" />
            </div>
          </div>
        </div>

           <% if (Session["role"].ToString() == "user" || Session["role"].ToString() == "admin") { %>
             <!-- Negative Inventory Card -->
             <div class="col">
               <div class="card h-100 border-0 shadow-sm rounded-4 transition">
                 <div class="card-body text-center p-4">
                   <div class="icon-wrapper bg-warning rounded-circle shadow d-flex align-items-center justify-content-center mb-3 mx-auto"
                        style="width: 80px; height: 80px; margin-top: -40px;">
                     <i class="fa-solid fa-folder-minus fa-2x text-white"></i>
                   </div>
                   <h4 class="card-title fw-semibold mb-3">Negative Inventory</h4>
                   <p class="card-text text-muted mb-4">Real-time tracking of negative inventory levels</p>
                   <asp:Button runat="server"
                               CssClass="btn btn-warning text-white rounded-pill px-4"
                               OnClick="ni_Click1"
                               Text="View More →" />
                 </div>
               </div>
             </div>
             <% } %>

        <% if (Session["role"] != null && Session["role"].ToString() == "admin") { %>
        <!-- System Settings Card -->
        <div class="col">
          <div class="card h-100 border-0 shadow-sm rounded-4 transition">
            <div class="card-body text-center p-4">
              <div class="icon-wrapper bg-success rounded-circle shadow d-flex align-items-center justify-content-center mb-3 mx-auto"
                   style="width: 80px; height: 80px; margin-top: -40px;">
                <i class="fas fa-sliders fa-2x text-white"></i>
              </div>
              <h4 class="card-title fw-semibold mb-3">System Settings</h4>
              <p class="card-text text-muted mb-4">Configure application parameters and user permissions</p>
              <a href="regeForm.aspx" class="btn btn-success rounded-pill px-4">View More →</a>
            </div>
          </div>
        </div>
        <% } %>

           <% if (Session["role"] != null && Session["role"].ToString() == "admin") { %>
             <!-- System Settings Card -->
             <div class="col">
               <div class="card h-100 border-0 shadow-sm rounded-4 transition">
                 <div class="card-body text-center p-4">
                   <div class="icon-wrapper bg-info rounded-circle shadow d-flex align-items-center justify-content-center mb-3 mx-auto"
                        style="width: 80px; height: 80px; margin-top: -40px;">
                        <i class="fa-solid fa-truck-fast fa-2x text-white"></i>
                   </div>
                   <h4 class="card-title fw-semibold mb-3">Car Way Plan</h4>
                   <p class="card-text text-muted mb-4">Manage car way plan for store and warehouse</p>
                   <asp:Button runat="server"
                        CssClass="btn btn-info text-white rounded-pill px-4"
                        OnClick="cw_Click1"
                        Text="View More →" />
                 </div>
               </div>
             </div>
             <% } %>

      </div>
    </div>
  </asp:Content>
>>>>>>> dd28a8dd26355ac93475b3760a0023853d81994b

