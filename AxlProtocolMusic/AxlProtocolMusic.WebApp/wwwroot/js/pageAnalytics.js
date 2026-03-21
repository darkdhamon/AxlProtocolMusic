let currentPage = null;
let pageStart = null;
const privacyPreferenceStorageKey = "axl-privacy-preferences";
const approximateLocationKey = "axl-approximate-location";

function normalizePath(urlOrPath) {
    try {
        return new URL(urlOrPath, window.location.origin).pathname || "/";
    } catch {
        return urlOrPath || "/";
    }
}

function postJson(url, payload, useBeacon) {
    const json = JSON.stringify(payload);

    if (useBeacon && navigator.sendBeacon) {
        navigator.sendBeacon(url, new Blob([json], { type: "application/json" }));
        return;
    }

    fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: json,
        keepalive: true
    });
}

function postVisit(pagePath, pageTitle, durationSeconds, useBeacon) {
    if (!pagePath || durationSeconds < 0.5) {
        return;
    }

    const approximateLocation = getApproximateLocation();

    postJson("/analytics/page-visit", {
        pagePath,
        pageTitle: pageTitle || document.title || "",
        durationSeconds,
        referrerPath: document.referrer ? normalizePath(document.referrer) : "",
        approximateLocation: approximateLocation?.displayRegion || "",
        approximateLatitude: approximateLocation?.approximateLatitude ?? null,
        approximateLongitude: approximateLocation?.approximateLongitude ?? null
    }, useBeacon);
}

function flushCurrent(useBeacon) {
    if (!currentPage || !pageStart) {
        return;
    }

    const durationSeconds = (Date.now() - pageStart) / 1000;
    postVisit(currentPage.path, currentPage.title, durationSeconds, useBeacon);
}

function isExternalLink(anchor) {
    try {
        const destination = new URL(anchor.href, window.location.origin);
        return destination.origin !== window.location.origin;
    } catch {
        return false;
    }
}

function handleDocumentClick(event) {
    const anchor = event.target instanceof Element
        ? event.target.closest("a[href]")
        : null;

    if (!anchor || !isExternalLink(anchor)) {
        return;
    }

    const approximateLocation = getApproximateLocation();

    postJson("/analytics/external-link-click", {
        sourcePagePath: currentPage?.path || window.location.pathname || "/",
        destinationUrl: anchor.href,
        linkLabel: (anchor.textContent || "").trim(),
        approximateLocation: approximateLocation?.displayRegion || "",
        approximateLatitude: approximateLocation?.approximateLatitude ?? null,
        approximateLongitude: approximateLocation?.approximateLongitude ?? null
    }, true);
}

export function startTracking(uri) {
    currentPage = {
        path: normalizePath(uri),
        title: document.title || ""
    };

    pageStart = Date.now();

    window.addEventListener("pagehide", () => flushCurrent(true), { once: true });
    document.addEventListener("click", handleDocumentClick, true);
}

export function trackNavigation(uri) {
    flushCurrent(false);
    currentPage = {
        path: normalizePath(uri),
        title: ""
    };
    pageStart = Date.now();
}

export function syncCurrentTitle() {
    if (!currentPage) {
        return;
    }

    currentPage.title = document.title || "";
}

function getApproximateLocation() {
    try {
        const rawPreferences = window.localStorage.getItem(privacyPreferenceStorageKey);
        if (!rawPreferences) {
            return null;
        }

        const preferences = JSON.parse(rawPreferences);
        if (!preferences.shareApproximateLocation) {
            return null;
        }

        const rawLocation = window.localStorage.getItem(approximateLocationKey);
        if (!rawLocation) {
            return null;
        }

        if (/^Approximate area\s/i.test(rawLocation)) {
            window.localStorage.removeItem(approximateLocationKey);
            return null;
        }

        try {
            return JSON.parse(rawLocation);
        } catch {
            const migratedLocation = {
                displayRegion: rawLocation,
                approximateLatitude: null,
                approximateLongitude: null
            };

            window.localStorage.setItem(approximateLocationKey, JSON.stringify(migratedLocation));
            return migratedLocation;
        }
    } catch {
        return null;
    }
}
