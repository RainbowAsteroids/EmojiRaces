using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace EmojiRaces;

public class BetModule : BaseCommandModule {
    [Command("bet"), Aliases("placebet"), Description("Places or increases a bet on a racer. Cannot be done while a race is happening.")]
    public async Task PlaceBetCommand(CommandContext ctx, string racer, int amount) {
        var gameLoop = ServerStates.Instance.GetGameLoop(ctx.Guild);
        if (gameLoop == null) {
            await ctx.RespondAsync("A server admin needs to set the game channel with the `gamechannel` command!");
        } else {
            var racePreface = gameLoop.RP;
            if (racePreface == null) {
                await ctx.RespondAsync("You cannot place a bet on a race that is ongoing.");
            } else {
                try {
                    racePreface.PlaceBet(ctx.User, racer, amount);
                    await ctx.RespondAsync("Bet placed.");
                } catch (RacePreface.InsufficentFundsException) {
                    await ctx.RespondAsync("You do not have enough shekelz to place that bet.");
                } catch (RacePreface.RacerDoesNotExistException) {
                    await ctx.RespondAsync($"Racer '{racer}' is not racing right now!");
                }
            }
        }
    }

    [Command("liftbet"), Aliases("removebet", "decbet", "unbet"), Description("Removes or decreases a bet on a racer. Cannot be done while a race is happening.")]
    public async Task LiftBetCommand(CommandContext ctx, string racer, int amount) {
        var gameLoop = ServerStates.Instance.GetGameLoop(ctx.Guild);
        if (gameLoop == null) {
            await ctx.RespondAsync("A server admin needs to set the game channel with the `gamechannel` command!");
        } else {
            var racePreface = gameLoop.RP;
            if (racePreface == null) {
                await ctx.RespondAsync("You cannot alter a bet on a race that is ongoing.");
            } else {
                try {
                    racePreface.RemoveBet(ctx.User, racer, amount);
                    await ctx.RespondAsync("Bet lifted.");
                } catch (RacePreface.RacerDoesNotExistException) {
                    await ctx.RespondAsync($"Racer '{racer}' is not racing right now!");
                } catch (RacePreface.UserNeverBetException) {
                    await ctx.RespondAsync("You cannot lift a bet on a racer you never bet on.");
                } catch (RacePreface.RacerBetDoesNotExistExpcetion) {
                    await ctx.RespondAsync("You cannot lift a bet on a racer you never bet on.");
                } catch (RacePreface.BetTooLowException) {
                    await ctx.RespondAsync("You never bet that much to begin with!");
                }
            }
        }
    }

    [Command("bets"), Description("Tells you the bets you have placed.")]
    public async Task ViewBetsCommand(CommandContext ctx) {
        var gameLoop = ServerStates.Instance.GetGameLoop(ctx.Guild);
        if (gameLoop == null) {
            await ctx.RespondAsync("A server admin needs to set the game channel with the `gamechannel` command!");
        } else {
            var racePreface = gameLoop.RP;
            if (racePreface == null) {
                await ctx.RespondAsync("You cannot view your bets on a race that's ongoing.");
            } else {
                var bets = racePreface.GetBets(ctx.User);
                if (bets.Count > 0) {
                    var text = "You placed bets on:\n";
                    foreach (var (racer, amount) in bets)
                        if (amount > 0)
                            text += $"{racer} for {amount} shekelz\n";
                    await ctx.RespondAsync(text);
                } else {
                    await ctx.RespondAsync("You don't have any bets on any racers.");
                }
            }
        }
    }
}
