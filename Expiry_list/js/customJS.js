

function formatDate(data, type) {
    if (!data) return '';

    const date = new Date(data);
    if (isNaN(date)) return data;

    if (type === 'display' || type === 'filter') {
        return date.toLocaleDateString('en-US', {
            month: 'long',
            year: 'numeric'
        });
    }

    return data;

}

function scrollToEditedRow() {
    const rowId = $('#<%= hfSelectedIDs.ClientID %>').val();
    if (rowId) {
        const $row = $(`tr[data-id='${rowId}']`);
        if ($row.length) {
            const $table = $('.dataTables_scrollBody');
            const rowTop = $row.position().top;
            $table.animate({
                scrollTop: rowTop + $table.scrollTop()
            }, 500);
        }
        $('#<%= hfSelectedIDs.ClientID %>').val('');
    }
}

function editRow(id) {
    document.getElementById('<%= hfSelectedIDs.ClientID %>').value = id;
    __doPostBack('', ''); 
}

function blockInvalidChars(event) {
    // Allow: Backspace, Delete, Arrow keys, Tab
    if ([8, 9, 37, 39, 46].includes(event.keyCode)) {
        return;
    }

    // Block if not a digit (0-9)
    if (!/[0-9]/.test(event.key)) {
        event.preventDefault();
    }
}

document.querySelectorAll('.nav-link').forEach(link => {
    link.addEventListener('click', () => {
        const navbar = document.getElementById('mainNav');
        if (window.innerWidth < 992 && navbar.classList.contains('show')) {
            new bootstrap.Collapse(navbar).hide();
        }
    });
});

// js code for reorder/viewer3.aspx
function onDeleteSuccess(result) {
    if (result) {
        Swal.fire('Deleted!', 'Your record has been deleted.', 'success');
        refreshDataTable();
    } else {
        Swal.fire('Error!', 'Failed to delete record.', 'error');
    }
}



function onDeleteError(error) {
    Swal.fire('Error!', 'An error occurred while deleting: ' + error.get_message(), 'error');
}


