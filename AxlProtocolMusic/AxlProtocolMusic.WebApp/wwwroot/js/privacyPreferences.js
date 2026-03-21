const storageKey = "axl-privacy-preferences";
const metricsCookieName = "axl_site_metrics";
const approximateLocationKey = "axl-approximate-location";

function getCookieValue(name) {
    const cookie = document.cookie
        .split("; ")
        .find(entry => entry.startsWith(`${name}=`));

    return cookie ? decodeURIComponent(cookie.split("=")[1]) : null;
}

export function getPreferences() {
    const raw = window.localStorage.getItem(storageKey);
    const metricsDisabled = getCookieValue(metricsCookieName) === "disabled";
    const defaults = {
        allowEssentialSiteMetrics: !metricsDisabled,
        shareApproximateLocation: false,
        allowEnhancedEngagementMetrics: false,
        allowPersonalizationMetrics: false
    };

    if (!raw) {
        return defaults;
    }

    try {
        const parsed = JSON.parse(raw);
        return {
            allowEssentialSiteMetrics: !metricsDisabled,
            shareApproximateLocation: !!parsed.shareApproximateLocation,
            allowEnhancedEngagementMetrics: !!parsed.allowEnhancedEngagementMetrics,
            allowPersonalizationMetrics: !!parsed.allowPersonalizationMetrics
        };
    } catch {
        return defaults;
    }
}

export async function savePreferences(preferences) {
    const result = await syncApproximateLocationPreference(preferences);

    await fetch("/privacy/essential-metrics", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            allowEssentialSiteMetrics: !!preferences.allowEssentialSiteMetrics
        }),
        credentials: "same-origin"
    });

    return {
        preferences: {
            allowEssentialSiteMetrics: !!preferences.allowEssentialSiteMetrics,
            shareApproximateLocation: !!result.preferences.shareApproximateLocation,
            allowEnhancedEngagementMetrics: !!preferences.allowEnhancedEngagementMetrics,
            allowPersonalizationMetrics: !!preferences.allowPersonalizationMetrics
        },
        locationPermissionDenied: !!result.locationPermissionDenied
    };
}

export function getApproximateLocation() {
    const storedValue = window.localStorage.getItem(approximateLocationKey) || "";
    if (isLegacyApproximateLocation(storedValue)) {
        window.localStorage.removeItem(approximateLocationKey);
        return null;
    }

    const parsed = parseApproximateLocationValue(storedValue);
    if (parsed) {
        window.localStorage.setItem(approximateLocationKey, JSON.stringify(parsed));
    }

    return parsed;
}

export async function syncApproximateLocationPreference(preferences) {
    let shareApproximateLocation = !!preferences.shareApproximateLocation;
    let locationPermissionDenied = false;

    if (shareApproximateLocation) {
        let approximateLocation = getApproximateLocation();
        if (!approximateLocation || !hasMappableCoordinates(approximateLocation)) {
            approximateLocation = await requestApproximateLocation();
        }

        if (approximateLocation) {
            window.localStorage.setItem(approximateLocationKey, JSON.stringify(approximateLocation));
        } else {
            window.localStorage.removeItem(approximateLocationKey);
            shareApproximateLocation = false;
            locationPermissionDenied = true;
        }
    } else {
        window.localStorage.removeItem(approximateLocationKey);
    }

    window.localStorage.setItem(storageKey, JSON.stringify({
        shareApproximateLocation,
        allowEnhancedEngagementMetrics: !!preferences.allowEnhancedEngagementMetrics,
        allowPersonalizationMetrics: !!preferences.allowPersonalizationMetrics
    }));

    return {
        preferences: {
            allowEssentialSiteMetrics: !!preferences.allowEssentialSiteMetrics,
            shareApproximateLocation,
            allowEnhancedEngagementMetrics: !!preferences.allowEnhancedEngagementMetrics,
            allowPersonalizationMetrics: !!preferences.allowPersonalizationMetrics
        },
        locationPermissionDenied
    };
}

function isLegacyApproximateLocation(value) {
    return /^Approximate area\s/i.test(value || "");
}

function parseApproximateLocationValue(value) {
    if (!value) {
        return null;
    }

    try {
        const parsed = JSON.parse(value);
        return parsed && typeof parsed === "object"
            ? {
                displayRegion: parsed.displayRegion || "",
                approximateLatitude: typeof parsed.approximateLatitude === "number" ? parsed.approximateLatitude : null,
                approximateLongitude: typeof parsed.approximateLongitude === "number" ? parsed.approximateLongitude : null
            }
            : null;
    } catch {
        return {
            displayRegion: value,
            approximateLatitude: null,
            approximateLongitude: null
        };
    }
}

function hasMappableCoordinates(location) {
    return !!location
        && typeof location.approximateLatitude === "number"
        && typeof location.approximateLongitude === "number";
}

