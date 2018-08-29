using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace LewdBox
{
    class Program
    {
        static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;

        public async Task RunBotAsync()
        {
            _client = new DiscordSocketClient();
            _commands = new CommandService();

            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .BuildServiceProvider();

            //Bot Token
            string[] lines = File.ReadAllLines("token");
            string TOKEN = lines[0];

            //event subscription
            _client.Log += Log;
            _client.JoinedGuild += JoinedGuild;
            _client.LeftGuild += LeftGuild;
            _client.Ready += Ready;
            _client.ChannelDestroyed += ChannelDestroyed;

            await RegisterCommandsAsync();

            await _client.LoginAsync(Discord.TokenType.Bot, TOKEN);

            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);

            return Task.CompletedTask;
        }

        public Task JoinedGuild(SocketGuild e)
        {
            //Set activity
            string[] Players = File.ReadAllLines("PlayerID");
            IActivity game = new Game("//help | " + Players[0] + " Players in " + _client.Guilds.Count + " Guild") as IActivity;
            _client.SetActivityAsync(game);

            //Send join message
            try
            {
                ITextChannel channel = e.DefaultChannel as ITextChannel;
                channel.SendMessageAsync(
                    "**Hello, I'm LewdBox.**\n\n" +
                    "I have been summoned to bring fun upon this server.\n" +
                    "If you haven't registered an account yet, use //register\n\n" +
                    "For more information use //help or //info\n" +
                    "Have fun.");
            }
            catch (Exception args)
            {
                Console.WriteLine(args.Message);
                return Task.CompletedTask;
            }
            
            return Task.CompletedTask;
        }

        public Task LeftGuild(SocketGuild e)
        {
            string[] Players = File.ReadAllLines("PlayerID");
            IActivity game = new Game("//help | " + Players[0] + " Players in " + _client.Guilds.Count + " Guild") as IActivity;
            _client.SetActivityAsync(game);
            return Task.CompletedTask;
        }

        public Task Ready()
        {
            string[] Players = File.ReadAllLines("PlayerID");
            IActivity game = new Game("//help | " + Players[0] + " Players in " + _client.Guilds.Count + " Guild") as IActivity;
            _client.SetActivityAsync(game);
            return Task.CompletedTask;
        }

        public Task ChannelDestroyed(SocketChannel e)
        {
            SocketGuildChannel channel = e as SocketGuildChannel;
            FileSystem.RemoveSettle(channel.Guild.Id);
            return Task.CompletedTask;
        }

        public async Task RegisterCommandsAsync()
        {
            _client.MessageReceived += HandleCommandAsync;

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;

            if (message is null || message.Author.IsBot) return;

            var context = new SocketCommandContext(_client, message);

            if (FileSystem.GetSettle(context.Guild.Id, context) != context.Channel)
                return;

            if (message.Content == "//resetprefix")
            {
                FileSystem.ResetPrefix(context.Guild.Id);
                await context.Channel.SendMessageAsync("Server prefix reset");
                return;
            }

            string prefix = FileSystem.GetPrefix(context.Guild.Id);

            int argPos = 0;

            if (message.HasStringPrefix(prefix, ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos) || IsDefaultHelp(message.Content, ref argPos))
            {
                var result = await _commands.ExecuteAsync(context, argPos, _services);

                if (!result.IsSuccess)
                    Console.WriteLine(result.ErrorReason);
            }
        }

        public bool IsDefaultHelp(string msg, ref int argPos)
        {
            if (msg.StartsWith("//help"))
            {
                argPos = 2;
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// The filesystem that handles everything outside the code
    /// </summary>
    static class FileSystem
    {
        public static void AddSettle(ulong serverID, ulong channel)
        {
            bool exists = false;
            string[] lines = { "" , "" };

            if (File.Exists("servers/" + serverID))
            {
                lines = File.ReadAllLines("servers/" + serverID);
                exists = true;
            }

            StreamWriter w = new StreamWriter("servers/" + serverID, false);

            if (exists)
            {
                w.WriteLine(lines[0]);
                w.WriteLine(channel);
            }
            else
            {
                w.WriteLine("//");
                w.WriteLine(channel);
            }

            w.Close();
        }

        public static bool CreateUser(ulong userID, string name, string avaURL)
        {
            if (File.Exists("users/" + userID))
                return true;

            string[] lines = File.ReadAllLines("PlayerID");
            int PlayerID = Convert.ToInt32(lines[0]);
            PlayerID++;
            StreamWriter id = new StreamWriter("PlayerID", false);
            id.Write(PlayerID);
            id.Close();

            StreamWriter w = new StreamWriter("users/" + userID);

            w.Write(
                "version" + GetVersion() + "\n" +
                userID + "\n" +
                name + "\n" +
                avaURL + "\n" +
                "2000\n" +
                PlayerID);

            w.Close();
            return false;
        }

        /// <summary>
        /// Gets the servers prefix if it has been changed
        /// </summary>
        /// <param name="serverID">ID of the server</param>
        /// <returns>New prefix</returns>
        public static string GetPrefix(ulong serverID)
        {
            string prefix = "//";

            if (File.Exists(@"servers/" + serverID))
            {
                string[] lines = File.ReadAllLines(@"servers/" + serverID);
                prefix = lines[0];
            }

            return prefix;
        }

        public static SocketTextChannel GetSettle(ulong serverID, SocketCommandContext msg)
        {
            SocketTextChannel conchannel = msg.Channel as SocketTextChannel;

            if (!File.Exists("servers/" + serverID))
                return conchannel;

            string[] lines = File.ReadAllLines("servers/" + serverID);
            if (lines.Length == 1)
                return conchannel;

            SocketTextChannel result = msg.Guild.GetChannel(Convert.ToUInt64(lines[1])) as SocketTextChannel;
            return result;
        }

        public static int GetUserMoney(ulong userID)
        {
            string[] lines = File.ReadAllLines("users/" + userID);
            int rslt = Convert.ToInt32(lines[4]);
            return rslt;
        }

        public static string GetVersion()
        {
            string[] lines = File.ReadAllLines("botinfo");
            return lines[0];
        }

        public static bool RemoveSettle(ulong serverID)
        {
            if (File.Exists("servers/" + serverID))
            {
                string[] lines = File.ReadAllLines("servers/" + serverID);
                Console.WriteLine(lines.Length);
                StreamWriter w = new StreamWriter("servers/" + serverID, false);
                w.WriteLine(lines[0]);
                w.Close();
                if (lines.Length == 1)
                    return false;
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Resets the prefix of the server back to //
        /// </summary>
        /// <param name="serverID">ID of the server</param>
        public static void ResetPrefix(ulong serverID)
        {
            if (File.Exists(@"servers/" + serverID))
            {
                string[] lines = File.ReadAllLines("servers/" + serverID);
                StreamWriter w = new StreamWriter("servers/" + serverID, false);
                lines[0] = "//";
                foreach(string line in lines)
                {
                    w.WriteLine(line);
                }
                w.Close();
            }
        }

        /// <summary>
        /// Changes the prefix of the server
        /// </summary>
        /// <param name="serverID">ID of the server</param>
        /// <param name="prefix">New prefix</param>
        public static void SetPrefix(ulong serverID, string prefix)
        {
            StreamWriter w = new StreamWriter("servers/" + serverID, false);

            w.Write(prefix);
            w.Close();
        }

        public static void UpdateUser(ulong userID)
        {
            if (!File.Exists("users/" + userID))
                return;

            string[] lines = File.ReadAllLines("users/" + userID);

            if (lines[0].StartsWith("version"))
            {
                if (lines[0].Substring(7).Equals(GetVersion()))
                    return;

                //TODO Put updates here
                StreamWriter w = new StreamWriter("users/" + userID, false);

                w.Close();
            }
            //update old accounts
            StreamWriter old = new StreamWriter("users/" + userID, false);
            old.WriteLine("version" + GetVersion());
            foreach(string line in lines)
            {
                old.WriteLine(line);
            }
            old.Close();
            return;
        }

        public static bool UserExists(ulong userID)
        {
            if (File.Exists("users/" + userID))
                return true;
            else
                return false;
        }
    }
}
