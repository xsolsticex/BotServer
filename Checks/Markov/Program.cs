using BotServer.Features.Chat.Markov;

var passed = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    Console.WriteLine($"PASS: {name}");
    passed++;
}

var clock = new ManualClock();
var service = new MarkovChatService(clock);
Check(service.Generate("a") == null, "Empty model");
service.Learn("a", "!comando no aprender");
service.Learn("a", "!!!");
service.Learn("a", "palabra");
Check(service.Generate("a") == null, "Ignore commands and messages without transitions");
service.Learn("a", "hola mundo");
Check(service.Generate("A") == "hola mundo", "Adjacent transitions and case insensitive channels");
service.Learn("b", "otro canal");
Check(service.Generate("b") == "otro canal" && service.Generate("a") == "hola mundo", "Channel isolation");
clock.Advance(TimeSpan.FromHours(1));
Check(service.Generate("a") == "hola mundo" && service.Generate("b") == "otro canal", "Retain all channels after one hour");
clock.Advance(TimeSpan.FromDays(30));
service.Learn("c", "nuevo mensaje");
Check(service.Generate("a") == "hola mundo" && service.Generate("b") == "otro canal", "Learning after thirty days preserves existing models");
Check(service.Generate("c") == "nuevo mensaje", "Continue learning after thirty days");

var bounded = new MarkovChatService(TimeProvider.System);
for (var i = 0; i < 2000; i++) bounded.Learn("busy", "uno dos");
bounded.Learn("busy", "prohibida nueva");
Check(Enumerable.Range(0, 100).All(_ => bounded.Generate("busy") == "uno dos"), "Stop learning at transition cap");
for (var i = 0; i < 63; i++) bounded.Learn($"channel{i}", "otro mensaje");
bounded.Learn("overflow", "sin espacio");
Check(bounded.Generate("overflow") == null, "Bound channel count");

var looping = new MarkovChatService(TimeProvider.System);
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

var postingClock = new ManualClock(TimeSpan.FromMinutes(5));
var automatic = new MarkovChatService(postingClock);
automatic.Learn("joined", "hola chat");
automatic.Learn("left", "no publicar");
automatic.Learn("offline", "canal apagado");
using var stopPosting = new CancellationTokenSource();
var sent = new List<(string Channel, string Phrase)>();
var posted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
var checkedChannels = new List<string>();
var posting = automatic.RunPostingAsync(TimeSpan.FromMinutes(5), () => ["offline", "joined", "empty"],
    (channel, phrase) =>
    {
        sent.Add((channel, phrase));
        posted.TrySetResult();
        return Task.CompletedTask;
    }, channel =>
    {
        checkedChannels.Add(channel);
        return Task.FromResult(channel == "joined");
    }, stopPosting.Token);
await postingClock.Ready.Task.WaitAsync(TimeSpan.FromSeconds(5));
Check(sent.Count == 0, "No automatic post before the interval");
postingClock.FireTick();
await posted.Task.WaitAsync(TimeSpan.FromSeconds(5));
stopPosting.Cancel();
try { await posting; }
catch (OperationCanceledException) when (stopPosting.IsCancellationRequested) { }
Check(sent.Count == 1 && sent[0] == ("joined", "hola chat"), "Periodic post only to joined channels with learned data");
Check(checkedChannels.SequenceEqual(new[] { "offline", "joined" }), "Check live status before sending and skip offline channels");
postingClock.FireTick();
Check(sent.Count == 1, "Posting stops on cancellation");
var storageDirectory = Path.Combine(Path.GetTempPath(), "markov-checks-" + Guid.NewGuid());
var storagePath = Path.Combine(storageDirectory, "markov.json");
try
{
    var persistent = new MarkovChatService(TimeProvider.System, storagePath);
    Check(File.Exists(storagePath) && File.ReadAllText(storagePath) == "{}", "Create empty storage on startup before any messages");
    persistent.Learn("A", "hola mundo");
    persistent.Learn("b", "otro canal");
    Check(File.Exists(storagePath) && !File.Exists(storagePath + ".tmp"), "Save learned data atomically to disk");
    var restored = new MarkovChatService(TimeProvider.System, storagePath);
    Check(restored.Generate("a") == "hola mundo" && restored.Generate("B") == "otro canal",
        "Restore channel models after restart with case insensitive lookup");
    restored.Learn("c", "nuevo mensaje");
    var restarted = new MarkovChatService(TimeProvider.System, storagePath);
    Check(restarted.Generate("a") == "hola mundo" && restarted.Generate("c") == "nuevo mensaje",
        "Continue saving after restart without losing previous channels");
    var capped = new MarkovChatService(TimeProvider.System, storagePath);
    for (var i = 0; i < 2000; i++) capped.Learn("cap", "uno dos");
    var restoredCap = new MarkovChatService(TimeProvider.System, storagePath);
    restoredCap.Learn("cap", "prohibida nueva");
    Check(Enumerable.Range(0, 100).All(_ => restoredCap.Generate("cap") == "uno dos"),
        "Restore transition counts and preserve learning cap");
    File.WriteAllText(storagePath, "broken json");
    var rejected = false;
    try { _ = new MarkovChatService(TimeProvider.System, storagePath); }
    catch (System.Text.Json.JsonException) { rejected = true; }
    Check(rejected && File.ReadAllText(storagePath) == "broken json", "Reject corrupt storage without overwriting it");
}
finally
{
    if (Directory.Exists(storageDirectory)) Directory.Delete(storageDirectory, recursive: true);
}
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
    public void FireTick() => _callback!(_state);
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
