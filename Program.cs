using DSharpPlus;
using DSharpPlus.CommandsNext;

namespace EmojiRaces;

public class Program {
	static void Main() {
		MainAsync().GetAwaiter().GetResult();
	}

	static async Task MainAsync() {
		// Initialize the client and run the bot
		var discord = new DiscordClient(new DiscordConfiguration {
			Token = System.Environment.GetEnvironmentVariable("EMOJI_RACES_TOKEN"),
			TokenType = TokenType.Bot
		});

		var commands = discord.UseCommandsNext(new CommandsNextConfiguration() {
			StringPrefixes = new[] { "~" }
		});
		commands.RegisterCommands<BalanceModule>();

		await discord.ConnectAsync();
		await Task.Delay(-1);
	}
}
