import { PayloadAction, createSlice } from '@reduxjs/toolkit';

import { DocsState } from '../types/types';
import { Constants, ImageGroupsIds } from '/app/constants';
import girlsMap from '/game/logic/components/girlsMap';

export const docsSlice = createSlice({
    name: 'docs',
    initialState: {
        pirate: {
            id: '100',
            teamId: 0,
            position: {
                level: 0,
                x: 0,
                y: 0,
            },
            photo: `${ImageGroupsIds.girls}/pirate_2.png`,
            backgroundColor: 'darkred',
            photoId: 0,
            type: Constants.pirateTypes.Usual,
        },
        availableMoves: [],
        fields: [[]],
    } satisfies DocsState as DocsState,
    reducers: {
        initPiratePosition: (state, action: PayloadAction<number>) => {
            girlsMap.Map = {};
            girlsMap.AddPosition(state.pirate, 1);
            const map = [];
            let j = 0;
            for (let i = 0; i < action.payload; i++) {
                const row: boolean[] = [];
                for (let col = 0; col < action.payload; col++) {
                    row.push(false);
                    j++;
                }
                map.push(row);
            }
            state.fields = map;
        },
        setPiratePosition: (state, action: PayloadAction<GamePiratePosition>) => {
            girlsMap.RemovePosition(state.pirate);
            state.pirate.position = action.payload.position;
            girlsMap.AddPosition(state.pirate, 1);
            const availMoves = [
                [-1, -1],
                [-1, 0],
                [-1, 1],
                [0, 1],
                [1, 1],
                [1, 0],
                [1, -1],
                [0, -1],
            ];
            state.availableMoves = availMoves.map((pos) => ({
                pirateIds: [],
                level: 0,
                x: state.pirate.position.x + pos[0],
                y: state.pirate.position.y + pos[1],
            }));
        },
    },
    selectors: {
        getPirate: (state): GamePirate => state.pirate,
        hasAvailableMove: (state, x: number, y: number): boolean =>
            state.availableMoves.some((it) => it.x == x && it.y == y),
    },
});

export const { initPiratePosition, setPiratePosition } = docsSlice.actions;

export const { getPirate, hasAvailableMove } = docsSlice.selectors;

export default docsSlice.reducer;
