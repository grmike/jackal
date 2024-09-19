﻿using System;
using System.Collections.Generic;
using System.Linq;
using Jackal.Core.Actions;
using Newtonsoft.Json;

namespace Jackal.Core
{
    public class Board
    {
        [JsonIgnore]
        public readonly IMapGenerator Generator;
        
        /// <summary>
        /// Размер стороны карты с учетом воды
        /// </summary>
        public readonly int MapSize;
        
        public readonly Map Map;

        public Team[] Teams;

        [JsonIgnore]
        public List<Pirate> AllPirates
        {
            get
            {
                var allPirates = new List<Pirate>();
                foreach (var teamPirates in Teams.Select(item => item.Pirates.ToList<Pirate>()))
                {
                    allPirates.AddRange(teamPirates);
                }

                return allPirates;
            }
        }
        
        [JsonIgnore]
        public List<Pirate>? DeadPirates { get; set; }

        public IEnumerable<Tile> AllTiles(Predicate<Tile> selector)
        {
            for (int i = 0; i < MapSize; i++)
            {
                for (int j = 0; j < MapSize; j++)
                {
                    var tile = Map[i, j];
                    if (selector(tile))
                        yield return tile;
                }
            }
        }

        [JsonConstructor]
        public Board(int mapSize, Map map, Team[] teams)
        {
            MapSize = mapSize;
            Map = map;
            Teams = teams;
        }

        public Board(IPlayer[] players, IMapGenerator mapGenerator, int mapSize, int piratesPerPlayer)
        {
            Generator = mapGenerator;
            MapSize = mapSize;
            Map = new Map(mapSize);
            InitMap();
            InitTeams(players, piratesPerPlayer);
        }

        private void InitTeams(IPlayer[] players, int piratesPerPlayer)
        {
            Teams = new Team[players.Length];
            switch (players.Length)
            {
                case 1:
                    InitTeam(0, players[0].GetType().Name, (MapSize - 1) / 2, 0, piratesPerPlayer);
                    Teams[0].Enemies = [];
                    break;
                case 2:
                    InitTeam(0, players[0].GetType().Name, (MapSize - 1) / 2, 0, piratesPerPlayer);
                    InitTeam(1, players[1].GetType().Name, (MapSize - 1) / 2, (MapSize - 1), piratesPerPlayer);
                    Teams[0].Enemies = [1];
                    Teams[1].Enemies = [0];
                    break;
                case 4:
                    InitTeam(0, players[0].GetType().Name, (MapSize - 1) / 2, 0, piratesPerPlayer);
                    InitTeam(1, players[1].GetType().Name, 0, (MapSize - 1) / 2, piratesPerPlayer);
                    InitTeam(2, players[2].GetType().Name, (MapSize - 1) / 2, (MapSize - 1), piratesPerPlayer);
                    InitTeam(3, players[3].GetType().Name, (MapSize - 1), (MapSize - 1) / 2, piratesPerPlayer);
                    Teams[0].Enemies = [1, 2, 3];
                    Teams[1].Enemies = [0, 2, 3];
                    Teams[2].Enemies = [0, 1, 3];
                    Teams[3].Enemies = [0, 1, 2];
                    break;
                default:
                    throw new NotSupportedException("Only one player, two players or four");
            }
        }

        private void InitMap()
        {
            for (int i = 0; i < MapSize; i++)
            {
                SetWater(i, 0);
                SetWater(0, i);
                SetWater(i, MapSize - 1);
                SetWater(MapSize - 1, i);
            }

            for (int x = 1; x < MapSize - 1; x++)
            {
                for (int y = 1; y < MapSize - 1; y++)
                {
                    if ((x==1 || x==MapSize-2) && (y==1||y==MapSize-2) )
                        SetWater(x, y);
                    else
                        SetUnknown(x, y);
                }
            }
        }

        private void SetWater(int x, int y)
        {
            var tile = new Tile(new TileParams {Type = TileType.Water, Position = new Position(x, y)});
            Map[x, y] = tile;
        }

        private void SetUnknown(int x, int y)
        {
            var tile = new Tile(new TileParams {Type = TileType.Unknown, Position = new Position(x, y)});
            Map[x, y] = tile;
        }

