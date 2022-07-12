using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace EmojiRaces;
public class BalanceModule : BaseCommandModule {
	[Command("balance")]
	public async Task BalanceCommand(CommandContext ctx) {
		await ctx.RespondAsync($"You have {UserStates.Instance.GetBalance(ctx.User)} shekelz.");
	}

	[Command("faucet")]
	public async Task FaucetCommand(CommandContext ctx) {
		var span = UserStates.Instance.Faucet(ctx.User, ctx.Guild);
		if (span == null) {
			await ctx.RespondAsync($"Your balance is now {UserStates.Instance.GetBalance(ctx.User)} shekelz.");
		} else {
			var interval = (TimeSpan)span;
			if (span < new TimeSpan(0, 1, 0)) {
				await ctx.RespondAsync($"You must wait {interval.Seconds} seconds before using the faucet on this server again.");
			} else {
				await ctx.RespondAsync($"You must wait {interval.Minutes} minutes and {interval.Seconds} seconds before using the faucet on this server again.");
			}
		}
	}

	/*
	[Command("give")]
	public async Task GiveCommmand(CommandContext ctx, int amount) {
		UserStates.Instance.IncrementBalance(ctx.User, amount);
		await ctx.RespondAsync("Balance incrememted.");
	}
	*/
}
