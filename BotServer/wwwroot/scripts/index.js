import { createOverlayConnection } from "./overlay-connection.js";

const container = document.getElementById("container");
const overlay = createOverlayConnection();
const timers = new Map();

function removeMessage(message) {
    clearTimeout(timers.get(message));
    timers.delete(message);
    message.remove();
}
function element(tag, className, text) {
    const node = document.createElement(tag);
    if (className) node.className = className;
    if (text != null) node.textContent = text;
    return node;
}
function image(url) {
    const img = element("img");
    img.alt = "";
    if (typeof url !== "string" || !url.trim()) return img;
    try {
        const parsed = new URL(url, window.location.origin);
        if (["http:", "https:"].includes(parsed.protocol)) img.src = parsed.href;
    } catch { /* Leave malformed URLs unset. */ }
    return img;
}
function createMessage(data) {
    if (!data || typeof data.content !== "string") return;
    const message = element("div", "mensaje");
    const profile = element("div", "sect-1");
    profile.append(image(data.profile));
    const body = element("div", "sect-2");
    const header = element("div", "subheader");
    const top = element("div", "top");
    const badges = element("span", "badges");
    for (const badge of (Array.isArray(data.badges) ? data.badges : []).slice(0, 4)) badges.append(image(badge));
    const username = element("span", "", data.username);
    username.style.color = CSS.supports("color", data.color) ? data.color : "#ffc107";
    top.append(badges, username);
    header.append(top);
    // Treat chat content as text, not HTML with executable event handlers.
    body.append(header, element("div", "", data.content));
    message.append(profile, body);
    container.append(message);
    while (container.children.length > 5) removeMessage(container.firstElementChild);
    timers.set(message, setTimeout(() => {
        message.classList.add("removing");
        timers.set(message, setTimeout(() => removeMessage(message), 1000));
    }, 60000));
}
overlay.connection.on("botMessage", createMessage);
overlay.connection.on("ColorChanged", color => {
    document.documentElement.style.setProperty("--border-message", CSS.supports("color", color) ? color : "#66b2ff");
});
window.addEventListener("pagehide", () => {
    for (const message of timers.keys()) removeMessage(message);
});
void overlay.start();