async function requestApproximateLocation() {
    if (!("geolocation" in navigator)) {
        return "";
    }

    const coordinates = await new Promise(resolve => {
        navigator.geolocation.getCurrentPosition(
            position => resolve({
                latitude: position.coords.latitude,
                longitude: position.coords.longitude
            }),
            () => resolve(null),
            {
                enableHighAccuracy: false,
                timeout: 10000,
                maximumAge: 1000 * 60 * 60 * 24
            });
    });

    if (!coordinates) {
        return "";
    }

    try {
        const url = new URL("https://api.bigdatacloud.net/data/reverse-geocode-client");
        url.searchParams.set("latitude", coordinates.latitude.toString());
        url.searchParams.set("longitude", coordinates.longitude.toString());
        url.searchParams.set("localityLanguage", "en");

        const response = await fetch(url.toString(), {
            method: "GET"
        });

        if (!response.ok) {
            return "";
        }

        const payload = await response.json();
        const city = getPreferredMetroArea(payload);
        const state = abbreviateState((payload.principalSubdivision || "").trim());
        const country = abbreviateCountry((payload.countryName || payload.countryCode || "").trim());
        const parts = [city, state, country].filter(Boolean);
        const coarseCoordinates = roundCoordinatesForPrivacy(coordinates.latitude, coordinates.longitude);

        return {
            displayRegion: parts.length > 0 ? parts.join(", ") : "",
            approximateLatitude: coarseCoordinates.latitude,
            approximateLongitude: coarseCoordinates.longitude
        };
    } catch {
        return "";
    }
}

function roundCoordinatesForPrivacy(latitude, longitude) {
    const milesPerLatDegree = 69;
    const gridMiles = 50;
    const latStep = gridMiles / milesPerLatDegree;
    const longitudeDenominator = Math.max(0.35, Math.cos(latitude * (Math.PI / 180)) * milesPerLatDegree);
    const lonStep = gridMiles / longitudeDenominator;

    return {
        latitude: roundToStep(latitude, latStep),
        longitude: roundToStep(longitude, lonStep)
    };
}

function roundToStep(value, step) {
    return Math.round(value / step) * step;
}

function getPreferredMetroArea(payload) {
    const directCandidates = [
        payload.city,
        payload.locality,
        payload.localityInfo?.locality
    ];

    const informativeCandidates = [
        ...(payload.localityInfo?.informative || []),
        ...(payload.localityInfo?.administrative || [])
    ]
        .map(item => typeof item === "string" ? item : (item?.name || item?.description || ""));

    const allCandidates = [...directCandidates, ...informativeCandidates]
        .map(value => normalizePlaceName(value || ""))
        .filter(Boolean);

    const metroCandidate = allCandidates.find(value => !/township/i.test(value) && !/county/i.test(value));
    if (metroCandidate) {
        return metroCandidate;
    }

    return allCandidates[0] || "";
}

function normalizePlaceName(value) {
    return String(value || "")
        .replace(/^Township of\s+/i, "")
        .replace(/\s+Township$/i, "")
        .trim();
}

function abbreviateCountry(value) {
    switch (value) {
        case "United States of America (the)":
        case "United States":
        case "US":
            return "USA";
        case "United Kingdom of Great Britain and Northern Ireland (the)":
        case "United Kingdom":
            return "UK";
        default:
            return value;
    }
}

function abbreviateState(value) {
    const states = {
        Alabama: "AL",
        Alaska: "AK",
        Arizona: "AZ",
        Arkansas: "AR",
        California: "CA",
        Colorado: "CO",
        Connecticut: "CT",
        Delaware: "DE",
        Florida: "FL",
        Georgia: "GA",
        Hawaii: "HI",
        Idaho: "ID",
        Illinois: "IL",
        Indiana: "IN",
        Iowa: "IA",
        Kansas: "KS",
        Kentucky: "KY",
        Louisiana: "LA",
        Maine: "ME",
        Maryland: "MD",
        Massachusetts: "MA",
        Michigan: "MI",
        Minnesota: "MN",
        Mississippi: "MS",
        Missouri: "MO",
        Montana: "MT",
        Nebraska: "NE",
        Nevada: "NV",
        "New Hampshire": "NH",
        "New Jersey": "NJ",
        "New Mexico": "NM",
        "New York": "NY",
        "North Carolina": "NC",
        "North Dakota": "ND",
        Ohio: "OH",
        Oklahoma: "OK",
        Oregon: "OR",
        Pennsylvania: "PA",
        "Rhode Island": "RI",
        "South Carolina": "SC",
        "South Dakota": "SD",
        Tennessee: "TN",
        Texas: "TX",
        Utah: "UT",
        Vermont: "VT",
        Virginia: "VA",
        Washington: "WA",
        "West Virginia": "WV",
        Wisconsin: "WI",
        Wyoming: "WY"
    };

    return states[value] || value;
}
