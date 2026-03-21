const maps = new Map();
const ACCURACY_RADIUS_MILES = 25;
const ACCURACY_SOURCE_ID = "privacy-location-accuracy-source";
const ACCURACY_FILL_LAYER_ID = "privacy-location-accuracy-fill";
const ACCURACY_OUTLINE_LAYER_ID = "privacy-location-accuracy-outline";

export function renderPrivacyLocationMap(elementId, locations, styleUrl) {
    const element = document.getElementById(elementId);
    if (!element || !window.maplibregl || !Array.isArray(locations) || locations.length === 0) {
        return;
    }

    let state = maps.get(elementId);
    if (!state) {
        const map = new window.maplibregl.Map({
            container: elementId,
            style: styleUrl || createOpenStreetMapStyle(),
            attributionControl: true,
            projection: "mercator"
        });

        map.addControl(new window.maplibregl.NavigationControl({ showCompass: false }), "top-right");

        state = {
            map,
            markers: [],
            pendingLocations: locations
        };

        map.on("load", () => {
            renderLocations(state);
        });

        maps.set(elementId, state);
    }

    state.pendingLocations = locations;

    if (!state.map.isStyleLoaded()) {
        return;
    }

    renderLocations(state);
}

function renderLocations(state) {
    const locations = Array.isArray(state.pendingLocations) ? state.pendingLocations : [];
    if (locations.length === 0) {
        return;
    }

    for (const marker of state.markers) {
        marker.remove();
    }

    state.markers = [];
    syncAccuracyCircles(state.map, locations);

    const bounds = new window.maplibregl.LngLatBounds();

    for (const location of locations) {
        const markerElement = document.createElement("div");
        markerElement.className = "privacy-location-marker";
        markerElement.innerHTML = `<span>${escapeHtml(String(location.count))}</span>`;
        applyMarkerStyles(markerElement);

        const popup = new window.maplibregl.Popup({ offset: 20 }).setHTML(
            `<strong>${escapeHtml(location.label)}</strong><br/>Events: ${escapeHtml(String(location.count))}`
        );

        const marker = new window.maplibregl.Marker({ element: markerElement })
            .setLngLat([location.longitude, location.latitude])
            .setPopup(popup)
            .addTo(state.map);

        state.markers.push(marker);
        bounds.extend([location.longitude, location.latitude]);
        extendBoundsWithAccuracyRadius(bounds, location);
    }

    state.map.resize();
    requestAnimationFrame(() => focusMap(state.map, locations, bounds));
}

function focusMap(map, locations, bounds) {
    if (locations.length === 1) {
        map.jumpTo({
            center: [locations[0].longitude, locations[0].latitude],
            zoom: 8.5
        });

        return;
    }

    map.fitBounds(bounds, {
        padding: {
            top: 56,
            right: 56,
            bottom: 56,
            left: 56
        },
        maxZoom: 7,
        duration: 800
    });
}

function escapeHtml(value) {
    return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll("\"", "&quot;")
        .replaceAll("'", "&#39;");
}

function createOpenStreetMapStyle() {
    return {
        version: 8,
        sources: {
            "osm-raster-tiles": {
                type: "raster",
                tiles: [
                    "https://tile.openstreetmap.org/{z}/{x}/{y}.png"
                ],
                tileSize: 256,
                attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
            }
        },
        layers: [
            {
                id: "osm-raster-layer",
                type: "raster",
                source: "osm-raster-tiles",
                minzoom: 0,
                maxzoom: 19
            }
        ]
    };
}

