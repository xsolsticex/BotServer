import { createOverlayConnection } from "./overlay-connection.js";

const msg = document.getElementById("message");
const logro = document.getElementById("logro");
const data = document.getElementById("data");
const image = document.getElementById("logo");
const content = document.getElementById("logroContent");
const sound = new Audio("/sounds/achievement.mp3");
const queue = [];
const timers = new Set();
let active = false;
let request;

function later(callback, milliseconds) {
    const timer = setTimeout(() => { timers.delete(timer); callback(); }, milliseconds);
    timers.add(timer);
}
function next() {
    if (active || queue.length === 0) return;
    active = true;
    msg.textContent = queue.shift();
    sound.currentTime = 0;
    content.classList.add("shown");
    later(() => {
        void sound.play().catch(error => console.warn("Audio no disponible:", error));
        image.classList.add("rotation");
        logro.classList.remove("closed");
    }, 2000);
    later(() => {
        image.classList.remove("rotation");
        data.classList.remove("closed");
    }, 3200);
    later(() => {
        image.classList.add("rotation-reverse");
        data.classList.add("closed");
    }, 6000);
    later(() => {
        logro.classList.add("closed");
        image.classList.remove("rotation-reverse");
    }, 7000);
    later(() => {
        content.classList.remove("shown");
        active = false;
        next();
    }, 8000);
}
const overlay = createOverlayConnection({
    onJoined: async () => {
        request?.abort();
        request = new AbortController();
        try {
            const response = await fetch(`/profile/${encodeURIComponent(overlay.channel)}`, { signal: request.signal });
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            const profile = await response.json();
            if (profile?.profile) image.src = profile.profile;
        } catch (error) {
            if (error.name !== "AbortError") throw error;
        }
    }
});
overlay.connection.on("newFollow", message => {
    if (typeof message?.follow !== "string" || queue.length >= 50) return;
    queue.push(message.follow);
    next();
});
window.addEventListener("pagehide", () => {
    for (const timer of timers) clearTimeout(timer);
    timers.clear();
    request?.abort();
    queue.length = 0;
    active = false;
    sound.pause();
    sound.currentTime = 0;
    content.classList.remove("shown");
    image.classList.remove("rotation", "rotation-reverse");
    logro.classList.add("closed");
    data.classList.add("closed");
});
void overlay.start();



