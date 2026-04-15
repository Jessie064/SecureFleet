Auth.requireRole(["driver", "admin", "manager"]);

let map, myVehicle;

async function load() {
  const [vehicles, routes] = await Promise.all([Api.getVehicles(), Api.getRoutes()]);
  myVehicle = vehicles[0];
  const route = routes[0];

  document.getElementById("d-vehicle").textContent = myVehicle ? myVehicle.plate : "—";
  document.getElementById("d-status").textContent = myVehicle ? myVehicle.status : "—";

  if (myVehicle && route) {
    const est = await Api.estimate(myVehicle.id, route.id);
    if (est) {
      document.getElementById("d-fuel").textContent = `${est.litersNeeded} L · ₱${est.totalCost.toFixed(2)}`;
      document.getElementById("route-info").innerHTML = `
        <strong>${route.name}</strong><br/>
        ${route.origin} → ${route.destination}<br/>
        <span class="muted">${route.distanceKm} km · est. ${est.litersNeeded} L of ${myVehicle.fuelType}</span>
      `;
      SFMap.drawRoute(map, route);
    }
  }

  if (myVehicle) SFMap.renderVehicles(map, [myVehicle]);
}

document.getElementById("share-loc").addEventListener("click", () => {
  if (!navigator.geolocation) return alert("Geolocation not supported");
  navigator.geolocation.getCurrentPosition(
    (pos) => {
      if (!myVehicle) return;
      myVehicle.currentLat = pos.coords.latitude;
      myVehicle.currentLng = pos.coords.longitude;
      SFMap.renderVehicles(map, [myVehicle]);
      alert("Location shared.");
    },
    () => alert("Could not get location.")
  );
});

document.addEventListener("DOMContentLoaded", () => {
  map = SFMap.init("map");
  load();
});
