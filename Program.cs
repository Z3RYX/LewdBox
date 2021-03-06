﻿using Discord;
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

            await _client.LoginAsync(TokenType.Bot, TOKEN);

            await _client.StartAsync();

            string inp = Console.ReadLine();
            if (inp.Equals("exit"))
                Environment.Exit(0);

            if (inp.Equals("start"))
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
        public static void AddDaily(SocketUser user)
        {
            string[] lines = File.ReadAllLines("users/" + user.Id);
            lines[6] = DateTime.UtcNow.ToUniversalTime().ToString();
            StreamWriter w = new StreamWriter("users/" + user.Id);
            foreach(string line in lines)
            {
                w.WriteLine(line);
            }
            w.Close();
        }

        public static void AddMoney(SocketUser user, int value)
        {
            string[] lines = File.ReadAllLines("users/" + user.Id);
            int money = Convert.ToInt32(lines[3]);
            money += value;
            lines[3] = money.ToString();
            StreamWriter w = new StreamWriter("users/" + user.Id);
            foreach(string line in lines)
            {
                w.WriteLine(line);
            }
            w.Close();
        }

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

        public static bool CheckDaily(ulong UserID)
        {
            DateTime now = DateTime.UtcNow;
            string[] lines = File.ReadAllLines("users/" + UserID);
            DateTime lastDaily = DateTime.Parse(lines[6]);
            if (now.Subtract(TimeSpan.FromHours(24)) > lastDaily)
                return true;
            else
                return false;
        }

        public static bool CreateUser(SocketUser user)
        {
            if (File.Exists("users/" + user.Id))
                return true;

            string[] lines = File.ReadAllLines("PlayerID");
            int PlayerID = Convert.ToInt32(lines[0]);
            PlayerID++;
            StreamWriter id = new StreamWriter("PlayerID", false);
            id.Write(PlayerID);
            id.Close();

            StreamWriter w = new StreamWriter("users/" + user.Id);

            w.Write(
                "version" + GetVersion() + "\n" + //0 = Version
                user.Id + "\n" + //1 = User ID
                user.Username + "\n" + //2 = Username
                "2000\n" + //3 = Money
                PlayerID + "\n" + //4 = Player ID
                DateTime.UtcNow + "\n" + //5 = Register Date
                DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1440))); //6 = Last Daily

            w.Close();
            return false;
        }

        public static TimeSpan GetDaily(ulong userID)
        {
            string[] lines = File.ReadAllLines("users/" + userID);
            DateTime lastDaily = DateTime.Parse(lines[6]);
            TimeSpan result = DateTime.UtcNow.Subtract(lastDaily);
            result = TimeSpan.FromHours(24) - result;
            return result;
        }

        public static int GetPlayerID(SocketUser user)
        {
            string[] lines = File.ReadAllLines("users/" + user.Id);
            return Convert.ToInt32(lines[4]);
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

        public static Profile GetProfile(SocketUser user)
        {
            Profile profile = new Profile(user);
            return profile;
        }

        public static DateTime GetRegisterDate(SocketUser user)
        {
            string[] lines = File.ReadAllLines("users/" + user.Id);
            DateTime result = DateTime.Parse(lines[5]);
            return result;
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

        public static int GetUserMoney(SocketUser user)
        {
            string[] lines = File.ReadAllLines("users/" + user.Id);
            int rslt = Convert.ToInt32(lines[3]);
            return rslt;
        }

        public static string GetVersion()
        {
            string[] lines = File.ReadAllLines("botinfo");
            return lines[0];
        }

        public static void RemoveMoney(SocketUser user, int value)
        {
            string[] lines = File.ReadAllLines("users/" + user.Id);
            int money = Convert.ToInt32(lines[3]);
            money = money - value;
            lines[3] = money.ToString();
            StreamWriter w = new StreamWriter("users/" + user.Id);
            foreach (string line in lines)
            {
                w.WriteLine(line);
            }
            w.Close();
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

        public static void UpdateUser(SocketUser user)
        {
            if (!File.Exists("users/" + user.Id))
                return;

            string[] lines = File.ReadAllLines("users/" + user.Id);

            if (lines[0].StartsWith("version"))
            {
                if (lines[0].Substring(7).Equals(GetVersion()))
                    return;

                //TODO Put updates here
                StreamWriter w = new StreamWriter("users/" + user.Id, false);

                w.Write(
                "version" + GetVersion() + "\n" + //0 = Version
                user.Id + "\n" + //1 = User ID
                user.Username + "\n" + //2 = Username
                lines[3] + "\n" + //3 = Money
                lines[4] + "\n" + //4 = Player ID
                DateTime.Now + "\n" + //5 = Register Date
                DateTime.Now.Subtract(TimeSpan.FromHours(24))); //6 = Last Daily

                w.Close();
            }
        }

        public static bool UserExists(SocketUser user)
        {
            if (File.Exists("users/" + user.Id))
                return true;
            else
                return false;
        }
    }

    public class Profile
    {
        SocketUser user;
        public ulong ID { get; }
        public int Money { get; set; }
        public int PlayerID { get; }
        public DateTime RegisterDate { get; }

        public Profile(SocketUser user)
        {
            this.user = user;
            ID = user.Id;
            Money = FileSystem.GetUserMoney(user);
            PlayerID = FileSystem.GetPlayerID(user);
            RegisterDate = FileSystem.GetRegisterDate(user);
        }

        public void AddMoney(int value)
        {
            FileSystem.AddMoney(user, value);
            Money = FileSystem.GetUserMoney(user);
        }
    }
}
