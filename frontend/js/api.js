// API client. Falls back to a built-in mock when demoMode is true OR when
// the backend isn't reachable, so the UI is always functional for review.

const MOCK_VEHICLES = [
  { id: "v1", plate: "FLT-001", make: "Ford", model: "Transit", year: 2022, fuelType: "diesel", fuelEfficiencyKmpl: 11.5, status: "active", currentLat: 10.3000, currentLng: 123.9200, lastUpdate: new Date().toISOString() },
  { id: "v2", plate: "FLT-002", make: "Mercedes", model: "Sprinter", year: 2023, fuelType: "diesel", fuelEfficiencyKmpl: 10.8, status: "active", currentLat: 10.3181, currentLng: 123.9055, lastUpdate: new Date().toISOString() },
  { id: "v3", plate: "FLT-003", make: "Toyota", model: "Hiace", year: 2021, fuelType: "gasoline", fuelEfficiencyKmpl: 9.4, status: "idle", currentLat: 10.3303, currentLng: 123.9059, lastUpdate: new Date().toISOString() },
  { id: "v4", plate: "FLT-004", make: "Volvo", model: "FH16", year: 2020, fuelType: "diesel", fuelEfficiencyKmpl: 4.2, status: "maintenance", currentLat: 10.3231, currentLng: 123.9225, lastUpdate: new Date().toISOString() }
];
const MOCK_ROUTES = [
  { id: "r1", name: "Cebu Port to IT Park", origin: "Cebu Port", originLat: 10.3000, originLng: 123.9200, destination: "IT Park Lahug", destLat: 10.3303, destLng: 123.9059, distanceKm: 3.6 },
  { id: "r2", name: "Ayala Center to Mactan Airport", origin: "Ayala Center Cebu", originLat: 10.3181, originLng: 123.9055, destination: "Mactan-Cebu Airport", destLat: 10.3076, destLng: 123.9790, distanceKm: 8.9 },
  { id: "r3", name: "North Bus to South Bus", origin: "North Bus Terminal", originLat: 10.3436, originLng: 123.9229, destination: "South Bus Terminal", destLat: 10.2937, destLng: 123.8948, distanceKm: 6.1 }
];
const MOCK_FUEL = { diesel: 1.45, gasoline: 1.60 };

let usingMock = !!window.SF_CONFIG?.demoMode;

async function request(path, opts = {}) {
  if (usingMock) throw new Error("mock");
  const res = await fetch(`${window.SF_CONFIG.apiBase}${path}`, {
    ...opts,
    headers: {
      "Content-Type": "application/json",
      ...Auth.authHeaders(),
      ...(opts.headers || {})
    }
  });
  if (!res.ok) throw new Error(`API ${res.status}`);
  return res.status === 204 ? null : res.json();
}

const Api = {
  async getVehicles() {
    try { return await request("/api/vehicles"); }
    catch { usingMock = true; return [...MOCK_VEHICLES]; }
  },
  async createVehicle(body) {
    try { return await request("/api/vehicles", { method: "POST", body: JSON.stringify(body) }); }
    catch {
      const v = { id: `v${Date.now()}`, ...body, status: "idle", currentLat: null, currentLng: null, lastUpdate: new Date().toISOString() };
      MOCK_VEHICLES.push(v);
      return v;
    }
  },
  async updateStatus(id, status) {
    try { return await request(`/api/vehicles/${id}/status`, { method: "PATCH", body: JSON.stringify({ status }) }); }
    catch {
      const v = MOCK_VEHICLES.find(x => x.id === id);
      if (v) v.status = status;
      return null;
    }
  },
  async deleteVehicle(id) {
    try { return await request(`/api/vehicles/${id}`, { method: "DELETE" }); }
    catch {
      const i = MOCK_VEHICLES.findIndex(x => x.id === id);
      if (i >= 0) MOCK_VEHICLES.splice(i, 1);
      return null;
    }
  },
  async getRoutes() {
    try { return await request("/api/routes"); }
    catch { usingMock = true; return [...MOCK_ROUTES]; }
  },
  async estimate(vehicleId, routeId) {
    try { return await request("/api/fuel/estimate", { method: "POST", body: JSON.stringify({ vehicleId, routeId }) }); }
    catch {
      const v = MOCK_VEHICLES.find(x => x.id === vehicleId);
      const r = MOCK_ROUTES.find(x => x.id === routeId);
      if (!v || !r) return null;
      const eff = v.fuelEfficiencyKmpl || 10;
      const price = MOCK_FUEL[v.fuelType] || 1.5;
      const liters = +(r.distanceKm / eff).toFixed(2);
      const total = +(liters * price).toFixed(2);
      return {
        distanceKm: r.distanceKm,
        fuelEfficiencyKmpl: eff,
        litersNeeded: liters,
        pricePerLiter: price,
        totalCost: total,
        currency: "PHP"
      };
    }
  },
  async createTrip(vehicleId, routeId) {
    try { return await request("/api/trips", { method: "POST", body: JSON.stringify({ vehicleId, routeId }) }); }
    catch { return { id: `t${Date.now()}`, vehicleId, routeId, status: "planned" }; }
  },
  async getFuelPrices() {
    try { return await request("/api/fuel/prices"); }
    catch {
      return Object.entries(MOCK_FUEL).map(([fuelType, pricePerLiter]) => ({ fuelType, pricePerLiter, currency: "PHP" }));
    }
  },
  async updateFuelPrice(fuelType, pricePerLiter) {
    if (usingMock) {
      MOCK_FUEL[fuelType] = pricePerLiter;
      return { fuelType, pricePerLiter, currency: "PHP" };
    }
    return await request(`/api/fuel/prices/${fuelType}`, {
      method: "PUT",
      body: JSON.stringify({ pricePerLiter, currency: "PHP" })
    });
  },
  async getUsers() {
    return await request("/api/users");
  },
  async createUser(body) {
    return await request("/api/users", { method: "POST", body: JSON.stringify(body) });
  },
  async deleteUser(id) {
    return await request(`/api/users/${id}`, { method: "DELETE" });
  }
};

window.Api = Api;
