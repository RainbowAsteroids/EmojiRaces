using DSharpPlus.Entities;

namespace EmojiRaces;
// Singleton holding user state
public sealed class UserStates {
	// Do singleton stuff
	private static readonly object _lock = new object();
	private static UserStates? instance = null;
	public static UserStates Instance {
		get {
			lock(_lock) {
				if (instance == null) {
					instance = new UserStates();
				}
				return instance;
			}
		}
	}

	// Do object stuff
	// Balance management
	private const int defaultBalance = 1000;
	private readonly object balanceLock = new object();
	private Dictionary<DiscordUser, int> balances = new Dictionary<DiscordUser, int>();

	// Sets m's balance.
	public void SetBalance(DiscordUser m, int balance) {
		lock(balanceLock) {
			balances[m] = balance;
		}
	}

	// Returns m's balance.
	public int GetBalance(DiscordUser m) {
		lock(balanceLock) {
			if (balances.ContainsKey(m)) {
				return balances[m];
			} else {
				SetBalance(m, defaultBalance);
				return defaultBalance;
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
	private readonly object timesLock = new object();
	private Dictionary<DiscordUser, Dictionary<DiscordGuild, DateTime>> times = new Dictionary<DiscordUser, Dictionary<DiscordGuild, DateTime>>();
	private const int faucetIncrement = 250;
	// Try to perform a faucet operation. Returns null if successful, otherwise returns a TimeSpan of the 
	// remaining time until the operation will be successful.
	public TimeSpan? Faucet(DiscordUser m, DiscordGuild g) { 
		lock(timesLock) {
			if (times.ContainsKey(m)) {
				if (times[m].ContainsKey(g)) {
					TimeSpan span = (times[m][g] + new TimeSpan(0, 15, 0)) - DateTime.UtcNow;
					if (span < TimeSpan.Zero)  {
						times[m][g] = DateTime.UtcNow;
						IncrementBalance(m, faucetIncrement);
						return null;
					} else {
						return span;
					}
				} else { // User hasn't used the faucet on this guild
					times[m][g] = DateTime.UtcNow;
					IncrementBalance(m, faucetIncrement);
					return null;
				}
			} else { // User hasn't used the faucet ever
				times[m] = new Dictionary<DiscordGuild, DateTime>();
				return Faucet(m, g); // Try again
			}
		}
	}
}
