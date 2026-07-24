(function () {
    const page = document.querySelector('[data-offers-page]');

    if (!page) {
        return;
    }

    const parseJson = (id, fallback) => {
        const node = document.getElementById(id);
        if (!node) {
            return fallback;
        }

        try {
            return JSON.parse(node.textContent || '');
        } catch {
            return fallback;
        }
    };

    const offers = parseJson('offerData', []);
    const quickBookingMovies = parseJson('quickBookingData', []);
    const offersById = new Map(offers.map((offer) => [offer.Id, offer]));

    const cards = Array.from(document.querySelectorAll('[data-offer-card]'));
    const filterButtons = Array.from(document.querySelectorAll('[data-offer-filter]'));
    const searchInput = document.querySelector('[data-offer-search]');
    const emptyState = document.querySelector('[data-offer-empty]');
    const resetButton = document.querySelector('[data-reset-offers]');
    const toast = document.querySelector('[data-offer-toast]');

    const selectedEmpty = document.querySelector('[data-selected-empty]');
    const selectedDetail = document.querySelector('[data-selected-detail]');
    const selectedValue = document.querySelector('[data-selected-value]');
    const selectedTitle = document.querySelector('[data-selected-title]');
    const selectedSummary = document.querySelector('[data-selected-summary]');
    const selectedCode = document.querySelector('[data-selected-code]');
    const selectedValidity = document.querySelector('[data-selected-validity]');
    const selectedDetailsButton = document.querySelector('[data-selected-details]');
    const clearOfferButton = document.querySelector('[data-clear-offer]');
    const offerCodeHidden = document.querySelector('[data-booking-offer-code]');

    const modal = document.querySelector('[data-offer-modal]');
    const modalDialog = modal?.querySelector('.offer-modal__dialog');
    const modalValue = document.querySelector('[data-modal-value]');
    const modalBadge = document.querySelector('[data-modal-badge]');
    const modalTitle = document.querySelector('[data-modal-title]');
    const modalDescription = document.querySelector('[data-modal-description]');
    const modalCode = document.querySelector('[data-modal-code]');
    const modalDate = document.querySelector('[data-modal-date]');
    const modalUsers = document.querySelector('[data-modal-users]');
    const modalMinimum = document.querySelector('[data-modal-minimum]');
    const modalTerms = document.querySelector('[data-modal-terms]');
    const modalUseButton = document.querySelector('[data-modal-use]');

    const quickForm = document.querySelector('[data-quick-booking-form]');
    const quickMovie = document.querySelector('[data-quick-movie]');
    const quickDate = document.querySelector('[data-quick-date]');
    const quickShowtime = document.querySelector('[data-quick-showtime]');
    const quickSubmit = document.querySelector('[data-quick-submit]');
    const quickMovieId = document.querySelector('[data-booking-movie-id]');
    const quickShowtimeId = document.querySelector('[data-booking-showtime-id]');
    const quickFormat = document.querySelector('[data-booking-format]');

    const codeForm = document.querySelector('[data-code-form]');
    const codeInput = document.querySelector('[data-code-input]');
    const codeSubmit = document.querySelector('[data-code-submit]');
    const codeResult = document.querySelector('[data-code-result]');

    let activeFilter = 'all';
    let selectedOfferId = null;
    let lastFocusedElement = null;
    let toastTimer = 0;
    let modalOfferId = null;
    let validationBusy = false;

    const showToast = (message) => {
        if (!toast) {
            return;
        }

        window.clearTimeout(toastTimer);
        toast.textContent = message;
        toast.classList.add('show');
        toastTimer = window.setTimeout(() => {
            toast.classList.remove('show');
        }, 2600);
    };

    const formatMoney = (value) => {
        if (value == null || Number.isNaN(Number(value))) {
            return 'Không yêu cầu';
        }

        return new Intl.NumberFormat('vi-VN').format(Number(value)) + 'đ';
    };

    const getOfferCode = (offer) => offer?.Code || 'Không cần mã';

    const applyFilters = () => {
        const query = (searchInput?.value || '').trim().toLowerCase();
        let visibleCount = 0;

        cards.forEach((card) => {
            const matchesSearch = !query || (card.dataset.search || '').includes(query);
            const matchesFilter =
                activeFilter === 'all' ||
                (activeFilter === 'expiring' && card.dataset.expiring === 'true') ||
                card.dataset.category === activeFilter;
            const visible = matchesSearch && matchesFilter;

            card.hidden = !visible;
            if (visible) {
                visibleCount += 1;
            }
        });

        if (emptyState) {
            emptyState.hidden = visibleCount > 0;
        }
    };

    filterButtons.forEach((button) => {
        button.addEventListener('click', () => {
            activeFilter = button.dataset.offerFilter || 'all';
            filterButtons.forEach((item) => {
                const active = item === button;
                item.classList.toggle('is-active', active);
                item.setAttribute('aria-pressed', active ? 'true' : 'false');
            });
            applyFilters();
        });
    });

    searchInput?.addEventListener('input', applyFilters);

    resetButton?.addEventListener('click', () => {
        activeFilter = 'all';
        if (searchInput) {
            searchInput.value = '';
        }

        filterButtons.forEach((item) => {
            const active = item.dataset.offerFilter === 'all';
            item.classList.toggle('is-active', active);
            item.setAttribute('aria-pressed', active ? 'true' : 'false');
        });
        applyFilters();
    });

    const markSelectedCard = () => {
        cards.forEach((card) => {
            card.classList.toggle('is-selected', card.dataset.offerId === selectedOfferId);
        });
    };

    const updateSelectedPanel = () => {
        const offer = selectedOfferId ? offersById.get(selectedOfferId) : null;

        if (!selectedEmpty || !selectedDetail) {
            return;
        }

        if (!offer) {
            selectedEmpty.hidden = false;
            selectedDetail.hidden = true;
            if (offerCodeHidden) {
                offerCodeHidden.value = '';
            }
            markSelectedCard();
            updateQuickBookingState();
            return;
        }

        selectedEmpty.hidden = true;
        selectedDetail.hidden = false;
        selectedValue.textContent = offer.DisplayValue || '';
        selectedTitle.textContent = offer.Title || '';
        selectedSummary.textContent = offer.Summary || '';
        selectedCode.textContent = getOfferCode(offer);
        selectedValidity.textContent = offer.ValidityLabel || '';
        if (offerCodeHidden) {
            offerCodeHidden.value = offer.Code || '';
        }
        markSelectedCard();
        updateQuickBookingState();
    };

    const selectOffer = (id, options = {}) => {
        const offer = offersById.get(id);
        if (!offer || offer.Status !== 'active') {
            showToast(offer?.StatusLabel || 'Ưu đãi chưa thể áp dụng');
            return;
        }

        selectedOfferId = id;
        updateSelectedPanel();

        if (options.toast !== false) {
            showToast('Đã chọn ưu đãi cho đơn hàng');
        }
    };

    const clearOffer = () => {
        selectedOfferId = null;
        updateSelectedPanel();
        showToast('Đã xóa ưu đãi đang chọn');
    };

    document.addEventListener('click', (event) => {
        const selectButton = event.target.closest('[data-select-offer]');
        if (selectButton) {
            selectOffer(selectButton.dataset.selectOffer);
            return;
        }

        const openButton = event.target.closest('[data-open-offer]');
        if (openButton) {
            openModal(openButton.dataset.openOffer, openButton);
            return;
        }

        const copyButton = event.target.closest('[data-copy-code]');
        if (copyButton) {
            copyCode(copyButton.dataset.copyCode || '', copyButton);
        }
    });

    clearOfferButton?.addEventListener('click', clearOffer);

    selectedDetailsButton?.addEventListener('click', () => {
        if (selectedOfferId) {
            openModal(selectedOfferId, selectedDetailsButton);
        }
    });

    const copyCode = async (code, button) => {
        if (!code) {
            return;
        }

        try {
            if (navigator.clipboard?.writeText) {
                await navigator.clipboard.writeText(code);
            } else {
                const temporaryInput = document.createElement('input');
                temporaryInput.value = code;
                document.body.appendChild(temporaryInput);
                temporaryInput.select();
                document.execCommand('copy');
                temporaryInput.remove();
            }

            const originalLabel = button.textContent;
            button.textContent = 'Đã sao chép';
            button.classList.add('is-copied');
            window.setTimeout(() => {
                button.textContent = originalLabel;
                button.classList.remove('is-copied');
            }, 1800);
            showToast('Đã sao chép mã ưu đãi');
        } catch {
            showToast('Không thể sao chép mã. Vui lòng thử lại.');
        }
    };

    const focusableSelector = [
        'a[href]',
        'button:not([disabled])',
        'input:not([disabled])',
        'select:not([disabled])',
        'textarea:not([disabled])',
        '[tabindex]:not([tabindex="-1"])'
    ].join(',');

    const getFocusableModalItems = () => modal ? Array.from(modal.querySelectorAll(focusableSelector)) : [];

    const openModal = (id, trigger) => {
        const offer = offersById.get(id);
        if (!offer || !modal) {
            return;
        }

        modalOfferId = id;
        lastFocusedElement = trigger || document.activeElement;

        modalValue.textContent = offer.DisplayValue || '';
        modalBadge.textContent = offer.Badge || offer.StatusLabel || '';
        modalTitle.textContent = offer.Title || '';
        modalDescription.textContent = offer.Description || offer.Summary || '';
        modalCode.textContent = getOfferCode(offer);
        modalDate.textContent = `${offer.StartDateLabel || formatDate(offer.StartDate)} - ${offer.EndDate ? formatDate(offer.EndDate) : 'Không thời hạn'}`;
        modalUsers.textContent = offer.IsMemberOnly ? 'Thành viên COSMOS' : 'Tất cả khách hàng COSMOS';
        modalMinimum.textContent = formatMoney(offer.MinimumOrder);

        if (modalTerms) {
            modalTerms.innerHTML = '';
            (offer.Terms || ['Không áp dụng đồng thời với ưu đãi khác.']).forEach((term) => {
                const item = document.createElement('li');
                item.textContent = term;
                modalTerms.appendChild(item);
            });
        }

        if (modalUseButton) {
            modalUseButton.disabled = offer.Status !== 'active';
            modalUseButton.textContent = offer.Status === 'active' ? 'Dùng ưu đãi ngay' : offer.StatusLabel;
        }

        modal.hidden = false;
        document.body.classList.add('offer-modal-open');
        window.setTimeout(() => {
            const focusable = getFocusableModalItems();
            (focusable[0] || modalDialog)?.focus?.();
        }, 0);
    };

    const closeModal = () => {
        if (!modal || modal.hidden) {
            return;
        }

        modal.hidden = true;
        document.body.classList.remove('offer-modal-open');
        modalOfferId = null;
        lastFocusedElement?.focus?.();
        lastFocusedElement = null;
    };

    const formatDate = (value) => {
        if (!value) {
            return '';
        }

        const date = new Date(value);
        if (Number.isNaN(date.getTime())) {
            return value;
        }

        return date.toLocaleDateString('vi-VN');
    };

    modalUseButton?.addEventListener('click', () => {
        if (modalOfferId) {
            selectOffer(modalOfferId);
            closeModal();
        }
    });

    modal?.addEventListener('click', (event) => {
        if (event.target.closest('[data-close-modal]')) {
            closeModal();
        }
    });

    document.addEventListener('keydown', (event) => {
        if (!modal || modal.hidden) {
            return;
        }

        if (event.key === 'Escape') {
            event.preventDefault();
            closeModal();
            return;
        }

        if (event.key !== 'Tab') {
            return;
        }

        const focusable = getFocusableModalItems();
        if (focusable.length === 0) {
            event.preventDefault();
            return;
        }

        const first = focusable[0];
        const last = focusable[focusable.length - 1];

        if (event.shiftKey && document.activeElement === first) {
            event.preventDefault();
            last.focus();
        } else if (!event.shiftKey && document.activeElement === last) {
            event.preventDefault();
            first.focus();
        }
    });

    const option = (value, text, extra = {}) => {
        const item = document.createElement('option');
        item.value = value;
        item.textContent = text;
        Object.entries(extra).forEach(([key, val]) => {
            item.dataset[key] = val;
        });
        return item;
    };

    const findMovie = () => quickBookingMovies.find((movie) => String(movie.MovieId) === String(quickMovie?.value));

    const updateQuickBookingState = () => {
        if (!quickMovie || !quickDate || !quickShowtime || !quickSubmit) {
            return;
        }

        const movie = findMovie();
        const showtimeOption = quickShowtime.selectedOptions[0];
        const ready = Boolean(movie && quickDate.value && quickShowtime.value);

        quickDate.disabled = !movie;
        quickShowtime.disabled = !movie || !quickDate.value;
        quickSubmit.disabled = !ready;

        if (quickMovieId) {
            quickMovieId.value = movie ? movie.MovieId : '';
        }
        if (quickShowtimeId) {
            quickShowtimeId.value = ready ? quickShowtime.value : '';
        }
        if (quickFormat) {
            quickFormat.value = showtimeOption?.dataset.format || '2D';
        }
        if (offerCodeHidden) {
            offerCodeHidden.value = selectedOfferId ? (offersById.get(selectedOfferId)?.Code || '') : '';
        }
    };

    const populateDates = () => {
        if (!quickDate || !quickShowtime) {
            return;
        }

        const movie = findMovie();
        quickDate.innerHTML = '';
        quickDate.appendChild(option('', 'Chọn ngày'));
        quickShowtime.innerHTML = '';
        quickShowtime.appendChild(option('', 'Chọn suất chiếu'));

        if (!movie) {
            quickDate.disabled = true;
            quickShowtime.disabled = true;
            updateQuickBookingState();
            return;
        }

        const dates = new Map();
        (movie.Showtimes || []).forEach((showtime) => {
            if (!dates.has(showtime.Date)) {
                dates.set(showtime.Date, showtime.DateLabel || formatDate(showtime.Date));
            }
        });

        dates.forEach((label, value) => {
            quickDate.appendChild(option(value, label));
        });

        updateQuickBookingState();
    };

    const populateShowtimes = () => {
        if (!quickShowtime) {
            return;
        }

        const movie = findMovie();
        quickShowtime.innerHTML = '';
        quickShowtime.appendChild(option('', 'Chọn suất chiếu'));

        if (!movie || !quickDate?.value) {
            updateQuickBookingState();
            return;
        }

        (movie.Showtimes || [])
            .filter((showtime) => showtime.Date === quickDate.value)
            .forEach((showtime) => {
                const text = `${showtime.Time} - ${showtime.RoomName} (${showtime.Format})`;
                quickShowtime.appendChild(option(showtime.ShowtimeId, text, { format: showtime.Format || '2D' }));
            });

        updateQuickBookingState();
    };

    quickMovie?.addEventListener('change', populateDates);
    quickDate?.addEventListener('change', populateShowtimes);
    quickShowtime?.addEventListener('change', updateQuickBookingState);

    quickForm?.addEventListener('submit', (event) => {
        updateQuickBookingState();
        if (quickSubmit?.disabled) {
            event.preventDefault();
            showToast('Vui lòng chọn đầy đủ phim, ngày và suất chiếu.');
        }
    });

    codeForm?.addEventListener('submit', async (event) => {
        event.preventDefault();
        if (!codeInput || !codeResult || validationBusy) {
            return;
        }

        const code = codeInput.value.trim().toUpperCase();
        codeInput.value = code;
        validationBusy = true;
        if (codeSubmit) {
            codeSubmit.disabled = true;
            codeSubmit.textContent = 'Đang kiểm tra';
        }

        codeResult.hidden = false;
        codeResult.className = 'code-result';
        codeResult.textContent = 'Đang kiểm tra mã ưu đãi...';

        try {
            const response = await fetch(`/Home/ValidateOfferCode?code=${encodeURIComponent(code)}`, {
                headers: { 'Accept': 'application/json' }
            });

            if (!response.ok) {
                throw new Error('Request failed');
            }

            const result = await response.json();
            codeResult.classList.toggle('is-valid', Boolean(result.isValid || result.IsValid));
            codeResult.classList.toggle('is-invalid', !Boolean(result.isValid || result.IsValid));
            codeResult.textContent = result.message || result.Message || 'Không thể kiểm tra mã ưu đãi.';

            const offer = result.offer || result.Offer;
            const offerId = offer?.id || offer?.Id;
            if ((result.isValid || result.IsValid) && offerId) {
                const useButton = document.createElement('button');
                useButton.type = 'button';
                useButton.textContent = 'Dùng mã và đặt vé';
                useButton.addEventListener('click', () => {
                    selectOffer(offerId);
                    quickMovie?.focus();
                });
                codeResult.appendChild(useButton);
            }
        } catch {
            codeResult.classList.add('is-invalid');
            codeResult.textContent = 'Không thể kiểm tra mã lúc này. Vui lòng thử lại sau.';
        } finally {
            validationBusy = false;
            if (codeSubmit) {
                codeSubmit.disabled = false;
                codeSubmit.textContent = 'Kiểm tra';
            }
        }
    });

    applyFilters();
    updateSelectedPanel();
    updateQuickBookingState();
})();
