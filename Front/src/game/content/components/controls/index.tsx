import { useSelector } from 'react-redux';
import cn from 'classnames';
import classes from './controls.module.less';
import { GameState, ReduxState } from '../../../../common/redux.types';
import { Alert } from 'react-bootstrap';
import { Constants } from '/app/constants';

function Controls() {
    const game = useSelector<ReduxState, GameState | undefined>((state) => state.game);

    return (
        <>
            <div className={classes.statistic}>
                <div>
                    Код игры: <span>{game?.gameName}</span>
                </div>
                <div>
                    Код карты: <span>{game?.mapId}</span>
                </div>
                <div>
                    Игровой набор: <span>{game?.tilesPackName}</span>
                </div>
                <div>
                    Режим игры:{' '}
                    <span>
                        {game?.gameMode == Constants.gameModeTypes.TwoPlayersInTeam ? '2x2' : 'каждый сам за себя'}
                    </span>
                </div>
                <div>
                    Размер карты: <span>{game?.mapSize}</span>
                </div>
                <div>
                    Номер хода: <span>{game?.stat?.turnNo}</span>
                </div>
                <div className={cn(classes.teams, 'container')}>
                    {game?.teamScores?.map((it) => {
                        const team = game.teams.find((tm) => tm.id === it.teamId);
                        return (
                            <div
                                key={`ctrl_${it.teamId}`}
                                className="row"
                                style={{ backgroundColor: team?.backColor ?? '' }}
                            >
                                <div className="col-md-8">{team?.name}</div>
                                <div className="col-md-4">{it.coins}</div>
                            </div>
                        );
                    })}
                </div>
            </div>

            {game?.stat?.gameMessage != undefined && (
                <Alert variant={game?.stat?.isGameOver ? 'success' : 'primary'} className="my-2">
                    {game?.stat?.gameMessage}
                </Alert>
            )}
        </>
    );
}

export default Controls;
