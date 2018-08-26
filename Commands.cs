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
            string[] lines = File.ReadAllLines(@"texts/help");

            string helpText = string.Join("\n", lines);

            await ReplyAsync(helpText);
        }

        [Command("help")]
        public async Task HelpAsync(string command)
        {
            string[] lines = File.ReadAllLines(@"texts/help_" + command);

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
            string[] lines = File.ReadAllLines(@"texts/" + command);
            StreamWriter w = new StreamWriter(@"texts/" + command, false);
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

        [Command("edit"), RequireOwner]
        public async Task EditAsync(string command, [Remainder]string text)
        {
            StreamWriter w = new StreamWriter(@"texts/" + command, false);

            w.Write(text);

            w.Close();

            await ReplyAsync("Changed text to " + text);
        }
        #endregion Edit

        #region Kick Me
        [Command("kickme"), RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task KickMeAsync()
        {
            await ReplyAsync("Goodbye People");
            await Context.Guild.LeaveAsync();
        }
        #endregion Kick Me

        #region Kill
        [Command("kill"),RequireOwner]
        public async Task KillAsync()
        {
            await ReplyAsync("Shutting down");
            Environment.Exit(0);
        }
        #endregion Kill

        #region Register
        [Command("register")]
        public async Task RegisterAsync()
        {
            FileSystem.CreateUser(Context.Message.Author.Id, Context.Message.Author.Username, Context.Message.Author.GetAvatarUrl(Discord.ImageFormat.Auto));

            string[] Players = File.ReadAllLines("PlayerID");
            await Context.Client.SetGameAsync("//help | " + Players[0] + " Players");

            await ReplyAsync("Thank you for registering " + Context.Message.Author.Username + ". Have fun collecting Lewd Boxes.");
        }
        #endregion Register

        #region Profile

        #endregion Profile
    }
}
