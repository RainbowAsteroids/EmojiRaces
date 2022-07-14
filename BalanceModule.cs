using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace EmojiRaces;

public class BalanceModule : BaseCommandModule {
    [Command("balance"), Aliases("bal")]
    public async Task BalanceCommand(CommandContext ctx) {
        await ctx.RespondAsync($"You have {UserStates.Instance.GetBalance(ctx.User)} shekelz.");
    }

    [Command("faucet")]
    public async Task FaucetCommand(CommandContext ctx) {
        var readyAt = UserStates.Instance.Faucet(ctx.User, ctx.Guild);
        if (readyAt == null) {
            await ctx.RespondAsync($"Your balance is now {UserStates.Instance.GetBalance(ctx.User)} shekelz.");
        } else {
            await ctx.RespondAsync($"You can use the faucet on this server <t:{((DateTimeOffset)readyAt).ToUnixTimeSeconds()}:R>");
        }
    }
}
