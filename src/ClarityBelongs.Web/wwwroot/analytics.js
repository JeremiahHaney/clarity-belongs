(() => {
    const sessionKey = "clarity.analytics.session.v1";
    const sessionId = sessionStorage.getItem(sessionKey) || crypto.randomUUID();
    sessionStorage.setItem(sessionKey, sessionId);

    const resolveProductSlug = () => {
        const match = window.location.pathname.match(/^\/products\/([^/]+)\/?/i);
        return match?.[1] || null;
    };

    const send = async (eventType, options = {}) => {
        try {
            await fetch("/api/analytics/event", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                keepalive: true,
                body: JSON.stringify({
                    sessionId,
                    eventType,
                    path: `${window.location.pathname}${window.location.search}`,
                    productSlug: options.productSlug ?? resolveProductSlug(),
                    referrer: document.referrer || null,
                    durationMs: Number.isFinite(options.durationMs)
                        ? Math.max(0, Math.min(86400000, Math.round(options.durationMs)))
                        : null,
                    eventDetail: options.detail || null
                })
            });
        }
        catch {
            // Analytics must never interfere with Clarity.
        }
    };

    let lastPath = "";

    const trackPage = () => {
        const currentPath = `${window.location.pathname}${window.location.search}`;

        if (currentPath === lastPath) {
            return;
        }

        lastPath = currentPath;
        send("PageViewed");

        if (resolveProductSlug()) {
            send("ProductOpened");
        }
    };

    document.addEventListener("click", event => {
        const download = event.target.closest("a[download], a[href*='/downloads/']");

        if (!download) {
            return;
        }

        const detail =
            download.getAttribute("download")
            || download.getAttribute("href")
            || "download";

        send("DownloadClicked", {
            detail
        });
    });

    trackPage();
    document.addEventListener("enhancedload", trackPage);
})();
