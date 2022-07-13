using DSharpPlus.Entities;

namespace EmojiRaces;
// Singleton holding user state
// TODO: Store this state on disk whenever the state changes
public sealed class UserStates {
	// Do singleton stuff
	private static readonly object s_lock = new object();
	private static UserStates? s_instance = null;
	public static UserStates Instance {
		get {
			lock(s_lock) {
				if (s_instance == null) {
					s_instance = new UserStates();
				}
				return s_instance;
			}
		}
	}

	// Do object stuff
	// Balance management
	private const int _defaultBalance = 1000;
	private readonly object _balanceLock = new object();
	private Dictionary<DiscordUser, int> _balances = new Dictionary<DiscordUser, int>();

	// Sets m's balance.
	public void SetBalance(DiscordUser m, int balance) {
		lock(_balanceLock) {
			_balances[m] = balance;
		}
	}

	// Returns m's balance.
	public int GetBalance(DiscordUser m) {
		lock(_balanceLock) {
			if (_balances.ContainsKey(m)) {
				return _balances[m];
			} else {
				SetBalance(m, _defaultBalance);
				return _defaultBalance;
			}
		}
	}

	// Increases m's balance by increment.
	public void IncrementBalance(DiscordUser m, int increment) => SetBalance(m, GetBalance(m) + increment);

	// Reduces m's balance by decrement. Returns if balance > decrement before the subtraction occurs.
	public bool DecrementBalance(DiscordUser m, int decrement) {
		var balance = GetBalance(m);
		if (balance > decrement) {
			SetBalance(m, balance - decrement);
			return true;
		} else {
			return false;
		}
	}

	// Faucet Management
	private readonly object _timesLock = new object();
	private Dictionary<DiscordUser, Dictionary<DiscordGuild, DateTime>> _times = new Dictionary<DiscordUser, Dictionary<DiscordGuild, DateTime>>();
	private const int _faucetIncrement = 250;
	// Try to perform a faucet operation. Returns null if successful, otherwise returns a DateTimeOffset pointing to
	// the time when the faucet will become available again
	public DateTimeOffset? Faucet(DiscordUser m, DiscordGuild g) { 
		lock(_timesLock) {
			if (_times.ContainsKey(m)) {
				if (_times[m].ContainsKey(g)) {
					DateTime readyTime = _times[m][g] + new TimeSpan(0, 15, 0);
					TimeSpan span = readyTime - DateTime.UtcNow;
					if (span < TimeSpan.Zero)  {
						_times[m][g] = DateTime.UtcNow;
						IncrementBalance(m, _faucetIncrement);
						return null;
					} else {
						return readyTime;
					}
				} else { // User hasn't used the faucet on this guild
					_times[m][g] = DateTime.UtcNow;
					IncrementBalance(m, _faucetIncrement);
					return null;
				}
			} else { // User hasn't used the faucet ever
				_times[m] = new Dictionary<DiscordGuild, DateTime>();
				return Faucet(m, g); // Try again
			}
		}
	}
}
