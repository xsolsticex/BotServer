import { createOverlayConnection } from "./overlay-connection.js";

const followCounter = document.getElementById("followCounter");
const subCounter = document.getElementById("subCounter");
let revision = 0;
let request;
function update(data) {
    if (Number.isFinite(data?.followers)) followCounter.textContent = data.followers;
    if (Number.isFinite(data?.subs)) subCounter.textContent = data.subs;
}
const overlay = createOverlayConnection({
    onJoined: async () => {
        request?.abort();
        request = new AbortController();
        const version = revision;
        try {
            const response = await fetch(`/data/${encodeURIComponent(overlay.channel)}`, { signal: request.signal });
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            const data = await response.json();
            // Do not overwrite a newer realtime event with an older HTTP snapshot.
            if (version === revision) update(data);
        } catch (error) {
            if (error.name !== "AbortError") throw error;
        }
    }
});
overlay.connection.on("followers", data => { revision++; update(data); });
window.addEventListener("pagehide", () => { revision++; request?.abort(); });
void overlay.start();
