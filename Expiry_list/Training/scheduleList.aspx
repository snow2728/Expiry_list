<%@ Page Title="" Language="C#" MasterPageFile="~/Site1.Master" AutoEventWireup="true" CodeBehind="scheduleList.aspx.cs" Inherits="Expiry_list.Training.scheduleList" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <script src="https://cdnjs.cloudflare.com/ajax/libs/jquery/3.6.0/jquery.min.js"></script>
    <style>
        /* Container */
        .multi-select-container {
            max-width: 300px;
            margin: 40px auto;
            font-family: Arial, sans-serif;
        }

        .multi-select-label {
            font-weight: 600;
            margin-bottom: 6px;
            display: block;
        }

        /* Input box */
        .multi-select-input {
            width: 100%;
            min-height: 45px;
            border: 1px solid #ced4da;
            border-radius: 8px;
            padding: 5px 8px;
            display: flex;
            flex-wrap: wrap;
            gap: 6px;
            align-items: center;
            cursor: text;
            background-color: #fff;
            position: relative;
        }

        .multi-select-input input {
            border: none;
            outline: none;
            flex: 1;
            min-width: 120px;
            padding: 4px;
        }

        .multi-select-input input.placeholder {
            color: #6c757d;
        }

        /* Pills */
        .pill {
            display: flex;
            align-items: center;
            padding: 5px 12px;
            background-color: #007bff;
            color: #fff;
            border-radius: 15px;
            font-size: 0.9em;
            box-shadow: 0 1px 3px rgba(0,0,0,0.15);
        }

        .pill .remove-pill {
            margin-left: 6px;
            font-weight: bold;
            cursor: pointer;
            color: #fff;
        }

        .pill .remove-pill:hover {
            color: #ffdcdc;
        }

        /* Dropdown */
        .multi-select-dropdown {
            position: absolute;
            width: 30%;
            max-height: 200px;
            overflow-y: auto;
            background: #fff;
            border: 1px solid #ced4da;
            border-radius: 6px;
            z-index: 100;
            display: none;
            margin-top: 4px;
            box-shadow: 0 2px 6px rgba(0,0,0,0.1);
        }

        .multi-select-dropdown div {
            padding: 8px 12px;
            cursor: pointer;
        }

        .multi-select-dropdown div:hover {
            background-color: #f1f1f1;
        }

        /* Search box inside dropdown */
        .dropdown-search {
            padding: 6px 10px;
            border-bottom: 1px solid #ddd;
            width: 100%;
            box-sizing: border-box;
        }
    </style>

    <script>
        $(document).ready(function () {
            var availableItems = ["Apple", "Banana", "Orange", "Grapes", "Mango", "Pineapple", "Strawberry"];
            var selectedItems = [];

            var container = $(".multi-select-container");
            var inputBox = $(".multi-select-input");
            var dropdown = $(".multi-select-dropdown");

            // Build dropdown with search input
            var searchInput = $('<input type="text" class="dropdown-search w-10" placeholder="Search...">');
            dropdown.append(searchInput);

            availableItems.forEach(function (item) {
                dropdown.append('<div data-value="' + item + '">' + item + '</div>');
            });

            // Show dropdown
            inputBox.click(function () {
                dropdown.show();
                searchInput.focus();
            });

            // Filter dropdown items
            searchInput.on("keyup", function () {
                var val = $(this).val().toLowerCase();
                dropdown.find('div[data-value]').each(function () {
                    $(this).toggle($(this).text().toLowerCase().indexOf(val) > -1);
                });
            });

            // Select item
            $(document).on("click", ".multi-select-dropdown div[data-value]", function () {
                var value = $(this).data("value");
                if (!selectedItems.includes(value)) {
                    selectedItems.push(value);
                    updateInput();
                }
            });

            // Remove pill
            $(document).on("click", ".remove-pill", function (e) {
                e.stopPropagation();
                var value = $(this).data("value");
                selectedItems = selectedItems.filter(function (v) { return v !== value; });
                updateInput();
            });

            // Update input box
            function updateInput() {
                inputBox.html('');

                if (selectedItems.length === 0) {
                    inputBox.append('<input type="text" class="placeholder" placeholder="Select fruits..." readonly />');
                } else {
                    selectedItems.forEach(function (item) {
                        inputBox.append('<span class="pill">' + item + '<span class="remove-pill" data-value="' + item + '">&times;</span></span>');
                    });
                    // Add a small invisible input to maintain focus for dropdown
                    inputBox.append('<input type="text" style="width:2px;border:none;outline:none;" readonly />');
                }
            }

            // Click outside to close dropdown
            $(document).click(function (e) {
                if (!$(e.target).closest(".multi-select-container").length) {
                    dropdown.hide();
                    searchInput.val('');
                    dropdown.find('div[data-value]').show();
                }
            });

            // Initialize
            updateInput();
        });
    </script>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="multi-select-container">
        <label class="multi-select-label">Select Fruits</label>
        <div class="multi-select-input"></div>
        <div class="multi-select-dropdown"></div>

        <br />
        <asp:Button ID="btnSubmit" runat="server" Text="Submit" CssClass="btn btn-primary" OnClick="btnSubmit_Click" />
        <br /><br />
        <asp:Label ID="lblResult" runat="server" ForeColor="Green"></asp:Label>
    </div>
</asp:Content>
