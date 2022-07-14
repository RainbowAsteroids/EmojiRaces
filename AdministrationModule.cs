using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace EmojiRaces;

public class AdministrationModule : BaseCommandModule {
	[Command("close"), Aliases("kill")]
	public async Task CloseCommand(CommandContext ctx) {
        if (ctx.User.Id == 250376950147842048) {
            await ctx.RespondAsync("Goodbye!");
		    Program.CancelSource.Cancel();
        }
	}
}