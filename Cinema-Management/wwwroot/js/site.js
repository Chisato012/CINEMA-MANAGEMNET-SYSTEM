
document.addEventListener('DOMContentLoaded', function () {
    const bookingLinks = document.querySelectorAll('[data-booking-nav-warning="true"]');
    let toastTimer;

    function showBookingToast() {
        let toast = document.getElementById('bookingNavToast');

        if (!toast) {
            toast = document.createElement('div');
            toast.id = 'bookingNavToast';
            toast.className = 'booking-nav-toast';
            toast.setAttribute('role', 'status');
            toast.setAttribute('aria-live', 'polite');
            document.body.appendChild(toast);
        }

        toast.textContent = 'Hãy chọn phim trước khi đặt vé!';
        toast.classList.add('show');

        clearTimeout(toastTimer);
        toastTimer = setTimeout(function () {
            toast.classList.remove('show');
        }, 3200);
    }

    bookingLinks.forEach(function (link) {
        link.addEventListener('click', function (event) {
            event.preventDefault();
            showBookingToast();
        });
    });
});

