Auth.requireRole(["admin"]);

const fmtUSD = n => `₱${(+n).toFixed(2)}`;

let map;

async function loadAll() {
  const [vehicles, routes, fuelPrices, users] = await Promise.all([
    Api.getVehicles(),
    Api.getRoutes(),
    Api.getFuelPrices(),
    Api.getUsers().catch(() => [])
  ]);

  document.getElementById("stat-vehicles").textContent = vehicles.length;
  document.getElementById("stat-active").textContent = vehicles.filter(v => v.status === "active").length;
  document.getElementById("stat-routes").textContent = routes.length;

  // forecast spend = sum of estimating route 0 for each active vehicle
  let spend = 0;
  for (const v of vehicles.filter(x => x.status === "active")) {
    const est = await Api.estimate(v.id, routes[0]?.id);
    if (est) spend += est.totalCost;
  }
  document.getElementById("stat-spend").textContent = fmtUSD(spend);

  renderTable(vehicles);
  renderFuel(fuelPrices);
  renderUsers(users);
  SFMap.renderVehicles(map, vehicles);
  document.getElementById("map-status").textContent = `${vehicles.length} vehicles tracked`;
}

function renderFuel(prices) {
  const tbody = document.getElementById("fuel-tbody");
  tbody.innerHTML = prices.map(p => `
    <tr>
      <td><strong>${p.fuelType}</strong></td>
      <td><input type="number" step="0.01" min="0" value="${p.pricePerLiter}" data-fuel="${p.fuelType}" class="fuel-input" /></td>
      <td>${p.currency}</td>
      <td><button class="btn btn-primary btn-sm" data-save="${p.fuelType}">Save</button></td>
    </tr>
  `).join("");

  tbody.querySelectorAll("[data-save]").forEach(btn => {
    btn.addEventListener("click", async () => {
      const fuelType = btn.dataset.save;
      const input = tbody.querySelector(`input[data-fuel="${fuelType}"]`);
      const price = parseFloat(input.value);
      if (isNaN(price) || price < 0) return alert("Enter a valid price");
      try {
        await Api.updateFuelPrice(fuelType, price);
        loadAll();
      } catch (err) {
        alert(`Failed to update fuel price: ${err.message}`);
      }
    });
  });
}

function renderTable(vehicles) {
  const tbody = document.getElementById("vehicles-tbody");
  tbody.innerHTML = vehicles.map(v => `
    <tr>
      <td><strong>${v.plate}</strong></td>
      <td>${v.make || ""} ${v.model || ""} ${v.year ? `· ${v.year}` : ""}</td>
      <td>${v.fuelType}</td>
      <td>${v.fuelEfficiencyKmpl}</td>
      <td>
        <select data-id="${v.id}" class="status-select">
          ${["idle","active","maintenance","offline"].map(s =>
            `<option value="${s}" ${s === v.status ? "selected" : ""}>${s}</option>`
          ).join("")}
        </select>
      </td>
      <td><button class="btn btn-ghost btn-sm" data-del="${v.id}">Delete</button></td>
    </tr>
  `).join("");

  tbody.querySelectorAll(".status-select").forEach(sel => {
    sel.addEventListener("change", async (e) => {
      await Api.updateStatus(e.target.dataset.id, e.target.value);
      loadAll();
    });
  });
  tbody.querySelectorAll("[data-del]").forEach(btn => {
    btn.addEventListener("click", async () => {
      if (confirm("Delete vehicle?")) {
        await Api.deleteVehicle(btn.dataset.del);
        loadAll();
      }
    });
  });
}

function renderUsers(users) {
  const tbody = document.getElementById("users-tbody");
  if (!users || !users.length) {
    tbody.innerHTML = `<tr><td colspan="5" class="muted">No users yet — click "+ Add user" to register a manager or driver.</td></tr>`;
    return;
  }
  const rolePill = r => r === "admin" ? "red" : r === "manager" ? "amber" : "green";
  tbody.innerHTML = users.map(u => `
    <tr>
      <td><strong>${u.email}</strong></td>
      <td>${u.fullName || ""}</td>
      <td><span class="pill pill-${rolePill(u.role)}">${u.role}</span></td>
      <td class="muted small">${u.phone || "—"}</td>
      <td>${u.role === "admin" ? "" : `<button class="btn btn-ghost btn-sm" data-del-user="${u.id}">Delete</button>`}</td>
    </tr>
  `).join("");

  tbody.querySelectorAll("[data-del-user]").forEach(btn => {
    btn.addEventListener("click", async () => {
      if (!confirm("Delete this user?")) return;
      try {
        await Api.deleteUser(btn.dataset.delUser);
        loadAll();
      } catch (err) {
        alert(`Failed to delete user: ${err.message}`);
      }
    });
  });
}

// Add user dialog
const userDlg = document.getElementById("user-dialog");
document.getElementById("add-user").addEventListener("click", () => userDlg.showModal());
document.getElementById("cancel-user").addEventListener("click", () => userDlg.close());
document.getElementById("user-form").addEventListener("submit", async (e) => {
  e.preventDefault();
  const fd = new FormData(e.target);
  try {
    await Api.createUser({
      email: fd.get("email"),
      fullName: fd.get("fullName"),
      role: fd.get("role"),
      phone: fd.get("phone") || null
    });
    e.target.reset();
    userDlg.close();
    loadAll();
  } catch (err) {
    alert(`Failed to create user: ${err.message}`);
  }
});

// Add vehicle dialog
const dlg = document.getElementById("vehicle-dialog");
document.getElementById("add-vehicle").addEventListener("click", () => dlg.showModal());
document.getElementById("cancel-vehicle").addEventListener("click", () => dlg.close());
document.getElementById("vehicle-form").addEventListener("submit", async (e) => {
  e.preventDefault();
  const fd = new FormData(e.target);
  await Api.createVehicle({
    plate: fd.get("plate"),
    make: fd.get("make"),
    model: fd.get("model"),
    year: fd.get("year") ? parseInt(fd.get("year")) : null,
    fuelType: fd.get("fuelType"),
    fuelEfficiencyKmpl: parseFloat(fd.get("fuelEfficiencyKmpl"))
  });
  e.target.reset();
  dlg.close();
  loadAll();
});

const VIEW_SECTIONS = {
  overview: ["overview-stats", "overview-map"],
  vehicles: ["vehicles"],
  fuel: ["fuel"],
  users: ["users"],
  audit: ["audit"]
};
const ALL_SECTION_IDS = [...new Set(Object.values(VIEW_SECTIONS).flat())];

function showView(view) {
  const visibleIds = VIEW_SECTIONS[view] || VIEW_SECTIONS.overview;
  ALL_SECTION_IDS.forEach(id => {
    const el = document.getElementById(id);
    if (el) el.style.display = visibleIds.includes(id) ? "" : "none";
  });
  document.querySelectorAll(".sidenav a").forEach(a => {
    a.classList.toggle("active", a.dataset.view === view);
  });
  if (visibleIds.includes("overview-map") && map) {
    setTimeout(() => map.invalidateSize(), 50);
  }
}

function currentView() {
  const hash = (location.hash || "").replace("#", "");
  return VIEW_SECTIONS[hash] ? hash : "overview";
}

window.addEventListener("hashchange", () => showView(currentView()));

document.addEventListener("DOMContentLoaded", () => {
  map = SFMap.init("map");
  showView(currentView());
  loadAll();
  setInterval(loadAll, 30000);
});
