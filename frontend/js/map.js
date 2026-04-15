// Leaflet helper for SecureFleet maps.

function statusColor(status) {
  return ({
    active: "#10b981",
    idle: "#f59e0b",
    maintenance: "#ef4444",
    offline: "#64748b"
  })[status] || "#4f46e5";
}

function vehicleIcon(status) {
  const color = statusColor(status);
  return L.divIcon({
    className: "vehicle-marker",
    html: `<div style="
      width:18px;height:18px;border-radius:50%;
      background:${color};border:3px solid white;
      box-shadow:0 0 0 2px ${color}33, 0 2px 6px rgba(0,0,0,.25)"></div>`,
    iconSize: [18, 18],
    iconAnchor: [9, 9]
  });
}

const SFMap = {
  init(elementId, center = [10.3157, 123.8854], zoom = 12) {
    const map = L.map(elementId, { zoomControl: true }).setView(center, zoom);
    L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
      maxZoom: 19,
      attribution: "© OpenStreetMap contributors"
    }).addTo(map);
    return map;
  },
  renderVehicles(map, vehicles) {
    if (map._sfLayer) map.removeLayer(map._sfLayer);
    const group = L.featureGroup();
    vehicles.forEach(v => {
      if (v.currentLat == null || v.currentLng == null) return;
      const m = L.marker([v.currentLat, v.currentLng], { icon: vehicleIcon(v.status) });
      m.bindPopup(`
        <strong>${v.plate}</strong><br/>
        ${v.make || ""} ${v.model || ""}<br/>
        Status: <b>${v.status}</b><br/>
        Fuel: ${v.fuelType} · ${v.fuelEfficiencyKmpl} km/L
      `);
      m.addTo(group);
    });
    group.addTo(map);
    map._sfLayer = group;
    if (vehicles.length) {
      try { map.fitBounds(group.getBounds().pad(0.2)); } catch {}
    }
  },
  drawRoute(map, route) {
    if (map._sfRoute) map.removeLayer(map._sfRoute);
    const line = L.polyline(
      [[route.originLat, route.originLng], [route.destLat, route.destLng]],
      { color: "#4f46e5", weight: 4, opacity: 0.7, dashArray: "6,6" }
    );
    line.addTo(map);
    map._sfRoute = line;
  }
};

window.SFMap = SFMap;
