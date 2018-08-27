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

        #region Help Command
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
            Log("Command called");
            if (!FileSystem.UserExists(Context.User.Id))
            {
                await ReplyAsync("Please register an account first.");
                return;
            }
            string desc = "Money: " + Convert.ToString(FileSystem.GetUserMoney(Context.User.Id));
            Log("User exists");
            EmbedBuilder e = new EmbedBuilder();
            Log("EmdedBuilder instanciated");
            e.WithColor(Color.Blue);
            Log("Color changed");
            e.WithTitle(Context.User.Username);
            Log("Title added");
            e.WithThumbnailUrl(Context.User.GetAvatarUrl());
            Log("Thumbnail added");
            e.AddField(desc, new object(), false);
            Log("EmbedBuilder completely edited");
            await ReplyAsync("", embed: e.Build());
            Log("Reply send");
        }
        #endregion Profile

        #region Update
        [Command("update")]
        public async Task UpdateAsync()
        {
            await ReplyAsync(FileSystem.UpdateUser(Context.Message.Author.Id));
        }
        #endregion Update

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
    }
}
