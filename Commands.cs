using Discord.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LewdBox
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        #region Help Command
        [Command("help")]
        public async Task HelpAsync()
        {
            string[] lines = File.ReadAllLines(@"texts\help");

            string helpText = string.Join("\n", lines);

            await ReplyAsync(helpText);
        }

        [Command("help")]
        public async Task HelpAsync(string command)
        {
            string[] lines = File.ReadAllLines(@"texts\help_" + command);

            string helpText = string.Join("\n", lines);

            await ReplyAsync(helpText);
        }
        #endregion Help Command

        #region Set Prefix
        [Command("setprefix"), RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task SetPrefixAsync(string prefix)
        {
            FileSystem.SetPrefix(Context.Guild.Id, prefix);

            await ReplyAsync("Set the new server prefix to " + prefix);
        }
        #endregion Set Prefix

        #region Edit
        [Command("edit"), RequireOwner]
        public async Task EditAsync(string command, int lineNum, [Remainder]string text)
        {
            string[] lines = File.ReadAllLines(@"texts\" + command);
            StreamWriter w = new StreamWriter(@"texts\" + command, false);
            lineNum--;

            lines[lineNum] = text;

            foreach(string line in lines)
            {
                w.WriteLine(line);
            }

            w.Close();

            lineNum++;

            await ReplyAsync("Changed line " + lineNum + " to " + text);
        }
        #endregion Edit
    }
}
