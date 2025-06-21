﻿using System;
using System.Collections.Generic;
using System.Linq;
using Jackal.Core;
using Jackal.Core.MapGenerator;
using Jackal.Core.MapGenerator.TilesPack;
using Jackal.Core.Players;

namespace Jackal.BotArena;

/// <summary>
/// Консольное приложение для оценки силы игроков ботов
/// </summary>
internal static class Program
{
    /// <summary>
    /// Количество запускаемых игр
    /// </summary>
    private const int ArenaGamesCount = 120;

    /// <summary>
    /// Размер карты
    /// </summary>
    private const int MapSize = 13;
    
    /// <summary>
    /// Комбинации игроков ботов и их позиций (зависит от порядка)
    /// </summary>
    private static readonly IPlayer[][] CombinationOfPlayers =
    [
        [
            new EasyPlayer(),
            new RandomPlayer()
        ],
        [
            new EasyPlayer(),
            new OakioPlayer(),
        ],
        [
            new OakioPlayer(),
            new RandomPlayer()
        ]
    ];
    
    /// <summary>
    /// Статистика по каждому игроку боту
    /// </summary>
    private static readonly Dictionary<string, GamePlayerStat> BotStat = new();

    private static void Main()
    {
        int gameNumber = 0;

        var timeElapsed = StopwatchMeter.GetElapsed(() =>
        {
            while (gameNumber < ArenaGamesCount)
            {
                var mapId = new Random().Next();
                var randomMap = new RandomMapGenerator(mapId, MapSize, TilesPackFactory.Extended);

                foreach (var players in CombinationOfPlayers)
                {
                    var gameRequest = new GameRequest(MapSize, randomMap, players);
                    var game = new Game(gameRequest);

                    while (game.IsGameOver == false)
                    {
                        game.Turn();
                    }

                    CalcStat(game);
                    
                    gameNumber++;
                    if (gameNumber == ArenaGamesCount)
                    {
                        break;
                    }
                }
            }
        });

        ShowStat(gameNumber, timeElapsed);
    }

    private static void CalcStat(Game game)
    {
        var maxCoins = game.Board.Teams.Max(x => x.Coins);
        foreach (var team in game.Board.Teams)
        {
            if (!BotStat.TryGetValue(team.Name, out var stat))
            {
                stat = new GamePlayerStat { PlayerName = team.Name };
                BotStat.Add(team.Name, stat);
            }
            stat.TotalWin += team.Coins == maxCoins ? 1 : 0;
            stat.TotalCoins += team.Coins;
            stat.GamesCount += 1;
        }
    }

    private static void ShowStat(int gamesCount, TimeSpan timeElapsed)
    {
        Console.WriteLine($"Arena games count = {gamesCount} | Time elapsed {timeElapsed}");
        var orderedBotStat = BotStat.OrderByDescending(p => p.Value.Rating);
        foreach (var (_, gamePlayerStat) in orderedBotStat)
        {
            Console.WriteLine(
                $"Player name = {gamePlayerStat.PlayerName} | " +
                $"Rating = {gamePlayerStat.Rating} | " +
                $"Average win = {gamePlayerStat.AverageWin:P} | " +
                $"Total win = {gamePlayerStat.TotalWin} | " +
                $"Games count = {gamePlayerStat.GamesCount} | " +
                $"Average coins = {gamePlayerStat.AverageCoins:F} | " +
                $"Total coins = {gamePlayerStat.TotalCoins}"
            );
        }
    }
}