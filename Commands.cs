using Discord.Commands;
using System;
using System.Collections.Generic;
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
            await ReplyAsync("hey");
        }

        [Command("help")]
        public async Task HelpAsync([Remainder]string command)
        {
            await ReplyAsync("hey");
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
    }
}
