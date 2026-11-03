import { createOverlayConnection } from "./overlay-connection.js";

const overlay = createOverlayConnection();
const wins = document.getElementById("win");
const losses = document.getElementById("lose");
const rate = document.getElementById("rate");

function updateRate() {
    const win = Number(wins.textContent) || 0;
    const lose = Number(losses.textContent) || 0;
    rate.textContent = (win + lose > 0 ? 100 * win / (win + lose) : 0).toFixed(2);
}

function change(element, delta) {
    element.textContent = Math.max(0, (Number(element.textContent) || 0) + delta);
    updateRate();
}

overlay.connection.on("win", () => change(wins, 1));
overlay.connection.on("lose", () => change(losses, 1));
overlay.connection.on("nowin", () => change(wins, -1));
overlay.connection.on("nolose", () => change(losses, -1));
overlay.connection.on("reset", () => {
    wins.textContent = losses.textContent = "0";
    updateRate();
});
updateRate();
void overlay.start();
