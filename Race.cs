using DSharpPlus.Entities;

namespace EmojiRaces;

public class RacePreface {
	public class InsufficentFundsException : Exception { }
	public class RacerDoesNotExistException : Exception { }
	public class RacerBetDoesNotExistExpcetion : Exception { }
    public class UserNeverBetException : Exception { }
	public class BetTooLowException : Exception { }
	public static readonly string[] _racers = new string[] {
		"🤑",
		"🥸",
		"😆",
		"🤣",
		"😍",
		"🤪",
		"🤓",
		"😭",
		"🥶",
		"😄",
	};
	private static string _randomRacer() {
		var random = new Random();
		return _racers[random.Next(_racers.Length)];
	}

	private readonly object _betsLock = new object();
	private Dictionary<DiscordUser, Dictionary<string, int>> _bets = new Dictionary<DiscordUser, Dictionary<string, int>>();
	public readonly DiscordGuild Guild;
	public readonly HashSet<string> Racers;

	public RacePreface(DiscordGuild g) {
		Guild = g;

		Racers = new HashSet<string>();
		while (Racers.Count < 5)
			Racers.Add(_randomRacer());
	}

	// Places a bet. Raises InsufficentFundsException and RacerDoesNotExistException.
	public void PlaceBet(DiscordUser m, string racer, int amount) {
		if (!Racers.Contains(racer)) {
			throw new RacerDoesNotExistException();
		}

		if (!UserStates.Instance.DecrementBalance(m, amount)) {
			throw new InsufficentFundsException();
		}

		lock(_betsLock) {
            if (!_bets.ContainsKey(m)) { // User hasn't bet at all
				_bets[m] = new Dictionary<string, int>();
            }

			if (_bets[m].ContainsKey(racer)) {
                _bets[m][racer] += amount;
            } else { // User hasn't bet on this racer yet
                _bets[m][racer] = amount;
            }
		}
	}

	// Removes a bet. Raises RacerBetDoesNotExistException, BetTooLowException, 
    // UserNeverBetException and RacerDoesNotExistException.
	public void RemoveBet(DiscordUser m, string racer, int amount) {
		if (!Racers.Contains(racer)) {
			throw new RacerDoesNotExistException();
		}

		lock(_betsLock) {
			if (_bets.ContainsKey(m)) {
				if (_bets[m].ContainsKey(racer)) {
                    if (_bets[m][racer] < amount) {
					throw new BetTooLowException();
				} else {
					_bets[m][racer] -= amount;
                    UserStates.Instance.IncrementBalance(m, amount);
				}
                } else {
                    throw new RacerBetDoesNotExistExpcetion();
                }
			} else {
				throw new UserNeverBetException();
			}
		}
	}
}
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
	private DiscordGuild _guild { get { return _message.Channel.Guild; } }

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
                total += amount;
                if (racer == winner) {
                    totalWon += amount;
                    if (winnings.ContainsKey(m)) {
                        winnings[m] += amount;
                    } else {
                        winnings[m] = amount;
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

        return new CalculateWinningsReturn(winningEntries, total - totalWon);
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

	public async Task Tick() {
		// Pick a random racer
		var random = new Random();
		var racers = new List<string>(_state.Keys);
		var racer = racers[random.Next(racers.Count)];

		// Move racer
		RenderDamage.DamageType damageType;
		if (_state[racer] != 0 && random.NextSingle() < 0.1) {
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
		}
	}

	private const string _leftTrackChar = "🟩";
	private const string _rightTrackChar = "🟫";
	// Render current state by editing _message
	public async Task Render(RenderDamage damage) {
		var embedBuilder = new DiscordEmbedBuilder() {
			Color = new DiscordColor(255, 255, 0),
			Footer = new DiscordEmbedBuilder.EmbedFooter() {
				Text = $"EmojiRaces: Race started <t:{_startTime}:R>"
			},	
		};

		if (damage.Type == RenderDamage.DamageType.Victory) {
			embedBuilder.Title = "Race Complete";

            embedBuilder.AddField("", $"The winner of the race is: {damage.Racer}");

			var cwr = CalculateWinnings(damage.Racer);
			string body;

			if (cwr.WinningEntries.Count != 0) {
				body = "The following people won:\n";

				foreach (var (m, w) in cwr.WinningEntries)
					body += $"{m.Mention} won {w.Total} ({w.Pot} from the pot)";
			} else {
				body = "Nobody won anything ☹️";
			}
			embedBuilder.AddField("", body);

			embedBuilder.AddField("", $"{cwr.PotIncrement} shekelz were added to the server's pot!");
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
                        string.Concat(Enumerable.Repeat(_rightTrackChar, 5 - position)) + "\n";
			}

			embedBuilder.AddField("", track);

            if (damage.Type == RenderDamage.DamageType.Increment) {
                embedBuilder.AddField("", $"{damage.Racer} has advanced!");
            } else if (damage.Type == RenderDamage.DamageType.Decrement) {
                embedBuilder.AddField("", $"{damage.Racer} has fallen back!");
            }
		}

		await _message.ModifyAsync(embedBuilder.Build());
	}

    
}
