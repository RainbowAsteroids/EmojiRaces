using DSharpPlus.Entities;

namespace EmojiRaces;



// Singleton holding server state
// TODO: Store this state on disk whenever the state changes
public class ServerStates {
    public class InvalidChannelException : Exception { }
    // Do singleton stuff
	private static readonly object _lock = new object();
	private static ServerStates? _instance = null;
	public static ServerStates Instance {
		get {
			lock(_lock) {
				if (_instance == null) {
					_instance = new ServerStates();
				}
				return _instance;
			}
		}
	}

	// Do object stuff
    // Store the preferred channel to place game posts
    private readonly object _gameChannelsLock = new object();
    private Dictionary<DiscordGuild, DiscordChannel> _gameChannels = new Dictionary<DiscordGuild, DiscordChannel>();

    public DiscordChannel? GetGameChannel(DiscordGuild g) {
        lock(_gameChannelsLock) {
            if (_gameChannels.ContainsKey(g)) {
                return _gameChannels[g];
            } else {
                return null;
            }
        }
    }

    // Sets the guild's game channel. Raises InvalidChannelException if the channel isn't a text channel
    public void SetGameChannel(DiscordGuild g, DiscordChannel c) {
        if (c.Type != DSharpPlus.ChannelType.Text) {
            throw new InvalidChannelException();
        } else {
            lock(_gameChannelsLock) {
                _gameChannels[g] = c;
            }
        }
    }

    // Store the pot for every guild
    private readonly object _potsLock = new object();
    private Dictionary<DiscordGuild, int> _pots = new Dictionary<DiscordGuild, int>();
    private const int _defaultPot = 0;

    public int GetPot(DiscordGuild g) {
        lock(_potsLock) {
            if (_pots.ContainsKey(g)) {
                return _pots[g];
            } else {
                SetPot(g, _defaultPot);
                return _defaultPot;
            }
        }
    }

    public void SetPot(DiscordGuild g, int amount) {
        lock(_potsLock) {
            _pots[g] = amount;
        }
    }

    public void IncrementPot(DiscordGuild g, int increment) => SetPot(g, GetPot(g) + increment);
    public void FoldPot(DiscordGuild g) => SetPot(g, GetPot(g) / 2);

    // Store instances of GameLoop to place bets against
    private readonly object _gameLoopsLock = new object();
    private Dictionary<DiscordGuild, GameLoop?> _gameLoops = new Dictionary<DiscordGuild, GameLoop?>();

    public GameLoop? GetGameLoop(DiscordGuild g) {
        lock(_gameLoopsLock) {
            if (_gameLoops.ContainsKey(g)) {
                return _gameLoops[g];
            } else {
                _gameLoops[g] = null;
                return null;
            }
        }
    }

    public void SetGameLoop(DiscordGuild g, GameLoop? rp) {
        lock(_gameLoopsLock) {
            _gameLoops[g] = rp;
        }
    }
}