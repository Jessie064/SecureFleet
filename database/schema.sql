-- SecureFleet — Supabase schema
-- Run this in the Supabase SQL editor on a fresh project.
-- Tables, RLS policies, helper functions, and demo seed data.

-- ---------------------------------------------------------------------------
-- Extensions
-- ---------------------------------------------------------------------------
create extension if not exists "pgcrypto";

-- ---------------------------------------------------------------------------
-- profiles  (1:1 with auth.users, holds role + display info)
-- ---------------------------------------------------------------------------
create table if not exists public.profiles (
    id          uuid primary key default gen_random_uuid(),
    email       text not null unique,
    full_name   text not null,
    role        text not null check (role in ('admin', 'manager', 'driver')),
    phone       text,
    created_at  timestamptz not null default now()
);

create index if not exists profiles_role_idx on public.profiles (role);

-- Helper: read the role of the currently authenticated user.
-- SECURITY DEFINER so RLS policies on `profiles` don't recurse when checked.
create or replace function public.current_role_name()
returns text
language sql
stable
security definer
set search_path = public
as $$
    select role from public.profiles where id = auth.uid()
$$;

-- ---------------------------------------------------------------------------
-- vehicles
-- ---------------------------------------------------------------------------
create table if not exists public.vehicles (
    id                    uuid primary key default gen_random_uuid(),
    plate                 text not null unique,
    make                  text,
    model                 text,
    year                  int,
    fuel_type             text not null check (fuel_type in ('diesel', 'gasoline', 'electric', 'hybrid')),
    fuel_efficiency_kmpl  numeric(6,2) not null check (fuel_efficiency_kmpl > 0),
    status                text not null default 'idle' check (status in ('active', 'idle', 'maintenance', 'offline')),
    driver_id             uuid references public.profiles(id) on delete set null,
    current_lat           numeric(9,6),
    current_lng           numeric(9,6),
    last_update           timestamptz default now(),
    created_at            timestamptz not null default now()
);

create index if not exists vehicles_driver_id_idx on public.vehicles (driver_id);
create index if not exists vehicles_status_idx    on public.vehicles (status);

-- ---------------------------------------------------------------------------
-- routes
-- ---------------------------------------------------------------------------
create table if not exists public.routes (
    id           uuid primary key default gen_random_uuid(),
    name         text not null,
    origin       text not null,
    origin_lat   numeric(9,6) not null,
    origin_lng   numeric(9,6) not null,
    destination  text not null,
    dest_lat     numeric(9,6) not null,
    dest_lng     numeric(9,6) not null,
    distance_km  numeric(8,2) not null check (distance_km >= 0),
    created_at   timestamptz not null default now()
);

-- ---------------------------------------------------------------------------
-- fuel_prices
-- ---------------------------------------------------------------------------
create table if not exists public.fuel_prices (
    fuel_type        text primary key check (fuel_type in ('diesel', 'gasoline', 'electric', 'hybrid')),
    price_per_liter  numeric(8,3) not null check (price_per_liter >= 0),
    currency         text not null default 'PHP',
    updated_at       timestamptz not null default now()
);

-- ---------------------------------------------------------------------------
-- trips
-- ---------------------------------------------------------------------------
create table if not exists public.trips (
    id                   uuid primary key default gen_random_uuid(),
    vehicle_id           uuid not null references public.vehicles(id) on delete cascade,
    route_id             uuid references public.routes(id) on delete set null,
    driver_id            uuid references public.profiles(id) on delete set null,
    status               text not null default 'planned' check (status in ('planned', 'in_progress', 'completed', 'cancelled')),
    estimated_liters     numeric(8,2),
    estimated_fuel_cost  numeric(10,2),
    started_at           timestamptz,
    completed_at         timestamptz,
    created_at           timestamptz not null default now()
);

create index if not exists trips_vehicle_id_idx on public.trips (vehicle_id);
create index if not exists trips_driver_id_idx  on public.trips (driver_id);
create index if not exists trips_status_idx     on public.trips (status);

-- ---------------------------------------------------------------------------
-- audit_log  (privileged actions recorded server-side)
-- ---------------------------------------------------------------------------
create table if not exists public.audit_log (
    id          bigserial primary key,
    actor_id    uuid references public.profiles(id) on delete set null,
    actor_email text,
    action      text not null,
    entity      text,
    entity_id   text,
    metadata    jsonb,
    created_at  timestamptz not null default now()
);

create index if not exists audit_log_actor_id_idx on public.audit_log (actor_id);
create index if not exists audit_log_action_idx   on public.audit_log (action);

-- ---------------------------------------------------------------------------
-- Row Level Security
-- ---------------------------------------------------------------------------
alter table public.profiles    enable row level security;
alter table public.vehicles    enable row level security;
alter table public.routes      enable row level security;
alter table public.fuel_prices enable row level security;
alter table public.trips       enable row level security;
alter table public.audit_log   enable row level security;

