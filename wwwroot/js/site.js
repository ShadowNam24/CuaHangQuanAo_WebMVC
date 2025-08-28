// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(document).ready(function () {
    // Initialize password security for all secure password fields
    $('.secure-password, input[data-secure="true"]').each(function () {
        const input = this;

        // Disable clipboard operations
        $(input).on('paste copy cut drag drop contextmenu selectstart', function (e) {
            e.preventDefault();
            showToast('Clipboard operations are disabled for security', 'warning');
            return false;
        });

        // Block keyboard shortcuts
        $(input).on('keydown', function (e) {
            if ((e.ctrlKey || e.metaKey) &&
                ['c', 'v', 'x', 'a'].includes(e.key.toLowerCase())) {
                e.preventDefault();
                showToast('Keyboard shortcuts disabled for security', 'warning');
            }
        });

        // Add CSS protection
        $(input).css({
            'user-select': 'none',
            '-webkit-user-select': 'none',
            '-moz-user-select': 'none'
        });
    });
});

function showToast(message, type = 'info') {
    const toast = $(`
        &lt;div class="toast align-items-center text-white bg-${type} border-0 position-fixed" 
             style="top: 20px; right: 20px; z-index: 9999;"&gt;
            &lt;div class="d-flex"&gt;
                &lt;div class="toast-body"&gt;${message}&lt;/div&gt;
                &lt;button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast"&gt;&lt;/button&gt;
            &lt;/div&gt;
        &lt;/div&gt;
    `);

    $('body').append(toast);
    new bootstrap.Toast(toast[0]).show();

    setTimeout(() => toast.remove(), 3000);
}
