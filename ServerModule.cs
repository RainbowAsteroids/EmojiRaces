using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace EmojiRaces;

public class ServerModule : BaseCommandModule {
    [Command("gamechannel")]
    public async Task GameChannelCommand(CommandContext ctx, DiscordChannel c) {
        if (ctx.Member != null) {
            if (!PermissionMethods.HasPermission(ctx.Member.PermissionsIn(ctx.Channel), Permissions.ManageGuild)) {
                await ctx.RespondAsync("You do not have the \"Manage Server\" permission!");
            } else {
                try {
                    ServerStates.Instance.SetGameChannel(ctx.Guild, c);
                    await ctx.RespondAsync("Game channel set!");
                    
                    var gameLoop = ServerStates.Instance.GetGameLoop(ctx.Guild);
                    if (gameLoop != null) // Change channels
                        await gameLoop.SetGameChannel(c);
                    else // Build a gameloop
                        await Program.StartGameLoop(ctx.Guild);
                } catch (ServerStates.InvalidChannelException) {
                    await ctx.RespondAsync("The channel you specified was not a text channel!");
                }
            }
        }
    }
    [Command("gamechannel")]
    public async Task GetGameChannelCommand(CommandContext ctx) {
        DiscordChannel? c = ServerStates.Instance.GetGameChannel(ctx.Guild);

        if (c == null) {
            await ctx.RespondAsync("No game channel has been set! Tell an admin to use the `gamechannel` command to set a game channel!");
        } else {
            await ctx.RespondAsync($"The game channel is {c.Mention}");
        }
    }

    [Command("pot")]
    public async Task PotCommand(CommandContext ctx) {
        await ctx.RespondAsync($"This server's pot is at {ServerStates.Instance.GetPot(ctx.Guild)} shekelz.");
    }
}