        private void InitTeam(int teamId, string teamName, int x, int y, int piratesPerPlayer)
        {
            var startPosition = new Position(x, y);
            var pirates = new Pirate[piratesPerPlayer];
            for (int i = 0; i < pirates.Length; i++)
            {
                pirates[i] = new Pirate(teamId, new TilePosition(startPosition), PirateType.Usual);
            }
            var ship = new Ship(teamId, startPosition);
            foreach (var pirate in pirates)
            {
                Map[ship.Position].Pirates.Add(pirate);
            }
            Teams[teamId] = new Team(teamId, teamName, ship, pirates);
        }

        /// <summary>
        /// Возвращаем список всех полей, в которые можно попасть из исходного поля
        /// </summary>
        public List<AvailableMove> GetAllAvailableMoves(
            AvailableMovesTask task, TilePosition source, TilePosition prev, bool airplaneFlying)
        {
            Tile sourceTile = Map[source.Position];

            int ourTeamId = task.TeamId;
            Team ourTeam = Teams[ourTeamId];
            Ship ourShip = ourTeam.Ship;

            if (sourceTile.Type is TileType.Arrow or TileType.Horse or TileType.Ice or TileType.Crocodile)
            {
                var prevMoveDelta = sourceTile.Type == TileType.Ice
                    ? Position.GetDelta(prev.Position, source.Position)
                    : null;

                // запоминаем, что в текущую клетку уже не надо возвращаться
                task.AlreadyCheckedList.Add(new CheckedPosition(source, prevMoveDelta));
            }

            var goodTargets = new List<AvailableMove>();
            
            // места всех возможных ходов
            IEnumerable<TilePosition> positionsForCheck = GetAllTargetsForSubTurn(source, prev, ourTeam, airplaneFlying);

            foreach (TilePosition newPosition in positionsForCheck)
            {
                if (task.AlreadyCheckedList.Count > 0)
                {
                    Position incomeDelta = Position.GetDelta(source.Position, newPosition.Position);
                    CheckedPosition currentCheck = new CheckedPosition(newPosition, incomeDelta);

                    if (WasCheckedBefore(task.AlreadyCheckedList, currentCheck))
                    {
                        // попали по рекурсии в ранее просмотренную клетку
                        continue;
                    }
                }
                
                var moving = new Moving(task.Source, newPosition, source);
                var movingWithCoin = new Moving(task.Source, newPosition, source, true);
                
                // проверяем, что на этой клетке
                var newPositionTile = Map[newPosition.Position];

                switch (newPositionTile.Type)
                {
                    case TileType.Unknown:
                        var availableMove = new AvailableMove(task.Source, newPosition, moving)
                        {
                            MoveType = sourceTile is { Type: TileType.Lighthouse, Used: false }
                                ? MoveType.WithLighthouse
                                : MoveType.Usual
                        };
                        goodTargets.Add(availableMove);
                        break;

                    case TileType.Water:
                        if (ourShip.Position == newPosition.Position)
                        {
                            // заходим на свой корабль
                            goodTargets.Add(new AvailableMove(task.Source, newPosition, moving));
                            if (Map[task.Source].Coins > 0)
                                goodTargets.Add(new AvailableMove(task.Source, newPosition, movingWithCoin)
                                {
                                    MoveType = MoveType.WithCoin
                                });
                        }
                        else if (sourceTile.Type == TileType.Water)
                        {
                            // из воды в воду
                            if (source.Position != ourShip.Position &&
                                GetPossibleSwimming(task.Source.Position).Contains(newPosition.Position))
                            {
                                // пират плавает
                                var move = new AvailableMove(task.Source, newPosition, moving);
                                goodTargets.Add(move);
                            }

                            if (source.Position == ourShip.Position &&
                                GetPossibleShipMoves(task.Source.Position, MapSize).Contains(newPosition.Position))
                            {
                                // корабль плавает
                                var move = new AvailableMove(task.Source, newPosition, moving);
                                goodTargets.Add(move);
                            }
                        }
                        else if (sourceTile.Type is TileType.Arrow or TileType.Cannon or TileType.Ice)
                        {
                            // с земли в воду мы можем попасть только если ранее попали на клетку, требующую хода
                            goodTargets.Add(new AvailableMove(task.Source, newPosition, moving));

                            if (Map[task.Source].Coins > 0)
                                goodTargets.Add(new AvailableMove(task.Source, newPosition, movingWithCoin)
                                {
                                    MoveType = MoveType.WithCoin
                                });
                        }

                        break;

                    case TileType.RespawnFort:
                        if (task.Source == newPosition)
                        {
                            if (ourTeam.Pirates.Length < 3)
                                goodTargets.Add(new AvailableMove(task.Source, newPosition, moving, new Respawn())
                                {
                                    MoveType = MoveType.WithRespawn
                                });
                        }
                        else if (newPositionTile.OccupationTeamId.HasValue == false ||
                                 newPositionTile.OccupationTeamId == ourTeamId)
                        {
                            // форт не занят противником
                            goodTargets.Add(new AvailableMove(task.Source, newPosition, moving));
                        }

                        break;

                    case TileType.Fort:
                        if (newPositionTile.OccupationTeamId.HasValue == false ||
                            newPositionTile.OccupationTeamId == ourTeamId)
                        {
                            // форт не занят противником
                            goodTargets.Add(new AvailableMove(task.Source, newPosition, moving));
                        }

                        break;
                    
                    case TileType.Arrow:
                    case TileType.Horse:
                    case TileType.Ice:
                    case TileType.Crocodile:
                    case TileType.Cannon:
                        goodTargets.AddRange(GetAllAvailableMoves(task, newPosition, source, airplaneFlying));
                        break;
                    default:
                        goodTargets.Add(new AvailableMove(task.Source, newPosition, moving));

                        if (Map[task.Source].Coins > 0
                            && (newPositionTile.OccupationTeamId == null ||
                                newPositionTile.OccupationTeamId == ourTeamId))
                        {
                            goodTargets.Add(new AvailableMove(task.Source, newPosition, movingWithCoin)
                            {
                                MoveType = MoveType.WithCoin
                            });
                        }

                        break;
                }
            }
            
            return goodTargets;
        }

