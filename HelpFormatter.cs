using System.Text;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;
using Serilog;

public class HelpFormatter : BaseHelpFormatter {
    protected DiscordEmbedBuilder _embedBuilder = new DiscordEmbedBuilder() {
        Title = "EmojiRaces Help",
        Color = new DiscordColor(255, 255, 0),
        Footer = new DiscordEmbedBuilder.EmbedFooter() {
            Text = "EmojiRaces: Help"
        }
    };

    public HelpFormatter(CommandContext ctx) : base(ctx) { }
    
    public override HelpFormatter WithCommand(Command cmd) {
        var sb = new StringBuilder();
        
        if (cmd.Aliases.Count != 0)
            sb.Append($"**Aliases:** {String.Join(", ", cmd.Aliases)}\n");

        foreach (CommandOverload overload in cmd.Overloads)
            if (overload.Arguments.Count == 0)
                sb.Append($"`{cmd.Name}`\n");
            else
                sb.Append($"`{cmd.Name} {String.Join(' ', overload.Arguments.Select(a => a.IsOptional ? $"[{a.Name}]" : $"<{a.Name}>"))}`\n");

        string description = cmd.Description == null ? "No description provided" : cmd.Description;        
        sb.Append(description);

        _embedBuilder.AddField(cmd.Name, sb.ToString());

        return this;
    }

    public override HelpFormatter WithSubcommands(IEnumerable<Command> cmds) {
        foreach (var cmd in cmds)
            if (!cmd.IsHidden)
                WithCommand(cmd);

        return this;
     }

     public override CommandHelpMessage Build() {
         return new CommandHelpMessage(embed: _embedBuilder);
     }
 }