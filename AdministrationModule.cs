using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Serilog;

namespace EmojiRaces;

public class AdministrationModule : BaseCommandModule {
    [Command("close"), Aliases("kill"), RequireOwner, Description("Shuts down the bot.")]
    public async Task CloseCommand(CommandContext ctx) {
        await ctx.RespondAsync("Goodbye!");
        Log.Information("CloseCommand invoked.");
        Program.CancelSource.Cancel();
    }

    [Command("invite"), Description("Gives you the link to invite the bot to your server.")]
    public async Task InviteCommand(CommandContext ctx) {
        await ctx.RespondAsync("Invite this bot with the following link:\nhttps://discord.com/api/oauth2/authorize?client_id=996308448159465534&permissions=68608&scope=bot");
    }
}