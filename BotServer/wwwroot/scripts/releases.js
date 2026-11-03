const card = document.getElementById("release-card");
const status = document.getElementById("release-status");
const cover = document.getElementById("release-cover");
let data;
let index = 0;
let active = false;
let request;
let refreshing = false;
let rotation;
let refresh;
let retry;

function currentWeekStart() {
    const parts = new Intl.DateTimeFormat("en-CA", { timeZone: "Europe/Madrid", year: "numeric", month: "2-digit", day: "2-digit" }).formatToParts(new Date());
    const part = type => parts.find(item => item.type === type).value;
    const day = new Date(`${part("year")}-${part("month")}-${part("day")}T12:00:00Z`);
    day.setUTCDate(day.getUTCDate() - (day.getUTCDay() + 6) % 7);
    return day.toISOString().slice(0, 10);
}

function dateText(date, short = false) {
    return new Intl.DateTimeFormat("es-ES", { timeZone: "UTC", day: "numeric", month: short ? "short" : "long", ...(short ? {} : { year: "numeric" }) })
        .format(new Date(date + "T12:00:00Z"));
}
function setText(id, text) { document.getElementById(id).textContent = text; }
function showStatus(message) { card.hidden = true; status.hidden = false; status.textContent = message; }

function render() {
    if (!data || data.weekStart !== currentWeekStart()) {
        showStatus("Actualizando los lanzamientos de esta semana…");
        return;
    }
    if (data.games.length === 0) { showStatus("No hay lanzamientos con fecha confirmada para esta semana."); return; }
    const game = data.games[index % data.games.length];
    status.hidden = true;
    card.hidden = false;
    setText("release-title", game.title);
    setText("release-art-title", game.title);
    setText("release-description", game.description);
    setText("release-position", `${index + 1} / ${data.games.length}`);
    setText("release-week", `${dateText(data.weekStart, true)} — ${dateText(data.weekEnd, true)} · Madrid`);
    setText("release-price", game.priceLabel || "Por confirmar");
    setText("release-freshness", data.isStale ? "Últimos datos disponibles · actualización pendiente" : "");
    const date = document.getElementById("release-date");
    date.dateTime = game.firstReleaseDate;
    date.textContent = dateText(game.firstReleaseDate);
    document.getElementById("release-platforms").replaceChildren(...game.platforms.map(platform => {
        const badge = document.createElement("span");
        badge.className = "platform";
        badge.textContent = `${platform.name} · ${platform.dates.map(date => dateText(date, true)).join(", ")}`;
        return badge;
    }));
    cover.hidden = true;
    cover.onload = () => { cover.hidden = false; };
    cover.onerror = () => { cover.hidden = true; };
    if (game.imageUrl) cover.src = game.imageUrl;
    else cover.removeAttribute("src");
}

async function load() {
    if (!active || refreshing) return;
    refreshing = true;
    const controller = new AbortController();
    request = controller;
    const timeout = setTimeout(() => controller.abort(), 30000);
    try {
        const response = await fetch("/api/releases/week", { signal: controller.signal, cache: "no-store" });
        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        const next = await response.json();
        if (!Array.isArray(next.games) || !/^\d{4}-\d{2}-\d{2}$/.test(next.weekStart)
            || !/^\d{4}-\d{2}-\d{2}$/.test(next.weekEnd)
            || next.games.some(game => typeof game.title !== "string" || !Array.isArray(game.platforms)
                || !/^\d{4}-\d{2}-\d{2}$/.test(game.firstReleaseDate)
                || game.platforms.some(platform => !Array.isArray(platform.dates)))) throw new Error("Invalid release data");
        if (!active || request !== controller) return;
        const selected = data?.games[index]?.id;
        index = Math.max(0, next.games.findIndex(game => game.id === selected));
        data = next;
        clearTimeout(retry);
        render();
    } catch (error) {
        if (!active || request !== controller) return;
        if (data?.weekStart === currentWeekStart() && data.games.length) {
            data.isStale = true;
            render();
        } else showStatus("No se han podido cargar los lanzamientos. Reintentando…");
        console.warn("No se pudo actualizar el overlay de lanzamientos.");
        clearTimeout(retry);
        retry = setTimeout(() => { void load(); }, 60000);
    } finally {
        clearTimeout(timeout);
        if (request === controller) refreshing = false;
    }
}

function start() {
    if (active) return;
    active = true;
    void load();
    rotation = setInterval(() => {
        if (data?.games.length) index = (index + 1) % data.games.length;
        render();
    }, 15000);
    refresh = setInterval(() => { void load(); }, 15 * 60000);
}
function stop() {
    active = false;
    request?.abort();
    request = undefined;
    refreshing = false;
    clearInterval(rotation);
    clearInterval(refresh);
    clearTimeout(retry);
}
window.addEventListener("pagehide", stop);
window.addEventListener("pageshow", event => { if (event.persisted) start(); });
start();
