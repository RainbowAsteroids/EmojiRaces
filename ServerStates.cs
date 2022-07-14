using System.Text.Json;
using DSharpPlus;
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
					throw new NullReferenceException();
				}
				return _instance;
			}
		}
	}

	// Do object stuff
    // Store the preferred channel to place game posts
    private readonly object _gameChannelsLock = new object();
    private Dictionary<DiscordGuild, DiscordChannel> _gameChannels = new Dictionary<DiscordGuild, DiscordChannel>();

    private const string gameChannelsFileName = "gamechannels.json";
    private const string potsFileName = "pots.json";
    public static async Task Initialize(DiscordClient client) {
        System.Console.WriteLine("Initialization start!");
        _instance = new ServerStates();

        try { // Read game channels
            using var openStream = File.OpenRead(gameChannelsFileName);
            var d = await JsonSerializer.DeserializeAsync<Dictionary<ulong, ulong>>(openStream);
            if (d == null) {
                // TODO: Log failure
                System.Console.WriteLine("FAIL: Couldn't deserialize game channels");   
            } else {
                // Convert d into Dictionary<DiscordGuild, DiscordChannel>
                foreach (var (gid, cid) in d) {
                    var guild = await client.GetGuildAsync(gid, true);
                    if (guild == null) {
                        // TODO: Log failure
                        System.Console.WriteLine("FAIL: Couldn't get guild for game channel");
                        continue;
                    }

                    var channel = await client.GetChannelAsync(cid);
                    if (channel == null) {
                        // TODO: Log failure
                        System.Console.WriteLine("FAIL: Couldn't get channel for game channel");
                        continue;
                    }

                    _instance._gameChannels[guild] = channel;
                }
            }
        } catch (FileNotFoundException) {
            // TODO: Log failure
            System.Console.WriteLine("FAIL: Couldn't read gamechannels.json");
        }

        try {
            using var openStream = File.OpenRead(potsFileName);
            var p = await JsonSerializer.DeserializeAsync<Dictionary<ulong, int>>(openStream);
            if (p == null) {
                // TODO: Log failure
                System.Console.WriteLine("FAIL: Couldn't deserialize pots");
            } else {
                // Convert p into Dictionary<DiscordGuild, int>
                foreach (var (gid, amount) in p) {
                    var guild = await client.GetGuildAsync(gid, true);
                    if (guild == null) {
                        // TODO: Log failure
                        System.Console.WriteLine("FAIL: Couldn't get guild for pot");
                        continue;
                    }

                    _instance._pots[guild] = amount;
                }
            }
        } catch (FileNotFoundException) {
            // TODO: Log failure
            System.Console.WriteLine("FAIL: Couldn't read pots.json");
        }

        System.Console.WriteLine("Initialization end!");
    }

    private void StoreState() {
        // Store game channels
        var d = new Dictionary<ulong, ulong>();
        lock(_gameChannelsLock)
            foreach (var (guild, channel) in _gameChannels)
                d[guild.Id] = channel.Id;
        File.WriteAllText(gameChannelsFileName, JsonSerializer.Serialize(d));

        // Store pots
        var p = new Dictionary<ulong, int>();
        lock(_potsLock)
            foreach (var (guild, amount) in _pots)
                p[guild.Id] = amount;
        File.WriteAllText(potsFileName, JsonSerializer.Serialize(p));
    }

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
            lock(_gameChannelsLock)
                _gameChannels[g] = c;
            StoreState();
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
        lock(_potsLock) 
            _pots[g] = amount;
        StoreState();
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
        lock(_gameLoopsLock)
            _gameLoops[g] = rp;
    }
}