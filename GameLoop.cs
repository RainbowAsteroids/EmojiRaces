using DSharpPlus;
using DSharpPlus.Entities;
using Serilog;

namespace EmojiRaces;

public class GameLoop {
    public RacePreface? RP { get; private set; }
    private DiscordChannel _gameChannel;
    private DiscordMessage? _gameMessage;
    private CancellationTokenSource _source = new CancellationTokenSource();
    public CancellationToken Token { get => _source.Token; }

    public GameLoop(DiscordChannel c) {
        _gameChannel = c;
    }

    private event Action stopEvent = () => { };
    public void Stop() {
        Log.Warning($"Stopping game loop for guild {_gameChannel.Guild} in channel {_gameChannel}");
        _source.Cancel();
        stopEvent();
    }

    public async Task Start() {
        Log.Information($"Starting game loop for guild {_gameChannel.Guild} in channel {_gameChannel}");
        RP = new RacePreface(_gameChannel.Guild);
        // Advertise the race
        _gameMessage = await _gameChannel.SendMessageAsync(new DiscordEmbedBuilder() { 
            Title = "Preparing Race",
            Color = new DiscordColor(255, 255, 0)
        });
        
        long? epoch = null;
        CancellationTokenSource? cancelSource = null;
        var render = async () => {
            var embedBuilder = new DiscordEmbedBuilder() {
                Title = "Upcoming Race",
                Color = new DiscordColor(255, 255, 0),
                Footer = new DiscordEmbedBuilder.EmbedFooter() {
                    Text = "EmojiRaces",
                }
            };

            var text = "The racers are:\n\n";
            foreach (var (racer, amount) in RP.BetsByRacer)
                text += $"{racer} `{RacePreface.RacerDictionary[racer]}` ({amount} shekelz bet in total)\n\n";
            embedBuilder.AddField("\u200B", text);
            
            embedBuilder.AddField("\u200B", $"The server's pot is at {ServerStates.Instance.GetPot(_gameChannel.Guild)} sheklez");

            if (epoch == null) {
                embedBuilder.AddField("\u200B", "Waiting for bets before starting the race");

                if (cancelSource != null && !cancelSource.IsCancellationRequested)
                    cancelSource.Cancel();
            } else {
                embedBuilder.AddField("\u200B", $"The race will start <t:{epoch}:R>");
            }

            await _gameMessage.ModifyAsync(embedBuilder.Build());
        };

        await render();
        cancelSource = new CancellationTokenSource();
        RP.BetsChanged += render;
        stopEvent += () => cancelSource.Cancel(); // Stop the loop in the event of a cancellation

        // Wait for a bet
        try { await Task.Delay(-1, cancelSource.Token); }
        catch (TaskCanceledException) { }

        // Setup timestamp for when the race will start
        const int betTime = 120;
        epoch = (DateTimeOffset.UtcNow + new TimeSpan(0, 0, betTime)).ToUnixTimeSeconds();
        await Task.Delay(100);
        await render();

        // Wait 120 seconds
        try { await Task.Delay(1000 * betTime, Token); }
        catch (TaskCanceledException) { return; }

        // Do the race
        var race = new Race(new List<string>(RP.Racers), _gameMessage, RP.Bets);
        RP = null;

        const int frametime = 1000;

        await race.Render(new Race.RenderDamage(Race.RenderDamage.DamageType.Start, ""));
        await Task.Delay(frametime);
        while (!Token.IsCancellationRequested && !(await race.Tick()))
            await Task.Delay(frametime);

        // Reset the loop
        try { await Task.Delay(10000, Token); } 
        catch (TaskCanceledException) { return; }
        await Start();
    }
 }