        /// <summary>
        /// Возвращаем все позиции, в которые в принципе достижимы из заданной клетки за один подход
        /// (не проверяется, допустим ли такой ход)
        /// </summary>
        private List<TilePosition> GetAllTargetsForSubTurn(
            TilePosition source, TilePosition prev, Team ourTeam, bool airplaneFlying)
        {
            var sourceTile = Map[source.Position];
            var ourShip = ourTeam.Ship;

            IEnumerable<TilePosition> rez = GetNearDeltas(source.Position)
                .Where(IsValidMapPosition)
                .Where(x => Map[x].Type != TileType.Water || x == ourShip.Position)
                .Select(IncomeTilePosition);
            
            switch (sourceTile.Type)
            {
                case TileType.Horse:
                    rez = GetHorseDeltas(source.Position)
                        .Where(IsValidMapPosition)
                        .Where(x =>
                            Map[x].Type != TileType.Water || Teams.Select(t => t.Ship.Position).Contains(x)
                        )
                        .Select(IncomeTilePosition);
                    break;
				case TileType.Cannon:
                    rez = new[] { IncomeTilePosition(GetCannonFly(sourceTile.CannonDirection, source.Position)) };
					break;
                case TileType.Arrow:
                    rez = GetArrowsDeltas(sourceTile.ArrowsCode, source.Position)
                        .Select(IncomeTilePosition);
                    break;
                case TileType.Lighthouse:
                    if (sourceTile.Used == false)
                    {
                        rez = AllTiles(x => x.Type == TileType.Unknown)
                            .Select(x => IncomeTilePosition(x.Position));
                    }
                    break;
                case TileType.Airplane:
                    if (sourceTile.Used == false)
                    {
                        rez = AllTiles(x =>
                                x.Type != TileType.Ice &&
                                (x.Type != TileType.Water || x.Position == ourShip.Position)
                            )
                            .Select(x => IncomeTilePosition(x.Position));
                    }
                    break;
                case TileType.Crocodile:
                    if (airplaneFlying)
                    {
                        rez = AllTiles(x =>
                                x.Type != TileType.Ice &&
                                (x.Type != TileType.Water || x.Position == ourShip.Position)
                            )
                            .Select(x => IncomeTilePosition(x.Position));
                        break;
                    }
                    
                    rez = new[] { IncomeTilePosition(prev.Position) };
                    break;
                case TileType.Ice:
                    if (airplaneFlying)
                    {
                        rez = AllTiles(x =>
                                x.Type != TileType.Ice &&
                                (x.Type != TileType.Water || x.Position == ourShip.Position)
                            )
                            .Select(x => IncomeTilePosition(x.Position));
                        break;
                    }

                    var prevDelta = Position.GetDelta(prev.Position, source.Position);
                    var target = Position.AddDelta(source.Position, prevDelta);
                    rez = new[] { IncomeTilePosition(target) };
                    break;
                case TileType.RespawnFort:
                    rez = rez.Concat(new[] {source});
                    break;
                case TileType.Spinning:
                    if (source.Level > 0)
                    {
                        rez = new[] {new TilePosition(source.Position, source.Level - 1)};
                    }
                    break;
                case TileType.Water:
                    if (source.Position == ourShip.Position)
                    {
                        // со своего корабля
                        rez = GetPossibleShipMoves(source.Position, MapSize)
                            .Concat(new[] {GetShipLanding(source.Position)})
                            .Select(IncomeTilePosition);
                    }
                    else
                    {
                        // пират плавает в воде
                        rez = GetPossibleSwimming(source.Position)
                            .Select(IncomeTilePosition);
                    }
                    break;
            }
            
            return rez.Where(x => IsValidMapPosition(x.Position)).ToList();
        }

