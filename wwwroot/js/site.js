(() => {
    let indicator = document.getElementById('notification-indicator');
    if (!indicator) return;
    const refreshUrl = indicator.dataset.refreshUrl;
    const storageKey = `dive-notifications-seen-${indicator.dataset.userId}`;
    let latestSeen = 0;
    try { latestSeen = Number(sessionStorage.getItem(storageKey)) || 0; } catch { /* Private browsing may block storage. */ }

    function updateAppearance(node) {
        const latest = Number(node.dataset.latestId) || 0;
        if (latest > latestSeen) {
            latestSeen = latest;
            try { sessionStorage.setItem(storageKey, String(latestSeen)); } catch { /* Keep the in-memory value. */ }
            if (!window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
                node.classList.add('notification-shake');
                node.addEventListener('animationend', () => node.classList.remove('notification-shake'), { once: true });
            }
        }
        const applicationAlert = document.getElementById('owner-application-alert');
        if (applicationAlert) {
            const count = Number(node.dataset.pendingApplications) || 0;
            applicationAlert.classList.toggle('alert', count > 0);
            applicationAlert.classList.toggle('alert-danger', count > 0);
            applicationAlert.classList.toggle('card', count === 0);
            applicationAlert.classList.toggle('card-body', count === 0);
            const badge = applicationAlert.querySelector('.badge');
            badge.textContent = String(count);
            badge.classList.toggle('text-bg-danger', count > 0);
            badge.classList.toggle('text-bg-secondary', count === 0);
            applicationAlert.querySelector('p').textContent = count > 0 ? 'New school owners are waiting for your review.' : 'No active school applications awaiting review.';
            const link = applicationAlert.querySelector('a');
            link.classList.toggle('btn-danger', count > 0);
            link.classList.toggle('btn-outline-primary', count === 0);
        }
    }
    updateAppearance(indicator);

    // Refresh a small authenticated MVC partial; no full-page reload is needed.
    let refreshing = false;
    async function refresh() {
        if (document.hidden || refreshing) return;
        refreshing = true;
        const controller = new AbortController();
        const timeout = setTimeout(() => controller.abort(), 10000);
        try {
            const response = await fetch(refreshUrl, { credentials: 'same-origin', cache: 'no-store', signal: controller.signal });
            if (!response.ok || response.redirected) return;
            const html = await response.text();
            const template = document.createElement('template');
            template.innerHTML = html.trim();
            const next = template.content.querySelector('#notification-indicator');
            if (!next || next.dataset.userId !== indicator.dataset.userId) return;
            indicator.replaceWith(next);
            indicator = next;
            updateAppearance(indicator);
            const list = document.getElementById("notification-list");
            if (list && !list.contains(document.activeElement)) {
                const inbox = await fetch(list.dataset.refreshUrl, { credentials: "same-origin", cache: "no-store", signal: controller.signal });
                if (inbox.ok && !inbox.redirected) list.innerHTML = await inbox.text();
            }
        } catch { /* Retry on the next interval after temporary network errors. */ }
        finally { clearTimeout(timeout); refreshing = false; }
    }
    setInterval(refresh, 30000);
    document.addEventListener('visibilitychange', () => { if (!document.hidden) refresh(); });
})();

// An estimate helps plan the trip; reservation prices remain server-authoritative.
(() => {
    const estimate = document.getElementById('reservation-estimate');
    const participants = document.getElementById('Participants');
    const date = document.getElementById('ReservationDate');
    if (!estimate || !participants || !date) return;
    const unitPrice = Number(estimate.dataset.unitPrice);
    const numberFormat = new Intl.NumberFormat(document.documentElement.lang || 'en', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    function updateSummary() {
        const count = Number(participants.value);
        const validCount = participants.value !== '' && participants.validity.valid && Number.isInteger(count) && count > 0;
        document.getElementById('reservation-participants-summary').textContent = validCount ? String(count) : 'Choose participants';
        estimate.textContent = validCount && Number.isFinite(unitPrice) ? numberFormat.format(unitPrice * count) : '—';
        const selectedDate = date.value ? new Date(`${date.value}T12:00:00`) : null;
        document.getElementById('reservation-date-summary').textContent = selectedDate && !Number.isNaN(selectedDate.getTime()) ? selectedDate.toLocaleDateString(document.documentElement.lang || 'en', { day: 'numeric', month: 'short', year: 'numeric' }) : 'Choose a date';
    }
    participants.addEventListener('input', updateSummary);
    date.addEventListener('change', updateSummary);
    updateSummary();
})();
