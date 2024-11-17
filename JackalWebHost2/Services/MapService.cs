using Jackal.Core;
using Jackal.Core.Domain;
using Jackal.Core.MapGenerator;
using JackalWebHost2.Controllers.Models.Map;
using JackalWebHost2.Models.Map;

namespace JackalWebHost2.Services;

public class MapService : IMapService
{
    public List<CheckLandingResult> CheckLanding(CheckLandingRequest request)
    {
        var mapGenerator = new RandomMapGenerator(request.MapId, request.MapSize, request.TilesPackName);
        
        var upLanding = new CheckLandingResult(DirectionType.Up);
        var rightLanding = new CheckLandingResult(DirectionType.Right);
        var downLanding = new CheckLandingResult(DirectionType.Down);
        var leftLanding = new CheckLandingResult(DirectionType.Left);
        
        for (int i = 2; i <= request.MapSize - 3; i++)
        {
            var upTile = mapGenerator.GetNext(new Position(i, request.MapSize - 2));
            SetLandingResult(upLanding, upTile);
            
            var rightTile = mapGenerator.GetNext(new Position(request.MapSize - 2, i));
            SetLandingResult(rightLanding, rightTile);
            
            var downTile = mapGenerator.GetNext(new Position(i, 1));
            SetLandingResult(downLanding, downTile);
            
            var leftTile = mapGenerator.GetNext(new Position(1, i));
            SetLandingResult(leftLanding, leftTile);
        }

        var landingResults = new List<CheckLandingResult>
        {
            upLanding, rightLanding, downLanding, leftLanding
        };

        foreach (var landing in landingResults)
        {
            SetLandingDifficulty(landing, request.MapSize - 4);
        }

        return landingResults;
    }

    private static void SetLandingResult(CheckLandingResult landing, Tile tile)
    {
        landing.Coins += tile.Type.CoinsCount();
        landing.Cannibals += tile.Type == TileType.Cannibal ? 1 : 0;
    }

    private static void SetLandingDifficulty(CheckLandingResult landing, int landSize)
    {
        switch (landSize)
        {
            case 1:
            case 3:
                if (landing.Cannibals > 0)
                {
                    landing.Difficulty = DifficultyLevel.Hard;
                }
                else if (landing.Coins == 0)
                {
                    landing.Difficulty = DifficultyLevel.Medium;
                }
                else
                {
                    landing.Difficulty = DifficultyLevel.Easy;
                }

                break;
            default:
                if (landing is { Cannibals: > 1, Coins: < 10 })
                {
                    landing.Difficulty = DifficultyLevel.Hard;
                }
                else if (landing.Cannibals > 1 ||
                         landing is { Cannibals: > 0, Coins: < 10 })
                {
                    landing.Difficulty = DifficultyLevel.Medium;
                }
                else
                {
                    landing.Difficulty = DifficultyLevel.Easy;
                }

                break;
        }
    }
}