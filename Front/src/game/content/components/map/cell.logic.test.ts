import { CalcTooltipType, TooltipTypes } from './cell.logic';
import { Constants } from '/app/constants';
import { getMapData } from '/app/mapDataForTests';
import { GamePirate, GameState, GameTeam } from '/common/redux.types';
import reducer, {
    highlightHumanMoves,
    initMap,
    initPiratePositions,
    initTeams,
    setCurrentHumanTeam,
} from '/game/redux/gameSlice';

const testTeamId = 12;

const twoTeamsData: GameTeam[] = [
    {
        id: 1,
        name: 'girls',
        coins: 0,
        isHuman: false,
        ship: {
            x: 5,
            y: 0,
        },
    },
    {
        id: testTeamId,
        name: 'boys',
        coins: 0,
        isHuman: true,
        ship: {
            x: 5,
            y: 10,
        },
    },
];

const testPirates: GamePirate[] = [
    {
        id: '100',
        teamId: testTeamId,
        position: {
            level: 0,
            x: 2,
            y: 0,
        },
        groupId: '',
        photo: '',
        photoId: 0,
        type: Constants.pirateTypes.Usual,
    },
];

const getState = (pirates: GamePirate[]) => ({
    mapSize: 5,
    fields: [[]],
    lastMoves: [],
    gameSettings: {
        cellSize: 50,
        pirateSize: 15,
        tilesPackNames: [],
    },
    userSettings: {
        groups: [
            Constants.groupIds.girls,
            Constants.groupIds.redalert,
            Constants.groupIds.orcs,
            Constants.groupIds.skulls,
        ],
        mapSize: 11,
        players: ['human', 'robot2', 'robot', 'robot2'],
        playersMode: 4,
        gameSpeed: 0,
    },
    stat: {
        turnNo: 1,
        currentTeamId: testTeamId,
        isGameOver: false,
        gameMessage: '',
    },
    teams: [],
    pirates: pirates,
    currentHumanTeamId: 0,
    highlight_x: 0,
    highlight_y: 0,
    hasPirateAutoChange: true,
});

describe('cell logic tests', () => {
    let defaultState: GameState;

    beforeAll(() => {
        defaultState = getState(testPirates);
        defaultState = reducer(defaultState, initMap(getMapData));
        defaultState = reducer(defaultState, initTeams(twoTeamsData));
        defaultState = reducer(defaultState, initPiratePositions());
        defaultState = reducer(defaultState, setCurrentHumanTeam());
        defaultState = reducer(
            defaultState,
            highlightHumanMoves({
                moves: [
                    {
                        moveNum: 1,
                        from: { pirateIds: ['100'], level: 0, x: 2, y: 0 },
                        to: { pirateIds: ['100'], level: 0, x: 2, y: 0 },
                        withCoin: false,
                        withRespawn: false,
                    },
                ],
            }),
        );
    });

    test('Пропускаем ход', () => {
        const row = 0;
        const col = 2;
        const result = CalcTooltipType({
            row,
            col,
            field: defaultState.fields[row][col],
            state: defaultState,
        });
        expect(result).toEqual(TooltipTypes.SkipMove);
    });
});
