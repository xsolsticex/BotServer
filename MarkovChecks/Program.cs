using BotServer.TwitchBotClient;

var passed = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    Console.WriteLine($"PASS: {name}");
    passed++;
}

var clock = new ManualClock();
using var service = new MarkovChatService(clock);
Check(service.Generate("a") == null, "Empty model");
service.Learn("a", "!comando no aprender");
service.Learn("a", "!!!");
service.Learn("a", "palabra");
Check(service.Generate("a") == null, "Ignore commands and messages without transitions");
service.Learn("a", "hola mundo");
Check(service.Generate("A") == "hola mundo", "Adjacent transitions and case insensitive channels");
service.Learn("b", "otro canal");
Check(service.Generate("b") == "otro canal" && service.Generate("a") == "hola mundo", "Channel isolation");
clock.Advance(TimeSpan.FromMinutes(59));
Check(service.Generate("a") == "hola mundo", "Retain model before one hour");
clock.Advance(TimeSpan.FromMinutes(1));
Check(service.Generate("a") == null && service.Generate("b") == null, "Clear all channels at one hour");
service.Learn("a", "nuevo mensaje");
Check(service.Generate("a") == "nuevo mensaje", "Learn again after reset");

using var bounded = new MarkovChatService(TimeProvider.System);
for (var i = 0; i < 2000; i++) bounded.Learn("busy", "uno dos");
bounded.Learn("busy", "prohibida nueva");
Check(Enumerable.Range(0, 100).All(_ => bounded.Generate("busy") == "uno dos"), "Stop learning at transition cap");
for (var i = 0; i < 63; i++) bounded.Learn($"channel{i}", "otro mensaje");
bounded.Learn("overflow", "sin espacio");
Check(bounded.Generate("overflow") == null, "Bound channel count");

using var looping = new MarkovChatService(TimeProvider.System);
looping.Learn("a", string.Join(" ", Enumerable.Repeat(new string('a', 40), 30)));
for (var i = 0; i < 100; i++)
{
    var phrase = looping.Generate("a")!;
    if (phrase.Length > 400 || phrase.Split(' ').Length > 30) throw new Exception("Unbounded output");
}
Check(true, "Bound output even for cyclic chains");
Parallel.For(0, 500, i =>
{
    looping.Learn("parallel", "hola chat");
    if (looping.Generate("parallel") != "hola chat") throw new Exception("Concurrent access failed");
});
Check(true, "Concurrent learning and generation");

var timerClock = new ManualClock();
using var hosted = new MarkovChatService(timerClock);
hosted.Learn("idle", "hola mundo");
await hosted.StartAsync(CancellationToken.None);
await timerClock.Ready.Task.WaitAsync(TimeSpan.FromSeconds(5));
timerClock.FireHourlyTick();
// Clock does not advance here: expiry on Generate cannot mask a broken background reset.
for (var i = 0; i < 100 && hosted.Generate("idle") != null; i++) await Task.Delay(10);
Check(hosted.Generate("idle") == null, "Background timer clears models without incoming messages");
await hosted.StopAsync(CancellationToken.None);

var postingClock = new ManualClock(TimeSpan.FromMinutes(5));
using var automatic = new MarkovChatService(postingClock);
automatic.Learn("joined", "hola chat");
automatic.Learn("left", "no publicar");
using var stopPosting = new CancellationTokenSource();
var sent = new List<(string Channel, string Phrase)>();
var posted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
var posting = automatic.RunPostingAsync(TimeSpan.FromMinutes(5), () => ["joined", "empty"],
    (channel, phrase) =>
    {
        sent.Add((channel, phrase));
        posted.TrySetResult();
        return Task.CompletedTask;
    }, stopPosting.Token);
await postingClock.Ready.Task.WaitAsync(TimeSpan.FromSeconds(5));
Check(sent.Count == 0, "No automatic post before the interval");
postingClock.FireHourlyTick();
await posted.Task.WaitAsync(TimeSpan.FromSeconds(5));
stopPosting.Cancel();
try { await posting; }
catch (OperationCanceledException) when (stopPosting.IsCancellationRequested) { }
Check(sent.Count == 1 && sent[0] == ("joined", "hola chat"), "Periodic post only to joined channels with learned data");
postingClock.FireHourlyTick();
Check(sent.Count == 1, "Posting stops on cancellation");
Console.WriteLine($"{passed} checks passed.");

sealed class ManualClock(TimeSpan? expectedInterval = null) : TimeProvider
{
    private long _ticks;
    private TimerCallback? _callback;
    private object? _state;
    public TaskCompletionSource Ready { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;
    public override long GetTimestamp() => _ticks;
    public void Advance(TimeSpan elapsed) => _ticks += elapsed.Ticks;
    public void FireHourlyTick() => _callback!(_state);
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        if (dueTime != (expectedInterval ?? TimeSpan.FromHours(1)) || period != dueTime)
            throw new Exception("Unexpected timer interval");
        _callback = callback;
        _state = state;
        Ready.SetResult();
        return new StubTimer();
    }
    private sealed class StubTimer : ITimer
    {
        public bool Change(TimeSpan dueTime, TimeSpan period) => true;
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
