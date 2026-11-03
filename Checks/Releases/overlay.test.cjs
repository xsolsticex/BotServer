const { test } = require('node:test');
const assert = require('node:assert/strict');
const vm = require('node:vm');
const fs = require('node:fs/promises');
const path = require('node:path');
const flush = async () => { for (let i = 0; i < 30; i++) await Promise.resolve(); };
const payload = () => ({ periodStart: '2026-11-03', periodEnd: '2026-11-04', isStale: false, games: [1, 2].map(id => ({
    id, title: 'Juego ' + id, description: '<b>Descripción</b>', imageUrl: null,
    firstReleaseDate: '2026-11-03', platforms: [{ name: 'PC', dates: ['2026-11-03'] }, { name: 'PS5', dates: ['2026-11-04'] }]
})) });
async function fixture(response = payload()) {
    let now = Date.parse('2026-11-03T12:00:00Z');
    let id = 0;
    const timers = new Map(), listeners = new Map(), elements = new Map();
    const element = () => ({ hidden: true, textContent: '', children: [], replaceChildren(...children) { this.children = children; }, removeAttribute(name) { delete this[name]; } });
    let fetches = 0, signal;
    let answer = async () => ({ ok: true, json: async () => response });
    class ClockDate extends Date {
        constructor(...args) { super(...(args.length ? args : [now])); }
        static now() { return now; }
    }
    const context = vm.createContext({
        Date: ClockDate, Intl, AbortController, console: { warn() {} },
        document: { getElementById(name) { if (!elements.has(name)) elements.set(name, element()); return elements.get(name); }, createElement: element },
        window: { addEventListener(name, handler) { listeners.set(name, handler); } },
        fetch: (url, options) => { assert.equal(url, '/api/releases'); fetches++; signal = options.signal; return answer(options.signal); },
        setTimeout(fn, delay) { const next = ++id; timers.set(next, { fn, due: now + delay }); return next; },
        clearTimeout(id) { timers.delete(id); },
        setInterval(fn, delay) { const next = ++id; timers.set(next, { fn, due: now + delay, interval: delay }); return next; },
        clearInterval(id) { timers.delete(id); }
    });
    const module = new vm.SourceTextModule(await fs.readFile(path.resolve(__dirname, '../../BotServer/wwwroot/scripts/releases.js'), 'utf8'), { context });
    await module.link(() => { throw Error('Unexpected import'); });
    await module.evaluate(); await flush();
    return {
        elements, timers, get fetches() { return fetches; }, get signal() { return signal; },
        answer(fn) { answer = fn; },
        async advance(milliseconds) {
            now += milliseconds;
            for (const [id, timer] of [...timers]) if (timer.due <= now && timers.has(id)) {
                if (timer.interval) timer.due = now + timer.interval;
                else timers.delete(id);
                timer.fn();
            }
            await flush();
        },
        async event(name, event = {}) { listeners.get(name)?.(event); await flush(); }
    };
}

test('renders one game with all today/tomorrow platforms and no price field', async () => {
    const f = await fixture();
    assert.equal(f.elements.get('release-title').textContent, 'Juego 1');
    assert.equal(f.elements.get('release-platforms').children.length, 2);
    assert.equal(f.elements.get('release-description').textContent, '<b>Descripción</b>');
    assert.equal(f.elements.has('release-price'), false);
    assert.equal(f.elements.get('release-cover').hidden, true);
    await f.event('pagehide');
});
test('rotates every fifteen seconds without refetching each card', async () => {
    const f = await fixture();
    await f.advance(14999);
    assert.equal(f.elements.get('release-title').textContent, 'Juego 1');
    await f.advance(1);
    assert.equal(f.elements.get('release-title').textContent, 'Juego 2');
    assert.equal(f.fetches, 1);
    await f.advance(15000);
    assert.equal(f.elements.get('release-title').textContent, 'Juego 1');
    await f.event('pagehide');
});
test('empty period has its own status', async () => {
    const value = payload(); value.games = [];
    const f = await fixture(value);
    assert.match(f.elements.get('release-status').textContent, /No hay lanzamientos/);
    assert.equal(f.elements.get('release-card').hidden, true);
    await f.event('pagehide');
});
test('failed updates are marked stale and never display the previous day', async () => {
    const f = await fixture();
    f.answer(async () => ({ ok: false, status: 503 }));
    await f.advance(15 * 60000);
    assert.match(f.elements.get('release-freshness').textContent, /actualización pendiente/);
    await f.advance(86400000);
    assert.equal(f.elements.get('release-card').hidden, true);
    assert.match(f.elements.get('release-status').textContent, /No se han podido cargar/);
    await f.event('pagehide');
});
test('an initial failure retries after one minute', async () => {
    const f = await fixture();
    f.answer(async () => ({ ok: false, status: 503 }));
    await f.advance(15 * 60000);
    const calls = f.fetches;
    f.answer(async () => ({ ok: true, json: async () => payload() }));
    await f.advance(60000);
    assert.equal(f.fetches, calls + 1);
    assert.equal(f.elements.get('release-freshness').textContent, '');
    await f.event('pagehide');
});
test('pagehide aborts requests and releases timers; pageshow resumes safely', async () => {
    const f = await fixture();
    f.answer(signal => new Promise((resolve, reject) => signal.addEventListener('abort', () => reject(new Error('aborted')))));
    await f.advance(15 * 60000);
    await f.event('pagehide');
    assert.equal(f.signal.aborted, true);
    assert.equal(f.timers.size, 0);
    f.answer(async () => ({ ok: true, json: async () => payload() }));
    await f.event('pageshow', { persisted: true });
    assert.equal(f.elements.get('release-card').hidden, false);
    await f.event('pagehide');
});

test('day rollover fetches the new today/tomorrow calendar', async () => {
    const f = await fixture();
    const next = payload();
    next.periodStart = '2026-11-04'; next.periodEnd = '2026-11-05';
    for (const game of next.games) {
        game.firstReleaseDate = '2026-11-04';
        game.platforms[0].dates = ['2026-11-04'];
        game.platforms[1].dates = ['2026-11-05'];
    }
    f.answer(async () => ({ ok: true, json: async () => next }));
    const calls = f.fetches;
    await f.advance(86400000);
    assert.equal(f.fetches, calls + 1);
    assert.match(f.elements.get('release-period').textContent, /4 nov.*5 nov/);
    assert.equal(f.elements.get('release-card').hidden, false);
    await f.event('pagehide');
});