function syncAccuracyCircles(map, locations) {
    const accuracyFeatures = locations.map(location => createAccuracyCircleFeature(location));
    const featureCollection = {
        type: "FeatureCollection",
        features: accuracyFeatures
    };

    const existingSource = map.getSource(ACCURACY_SOURCE_ID);
    if (existingSource) {
        existingSource.setData(featureCollection);
        return;
    }

    map.addSource(ACCURACY_SOURCE_ID, {
        type: "geojson",
        data: featureCollection
    });

    map.addLayer({
        id: ACCURACY_FILL_LAYER_ID,
        type: "fill",
        source: ACCURACY_SOURCE_ID,
        paint: {
            "fill-color": "#2f6fd6",
            "fill-opacity": 0.14
        }
    });

    map.addLayer({
        id: ACCURACY_OUTLINE_LAYER_ID,
        type: "line",
        source: ACCURACY_SOURCE_ID,
        paint: {
            "line-color": "#102a69",
            "line-width": 2,
            "line-opacity": 0.5
        }
    });
}

function createAccuracyCircleFeature(location) {
    const coordinates = [];
    const steps = 72;

    for (let step = 0; step <= steps; step += 1) {
        const bearingDegrees = (step / steps) * 360;
        coordinates.push(destinationPoint(location.latitude, location.longitude, ACCURACY_RADIUS_MILES, bearingDegrees));
    }

    return {
        type: "Feature",
        properties: {
            label: location.label,
            count: location.count
        },
        geometry: {
            type: "Polygon",
            coordinates: [coordinates]
        }
    };
}

function destinationPoint(latitude, longitude, distanceMiles, bearingDegrees) {
    const earthRadiusMiles = 3958.7613;
    const angularDistance = distanceMiles / earthRadiusMiles;
    const bearing = toRadians(bearingDegrees);
    const lat1 = toRadians(latitude);
    const lon1 = toRadians(longitude);

    const lat2 = Math.asin(
        Math.sin(lat1) * Math.cos(angularDistance) +
        Math.cos(lat1) * Math.sin(angularDistance) * Math.cos(bearing)
    );

    const lon2 = lon1 + Math.atan2(
        Math.sin(bearing) * Math.sin(angularDistance) * Math.cos(lat1),
        Math.cos(angularDistance) - Math.sin(lat1) * Math.sin(lat2)
    );

    return [normalizeLongitude(toDegrees(lon2)), toDegrees(lat2)];
}

function extendBoundsWithAccuracyRadius(bounds, location) {
    const north = destinationPoint(location.latitude, location.longitude, ACCURACY_RADIUS_MILES, 0);
    const east = destinationPoint(location.latitude, location.longitude, ACCURACY_RADIUS_MILES, 90);
    const south = destinationPoint(location.latitude, location.longitude, ACCURACY_RADIUS_MILES, 180);
    const west = destinationPoint(location.latitude, location.longitude, ACCURACY_RADIUS_MILES, 270);

    bounds.extend(north);
    bounds.extend(east);
    bounds.extend(south);
    bounds.extend(west);
}

function applyMarkerStyles(markerElement) {
    Object.assign(markerElement.style, {
        display: "grid",
        placeItems: "center",
        width: "2.6rem",
        height: "2.6rem",
        borderRadius: "999px",
        border: "2px solid rgba(255, 255, 255, 0.92)",
        background: "radial-gradient(circle at 30% 30%, rgba(243, 181, 84, 0.95), rgba(243, 181, 84, 0.35) 40%, transparent 42%), linear-gradient(180deg, #102a69 0%, #16567d 100%)",
        color: "#ffffff",
        fontSize: "0.95rem",
        fontWeight: "700",
        boxShadow: "0 12px 24px rgba(16, 42, 105, 0.28)",
        cursor: "pointer"
    });

    const label = markerElement.querySelector("span");
    if (label) {
        Object.assign(label.style, {
            display: "inline-block",
            lineHeight: "1"
        });
    }
}

function toRadians(degrees) {
    return degrees * (Math.PI / 180);
}

function toDegrees(radians) {
    return radians * (180 / Math.PI);
}

function normalizeLongitude(longitude) {
    return ((((longitude + 180) % 360) + 360) % 360) - 180;
}
