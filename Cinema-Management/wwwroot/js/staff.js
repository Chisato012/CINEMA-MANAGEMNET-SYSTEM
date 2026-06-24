(function () {
    const roomSelect = document.getElementById('screeningRoomSelect');
    const roomButtons = Array.from(document.querySelectorAll('[data-screening-room-button]'));
    const roomTitle = document.getElementById('screeningRoomTitle');
    const showtimeSelect = document.getElementById('screeningShowtimeSelect');
    const message = document.getElementById('screeningSeatMessage');
    const map = document.getElementById('screeningSeatMap');
    const grid = document.getElementById('screeningSeatGrid');
    const labelsLeft = document.getElementById('screeningRowLabelsLeft');
    const labelsRight = document.getElementById('screeningRowLabelsRight');
    const statValues = document.querySelectorAll('[data-seat-count]');
    const reservationPanels = Array.from(document.querySelectorAll('[data-room-reservations]'));

    if ((!roomSelect && roomButtons.length === 0) || !showtimeSelect || !message || !map || !grid || !labelsLeft || !labelsRight) {
        return;
    }

    let requestId = 0;
    let activeRoomId = getInitialRoomId();

    function getInitialRoomId() {
        if (roomSelect && roomSelect.value) {
            return roomSelect.value;
        }

        const activeButton = roomButtons.find(button => button.classList.contains('active'));
        return activeButton ? activeButton.dataset.roomId : roomButtons[0]?.dataset.roomId || '';
    }

    function getRoomName(roomId) {
        if (roomSelect) {
            const selectedOption = Array.from(roomSelect.options).find(option => option.value === roomId);
            return selectedOption ? selectedOption.textContent : '';
        }

        const button = roomButtons.find(item => item.dataset.roomId === roomId);
        return button ? button.dataset.roomName : '';
    }

    function setActiveRoom(roomId) {
        activeRoomId = roomId;

        if (roomSelect && roomSelect.value !== roomId) {
            roomSelect.value = roomId;
        }

        roomButtons.forEach(button => {
            button.classList.toggle('active', button.dataset.roomId === roomId);
        });

        reservationPanels.forEach(panel => {
            panel.classList.toggle('active', panel.dataset.roomReservations === roomId);
        });

        if (roomTitle) {
            roomTitle.textContent = getRoomName(roomId) || 'Screening room';
        }
    }

    function setMessage(text, show = true) {
        message.textContent = text;
        message.hidden = !show;
    }

    function resetCounts() {
        statValues.forEach(item => {
            item.textContent = '0';
        });
    }

    function updateCounts(counts) {
        statValues.forEach(item => {
            const key = item.dataset.seatCount;
            item.textContent = counts && counts[key] !== undefined ? counts[key] : '0';
        });
    }

    function clearSeatMap() {
        grid.innerHTML = '';
        labelsLeft.innerHTML = '';
        labelsRight.innerHTML = '';
        map.hidden = true;
    }

    function populateShowtimes(showtimes, selectedShowtimeId) {
        showtimeSelect.innerHTML = '';

        if (!showtimes || showtimes.length === 0) {
            const option = document.createElement('option');
            option.value = '';
            option.textContent = 'No showtimes';
            showtimeSelect.appendChild(option);
            showtimeSelect.disabled = true;
            return;
        }

        showtimes.forEach(showtime => {
            const option = document.createElement('option');
            option.value = showtime.showtimeID;
            option.textContent = showtime.label;
            option.selected = showtime.showtimeID === selectedShowtimeId;
            showtimeSelect.appendChild(option);
        });

        showtimeSelect.disabled = false;
    }

    function getSeatClasses(seat) {
        const classes = ['screening-seat'];

        if (seat.isPlaceholder) {
            classes.push('is-placeholder');
            return classes.join(' ');
        }

        if (seat.seatType === 'VIP') {
            classes.push('seat-vip');
        } else if (seat.seatType === 'Couple') {
            classes.push('seat-couple');
        } else {
            classes.push('seat-regular');
        }

        if (seat.reservationState === 'reserved') {
            classes.push('is-reserved');
        } else if (seat.reservationState === 'pending') {
            classes.push('is-pending');
        }

        return classes.join(' ');
    }

    function renderSeatMap(rows) {
        clearSeatMap();

        if (!rows || rows.length === 0) {
            setMessage('This room has no seats in the database.');
            return;
        }

        const maxColumns = rows.reduce((max, row) => {
            const width = row.seats.reduce((sum, seat) => sum + (seat.columnSpan || 1), 0);
            return Math.max(max, width);
        }, 0);

        grid.style.setProperty('--screening-seat-columns', String(Math.max(maxColumns, 1)));

        rows.forEach(row => {
            const leftLabel = document.createElement('span');
            const rightLabel = document.createElement('span');
            const seatRow = document.createElement('div');

            leftLabel.textContent = row.rowLabel;
            rightLabel.textContent = row.rowLabel;
            seatRow.className = 'screening-seat-row';

            row.seats.forEach(seat => {
                const seatCell = document.createElement('span');
                seatCell.className = getSeatClasses(seat);
                seatCell.style.gridColumn = `span ${seat.columnSpan || 1}`;

                if (!seat.isPlaceholder) {
                    seatCell.textContent = seat.seatNumber || seat.seatCode;
                    seatCell.title = `${seat.seatCode} - ${seat.seatType} - ${seat.reservationState}`;
                }

                seatRow.appendChild(seatCell);
            });

            labelsLeft.appendChild(leftLabel);
            grid.appendChild(seatRow);
            labelsRight.appendChild(rightLabel);
        });

        map.hidden = false;
        setMessage('', false);
    }

    async function loadScreeningRoomState(showtimeId) {
        const roomId = activeRoomId;

        if (!roomId) {
            clearSeatMap();
            resetCounts();
            populateShowtimes([], null);
            setMessage('No rooms found in the database.');
            return;
        }

        const currentRequest = ++requestId;
        const url = new URL('/Staff/GetScreeningRoomState', window.location.origin);
        url.searchParams.set('roomId', roomId);

        if (showtimeId) {
            url.searchParams.set('showtimeId', showtimeId);
        }

        setMessage('Loading seats...');

        try {
            const response = await fetch(url);
            if (!response.ok) {
                throw new Error('Request failed');
            }

            const state = await response.json();
            if (currentRequest !== requestId) {
                return;
            }

            populateShowtimes(state.showtimes, state.selectedShowtimeID);
            updateCounts(state.counts);
            renderSeatMap(state.rows);

            if (!state.showtimes || state.showtimes.length === 0) {
                setMessage('This room has no showtimes. Showing base seats only.');
            }
        } catch (error) {
            clearSeatMap();
            resetCounts();
            setMessage('Could not load room seats from the database.');
        }
    }

    if (roomSelect) {
        roomSelect.addEventListener('change', () => {
            setActiveRoom(roomSelect.value);
            loadScreeningRoomState();
        });
    }

    roomButtons.forEach(button => {
        button.addEventListener('click', () => {
            setActiveRoom(button.dataset.roomId);
            loadScreeningRoomState();
        });
    });

    showtimeSelect.addEventListener('change', () => loadScreeningRoomState(showtimeSelect.value));

    setActiveRoom(activeRoomId);
    loadScreeningRoomState();
})();
