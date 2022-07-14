using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace EmojiRaces;

public class Program {
	public static CancellationTokenSource CancelSource = new CancellationTokenSource();

	static void Main() =>
		MainAsync().GetAwaiter().GetResult();

	public static async Task StartGameLoop(DiscordGuild g) {
		var gameChannel = ServerStates.Instance.GetGameChannel(g);
		if (gameChannel == null) {
			if (g.SystemChannel != null) {
				await g.SystemChannel.SendMessageAsync("A server admin needs to set the game channel with the `gamechannel` command!");
			}
		} else {
			var gameLoop = new GameLoop(gameChannel);
			ServerStates.Instance.SetGameLoop(g, gameLoop);
			Task.Run(gameLoop.Start);
		}
	}

	static async Task MainAsync() {
		// Initialize the client and run the bot
		var discord = new DiscordClient(new DiscordConfiguration {
			Token = System.Environment.GetEnvironmentVariable("EMOJI_RACES_TOKEN"),
			TokenType = TokenType.Bot,
			Intents = DiscordIntents.Guilds | DiscordIntents.GuildMessages
		});

		// Initialize state singletons
		Task.WaitAll(ServerStates.Initialize(discord), UserStates.Initialize(discord));

		var commands = discord.UseCommandsNext(new CommandsNextConfiguration() {
			StringPrefixes = new[] { "~" }
		});
		commands.RegisterCommands<BalanceModule>();
		commands.RegisterCommands<ServerModule>();
		commands.RegisterCommands<BetModule>();
		commands.RegisterCommands<AdministrationModule>();

		discord.GuildDownloadCompleted += async (client, args) => {
			foreach (var (gid, guild) in args.Guilds) {
				await StartGameLoop(guild);
			}
		};

		discord.GuildCreated += async (client, args) => {
			await StartGameLoop(args.Guild);
		};

		discord.Ready += async (client, args) => {
			System.Console.WriteLine("Ready!");
		};

		await discord.ConnectAsync();

		try { await Task.Delay(-1, CancelSource.Token); }
		catch (TaskCanceledException) { }

		await discord.DisconnectAsync();
	}
}
