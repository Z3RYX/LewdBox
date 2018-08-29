using Discord;
using Discord.Commands;
using Discord.Webhook;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LewdBox
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        public static void Log(string msg)
        {
            Console.WriteLine(msg);
        }

        #region Help
        [Command("help")]
        public async Task HelpAsync()
        {
            string[] lines = File.ReadAllLines("texts/help");

            string helpText = string.Join("\n", lines);

            await ReplyAsync(helpText);
        }

        [Command("help")]
        public async Task HelpAsync(string command)
        {
            string[] lines = File.ReadAllLines("texts/help_" + command);

            string helpText = string.Join("\n", lines);

            await ReplyAsync(helpText);
        }
        #endregion Help

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
        public async Task EditAsync(string path, int lineNum, [Remainder]string text)
        {
            if (!File.Exists(path))
            {
                await ReplyAsync("File does not exist");
                return;
            }
            string[] lines = File.ReadAllLines(path);
            StreamWriter w = new StreamWriter(path, false);
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
        public async Task EditAsync(string path, [Remainder]string text)
        {
            try
            {
                StreamWriter w = new StreamWriter(path, false);

                w.Write(text);

                w.Close();

                await ReplyAsync("Changed text to " + text);
            }
            catch (Exception e)
            {
                await ReplyAsync("Couldn't edit file\n`" + e.Message + "`");
            }
        }
        #endregion Edit

        #region Kick Me
        [Command("kickme"), RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task KickMeAsync()
        {
            await ReplyAsync("Goodbye People");
            FileSystem.ResetPrefix(Context.Guild.Id);
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
            if (FileSystem.CreateUser(Context.Message.Author.Id, Context.Message.Author.Username, Context.Message.Author.GetAvatarUrl(Discord.ImageFormat.Auto)))
            {
                await ReplyAsync("You are already registered");
                return;
            }

            string[] Players = File.ReadAllLines("PlayerID");
            IActivity game = new Game("//help | " + Players[0] + " Players in " + Context.Client.Guilds.Count + " Guild") as IActivity;
            await Context.Client.SetActivityAsync(game);

            await ReplyAsync("Thank you for registering " + Context.Message.Author.Username + ". Have fun collecting Lewd Boxes.");
        }
        #endregion Register

        #region Profile
        [Command("profile")]
        public async Task ProfileAsync()
        {
            if (!FileSystem.UserExists(Context.User.Id))
            {
                await ReplyAsync("Please register an account first.");
                return;
            }
            EmbedBuilder e = new EmbedBuilder();

            object money = FileSystem.GetUserMoney(Context.User.Id) + " ℄";

            e.WithColor(Color.Blue);
            e.WithTitle(Context.User.Username);
            e.WithThumbnailUrl(Context.User.GetAvatarUrl());
            e.AddField("LewdCoins", money, false);

            await ReplyAsync("", embed: e.Build());
        }
        #endregion Profile

        #region Test
        [Command("test"), RequireOwner]
        public async Task TestAsync(string command, string name, string msg)
        {
            switch (command)
            {
                case "webhook":
                    ITextChannel t = Context.Channel as ITextChannel;
                    var webhook = await t.CreateWebhookAsync(name);

                    DiscordWebhookClient w = new DiscordWebhookClient(webhook);
                    await w.SendMessageAsync(msg);
                    await w.DeleteWebhookAsync();
                    await webhook.DeleteAsync();
                    break;
            }
        }
        #endregion Test

        #region Settle
        [Command("settle"), RequireUserPermission(GuildPermission.Administrator)]
        public async Task SettleAsync()
        {
            FileSystem.AddSettle(Context.Guild.Id, Context.Channel.Id);
            await ReplyAsync("I've settled myself here\nDo " + FileSystem.GetPrefix(Context.Guild.Id) + "unsettle to set me free again");
        }
        #endregion Settle

        #region Unsettle
        [Command("unsettle"), RequireUserPermission(GuildPermission.Administrator)]
        public async Task UnsettleAsync()
        {
            if (FileSystem.RemoveSettle(Context.Guild.Id))
            {
                await ReplyAsync("Thanks for setting me free again");
                return;
            }
            else
            {
                await ReplyAsync("I haven't been settled yet...\nTo do that use " + FileSystem.GetPrefix(Context.Guild.Id) + "settle");
                return;
            }
        }
        #endregion Unsettle
    }
}
