﻿using System.Collections.Generic;
using System.Linq;
using Jackal.Core.Domain;

namespace Jackal.Core.Actions;

internal class MovingAction(TilePosition from, TilePosition to, TilePosition prev) : IGameAction
{
    public TilePosition To = to;

    public void Act(Game game, Pirate pirate)
    {
        Board board = game.Board;
        Map map = board.Map;

        Team ourTeam = board.Teams[pirate.TeamId];
        Team? allyTeam = ourTeam.AllyTeamId.HasValue 
            ? board.Teams[ourTeam.AllyTeamId.Value] 
            : null;
            
        Tile targetTile = map[To.Position];
        Tile sourceTile = map[from.Position];
        Tile prevTile = map[prev.Position];
            
        // открываем закрытую клетку
        bool newTile = false;
        if (targetTile.Type == TileType.Unknown)
        {
            targetTile = board.OpenTile(To.Position);
            game.LastActionTurnNumber = game.TurnNumber;
            game.CoinsOnMap += targetTile.CoinsCount();
            game.CoinsOnMap += targetTile.BigCoinsCount() * Constants.BigCoinValue;
            newTile = true;
        }
        
        // воздушный шар переносит сразу на наш корабль
        if (targetTile.Type == TileType.Balloon)
        {
            To = new TilePosition(ourTeam.ShipPosition);
        }
            
        // пушка выстреливает сразу в воду
        if (targetTile.Type == TileType.Cannon)
        {
            To = GetCannonFly(targetTile.Direction, To.Position, board.MapSize);
        }
            
        // ходим по задерживающей клетке
        if (newTile && targetTile.Type == TileType.Spinning)
        {
            To = new TilePosition(To.Position, targetTile.SpinningCount - 1);
        }
            
        // нашли карамбу
        if (targetTile is { Type: TileType.Caramba, Used: false })
        {
            // проходим по всем командам и собираем пиратов на кораблях
            foreach (var team in board.Teams)
            {
                foreach (var pirateOnMap in team.Pirates)
                {
                    if (pirateOnMap.Position.Position == team.ShipPosition)
                        continue;

                    // возвращаем пирата на его корабль
                    game.MovePirateToTheShip(pirateOnMap);
                }
            }
                
            To = new TilePosition(ourTeam.ShipPosition);
            targetTile.Used = true;
        }
        
        // нашли разлом
        if (targetTile is { Type: TileType.Quake, Used: false })
        {
            game.SubTurn.QuakePhase = 2;
            game.NeedSubTurnPirate = pirate;
            game.PrevSubTurnPosition = prev;
            targetTile.Used = true;
        }
        
        // нашли Бен Ганна не маяком
        if (targetTile is { Type: TileType.BenGunn, Used: false } && 
            game.SubTurn.LighthouseViewCount == 0)
        {
            game.AddPirate(pirate.TeamId, To, PirateType.BenGunn);
            targetTile.Used = true;
        }
        
        // нашли бутылки с ромом не маяком
        if (targetTile is { Type: TileType.RumBottle, Used: false } && 
            game.SubTurn.LighthouseViewCount == 0)
        {
            board.Teams[pirate.TeamId].RumBottles += targetTile.Code;
            if (allyTeam != null)
            {
                allyTeam.RumBottles += targetTile.Code;
            }
            
            targetTile.Used = true;
        }
        
        // нашли хи-хи траву не маяком
        if (targetTile is { Type: TileType.Cannabis, Used: false } && 
            game.SubTurn.LighthouseViewCount == 0)
        {
            game.SubTurn.CannabisTurnCount += board.Teams.Length + 1;
            targetTile.Used = true;
        }
            
        // просматриваем карту с маяка,
        // перезатираем просматриваемую клетку текущей позицией пирата,
        // важно вызвать после всех установок поля to
        if (game.SubTurn.LighthouseViewCount > 0)
        {
            game.SubTurn.LighthouseViewCount--;
            To = pirate.Position;

            if (targetTile.Type == TileType.Hole)
            {
                var holeTiles = board.AllTiles(x => x.Type == TileType.Hole).ToList();
                if (holeTiles.Count == 2)
                {
                    // открыли вторую дыру - пираты меняются местами
                    game.SwapPiratePosition(holeTiles[0], holeTiles[1]);
                }
            }
        }
            
        // нашли маяк
        if (targetTile is { Type: TileType.Lighthouse, Used: false })
        {
            var unknownTilesCount = game.Board.AllTiles(x => x.Type == TileType.Unknown).Count();
            var remainedTilesViewCount = unknownTilesCount - game.SubTurn.LighthouseViewCount;
            game.SubTurn.LighthouseViewCount += remainedTilesViewCount < 4 ? remainedTilesViewCount : 4;
            targetTile.Used = true;
        }
            
        targetTile = map[To.Position];
        TileLevel targetTileLevel = map[To];
        TileLevel fromTileLevel = map[from];
            
        if (from.Position == ourTeam.ShipPosition &&
            targetTile.Type == TileType.Water &&
            Board.GetPossibleShipMoves(ourTeam.ShipPosition, game.Board.MapSize).Contains(To.Position)) 
        {
            // двигаем свой корабль
            var pirateOnShips = map[ourTeam.ShipPosition].Pirates;
            foreach (var pirateOnShip in pirateOnShips)
            {
                pirateOnShip.Position = To;
                targetTileLevel.Pirates.Add(pirateOnShip);
            }
            ourTeam.ShipPosition = To.Position;
            sourceTile.Pirates.Clear();
        }
        else if (allyTeam != null &&
                 from.Position == allyTeam.ShipPosition &&
                 targetTile.Type == TileType.Water &&
                 Board.GetPossibleShipMoves(allyTeam.ShipPosition, game.Board.MapSize).Contains(To.Position))
        {
            // двигаем союзный корабль
            var pirateOnShips = map[allyTeam.ShipPosition].Pirates;
            foreach (var pirateOnShip in pirateOnShips)
            {
                pirateOnShip.Position = To;
                targetTileLevel.Pirates.Add(pirateOnShip);
            }
            allyTeam.ShipPosition = To.Position;
            sourceTile.Pirates.Clear();
        }
        else
        {
            // двигаем своего пирата
            fromTileLevel.Pirates.Remove(pirate);

            pirate.Position = To;
            targetTileLevel.Pirates.Add(pirate);
        }

        if (game.SubTurn.LighthouseViewCount > 0 ||
            (targetTile is { Used: false, Type: TileType.Airplane } && 
             from != To))
        {
            game.NeedSubTurnPirate = pirate;
            game.PrevSubTurnPosition = prev;
        }
        
        // заход в дыру, не из дыры
        if (targetTile.Type == TileType.Hole && !game.SubTurn.FallingInTheHole)
        {
            var holeTiles = board.AllTiles(x => x.Type == TileType.Hole).ToList();
            
            var freeHoleTiles = holeTiles
                .Where(x => x.Position != targetTile.Position && x.HasNoEnemy(ourTeam.EnemyTeamIds))
                .ToList();
            
            if(holeTiles.Count == 1)
            {
                // пират застрял в единственной дыре
                pirate.IsInHole = true;
            }
            else if (newTile && holeTiles.Count == 2)
            {
                // открыли вторую дыру - пираты меняются местами
                game.SwapPiratePosition(holeTiles[0], holeTiles[1]);
            }
            else if (freeHoleTiles.Count >= 1)
            {
                // даем выбор куда идти, брать монету или нет:
                // доступная одна свободная дыра, но на ней монета или идем с монетой
                // доступно несколько свободных дыр
                game.NeedSubTurnPirate = pirate;
                game.PrevSubTurnPosition = prev;
                game.SubTurn.FallingInTheHole = true;
            }
        }

        if (newTile && targetTile.Type is TileType.Arrow or TileType.Horse or TileType.Ice or TileType.Crocodile)
        {
            var airplaneFlying = targetTile.Type is TileType.Ice or TileType.Crocodile &&
                                 (prevTile is { Type: TileType.Airplane, Used: false } ||
                                  game.SubTurn.AirplaneFlying);

            AvailableMovesTask task = new AvailableMovesTask(pirate.TeamId, To, prev);
            List<AvailableMove> moves = game.Board.GetAllAvailableMoves(
                task,
                task.Source,
                task.Prev,
                new SubTurnState()
            );

            if (moves.Count == 0 && 
                !airplaneFlying)
            {
                game.KillPirate(pirate);
                return;
            }
                
            game.NeedSubTurnPirate = pirate;
            game.PrevSubTurnPosition = prev;
            game.SubTurn.AirplaneFlying = airplaneFlying;
        }
        else
        {
            game.SubTurn.AirplaneFlying = false;
        }
            
        // отмечаем, что мы использовали самолет
        if (from != To)
        {
            if(sourceTile is { Type: TileType.Airplane, Used: false })
                sourceTile.Used = true;
                
            if(prevTile is { Type: TileType.Airplane, Used: false })
                prevTile.Used = true;
        }

        // проверяем, не попадаем ли мы на чужой корабль - тогда мы погибли
        IEnumerable<Position> enemyShips = game.Board.Teams
            .Where(x => ourTeam.EnemyTeamIds.Contains(x.Id))
            .Select(x => x.ShipPosition);
            
        if (enemyShips.Contains(To.Position))
        {
            game.KillPirate(pirate);
            return;
        }
        
        if (targetTileLevel.Pirates.Any(x => x.TeamId == pirate.TeamId))
        {
            // убиваем чужих пиратов
            var enemyPirates = targetTileLevel.Pirates
                .Where(x => ourTeam.EnemyTeamIds.Contains(x.TeamId))
                .ToList();

            foreach (var enemyPirate in enemyPirates)
            {
                if (targetTile.Type == TileType.Jungle)
                    continue;

                if (targetTile.Type == TileType.Water)
                    game.KillPirate(enemyPirate);

                game.MovePirateToTheShip(enemyPirate);
            }
        }
        
        switch (targetTile.Type)
        {
            case TileType.RumBarrel:
                // проводим пьянку для пирата
                pirate.DrunkSinceTurnNumber = game.TurnNumber;
                pirate.IsDrunk = true;
                break;
            case TileType.Trap:
                if (targetTile.Pirates.Count > 1)
                {
                    foreach (Pirate pirateOnTile in targetTile.Pirates)
                    {
                        pirateOnTile.IsInTrap = false;
                    }
                }
                else
                {
                    pirate.IsInTrap = true;
                }
                break;
            case TileType.Cannibal:
                game.KillPirate(pirate);
                return;
        }
    }
        
    private static TilePosition GetCannonFly(DirectionType direction, Position pos, int mapSize) =>
        direction switch
        {
            DirectionType.Up => new TilePosition(pos.X, mapSize - 1),
            DirectionType.Right => new TilePosition(mapSize - 1, pos.Y),
            DirectionType.Down => new TilePosition(pos.X, 0),
            _ => new TilePosition(0, pos.Y)
        };
}