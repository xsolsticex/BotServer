using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace BotServer.Features.Chat.Markov;

/// <summary>Bounded, per-channel chat models with optional local persistence.</summary>
public sealed class MarkovChatService
{
    private readonly TimeProvider clock;
    private readonly string? _storagePath;
    private readonly ILogger<MarkovChatService>? _logger;

    public MarkovChatService(TimeProvider clock, string? storagePath = null, ILogger<MarkovChatService>? logger = null)
    {
        this.clock = clock;
        _logger = logger;
        _storagePath = storagePath is null ? null : Path.GetFullPath(storagePath);
        if (_storagePath is null) return;
        if (!File.Exists(_storagePath))
        {
            Save();
            _logger?.LogInformation("Created Markov storage at {Path}", _storagePath);
            return;
        }

        var saved = JsonSerializer.Deserialize<Dictionary<string, Model>>(File.ReadAllText(_storagePath))
            ?? throw new InvalidDataException("The Markov storage file is empty.");
        if (saved.Count > MaxChannels)
        {
            throw new InvalidDataException("Too many stored Markov channels.");
        }
        foreach (var (channel, model) in saved)
        {
            if (model is null || model.Next is null || model.Starts is null
                || model.Starts.Count == 0 || model.Next.Any(pair => pair.Value is null)
                || model.Next.Values.Sum(next => (long)next.Count) > MaxTransitions
                || model.Starts.Any(start => !model.Next.ContainsKey(start)))
                throw new InvalidDataException("Invalid stored Markov model.");
            model.Next = new(model.Next, StringComparer.OrdinalIgnoreCase);
            model.Count = model.Next.Values.Sum(next => next.Count);
            _channels.Add(channel, model);
        }
        _logger?.LogInformation("Loaded {Channels} Markov channels from {Path}", _channels.Count, _storagePath);
    }

    private void Save()
    {
        if (_storagePath is null)
        {
            return;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(_storagePath)!);
        var temporaryPath = _storagePath + ".tmp";
        try
        {
            using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, _channels);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporaryPath, _storagePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger?.LogError(ex, "Could not save Markov storage at {Path}", _storagePath);
            throw;
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private const int MaxChannels = 64;
    private const int MaxTransitions = 4000;
    private readonly object _gate = new();
    private readonly Dictionary<string, Model> _channels = new(StringComparer.OrdinalIgnoreCase);

    private sealed class Model
    {
        public Model()
        {
        }
        public Dictionary<string, List<string>> Next { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public List<string> Starts { get; set; } = [];
        public int Count { get; set; }
    }

    public void Learn(string channel, string message)
    {
        if (string.IsNullOrWhiteSpace(channel) || string.IsNullOrWhiteSpace(message)
            || message.TrimStart().StartsWith('!'))
        {
            return;
        }

        // Keep adjacent words: randomly shuffling individual words loses Markov transitions.
        var words = Regex.Matches(message[..Math.Min(message.Length, 500)], @"[\p{L}\p{N}]+(?:['’][\p{L}\p{N}]+)*")
            .Select(match => match.Value.ToLowerInvariant()).Where(word => word.Length <= 40).Take(80).ToArray();
        if (words.Length < 2)
        {
            _logger?.LogInformation("Markov skipped message in {Channel}: fewer than two valid words", channel);
            return;
        }
        var length = Math.Min(words.Length, 30);
        var start = Random.Shared.Next(words.Length - length + 1);

        lock (_gate)
        {
            if (!_channels.TryGetValue(channel, out var model))
            {
                if (_channels.Count >= MaxChannels)
                {
                    _logger?.LogWarning("Markov skipped channel {Channel}: channel limit reached", channel);
                    return;
                }
                _channels[channel] = model = new Model();
            }
            // Stop learning at the cap; existing phrases remain usable.
            if (model.Count + length > MaxTransitions)
            {
                _logger?.LogInformation("Markov skipped learning in {Channel}: transition limit reached", channel);
                return;
            }
            model.Starts.Add(words[start]);
            for (var i = start; i < start + length; i++)
            {
                if (!model.Next.TryGetValue(words[i], out var next))
                    model.Next[words[i]] = next = [];
                next.Add(i + 1 < start + length ? words[i + 1] : string.Empty);
                model.Count++;
            }
            Save();
            _logger?.LogInformation("Markov learned {Transitions} transitions in {Channel}; storage: {Path}", length, channel, _storagePath);
        }
    }

    public void ClearChannel(string channel)
    {
        lock (_gate)
        {
            _channels.Remove(channel, out var previous);
            try
            {
                Save();
            }
            catch
            {
                if (previous is not null)
                {
                    _channels[channel] = previous;
                }
                throw;
            }
            _logger?.LogInformation("Cleared Markov model for {Channel}", channel);
        }
    }

    public string? Generate(string channel)
    {
        lock (_gate)
        {
            if (!_channels.TryGetValue(channel, out var model))
            {
                return null;
            }
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

}
