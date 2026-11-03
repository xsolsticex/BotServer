const { test } = require('node:test');
const assert = require('node:assert/strict');
const vm = require('node:vm');
const fs = require('node:fs/promises');
const path = require('node:path');

const scripts = path.resolve(__dirname, '../BotServer/wwwroot/scripts');
const flush = async () => { for (let i = 0; i < 30; i++) await Promise.resolve(); };

class Element {
    children = [];
    textContent = '0';
    style = { setProperty() {} };
    classList = { add() {}, remove() {} };
    append(...children) { for (const child of children) { child.parent = this; this.children.push(child); } }
    remove() { if (this.parent) this.parent.children.splice(this.parent.children.indexOf(this), 1); }
    get firstElementChild() { return this.children[0]; }
}

async function fixture() {
    let now = 0;
    let timerId = 0;
    const timers = new Map();
    const listeners = new Map();
    const elements = new Map();
    const states = { Disconnected: 'Disconnected', Connected: 'Connected', Reconnecting: 'Reconnecting' };
    const connection = {
        state: states.Disconnected, connectionId: null, starts: 0, joins: 0, stops: 0,
        startFailures: 0, joinFailures: 0, handlers: new Map(),
        async start() {
            this.starts++;
            if (this.startFailures-- > 0) throw Error('offline');
            this.state = states.Connected;
            this.connectionId = String(this.starts);
        },
        async invoke(method, channel) {
            assert.equal(method, 'Join'); assert.equal(channel, 'test'); this.joins++;
            if (this.joinFailures-- > 0) throw Error('join failed');
        },
        async stop() { this.stops++; this.state = states.Disconnected; this.closed?.(); },
        on(name, fn) { assert.ok(!this.handlers.has(name), 'duplicate handler'); this.handlers.set(name, fn); },
        onreconnected(fn) { this.reconnected = fn; },
        onclose(fn) { this.closed = fn; }
    };
    let policy;
    class Builder {
        withUrl(url) { assert.equal(url, '/chatHub'); return this; }
        withAutomaticReconnect(value) { policy = value; return this; }
        build() { return connection; }
    }
    const window = {
        location: { pathname: '/chat/Test', origin: 'https://example.test' },
        addEventListener(name, fn) {
            if (!listeners.has(name)) listeners.set(name, []);
            listeners.get(name).push(fn);
        }
    };
    const context = vm.createContext({
        window, signalR: { HubConnectionBuilder: Builder, HubConnectionState: states },
        console: { warn() {}, error() {} }, URL, AbortController,
        CSS: { supports: () => true },
        Audio: class { currentTime = 0; play() { return Promise.reject(Error('autoplay blocked')); } pause() {} },
        fetch: async () => ({ ok: true, json: async () => ({ followers: 1, subs: 2, profile: '/logo.png' }) }),
        document: {
            getElementById(id) { if (!elements.has(id)) elements.set(id, new Element()); return elements.get(id); },
            createElement: () => new Element(), documentElement: new Element()
        },
        setTimeout(fn, delay) { const id = ++timerId; timers.set(id, { fn, time: now + delay }); return id; },
        clearTimeout(id) { timers.delete(id); }
    });
    const modules = new Map();
    async function load(name) {
        if (modules.has(name)) return modules.get(name);
        const module = new vm.SourceTextModule(await fs.readFile(path.join(scripts, name), 'utf8'), { context });
        modules.set(name, module);
        await module.link(specifier => load(path.basename(specifier)));
        return module;
    }
    async function evaluate(name) { const module = await load(name); await module.evaluate(); await flush(); return module.namespace; }
    async function advance(milliseconds) {
        now += milliseconds;
        for (const [id, timer] of [...timers]) if (timer.time <= now && timers.delete(id)) timer.fn();
        await flush();
    }
    async function dispatch(name, event = {}) { for (const fn of listeners.get(name) ?? []) fn(event); await flush(); }
    return { evaluate, connection, context, states, timers, elements, advance, dispatch,
        get policy() { return policy; } };
}

test('initial failures retry once; concurrent starts do not create duplicate connections', async () => {
    const f = await fixture();
    f.connection.startFailures = 1;
    const { createOverlayConnection } = await f.evaluate('overlay-connection.js');
    const overlay = createOverlayConnection();
    const first = overlay.start();
    const second = overlay.start();
    await flush();
    assert.equal(f.connection.starts, 1);
    assert.equal(f.timers.size, 1);
    await f.advance(40000);
    await Promise.all([first, second]);
    assert.equal(f.connection.starts, 2);
    assert.equal(f.connection.joins, 1);
    await overlay.start();
    assert.equal(f.connection.joins, 1);
    assert.ok(Number.isFinite(f.policy.nextRetryDelayInMilliseconds({ previousRetryCount: 1000 })));
    await overlay.stop();
});

