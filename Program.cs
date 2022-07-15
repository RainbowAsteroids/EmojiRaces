using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using Serilog;

namespace EmojiRaces;

public class Program {
    public static CancellationTokenSource CancelSource = new CancellationTokenSource();
    public static string CommandPrefix = ";;";

    static void Main() =>
        MainAsync().GetAwaiter().GetResult();

    public static async Task StartGameLoop(DiscordGuild g) {
        var gameChannel = ServerStates.Instance.GetGameChannel(g);
        if (gameChannel == null) {
            Log.Information($"Could not find game channel for {g}");
            if (g.SystemChannel != null) {
                await g.SystemChannel.SendMessageAsync("A server admin needs to set the game channel with the `gamechannel` command!");
            } else {
                Log.Warning($"Could not find a system channel for guild {g}");
            }
        } else {
            var gameLoop = new GameLoop(gameChannel);
            ServerStates.Instance.SetGameLoop(g, gameLoop);
            Task.Run(gameLoop.Start);
        }
    }

    static async Task MainAsync() {
        // Initialize the logger
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        // Initialize the client and run the bot
        var discord = new DiscordClient(new DiscordConfiguration {
            Token = System.Environment.GetEnvironmentVariable("EMOJI_RACES_TOKEN"),
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.Guilds | DiscordIntents.GuildMessages,
            LoggerFactory = new LoggerFactory().AddSerilog()
        });

        // Initialize state singletons
        Task.WaitAll(ServerStates.Initialize(discord), UserStates.Initialize(discord));

        var commands = discord.UseCommandsNext(new CommandsNextConfiguration() {
            StringPrefixes = new[] { CommandPrefix }
        });
        commands.RegisterCommands<BalanceModule>();
        commands.RegisterCommands<ServerModule>();
        commands.RegisterCommands<BetModule>();
        commands.RegisterCommands<AdministrationModule>();
        commands.SetHelpFormatter<HelpFormatter>();

        discord.GuildDownloadCompleted += async (client, args) => {
            foreach (var (gid, guild) in args.Guilds) {
                await StartGameLoop(guild);
            }
        };

        discord.GuildCreated += async (client, args) => {
            Log.Information($"Joined new guild: {args.Guild}");
            await StartGameLoop(args.Guild);
        };

        discord.ClientErrored += async (client, args) => {
            Log.Error(args.Exception, "ClientErrored invoked.");
        };

        discord.SocketErrored += async (client, args) => {
            Log.Error(args.Exception, "SocketErrored invoked.");
        };

        discord.Ready += async (client, args) => {
            Log.Information($"EmojiRaces Ready. Logged in as {client.CurrentUser}.");
        };

        await discord.ConnectAsync();

        try { await Task.Delay(-1, CancelSource.Token); }
        catch (TaskCanceledException) { }

        await discord.DisconnectAsync();
        Log.CloseAndFlush();
    }
}
