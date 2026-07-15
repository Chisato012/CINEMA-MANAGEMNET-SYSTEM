document.addEventListener('DOMContentLoaded', () => {
    const page = document.querySelector('.booking-schedule-page');
    if (!page) return;

    const dateStrip = page.querySelector('[data-date-strip]');
    const dateButtons = [...page.querySelectorAll('[data-date-option]')];
    const filterButtons = [...page.querySelectorAll('[data-schedule-filter]')];
    const searchInput = page.querySelector('[data-schedule-search]');
    const movieCards = [...page.querySelectorAll('[data-movie-card]')];
    const showtimeButtons = [...page.querySelectorAll('[data-showtime-button]')];
    const emptyDate = page.querySelector('[data-empty-date]');
    const emptySearch = page.querySelector('[data-empty-search]');
    const selectTodayButton = page.querySelector('[data-select-today]');
    const summaryEmpty = page.querySelector('[data-summary-empty]');
    const summarySelected = page.querySelector('[data-summary-selected]');
    const continueButton = page.querySelector('[data-continue-button]');
    const selectedMovieInput = page.querySelector('[data-selected-movie]');
    const selectedShowtimeInput = page.querySelector('[data-selected-showtime]');
    const selectedFormatInput = page.querySelector('[data-selected-format]');
    const bookingForm = document.getElementById('bookingScheduleForm');
    const todayButton = dateButtons.find(button => button.querySelector('.booking-date-card__today'));
    let selectedDate = page.dataset.selectedDate || dateButtons[0]?.dataset.date || '';
    let activeFilters = new Set();
    let selectedShowtimeId = null;

    function normalize(value) {
        return (value || '')
            .toLowerCase()
            .normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '')
            .replace(/đ/g, 'd')
            .trim();
    }

    function buttonMatchesFilters(button) {
        if (activeFilters.size === 0) return true;

        const format = normalize(button.dataset.formatKey || button.dataset.format);
        const isLate = button.dataset.late === 'true';

        for (const filter of activeFilters) {
            if (filter === '2d' && !format.includes('2d')) return false;
            if (filter === '3d' && !format.includes('3d')) return false;
            if (filter === 'subtitle' && !format.includes('phu de')) return false;
            if (filter === 'dubbed' && !format.includes('long tieng')) return false;
            if (filter === 'late' && !isLate) return false;
        }

        return true;
    }

    function setDate(date, clearSelection = true) {
        selectedDate = date;
        page.dataset.selectedDate = date;

        dateButtons.forEach(button => {
            const selected = button.dataset.date === date;
            button.classList.toggle('is-selected', selected);
            button.setAttribute('aria-pressed', selected ? 'true' : 'false');
        });

        if (clearSelection) {
            clearSelectionState();
        }

        applyFilters();
    }

    function setFilter(button) {
        const filter = button.dataset.scheduleFilter;

        if (filter === 'all') {
            activeFilters.clear();
        } else if (activeFilters.has(filter)) {
            activeFilters.delete(filter);
        } else {
            activeFilters.add(filter);
        }

        filterButtons.forEach(item => {
            const itemFilter = item.dataset.scheduleFilter;
            const active = itemFilter === 'all' ? activeFilters.size === 0 : activeFilters.has(itemFilter);
            item.classList.toggle('is-active', active);
            item.setAttribute('aria-pressed', active ? 'true' : 'false');
        });

        applyFilters();
    }

    function applyFilters() {
        const query = normalize(searchInput?.value);
        let visibleMovieCount = 0;
        let hasScheduleForDate = false;

        movieCards.forEach(card => {
            const titleMatches = !query || normalize(card.dataset.title).includes(query);
            let visibleShowtimes = 0;

            card.querySelectorAll('[data-showtime-button]').forEach(button => {
                const isOnDate = button.dataset.date === selectedDate;
                const dateMatch = isOnDate;
                if (isOnDate) hasScheduleForDate = true;

                const visible = dateMatch && buttonMatchesFilters(button);
                button.hidden = !visible;
                if (visible) visibleShowtimes++;
            });

            const cardVisible = titleMatches && visibleShowtimes > 0;
            card.hidden = !cardVisible;
            if (cardVisible) visibleMovieCount++;
        });

        if (emptyDate) {
            emptyDate.hidden = hasScheduleForDate || visibleMovieCount > 0;
        }

        if (emptySearch) {
            emptySearch.hidden = !hasScheduleForDate || visibleMovieCount > 0;
        }
    }

    function selectShowtime(button) {
        selectedShowtimeId = button.dataset.showtimeId;

        showtimeButtons.forEach(item => {
            const selected = item === button;
            item.classList.toggle('is-selected', selected);
            item.setAttribute('aria-pressed', selected ? 'true' : 'false');
        });

        selectedMovieInput.value = button.dataset.movieId;
        selectedShowtimeInput.value = button.dataset.showtimeId;
        selectedFormatInput.value = button.dataset.format;

        page.querySelector('[data-summary-poster]').src = button.dataset.poster;
        page.querySelector('[data-summary-poster]').alt = `Poster phim ${button.dataset.movieTitle}`;
        page.querySelector('[data-summary-title]').textContent = button.dataset.movieTitle;
        page.querySelector('[data-summary-format]').textContent = button.dataset.format;
        page.querySelector('[data-summary-date]').textContent = button.dataset.dateLabel;
        page.querySelector('[data-summary-time]').textContent = button.dataset.time;
        page.querySelector('[data-summary-room]').textContent = button.dataset.room;

        summaryEmpty.hidden = true;
        summarySelected.hidden = false;
        continueButton.disabled = false;
    }

    function clearSelectionState() {
        selectedShowtimeId = null;
        showtimeButtons.forEach(item => {
            item.classList.remove('is-selected');
            item.setAttribute('aria-pressed', 'false');
        });

        selectedMovieInput.value = '';
        selectedShowtimeInput.value = '';
        selectedFormatInput.value = '';
        summaryEmpty.hidden = false;
        summarySelected.hidden = true;
        continueButton.disabled = true;
    }

    dateButtons.forEach(button => {
        button.addEventListener('click', () => setDate(button.dataset.date));
    });

    filterButtons.forEach(button => {
        button.addEventListener('click', () => setFilter(button));
    });

    searchInput?.addEventListener('input', applyFilters);

    showtimeButtons.forEach(button => {
        button.addEventListener('click', () => {
            if (!button.disabled) selectShowtime(button);
        });
    });

    page.querySelectorAll('[data-date-scroll]').forEach(button => {
        button.addEventListener('click', () => {
            const direction = button.dataset.dateScroll === 'next' ? 1 : -1;
            dateStrip?.scrollBy({ left: direction * 320, behavior: 'smooth' });
        });
    });

    selectTodayButton?.addEventListener('click', () => {
        const today = todayButton?.dataset.date || dateButtons[0]?.dataset.date;
        if (today) setDate(today);
    });

    bookingForm?.addEventListener('submit', event => {
        if (!selectedShowtimeId) {
            event.preventDefault();
        }
    });

    setDate(selectedDate, false);
});