test('failed Join retries without opening another socket', async () => {
    const f = await fixture();
    f.connection.joinFailures = 1;
    const { createOverlayConnection } = await f.evaluate('overlay-connection.js');
    const overlay = createOverlayConnection();
    const start = overlay.start();
    await flush();
    await f.advance(40000);
    await start;
    assert.equal(f.connection.starts, 1);
    assert.equal(f.connection.joins, 2);
    await overlay.stop();
});

test('reconnect rejoins the group, final close restarts, pagehide cancels retries, pageshow recovers', async () => {
    const f = await fixture();
    const { createOverlayConnection } = await f.evaluate('overlay-connection.js');
    const overlay = createOverlayConnection();
    await overlay.start();
    f.connection.connectionId = 'new-id';
    f.connection.reconnected(); await flush();
    assert.equal(f.connection.joins, 2);
    f.connection.state = f.states.Disconnected;
    f.connection.closed(); await flush();
    assert.equal(f.connection.starts, 2);
    f.connection.state = f.states.Disconnected;
    f.connection.startFailures = 1;
    f.connection.closed(); await flush();
    await f.dispatch('pagehide');
    const starts = f.connection.starts;
    await f.advance(100000);
    assert.equal(f.connection.starts, starts);
    assert.equal(f.timers.size, 0);
    await f.dispatch('pageshow', { persisted: true });
    assert.equal(f.connection.starts, starts + 1);
    await overlay.stop();
});

test('reconnection during snapshot refresh still rejoins the new connection', async () => {
    const f = await fixture();
    const { createOverlayConnection } = await f.evaluate('overlay-connection.js');
    let release;
    let calls = 0;
    const overlay = createOverlayConnection({ onJoined: () => ++calls === 1 ? new Promise(r => { release = r; }) : Promise.resolve() });
    const start = overlay.start(); await flush();
    f.connection.connectionId = 'new-id'; f.connection.reconnected();
    release(); await start; await flush();
    assert.equal(f.connection.joins, 2);
    await overlay.stop();
});

test('counter events decrement correctly and zero totals never produce NaN', async () => {
    const f = await fixture(); await f.evaluate('counter.js');
    const emit = name => f.connection.handlers.get(name)();
    emit('win'); emit('win'); emit('lose'); emit('nowin');
    assert.equal(f.elements.get('win').textContent, 1);
    assert.equal(f.elements.get('rate').textContent, '50.00');
    emit('nolose'); emit('nolose');
    assert.equal(f.elements.get('lose').textContent, 0);
    emit('reset');
    assert.equal(f.elements.get('rate').textContent, '0.00');
    await f.dispatch('pagehide');
});

test('chat bounds DOM and timers, displays messages as text, and cleans up on pagehide', async () => {
    const f = await fixture(); await f.evaluate('index.js');
    for (let i = 0; i < 100; i++) f.connection.handlers.get('botMessage')({ username: 'user', content: '<img onerror=alert(1)>', badges: null });
    const container = f.elements.get('container');
    assert.equal(container.children.length, 5);
    assert.equal(f.timers.size, 5);
    const body = container.children[0].children[1];
    assert.equal(body.children[1].textContent, '<img onerror=alert(1)>');
    await f.advance(60000); await f.advance(1000);
    assert.equal(container.children.length, 0);
    assert.equal(f.timers.size, 0);
    await f.dispatch('pagehide');
});

test('followers snapshot cannot overwrite a newer realtime event', async () => {
    const f = await fixture(); let release;
    f.context.fetch = () => new Promise(resolve => { release = resolve; });
    await f.evaluate('followers.js');
    f.connection.handlers.get('followers')({ followers: 42, subs: 7 });
    release({ ok: true, json: async () => ({ followers: 1, subs: 2 }) });
    await flush();
    assert.equal(f.elements.get('followCounter').textContent, 42);
    await f.dispatch('pagehide');
});

test('follower alerts are queued without overlapping animations; timers are released on close', async () => {
    const f = await fixture(); await f.evaluate('logro.js');
    const emit = follow => f.connection.handlers.get('newFollow')({ follow });
    emit('first'); emit('second');
    assert.equal(f.elements.get('message').textContent, 'first');
    await f.advance(2000); await f.advance(6000);
    assert.equal(f.elements.get('message').textContent, 'second');
    await f.dispatch('pagehide');
    assert.equal(f.timers.size, 0);
});
