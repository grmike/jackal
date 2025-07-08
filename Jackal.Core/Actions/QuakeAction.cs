using Jackal.Core.Domain;

namespace Jackal.Core.Actions;

public class QuakeAction(TilePosition from, TilePosition to) : IGameAction
{
    public void Act(Game game, Pirate pirate)
    {
        var map = game.Board.Map;
        
        // выбираем вторую клетку для разлома
        if (game.SubTurn.QuakePhase == 1)
        {
            game.SubTurn.QuakePhase = 0;
            game.Board.Generator.Swap(from.Position, to.Position);
            
            // меняем клетки местами
            var fromTile = map[from.Position];
            var toTile = map[to.Position];
            
            map[from.Position] = new Tile(from.Position, toTile)
            {
                Used = toTile.Used
            };
            
            map[to.Position] = new Tile(to.Position, fromTile)
            {
                Used = fromTile.Used
            };
            
            // даем доиграть маяк, если разлом был открыт с маяка
            if (game.SubTurn.LighthouseViewCount > 0)
            {
                game.NeedSubTurnPirate = pirate;
                game.PrevSubTurnPosition = pirate.Position;
            }
        }
        
        // выбираем первую клетку для разлома
        if (game.SubTurn.QuakePhase == 2)
        {
            game.SubTurn.QuakePhase = 1;
            game.NeedSubTurnPirate = pirate;
            game.PrevSubTurnPosition = to;
        }
    }
}