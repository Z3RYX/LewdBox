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

            await RegisterCommandsAsync();

            await _client.LoginAsync(Discord.TokenType.Bot, TOKEN);

            await _client.StartAsync();

            string[] Players = File.ReadAllLines("PlayerID");

            await _client.SetGameAsync("//help | " + Players[0] + " Players");

            await Task.Delay(-1);
        }

        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);

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
        /// <summary>
        /// Creates the user profile
        /// </summary>
        /// <param name="userID">ID of the user</param>
        /// <param name="name">Username of the user</param>
        /// <param name="avaURL">URL to the users avatar</param>
        public static void CreateUser(ulong userID, string name, string avaURL)
        {
            if (File.Exists("users/" + userID))
                return;

            string[] lines = File.ReadAllLines("PlayerID");
            int PlayerID = Convert.ToInt32(lines[0]);
            PlayerID++;
            StreamWriter id = new StreamWriter("PlayerID", false);
            id.Write(PlayerID);
            id.Close();

            StreamWriter w = new StreamWriter("users/" + userID);

            w.Write(
                userID + "\n" +
                name + "\n" +
                avaURL + "\n" +
                "2000\n" +
                PlayerID);

            w.Close();
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

        /// <summary>
        /// Resets the prefix of the server back to //
        /// </summary>
        /// <param name="serverID">ID of the server</param>
        public static void ResetPrefix(ulong serverID)
        {
            if (File.Exists(@"servers/" + serverID))
                File.Delete(@"servers/" + serverID);
        }

        /// <summary>
        /// Changes the prefix of the server
        /// </summary>
        /// <param name="serverID">ID of the server</param>
        /// <param name="prefix">New prefix</param>
        public static void SetPrefix(ulong serverID, string prefix)
        {
            Directory.CreateDirectory("servers");
            StreamWriter w = new StreamWriter(@"servers/" + serverID, false);

            w.Write(prefix);
            w.Close();
        }
    }
}
