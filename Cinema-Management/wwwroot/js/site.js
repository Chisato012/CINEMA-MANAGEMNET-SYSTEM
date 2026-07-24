document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.staff-alert').forEach(function (alert) {
        var duration = Number.parseInt(alert.dataset.alertDuration || '', 10);

        if (!Number.isFinite(duration) || duration <= 0) {
            duration = alert.classList.contains('staff-alert-error') ? 5200 : 3200;
        }

        window.setTimeout(function () {
            alert.classList.add('is-hiding');

            window.setTimeout(function () {
                alert.remove();
            }, 240);
        }, duration);
    });
});
