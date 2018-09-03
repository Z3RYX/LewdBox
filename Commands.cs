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
        // Normal Methods
        public static void Log(string msg)
        {
            Console.WriteLine(msg);
        }

        #region GetStreamFromURL
        public Stream GetStreamFromURL(string imageUrl)
        {
            Stream stream;

            try
            {
                System.Net.HttpWebRequest webRequest = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(imageUrl);
                webRequest.AllowWriteStreamBuffering = true;
                webRequest.Timeout = 30000;

                System.Net.WebResponse webResponse = webRequest.GetResponse();

                stream = webResponse.GetResponseStream();
            }
            catch (Exception e)
            {
                Log(e.Message);
                return null;
            }

            return stream;
        }
        #endregion GetStreamFromURL

        // Async Methods
        //Todo Order Methods!
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
            await Context.Client.StopAsync();
        }
        #endregion Kill

        #region Register
        [Command("register")]
        public async Task RegisterAsync()
        {
            if (FileSystem.CreateUser(Context.User))
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
            if (!FileSystem.UserExists(Context.User))
            {
                await ReplyAsync("Please register an account first.");
                return;
            }
            FileSystem.UpdateUser(Context.User);
            
            EmbedBuilder e = new EmbedBuilder();

            object money = FileSystem.GetUserMoney(Context.User) + " ℄";

            e.WithColor(Color.Blue);
            e.WithTitle(Context.User.Username);
            e.WithThumbnailUrl(Context.User.GetAvatarUrl());
            e.AddField("LewdCoins", money, false);
            e.AddField("Registered at", FileSystem.GetRegisterDate(Context.User), true);
            e.AddField("PlayerID", FileSystem.GetProfile(Context.User).PlayerID, true);

            await ReplyAsync("", embed: e.Build());
        }

        [Command("profile")]
        public async Task ProfileAsync(SocketUser user)
        {
            if (!FileSystem.UserExists(user))
            {
                await ReplyAsync("User isn't registered");
                return;
            }
            FileSystem.UpdateUser(user);

            Profile profile = new Profile(user);

            EmbedBuilder e = new EmbedBuilder();

            object money = profile.Money + " ℄";

            e.WithColor(Color.Blue);
            e.WithTitle(user.Username);
            e.WithThumbnailUrl(user.GetAvatarUrl());
            e.AddField("LewdCoins", money, false);
            e.AddField("Registered at", profile.RegisterDate, true);
            e.AddField("PlayerID", profile.PlayerID, true);

            await ReplyAsync("", embed: e.Build());
        }
        #endregion Profile

        #region Test
        [Command("test"), RequireOwner]
        public async Task TestAsync(string command, string name = "Default Name", string msg = "")
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

                case "box":
                    ulong id = 0;
                    try
                    {
                        ITextChannel here = Context.Channel as ITextChannel;
                        var iBox = await here.CreateWebhookAsync("Touhou Box");
                        id = iBox.Id;

                        DiscordWebhookClient weeb = new DiscordWebhookClient(iBox);

                        await weeb.ModifyWebhookAsync(x => { x.Image = new Image(GetStreamFromURL("http://img.zeryx.xyz/LewdBoxLogo.jpg")); });
                        await weeb.SendFileAsync(GetStreamFromURL("http://img.zeryx.xyz/lewdboxes/touhou/cir001.png"), "cir001.png", "You got Cirno!");
                        await weeb.DeleteWebhookAsync();
                        break;
                    }
                    catch (Exception e)
                    {
                        Log(e.Message);

                        ITextChannel tempc = Context.Channel as ITextChannel;
                        var tempw = await tempc.GetWebhookAsync(id);
                        await tempw.DeleteAsync();
                        break;
                    }
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

        #region Info
        [Command("info")]
        public async Task InfoAsync()
        {
            EmbedBuilder embed = new EmbedBuilder();
            embed
                .WithColor(Color.Blue)
                .WithTitle("LewdBox Info")
                .WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl())
                .AddField("Basic", "I am a Discord bot written in C# by Z3RYX (ZeRyX#1079)\nThe official LewdBox Discord server is http://discord.gg/NczBD87", false)
                .AddField("Nerd Stuff", "Wrapper: `Discord.Net beta 2.0`\nHost: Digital Ocean, Ubuntu server\nGitHub: http://github.com/Z3RYX/LewdBox \nTrello: https://trello.com/b/LjQucLAr/lewdbox-discord-bot", false);

            await ReplyAsync("", embed: embed.Build());
        }
        #endregion Info

        #region Daily
        [Command("daily")]
        public async Task DailyAsync()
        {
            if (!FileSystem.UserExists(Context.User))
            {
                await ReplyAsync("You haven't registered an account yet.");
                return;
            }
            FileSystem.UpdateUser(Context.User);
            if (!FileSystem.CheckDaily(Context.User.Id))
            {
                TimeSpan time = FileSystem.GetDaily(Context.User.Id);
                await ReplyAsync("Cannot use " + FileSystem.GetPrefix(Context.Guild.Id) + "daily for " + time.Hours + " hour(s) and " + time.Minutes + " minute(s).");
                return;
            }
            FileSystem.AddMoney(Context.User, 500);
            FileSystem.AddDaily(Context.User);
            await ReplyAsync("Added 500 ℄ to your account.\nYou can use the daily command again in 24 hours.");
        }
        #endregion Daily

        #region Donate
        [Command("donate")]
        public async Task DonateAsync(SocketUser user, int value)
        {
            if (!FileSystem.UserExists(Context.User))
            {
                await ReplyAsync("Please register an account first.");
                return;
            }

            if (!FileSystem.UserExists(user))
            {
                await ReplyAsync("The person you want to donate to does not have an account yet.");
                return;
            }

            if (FileSystem.GetRegisterDate(Context.User) > DateTime.UtcNow.Subtract(TimeSpan.FromDays(7)))
            {
                await ReplyAsync("You account needs to be at least one week old to be able to donate.");
                return;
            }

            if (value > FileSystem.GetUserMoney(Context.User))
            {
                await ReplyAsync("You don't have enough LewdCoins.");
                return;
            }

            FileSystem.RemoveMoney(Context.User, value);
            FileSystem.AddMoney(user, value);

            await ReplyAsync("Donated " + value + " ℄ to " + user.Username);
        }
        #endregion Donate

        #region Read
        [Command("read"), RequireOwner]
        public async Task ReadAsync(string path)
        {
            if (!File.Exists(path))
            {
                await ReadAsync("Could not find file");
            }
            string[] lines = File.ReadAllLines(path);
            await ReplyAsync(string.Join('\n', lines));
        }
        #endregion Read
    }
}