        private TilePosition IncomeTilePosition(Position pos)
        {
            return IsValidMapPosition(pos) && Map[pos].Type == TileType.Spinning
                ? new TilePosition(pos, Map[pos].SpinningCount - 1)
                : new TilePosition(pos);
        }

        private static IEnumerable<Position> GetHorseDeltas(Position pos)
        {
            for (int x = -2; x <= 2; x++)
            {
                if (x == 0) continue;
                int deltaY = (Math.Abs(x) == 2) ? 1 : 2;
                yield return new Position(pos.X + x, pos.Y - deltaY);
                yield return new Position(pos.X + x, pos.Y + deltaY);
            }
        }

        private static IEnumerable<Position> GetNearDeltas(Position pos)
        {
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue;
                    yield return new Position(pos.X + x, pos.Y + y);
                }
            }
        }

        private bool IsValidMapPosition(Position pos)
        {
            return (
                pos.X >= 0 && pos.X < MapSize
                           && pos.Y >= 0 && pos.Y < MapSize //попадаем в карту
                           && Utils.InCorners(pos, 0, MapSize - 1) == false //не попадаем в углы карты
            );
        }

        public static IEnumerable<Position> GetPossibleShipMoves(Position shipPosition, int mapSize)
        {
            if (shipPosition.X == 0 || shipPosition.X == mapSize - 1)
            {
                if (shipPosition.Y > 2)
                    yield return new Position(shipPosition.X, shipPosition.Y - 1);
                if (shipPosition.Y < mapSize - 3)
                    yield return new Position(shipPosition.X, shipPosition.Y + 1);
            }
            else if (shipPosition.Y == 0 || shipPosition.Y == mapSize - 1)
            {
                if (shipPosition.X > 2)
                    yield return new Position(shipPosition.X - 1, shipPosition.Y);
                if (shipPosition.X < mapSize - 3)
                    yield return new Position(shipPosition.X + 1, shipPosition.Y);
            }
            else
            {
                throw new Exception("wrong ship position");
            }
        }

        private Position GetShipLanding(Position pos)
        {
            if (pos.X == 0)
                return new Position(1, pos.Y);

            if (pos.X == MapSize - 1)
                return new Position(MapSize - 2, pos.Y);

            if (pos.Y == 0)
                return new Position(pos.X, 1);

            if (pos.Y == MapSize - 1)
                return new Position(pos.X, MapSize - 2);

            throw new Exception("wrong ship position");
        }

        private Position GetCannonFly(int arrowsCode, Position pos) =>
            arrowsCode switch
            {
                // вверх
                0 => new Position(pos.X, MapSize - 1),
                // вправо
                1 => new Position(MapSize - 1, pos.Y),
                // вниз
                2 => new Position(pos.X, 0),
                // влево
                _ => new Position(0, pos.Y)
            };

        private static IEnumerable<Position> GetArrowsDeltas(int arrowsCode, Position source)
        {
            foreach (var delta in ArrowsCodesHelper.GetExitDeltas(arrowsCode))
            {
                yield return new Position(source.X + delta.X, source.Y + delta.Y);
            }
        }

        /// <summary>
        /// Все возможные цели для плавающего пирата
        /// </summary>
        private IEnumerable<Position> GetPossibleSwimming(Position pos)
        {
            return GetNearDeltas(pos).Where(IsValidMapPosition).Where(x => Map[x].Type == TileType.Water);
        }

        private static bool WasCheckedBefore(List<CheckedPosition> alreadyCheckedList, CheckedPosition currentCheck)
        {
            foreach (var info in alreadyCheckedList)
            {
                if (info.Position == currentCheck.Position)
                {
                    if (info.IncomeDelta == null || info.IncomeDelta == currentCheck.IncomeDelta) 
                        return true;
                }
            }
            
            return false;
        }
    }
}