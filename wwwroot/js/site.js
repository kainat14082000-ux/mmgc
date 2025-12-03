// Sidebar Toggle Functionality
$(document).ready(function () {
    $('#sidebarCollapse').on('click', function () {
        $('#sidebar').toggleClass('active');
        $('#content').toggleClass('active');
    });

    // Close sidebar on mobile when clicking outside
    if ($(window).width() <= 768) {
        $('#content').on('click', function () {
            if (!$('#sidebar').hasClass('active')) {
                $('#sidebar').addClass('active');
            }
        });
    }

    // Highlight active menu item
    var currentPath = window.location.pathname;
    $('.sidebar-link, .sidebar-submenu a').each(function () {
        var href = $(this).attr('href');
        if (href && currentPath.includes(href) && href !== '/') {
            $(this).addClass('active');
            // Expand parent submenu if exists
            $(this).closest('.collapse').addClass('show');
        }
    });

    // Add animation to cards on page load
    $('.card, .dashboard-card').addClass('fade-in');

    // Smooth scroll for anchor links
    $('a[href^="#"]').on('click', function (e) {
        var target = $(this.hash);
        if (target.length) {
            e.preventDefault();
            $('html, body').animate({
                scrollTop: target.offset().top - 100
            }, 800);
        }
    });

    // Initialize tooltips if Bootstrap is available
    if (typeof bootstrap !== 'undefined') {
        var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }

    // Auto-hide alerts after 5 seconds
    $('.alert:not(.alert-permanent)').delay(5000).fadeOut('slow');

    // Form validation styling
    $('form').on('submit', function () {
        $(this).find('.btn[type="submit"]').prop('disabled', true).html('<i class="bi bi-hourglass-split"></i> Processing...');
    });

    // Confirmation dialogs for delete actions
    $('.btn-delete, .delete-action').on('click', function (e) {
        if (!confirm('Are you sure you want to delete this item? This action cannot be undone.')) {
            e.preventDefault();
            return false;
        }
    });

    // Number formatting for currency
    $('.currency').each(function () {
        var value = parseFloat($(this).text());
        if (!isNaN(value)) {
            $(this).text('PKR ' + value.toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
        }
    });

    // Date formatting
    $('.date-format').each(function () {
        var date = new Date($(this).text());
        if (date instanceof Date && !isNaN(date)) {
            $(this).text(date.toLocaleDateString('en-IN'));
        }
    });

    // Search functionality for tables
    $('#tableSearch').on('keyup', function () {
        var value = $(this).val().toLowerCase();
        $('table tbody tr').filter(function () {
            $(this).toggle($(this).text().toLowerCase().indexOf(value) > -1);
        });
    });

    // Responsive table wrapper
    $('table').wrap('<div class="table-responsive"></div>');
});

// Window resize handler
$(window).on('resize', function () {
    if ($(window).width() <= 768) {
        $('#sidebar').addClass('active');
        $('#content').removeClass('active');
    } else {
        $('#sidebar').removeClass('active');
        $('#content').removeClass('active');
    }
});
