using DSharpPlus.Entities;

namespace EmojiRaces;
public class Race {
    public readonly struct RenderDamage {
        public enum DamageType {
            Increment,
            Decrement,
            Victory,
            Start
        }

        public DamageType Type { get; init; }
        public string Racer { get; init; }

        public RenderDamage(DamageType type, string racer) => (Type, Racer) = (type, racer);
    }
    private Dictionary<string, int> _state;
    private DiscordMessage _message;
    private long _startTime;
    private Dictionary<DiscordUser, Dictionary<string, int>> _bets;
    private DiscordGuild _guild { get => _message.Channel.Guild; }

    public readonly struct WinningEntry {
        public int Base { get; init; }
        public int Pot { get; init; }
        public int Total { get { return Base + Pot; } }

        public WinningEntry(int b, int p) => (Base, Pot) = (b, p);
    }

    public readonly struct CalculateWinningsReturn {
        public Dictionary<DiscordUser, WinningEntry> WinningEntries { get; init; }
        public int PotIncrement { get; init; }

        public CalculateWinningsReturn(Dictionary<DiscordUser, WinningEntry> w, int p) => 
            (WinningEntries, PotIncrement) = (w, p);

    }
    public CalculateWinningsReturn CalculateWinnings(string winner) { 
        var winnings = new Dictionary<DiscordUser, int>();

        int total = 0;
        int totalWon = 0;

        foreach (var (m, dict) in _bets) {
            foreach (var (racer, amount) in dict) {
                total += amount * 4;
                if (racer == winner) {
                    totalWon += amount * 4;
                    if (winnings.ContainsKey(m)) {
                        winnings[m] += amount * 4;
                    } else {
                        winnings[m] = amount * 4;
                    }
                }
            }
        }

        var winningEntries = new Dictionary<DiscordUser, WinningEntry>();
        var pot = ServerStates.Instance.GetPot(_guild) / 2;

        foreach (var (m, b) in winnings) {
            var potPercentage = (float)b / totalWon;
            var potWinnings = (int)(potPercentage * pot);
            winningEntries[m] = new WinningEntry(b, potWinnings);
        }

        return new CalculateWinningsReturn(winningEntries, (total - totalWon) / 4);
    }
    
    public Race(
        List<string> racers, 
        DiscordMessage message, 
        Dictionary<DiscordUser, Dictionary<string, int>> bets
    ) {
        _message = message;
        _bets = bets;

        _state = new Dictionary<string, int>();
        foreach (string racer in racers) {
            _state[racer] = 0;
        }

        _startTime = DateTimeOffset.Now.ToUnixTimeSeconds();
    }

    // Moves the race forward. Returns whether or not the race has ended.
    public async Task<bool> Tick() {
        // Pick a random racer
        var random = new Random();
        var racers = new List<string>(_state.Keys);
        var racer = racers[random.Next(racers.Count)];

        // Move racer
        RenderDamage.DamageType damageType;
        if (_state[racer] != 0 && random.NextSingle() < 0.2) {
            _state[racer] -= 1;
            damageType = RenderDamage.DamageType.Decrement;
        } else {
            _state[racer] += 1;
            if (_state[racer] == 5) {
                damageType = RenderDamage.DamageType.Victory;
            }
            else {
                damageType = RenderDamage.DamageType.Increment;
            }
        }

        // Render change
        await Render(new RenderDamage(damageType, racer));

        if (damageType == RenderDamage.DamageType.Victory) { // The race is completed
            var cwr = CalculateWinnings(racer);

            foreach (var (m, w) in cwr.WinningEntries) {
                UserStates.Instance.IncrementBalance(m, w.Total);
            }

            if (cwr.WinningEntries.Count > 0) 
                ServerStates.Instance.FoldPot(_guild);
            ServerStates.Instance.IncrementPot(_guild, cwr.PotIncrement);

            return true;
        }

        return false;
    }

    private const string _leftTrackChar = "🟩";
    private const string _rightTrackChar = "🟫";
    // Render current state by editing _message
    public async Task Render(RenderDamage damage) {
        var embedBuilder = new DiscordEmbedBuilder() {
            Color = new DiscordColor(255, 255, 0),
            Footer = new DiscordEmbedBuilder.EmbedFooter() {
                Text = "EmojiRaces"
            },	
        };

        if (damage.Type == RenderDamage.DamageType.Victory) {
            embedBuilder.Title = "Race Complete";

            embedBuilder.AddField("\u200B", $"The winner of the race is: {damage.Racer}");

            var cwr = CalculateWinnings(damage.Racer);
            string body;

            if (cwr.WinningEntries.Count != 0) {
                body = "The following people won:\n";

                foreach (var (m, w) in cwr.WinningEntries)
                    body += $"{m.Mention} won {w.Total} ({w.Pot} from the pot)";
            } else {
                body = "Nobody won anything ☹️";
            }
            embedBuilder.AddField("\u200B", body);

            embedBuilder.AddField("\u200B", $"{cwr.PotIncrement} shekelz were added to the server's pot!");
        } else {
            embedBuilder.Title = "Ongoing Race";

            var track = "";
            foreach (var (racer, position) in _state) {
                /*
                racer = 🤑, position = 2
                track = 
                🟩 * 2 = 🟩🟩
                🤑
                🟫 * (4 - 2) = 🟫 * 2 = 🟫🟫
                track = 🟩🟩🤑🟫🟫

                racer = 🤑, position = 3
                track = 🟩🟩🟩🤑🟫
                */
                track += string.Concat(Enumerable.Repeat(_leftTrackChar, position)) + racer + 
                        string.Concat(Enumerable.Repeat(_rightTrackChar, 4 - position)) + "\n";
            }

            embedBuilder.AddField("\u200B", track);

            if (damage.Type == RenderDamage.DamageType.Increment) {
                embedBuilder.AddField("\u200B", $"{damage.Racer} has advanced!");
            } else if (damage.Type == RenderDamage.DamageType.Decrement) {
                embedBuilder.AddField("\u200B", $"{damage.Racer} has fallen back!");
            }
        }

        embedBuilder.AddField("\u200B", $"Race started <t:{_startTime}:R>");

        await _message.ModifyAsync(embedBuilder.Build());
    }

    
}
