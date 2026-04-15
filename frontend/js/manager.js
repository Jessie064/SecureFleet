Auth.requireRole(["manager", "admin"]);

const fmt = n => `₱${(+n).toFixed(2)}`;
let map, currentVehicles = [], currentRoutes = [];

async function load() {
  const [vehicles, routes] = await Promise.all([Api.getVehicles(), Api.getRoutes()]);
  currentVehicles = vehicles;
  currentRoutes = routes;

  document.getElementById("stat-vehicles").textContent = vehicles.length;
  document.getElementById("stat-active").textContent = vehicles.filter(v => v.status === "active").length;
  document.getElementById("stat-routes").textContent = routes.length;

  let spend = 0;
  for (const v of vehicles.filter(x => x.status === "active")) {
    const est = await Api.estimate(v.id, routes[0]?.id);
    if (est) spend += est.totalCost;
  }
  document.getElementById("stat-spend").textContent = fmt(spend);

  document.getElementById("vehicles-tbody").innerHTML = vehicles.map(v => `
    <tr>
      <td><strong>${v.plate}</strong> <span class="muted small">${v.make || ""}</span></td>
      <td><span class="pill pill-${v.status === 'active' ? 'green' : v.status === 'idle' ? 'amber' : 'red'}">${v.status}</span></td>
      <td class="muted small">${v.lastUpdate ? new Date(v.lastUpdate).toLocaleTimeString() : "—"}</td>
    </tr>
  `).join("");

  document.getElementById("vehicle-select").innerHTML = vehicles
    .map(v => `<option value="${v.id}">${v.plate} (${v.fuelType})</option>`).join("");
  document.getElementById("route-select").innerHTML = routes
    .map(r => `<option value="${r.id}">${r.name} — ${r.distanceKm} km</option>`).join("");

  SFMap.renderVehicles(map, vehicles);
  document.getElementById("map-status").textContent = `${vehicles.length} vehicles tracked`;
}

document.getElementById("estimate-form").addEventListener("submit", async (e) => {
  e.preventDefault();
  const vehicleId = document.getElementById("vehicle-select").value;
  const routeId = document.getElementById("route-select").value;
  const est = await Api.estimate(vehicleId, routeId);
  if (!est) return;

  document.getElementById("r-distance").textContent = `${est.distanceKm} km`;
  document.getElementById("r-liters").textContent = `${est.litersNeeded} L`;
  document.getElementById("r-price").textContent = `${fmt(est.pricePerLiter)}/L`;
  document.getElementById("r-total").textContent = fmt(est.totalCost);
  document.getElementById("estimate-result").hidden = false;

  const route = currentRoutes.find(r => r.id === routeId);
  if (route) SFMap.drawRoute(map, route);
});

document.getElementById("create-trip").addEventListener("click", async () => {
  const vehicleId = document.getElementById("vehicle-select").value;
  const routeId = document.getElementById("route-select").value;
  await Api.createTrip(vehicleId, routeId);
  alert("Trip created.");
});

document.addEventListener("DOMContentLoaded", () => {
  map = SFMap.init("map");
  load();
  setInterval(load, 30000);
});