-- profiles ------------------------------------------------------------------
drop policy if exists profiles_self_read on public.profiles;
create policy profiles_self_read on public.profiles
    for select using (
        auth.uid() = id
        or public.current_role_name() in ('admin', 'manager')
    );

drop policy if exists profiles_admin_write on public.profiles;
create policy profiles_admin_write on public.profiles
    for all using (public.current_role_name() = 'admin')
    with check (public.current_role_name() = 'admin');

-- vehicles ------------------------------------------------------------------
drop policy if exists vehicles_read_all on public.vehicles;
create policy vehicles_read_all on public.vehicles
    for select using (auth.uid() is not null);

drop policy if exists vehicles_manager_insert on public.vehicles;
create policy vehicles_manager_insert on public.vehicles
    for insert with check (public.current_role_name() in ('admin', 'manager'));

drop policy if exists vehicles_manager_update on public.vehicles;
create policy vehicles_manager_update on public.vehicles
    for update using (
        public.current_role_name() in ('admin', 'manager')
        or driver_id = auth.uid()
    )
    with check (
        public.current_role_name() in ('admin', 'manager')
        or driver_id = auth.uid()
    );

drop policy if exists vehicles_admin_delete on public.vehicles;
create policy vehicles_admin_delete on public.vehicles
    for delete using (public.current_role_name() = 'admin');

-- routes --------------------------------------------------------------------
drop policy if exists routes_read_all on public.routes;
create policy routes_read_all on public.routes
    for select using (auth.uid() is not null);

drop policy if exists routes_manager_write on public.routes;
create policy routes_manager_write on public.routes
    for all using (public.current_role_name() in ('admin', 'manager'))
    with check (public.current_role_name() in ('admin', 'manager'));

-- fuel_prices ---------------------------------------------------------------
drop policy if exists fuel_prices_read_all on public.fuel_prices;
create policy fuel_prices_read_all on public.fuel_prices
    for select using (auth.uid() is not null);

drop policy if exists fuel_prices_admin_write on public.fuel_prices;
create policy fuel_prices_admin_write on public.fuel_prices
    for all using (public.current_role_name() = 'admin')
    with check (public.current_role_name() = 'admin');

-- trips ---------------------------------------------------------------------
drop policy if exists trips_read_scope on public.trips;
create policy trips_read_scope on public.trips
    for select using (
        public.current_role_name() in ('admin', 'manager')
        or driver_id = auth.uid()
    );

drop policy if exists trips_manager_insert on public.trips;
create policy trips_manager_insert on public.trips
    for insert with check (public.current_role_name() in ('admin', 'manager'));

drop policy if exists trips_update_scope on public.trips;
create policy trips_update_scope on public.trips
    for update using (
        public.current_role_name() in ('admin', 'manager')
        or driver_id = auth.uid()
    )
    with check (
        public.current_role_name() in ('admin', 'manager')
        or driver_id = auth.uid()
    );

-- audit_log -----------------------------------------------------------------
drop policy if exists audit_log_admin_read on public.audit_log;
create policy audit_log_admin_read on public.audit_log
    for select using (public.current_role_name() = 'admin');

-- ---------------------------------------------------------------------------
-- Seed data (matches InMemoryDataStore demo defaults)
-- ---------------------------------------------------------------------------
insert into public.fuel_prices (fuel_type, price_per_liter, currency) values
    ('diesel',   1.45, 'PHP'),
    ('gasoline', 1.60, 'PHP')
on conflict (fuel_type) do nothing;

insert into public.routes (name, origin, origin_lat, origin_lng, destination, dest_lat, dest_lng, distance_km) values
    ('Cebu Port to IT Park',           'Cebu Port',          10.300000, 123.920000, 'IT Park Lahug',        10.330300, 123.905900, 3.6),
    ('Ayala Center to Mactan Airport', 'Ayala Center Cebu',  10.318100, 123.905500, 'Mactan-Cebu Airport',  10.307600, 123.979000, 8.9),
    ('North Bus to South Bus',         'North Bus Terminal', 10.343600, 123.922900, 'South Bus Terminal',   10.293700, 123.894800, 6.1)
on conflict do nothing;

insert into public.vehicles (plate, make, model, year, fuel_type, fuel_efficiency_kmpl, status, current_lat, current_lng) values
    ('FLT-001', 'Ford',     'Transit',  2022, 'diesel',    11.5, 'active',      10.300000, 123.920000),
    ('FLT-002', 'Mercedes', 'Sprinter', 2023, 'diesel',    10.8, 'active',      10.318100, 123.905500),
    ('FLT-003', 'Toyota',   'Hiace',    2021, 'gasoline',   9.4, 'idle',        10.330300, 123.905900),
    ('FLT-004', 'Volvo',    'FH16',     2020, 'diesel',     4.2, 'maintenance', 10.323100, 123.922500)
on conflict (plate) do nothing;

insert into public.profiles (email, full_name, role) values
    ('admin@securefleet.io', 'Bootstrap Admin', 'admin')
on conflict (email) do nothing;
