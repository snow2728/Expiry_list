<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="whView2.aspx.cs" Inherits="Expiry_list.CarWay.whView2" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="container py-4">
         <a href="../AdminDashboard.aspx" class="btn text-white mb-2" style="background-color: #158396;">
             <i class="fa-solid fa-left-long"></i> Home
         </a>

        <!-- Header Section -->
         <div class="header-section p-4 mb-4 rounded-2" style="background: gray;">
             <div class="d-flex flex-column flex-md-row justify-content-between align-items-md-center mb-4">
                 <div>
                     <asp:Button runat="server" CssClass="btn btn-info text-white btn-lg px-3 py-1 action-btn"
                         Text='Save' />
                 </div>
             </div>

             <div class="row g-3">
                 <div class="col-md-3">
                     <label class="form-label text-white">Way No.</label>
                     <asp:TextBox runat="server" CssClass="form-control bg-white text-black border-light" 
                         Text="PC-2023-00145" ReadOnly="true" />
                 </div>
                 <div class="col-md-3">
                     <label class="form-label text-white">Way Type</label>
                     <asp:DropDownList runat="server" CssClass="form-select bg-white text-black border-light">
                         <asp:ListItem Text="Choose One Way Type" Selected="True" />
                         <asp:ListItem Text="DC To Store" />
                         <asp:ListItem Text="Store To Store" />
                         <asp:ListItem Text="Department To Department" />
                     </asp:DropDownList>
                 </div>
                 <div class="col-md-3">
                     <label class="form-label text-white">Date</label>
                     <asp:TextBox runat="server" TextMode="Date" CssClass="form-control bg-white text-black border-light" 
                         Text="2023-10-15" />
                 </div>
                 <div class="col-md-3">
                     <label class="form-label text-white">Car No.</label>
                     <asp:DropDownList runat="server" CssClass="form-select bg-white text-black border-light">
                         <asp:ListItem Text="Choose Car No." Selected="True" />
                         <asp:ListItem Text="carNo1" />
                         <asp:ListItem Text="carNo2" />
                         <asp:ListItem Text="carNo3" />
                     </asp:DropDownList>
                 </div>
                 <div class="col-md-3">
                     <label class="form-label text-white">Driver Name</label>
                     <asp:TextBox runat="server" CssClass="form-control bg-white text-black border-light" />
                 </div>
                 <div class="col-md-3">
                     <label class="form-label text-white">Departure Date</label>
                     <asp:TextBox runat="server" TextMode="Date" CssClass="form-control bg-white text-black border-light" />
                 </div>
                 <div class="col-md-3">
                     <label class="form-label text-white">Departure Time</label>
                     <asp:TextBox runat="server" TextMode="Time" CssClass="form-control bg-white text-black border-light" />
                 </div>
                 <div class="col-md-3">
                     <label class="form-label text-white">From Location</label>
                     <asp:TextBox runat="server" CssClass="form-control bg-white text-black border-light" />
                 </div>
                 <div class="col-md-1">
                     <label class="form-label text-white">Transit</label>
                     <asp:CheckBox runat="server" CssClass="form-check-input" />
                 </div>
             </div>
         </div>

         <!-- Detail Section -->
         <div class="card border-0 shadow mb-4">
             <div class="card-header bg-white py-3 border-0">
                 <div class="d-flex justify-content-between align-items-center">
                     <h3 class="mb-0 text-info"><i class="fas fa-list me-2"></i> Lines</h3>
                     <button type="button" class="btn px-4 action-btn text-white" 
                         style="background: #1995AD;" onclick="addNewRow(event)">
                         <i class="fas fa-plus me-2"></i>Add Line
                     </button>
                 </div>
             </div>
             <div class="card-body p-0">
                 <div class="table-responsive">
                     <table class="table table-hover align-middle mb-0">
                         <thead class="table-light">
                             <tr>
                                 <th style="width: 47px;" class="text-center">No.</th>
                                 <th style="width: 250px;">Packing Type</th>
                                 <th style="width: 290px;">Destination Store.</th>
                                 <th style="width: 90px;" class="text-center">Qty.</th>
                                 <th style="width:190px">ToteBox No.</th>
                                 <th style="width: 100px;" class="text-center">Actions</th> 
                             </tr>
                         </thead>
                         <tbody id="gridBody">
                             <!-- Row 1 -->
                             <tr class="grid-row text-center">
                                 <td class="text-center">
                                     <input type="text" class="form-control" value="1" style="width:47px;" readonly>
                                 </td>
                                 <td>
                                     <select class="form-select">
                                         <option selected>Choose One Packing Type.</option>
                                         <option>Tote Box</option>
                                         <option>Plastic Bag</option>
                                         <option>Carton</option>
                                         <option>Other</option>
                                     </select>
                                 </td>
                                 <td>
                                     <div class="store-dropdown-wrapper">
                                         <asp:ListBox ID="lstStoreFilter1" runat="server" 
                                             CssClass="form-control store-select" 
                                             SelectionMode="Multiple" style="width: 100%;">
                                         </asp:ListBox>
                                     </div>
                                 </td>
                                 <td class="text-center">
                                     <input type="number" min="1" value="7" class="form-control text-center" style="width: 90px;">
                                 </td>
                                 <td>
                                     <select class="form-select" style="width:190px">
                                         <option selected>Choose ToteBox No.</option>
                                         <option>Box1</option>
                                         <option>Box2</option>
                                         <option>Box3</option>
                                         <option>Other</option>
                                     </select>
                                 </td>
                                 <td>
                                     <button class="btn btn-sm btn-info text-white">Set</button>
                                     <button class="btn btn-sm btn-danger text-white ms-1" onclick="deleteRow(this)">X</button>
                                 </td>
                             </tr>
                         </tbody>
                     </table>
                 </div>
             </div>
         </div> 

         <!-- Status Bar -->
         <div class="d-flex flex-column flex-md-row justify-content-between align-items-center bg-white p-3 rounded shadow-sm">
             <div class="d-flex align-items-center mb-3 mb-md-0">
                 <button class="btn btn-outline-primary btn-sm rounded-circle action-btn me-2">
                     <i class="fas fa-arrow-left"></i>
                 </button>
                 <span class="me-3 fw-medium">Record 1 of 1</span>
                 <button class="btn btn-outline-primary btn-sm rounded-circle action-btn me-3">
                     <i class="fas fa-arrow-right"></i>
                 </button>
             </div>
             <div class="d-flex align-items-center">
                 <button class="btn btn-outline-secondary btn-sm rounded-circle action-btn me-2">
                     <i class="fas fa-chevron-left"></i>
                 </button>
                 <span class="mx-2 fw-medium">Page 1</span>
                 <button class="btn btn-outline-secondary btn-sm rounded-circle action-btn">
                     <i class="fas fa-chevron-right"></i>
                 </button>
             </div>
         </div>
     </div>
</asp:Content>
