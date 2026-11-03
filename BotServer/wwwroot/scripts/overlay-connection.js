// One connection and one recovery loop per overlay. Register handlers before start().
export function createOverlayConnection({ onJoined = async () => {} } = {}) {
    const channel = decodeURIComponent(window.location.pathname.split("/").filter(Boolean).pop() ?? "").toLowerCase();
    const states = signalR.HubConnectionState;
    const delayFor = attempt => Math.min(30000, 1000 * 2 ** Math.min(attempt, 5)) + Math.floor(Math.random() * 1000);
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/chatHub")
        .withAutomaticReconnect({ nextRetryDelayInMilliseconds: context => delayFor(context.previousRetryCount) })
        .build();
    connection.serverTimeoutInMilliseconds = 60000;
    let stopped = true;
    let recovery;
    let retryTimer;
    let wakeRetry;
    let joinedConnectionId;

    function wait(attempt) {
        return new Promise(resolve => {
            wakeRetry = resolve;
            retryTimer = setTimeout(() => {
                retryTimer = undefined;
                wakeRetry = undefined;
                resolve();
            }, delayFor(attempt));
        });
    }

    function recover() {
        if (stopped || recovery) return recovery;
        recovery = (async () => {
            let attempt = 0;
            while (!stopped) {
                if (connection.state === states.Reconnecting) return;
                try {
                    if (connection.state === states.Disconnected) await connection.start();
                    if (stopped) {
                        await connection.stop();
                        return;
                    }
                    if (connection.state !== states.Connected) return;
                    const connectionId = connection.connectionId;
                    await connection.invoke("Join", channel);
                    if (connection.state !== states.Connected || connection.connectionId !== connectionId) continue;
                    joinedConnectionId = connectionId;
                    if (!stopped && connection.state === states.Connected) {
                        try { await onJoined(); }
                        catch (error) { console.error("Error al actualizar el overlay:", error); }
                    }
                    return;
                } catch (error) {
                    if (stopped) return;
                    console.warn("SignalR: reintentando conexión o unión al canal.", error);
                    await wait(attempt++);
                }
            }
        })().finally(() => {
            recovery = undefined;
            if (!stopped && (connection.state === states.Disconnected ||
                (connection.state === states.Connected && joinedConnectionId !== connection.connectionId))) {
                void recover();
            }
        });
        return recovery;
    }

    async function start() {
        if (!channel) return;
        if (!stopped && connection.state === states.Connected && joinedConnectionId === connection.connectionId) return recovery;
        stopped = false;
        return recover();
    }

    async function stop() {
        stopped = true;
        joinedConnectionId = undefined;
        clearTimeout(retryTimer);
        wakeRetry?.();
        wakeRetry = undefined;
        await connection.stop();
        await recovery;
    }

    connection.onreconnected(() => { joinedConnectionId = undefined; void recover(); });
    connection.onclose(() => { if (!stopped) void recover(); });
    // A suspended browser source must release its socket and recover after bfcache restore.
    let stopping = Promise.resolve();
    window.addEventListener("pagehide", () => {
        stopping = stop().catch(error => console.error("Error al cerrar SignalR:", error));
    });
    window.addEventListener("pageshow", event => {
        if (event.persisted) void stopping.then(start);
    });
    return { channel, connection, start, stop };
}

