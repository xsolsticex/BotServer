using System.Text.RegularExpressions;

namespace BotServer.Features.Chat.Markov;

/// <summary>Bounded, per-channel chat models. Nothing is written to disk.</summary>
public sealed class MarkovChatService(TimeProvider clock) : BackgroundService
{
    private const int MaxChannels = 64;
    private const int MaxTransitions = 4000;
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);
    private readonly object _gate = new();
    private readonly Dictionary<string, Model> _channels = new(StringComparer.OrdinalIgnoreCase);
    private long _resetAt = clock.GetTimestamp();

    private sealed class Model
    {
        public Dictionary<string, List<string>> Next { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<string> Starts { get; } = [];
        public int Count { get; set; }
    }

    public void Learn(string channel, string message)
    {
        if (string.IsNullOrWhiteSpace(channel) || string.IsNullOrWhiteSpace(message)
            || message.TrimStart().StartsWith('!')) return;

        // Keep adjacent words: randomly shuffling individual words loses Markov transitions.
        var words = Regex.Matches(message[..Math.Min(message.Length, 500)], @"[\p{L}\p{N}]+(?:['’][\p{L}\p{N}]+)*")
            .Select(match => match.Value.ToLowerInvariant()).Where(word => word.Length <= 40).Take(80).ToArray();
        if (words.Length < 2) return;
        var length = Math.Min(words.Length, 30);
        var start = Random.Shared.Next(words.Length - length + 1);

        lock (_gate)
        {
            ResetIfExpired();
            if (!_channels.TryGetValue(channel, out var model))
            {
                if (_channels.Count >= MaxChannels) return;
                _channels[channel] = model = new Model();
            }
            // Stop learning at the cap until the hourly reset; existing phrases remain usable.
            if (model.Count + length > MaxTransitions) return;
            model.Starts.Add(words[start]);
            for (var i = start; i < start + length; i++)
            {
                if (!model.Next.TryGetValue(words[i], out var next))
                    model.Next[words[i]] = next = [];
                next.Add(i + 1 < start + length ? words[i + 1] : string.Empty);
                model.Count++;
            }
        }
    }

    public string? Generate(string channel)
    {
        lock (_gate)
        {
            ResetIfExpired();
            if (!_channels.TryGetValue(channel, out var model)) return null;
            var word = model.Starts[Random.Shared.Next(model.Starts.Count)];
            var result = new List<string>();
            var characters = 0;
            while (result.Count < 30 && word.Length > 0)
            {
                if (characters + word.Length + (result.Count > 0 ? 1 : 0) > 400) break;
                characters += word.Length + (result.Count > 0 ? 1 : 0);
                result.Add(word);
                if (!model.Next.TryGetValue(word, out var next)) break;
                word = next[Random.Shared.Next(next.Count)];
            }
            return string.Join(" ", result);
        }
    }

    public async Task RunPostingAsync(TimeSpan interval, Func<IEnumerable<string>> joinedChannels,
        Func<string, string, Task> send, Func<string, Task<bool>> isLive,
        CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(interval, clock);
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            foreach (var channel in joinedChannels())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var phrase = Generate(channel);
                if (!string.IsNullOrWhiteSpace(phrase) && await isLive(channel))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await send(channel, phrase);
                }
            }
        }
    }

    private void ResetIfExpired()
    {
        if (clock.GetElapsedTime(_resetAt) < Lifetime) return;
        Reset();
    }

    private void Reset()
    {
        _channels.Clear();
        _resetAt = clock.GetTimestamp();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Lifetime, clock);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                lock (_gate) Reset();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
