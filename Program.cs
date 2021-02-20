using AsyncAwaitBestPractices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TwitchBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var configBuilder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .AddEnvironmentVariables();

            IConfigurationRoot configuration = configBuilder.Build();
            var mySettingsConfig = new MySettingsConfig();
            configuration.GetSection("MySettings").Bind(mySettingsConfig);

            var builder = new HostBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHttpClient();
                services.AddTransient<MyApplication>();
            }).UseConsoleLifetime();

            var host = builder.Build();

            var twitchBot = new TwitchBot(mySettingsConfig.Ip, mySettingsConfig.Port, mySettingsConfig.UserName, mySettingsConfig.Password, mySettingsConfig.Channel);
            twitchBot.Start().SafeFireAndForget();

            await twitchBot.JoinChannel();
            await twitchBot.SendMessage("SqweebyBot has started listening.");

            twitchBot.OnMessage += async (sender, twitchChatMessage) =>
            {
                var apiUrl = "";

                if (twitchChatMessage.Message.StartsWith("!rank"))
                {
                    var name = twitchChatMessage.Message.Substring(twitchChatMessage.Message.IndexOf(' ') + 1);

                    if (twitchChatMessage.Message.Split(' ').First().ToLower() == "!rank")
                    {
                        await twitchBot.SendMessage($"Type !rankna for Americas !rankeu for Europe !ranksea for SEA and !rankasia for Asia.");
                        return;
                    }
                    else if (twitchChatMessage.Message.Split(' ').First().ToLower() == "!rankna")
                    {
                        apiUrl = "https://americas.api.riotgames.com/lor/ranked/v1/leaderboards";
                        LookUpPlayer(host, apiUrl, mySettingsConfig, twitchBot, name);
                    }
                    else if (twitchChatMessage.Message.Split(' ').First().ToLower() == "!rankeu")
                    {
                        apiUrl = "https://europe.api.riotgames.com/lor/ranked/v1/leaderboards";
                        LookUpPlayer(host, apiUrl, mySettingsConfig, twitchBot, name);
                    }
                    else if (twitchChatMessage.Message.Split(' ').First().ToLower() == "!ranksea")
                    {
                        apiUrl = "https://sea.api.riotgames.com/lor/ranked/v1/leaderboards";
                        LookUpPlayer(host, apiUrl, mySettingsConfig, twitchBot, name);
                    }
                    else if (twitchChatMessage.Message.Split(' ').First().ToLower() == "!rankasia")
                    {
                        apiUrl = "https://asia.api.riotgames.com/lor/ranked/v1/leaderboards";
                        LookUpPlayer(host, apiUrl, mySettingsConfig, twitchBot, name);
                    }
                    else { return; }
                }
                else if (twitchChatMessage.Message.StartsWith("!leaderboard"))
                {
                    var words = twitchChatMessage.Message.Split(' ');
                    if (twitchChatMessage.Message.Split(' ').First().ToLower() == "!leaderboard")
                    {
                        await twitchBot.SendMessage($"Type '!leaderboardna page 1' for Americas '!leaderboardeu page 1' for Europe '!leaderboardsea page 1' for SEA and '!leaderboardasia page 1' for Asia.");
                        return;
                    }
                    else if (words[0].ToLower() == "!leaderboardna" && words[1].ToLower() == "page" && !string.IsNullOrWhiteSpace(words[2]))
                    {
                        apiUrl = "https://americas.api.riotgames.com/lor/ranked/v1/leaderboards";
                        // Check if the thirdWord is a page number.
                        if (!long.TryParse(words[2], out long number1))
                        {
                            await twitchBot.SendMessage($"{words[2]} is not a valid page number. Try !rankna page 1");
                            return;
                        }

                        GetLeaderboardByPage(host, apiUrl, mySettingsConfig, twitchBot, words[2]);
                    }
                    else if (words[0].ToLower() == "!leaderboardeu" && words[1].ToLower() == "page" && !string.IsNullOrWhiteSpace(words[2]))
                    {
                        apiUrl = "https://europe.api.riotgames.com/lor/ranked/v1/leaderboards";
                        // Check if the thirdWord is a page number.
                        if (!long.TryParse(words[2], out long number1))
                        {
                            await twitchBot.SendMessage($"{words[2]} is not a valid page number. Try !rankna page 1");
                            return;
                        }

                        GetLeaderboardByPage(host, apiUrl, mySettingsConfig, twitchBot, words[2]);
                    }
                    else if (words[0].ToLower() == "!leaderboardsea" && words[1].ToLower() == "page" && !string.IsNullOrWhiteSpace(words[2]))
                    {
                        apiUrl = "https://sea.api.riotgames.com/lor/ranked/v1/leaderboards";
                        // Check if the thirdWord is a page number.
                        if (!long.TryParse(words[2], out long number1))
                        {
                            await twitchBot.SendMessage($"{words[2]} is not a valid page number. Try !rankna page 1");
                            return;
                        }

                        GetLeaderboardByPage(host, apiUrl, mySettingsConfig, twitchBot, words[2]);
                    }
                    else if (words[0].ToLower() == "!leaderboardasia" && words[1].ToLower() == "page" && !string.IsNullOrWhiteSpace(words[2]))
                    {
                        apiUrl = "https://asia.api.riotgames.com/lor/ranked/v1/leaderboards";
                        // Check if the thirdWord is a page number.
                        if (!long.TryParse(words[2], out long number1))
                        {
                            await twitchBot.SendMessage($"{words[2]} is not a valid page number. Try !rankna page 1");
                            return;
                        }

                        GetLeaderboardByPage(host, apiUrl, mySettingsConfig, twitchBot, words[2]);
                    }
                }
                else { return; }
            };

            await Task.Delay(-1);
        }

        public static async void LookUpPlayer(IHost host, string apiUrl, MySettingsConfig mySettingsConfig, TwitchBot twitchBot, string player)
        {
            using (var serviceScope = host.Services.CreateScope())
            {
                var services = serviceScope.ServiceProvider;

                try
                {

                    int rank = 0;
                    string lp = "";
                    string name = "";
                    var inMasters = false;

                    Players players = JsonConvert.DeserializeObject<Players>(await services.GetRequiredService<MyApplication>().Run(apiUrl, mySettingsConfig.RiotApiKey));

                    foreach (var p in players.data)
                    {
                        if (p.Name.ToLower() == player.ToLower())
                        {
                            inMasters = true;
                            name = p.Name;
                            rank = Convert.ToInt32(p.Rank) + 1;
                            lp = p.Lp.Substring(0, p.Lp.IndexOf('.', 0));
                        }
                    }

                    if (inMasters)
                    {
                        await twitchBot.SendMessage($"{name}'s rank is {rank} and their LP is {lp}.");
                    }
                    else
                    {
                        await twitchBot.SendMessage($"{player} is not in masters KEKW");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error Occured {ex}");
                }
            }
        }

        public static async void GetLeaderboardByPage(IHost host, string apiUrl, MySettingsConfig mySettingsConfig, TwitchBot twitchBot, string page)
        {
            using (var serviceScope = host.Services.CreateScope())
            {
                var services = serviceScope.ServiceProvider;

                try
                {

                    int rank = 0;
                    string lp = "";
                    string name = "";
                    string playersOutput = "";

                    Players players = JsonConvert.DeserializeObject<Players>(await services.GetRequiredService<MyApplication>().Run(apiUrl, mySettingsConfig.RiotApiKey));
                    var queryable = players.data.AsQueryable();
                    var pager = new Pager(queryable.Count(), Convert.ToInt32(page), 8, queryable.Count() / 8);

                    if (pager.CurrentPage > 1) 
                    {
                        queryable = queryable.Skip(pager.PageSize * (pager.CurrentPage - 1));
                    }

                    if (pager.Pages.Count() > pager.PageSize) 
                    {
                        queryable = queryable.Take(pager.PageSize);
                    }

                    foreach (var p in queryable)
                    {
                        name = p.Name;
                        rank = Convert.ToInt32(p.Rank) + 1;
                        lp = p.Lp.Substring(0, p.Lp.IndexOf('.', 0));
                        playersOutput += $" rank:_{rank}_name:_{name}_LP:_{lp}______";
                    }
                    await twitchBot.SendMessage(playersOutput);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error Occured {ex}");
                }
            }
        }
    }
}
