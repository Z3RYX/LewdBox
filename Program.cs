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
            string TOKEN = null;

            //event subscription
            _client.Log += Log;

            await RegisterCommandsAsync();

            await _client.LoginAsync(Discord.TokenType.Bot, TOKEN);

            await _client.StartAsync();

            await _client.SetGameAsync("//help | lewdbox.zeryx.xyz");

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

            if (message.HasStringPrefix(prefix, ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos) || message.Content == "//help")
            {
                var result = await _commands.ExecuteAsync(context, argPos, _services);

                if (!result.IsSuccess)
                    Console.WriteLine(result.ErrorReason);
            }
        }
    }

    static class FileSystem
    {
        public static string GetPrefix(ulong serverID)
        {
            string prefix = "//";

            if (File.Exists(@"servers\" + serverID))
            {
                string[] lines = File.ReadAllLines(@"servers\" + serverID);
                prefix = lines[0];
            }

            return prefix;
        }

        public static void ResetPrefix(ulong serverID)
        {
            if (File.Exists(@"servers\" + serverID))
                File.Delete(@"servers\" + serverID);
        }

        public static void SetPrefix(ulong serverID, string prefix)
        {
            Directory.CreateDirectory("servers");
            StreamWriter w = new StreamWriter(@"servers\" + serverID, false);

            w.Write(prefix);
            w.Close();
        }
    }
}
