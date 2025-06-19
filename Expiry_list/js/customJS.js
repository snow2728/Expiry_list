

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

document.querySelectorAll('.nav-link').forEach(link => {
    link.addEventListener('click', () => {
        const navbar = document.getElementById('mainNav');
        if (window.innerWidth < 992 && navbar.classList.contains('show')) {
            new bootstrap.Collapse(navbar).hide();
        }
    });
});

// js code for dash2.aspx




