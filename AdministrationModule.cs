using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Serilog;

namespace EmojiRaces;

public class AdministrationModule : BaseCommandModule {
    [Command("close"), Aliases("kill"), RequireOwner]
    public async Task CloseCommand(CommandContext ctx) {
        await ctx.RespondAsync("Goodbye!");
        Log.Information("CloseCommand invoked.");
        Program.CancelSource.Cancel();
    }
}