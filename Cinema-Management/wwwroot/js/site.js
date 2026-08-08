document.addEventListener('DOMContentLoaded', function () {
    function applyNavbarProfileAvatar(src) {
        const navImages = document.querySelectorAll('[data-profile-avatar-image]');
        const navInitials = document.querySelectorAll('[data-profile-avatar-initial]');

        if (!navImages.length && !navInitials.length) {
            return;
        }

        if (!src) {
            navImages.forEach(function (image) {
                image.removeAttribute('src');
                image.hidden = true;
            });
            navInitials.forEach(function (initial) {
                initial.hidden = false;
            });
            return;
        }

        navImages.forEach(function (image) {
            image.src = src;
            image.hidden = false;
        });
        navInitials.forEach(function (initial) {
            initial.hidden = true;
        });
    }

    const profileAvatarButton = document.querySelector('[data-profile-avatar-key]');
    const profileAvatarKey = profileAvatarButton?.dataset.profileAvatarKey;

    if (profileAvatarKey) {
        applyNavbarProfileAvatar(localStorage.getItem(profileAvatarKey));

        window.addEventListener('storage', function (event) {
            if (event.key === profileAvatarKey) {
                applyNavbarProfileAvatar(event.newValue);
            }
        });
    }

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
    })
    const widget = document.querySelector('.chatbot-widget');
    if (!widget) return;

    const toggle = widget.querySelector('.chatbot-toggle');
    const close = widget.querySelector('.chatbot-close');
    const panel = widget.querySelector('.chatbot-panel');
    const form = widget.querySelector('.chatbot-form');
    const firstSelect = widget.querySelector('.chatbot-select');
    const messages = widget.querySelector('.chatbot-messages');

    function setOpen(isOpen) {
        widget.classList.toggle('chatbot-widget--open', isOpen);
        panel.setAttribute('aria-hidden', String(!isOpen));
        if (isOpen) firstSelect.focus();
    }

    function appendMessage(text, sender) {
        const bubble = document.createElement('div');
        bubble.className = `chatbot-message chatbot-message--${sender}`;
        bubble.textContent = text;
        messages.appendChild(bubble);
        messages.scrollTop = messages.scrollHeight;
        return bubble;
    }

    function formatSelection(payload) {
        const labels = {
            mood: form.elements.mood.selectedOptions[0].text,
            companion: form.elements.companion.selectedOptions[0].text,
            intensity: form.elements.intensity.selectedOptions[0].text,
            ageRating: payload.ageRating
        };

        return `${labels.mood} · ${labels.companion} · ${labels.intensity} · ${labels.ageRating}`;
    }

    function formatRecommendation(data) {
        const reply = data.reply ? `${data.reply}\n\n` : '';
        const movies = Array.isArray(data.movies) && data.movies.length > 0
            ? data.movies
                .map((movie, index) => `${index + 1}. ${movie.title} (${movie.genres}, ${movie.ageRating})`)
                .join('\n')
            : 'Chưa tìm thấy phim phù hợp với lựa chọn này.';

        return `${reply}${movies}`.trim();
    }

    async function requestRecommendation(payload) {
        appendMessage(formatSelection(payload), 'user');
        const pending = appendMessage('Đang tìm phim...', 'bot');

        try {
            // Frontend chỉ gọi endpoint guided recommendation, không còn gửi câu hỏi tự do.
            const response = await fetch('/api/chatbot/recommend', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (!response.ok) {
                throw new Error('Recommendation request failed.');
            }

            const data = await response.json();
            pending.textContent = formatRecommendation(data);
        } catch {
            pending.textContent = 'Không gọi được chatbot. Hãy kiểm tra server ứng dụng, SQL Server và model ML.NET.';
        }
    }

    toggle.addEventListener('click', () => setOpen(!widget.classList.contains('chatbot-widget--open')));
    close.addEventListener('click', () => setOpen(false));

    form.addEventListener('submit', function (event) {
        event.preventDefault();

        const formData = new FormData(form);
        requestRecommendation({
            mood: formData.get('mood'),
            companion: formData.get('companion'),
            intensity: formData.get('intensity'),
            ageRating: formData.get('ageRating')
        });
    });
});
