﻿using Jackal.Core;
using Jackal.Core.MapGenerator;
using Jackal.Core.Players;
using JackalWebHost2.Data.Interfaces;
using JackalWebHost2.Exceptions;
using JackalWebHost2.Models;
using JackalWebHost2.Models.Player;

namespace JackalWebHost2.Services;

public class GameService : IGameService
{
    private readonly IGameStateRepository _gameStateRepository;
    private readonly IGameRepository _gameRepository;
    private readonly IDrawService _drawService;
    
    public GameService(
        IGameStateRepository gameStateRepository, 
        IGameRepository gameRepository,
        IDrawService drawService)
    {
        _gameStateRepository = gameStateRepository;
        _gameRepository = gameRepository;
        _drawService = drawService;
    }
    
    public async Task<StartGameResult> StartGame(long userId, StartGameModel request)
    {
        GameSettings gameSettings = request.Settings;
        IPlayer[] gamePlayers = new IPlayer[gameSettings.Players.Length];
        int index = 0;

        foreach (var player in gameSettings.Players)
        {
            gamePlayers[index++] = player.Type switch
            {
                PlayerType.Robot => new RandomPlayer(),
                PlayerType.Human => new WebHumanPlayer(),
                _ => new EasyPlayer()
            };
        }

        gameSettings.MapId ??= new Random().Next();

        // для ручной отладки можно использовать закомментированные генераторы карт
        int mapSize = gameSettings.MapSize ?? 5;
        IMapGenerator mapGenerator = new RandomMapGenerator(gameSettings.MapId.Value, mapSize, gameSettings.TilesPackName);
        //mapGenerator = new OneTileMapGenerator(new TileParams(TileType.Airplane));
        // mapGenerator = new ThreeTileMapGenerator(
        //     new TileParams(TileType.Arrow) { ArrowsCode = ArrowsCodesHelper.ThreeArrows },
        //     new TileParams(TileType.Arrow) { ArrowsCode = ArrowsCodesHelper.FourArrowsDiagonal },
        //     new TileParams(TileType.Quake)
        // );

        var gameMode = gameSettings.GameMode ?? GameModeType.FreeForAll;
        var gameRequest = new GameRequest(mapSize, mapGenerator, gamePlayers, gameMode);
        var game = new Game(gameRequest);

        var gameId = await _gameRepository.CreateGame(userId, game);
        await _gameStateRepository.CreateGame(gameId, game);
        
        var map = _drawService.Map(game.Board);

        List<PirateChange> pirateChanges = [];
        foreach (var pirate in game.Board.AllPirates)
        {
            pirateChanges.Add(new PirateChange(pirate));
        }
        
        return new StartGameResult
        {
            GameId = gameId,
            GameMode = gameMode,
            Pirates = pirateChanges,
            Map = map,
            MapId = gameSettings.MapId.Value,
            Statistics = _drawService.GetStatistics(game),
            Teams = game.Board.Teams.Select(team => new DrawTeam(team)).ToList(),
            Moves = game.CurrentPlayer is WebHumanPlayer
                ? _drawService.GetAvailableMoves(game)
                : []
        };
    }
    
    public async Task<TurnGameResult> MakeGameTurn(long userId, TurnGameModel request)
    {
        var game = await _gameStateRepository.GetGame(request.GameId);
        if (game == null)
        {
            throw new GameNotFoundException();
        }

        if (game.IsGameOver)
        {
            return new TurnGameResult
            {
                PirateChanges = [],
                Changes = [],
                Statistics = _drawService.GetStatistics(game),
                TeamScores = game.Board.Teams.Select(team => new TeamScore(team)).ToList(),
                Moves = []
            };
        }
        
        var prevBoardStr = JsonHelper.SerializeWithType(game.Board);
            
        if (game.CurrentPlayer is WebHumanPlayer && request.TurnNum.HasValue)
        {
            game.CurrentPlayer.SetHumanMove(request.TurnNum.Value, request.PirateId);
        }

        game.Turn();
        if (game.IsGameOver)
        {
            game.Board.ShowUnknownTiles();
        }
        
        await _gameStateRepository.UpdateGame(request.GameId, game);
        var prevBoard = JsonHelper.DeserializeWithType<Board>(prevBoardStr);
        
        return new TurnGameResult
        {
            PirateChanges = _drawService.GetPirateChanges(game.Board, prevBoard),
            Changes = _drawService.GetTileChanges(game.Board, prevBoard),
            Statistics = _drawService.GetStatistics(game),
            TeamScores = game.Board.Teams.Select(team => new TeamScore(team)).ToList(),
            Moves = game.CurrentPlayer is WebHumanPlayer 
                ? _drawService.GetAvailableMoves(game) 
                : []
        };
    }
}