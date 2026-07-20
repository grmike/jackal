import { PayloadAction, createSlice } from '@reduxjs/toolkit';

import { DocsState } from '../types/types';
import { Constants, ImageGroupsIds } from '/app/constants';

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
    } satisfies DocsState as DocsState,
    reducers: {
        setPiratePosition: (state, action: PayloadAction<GamePiratePosition>) => {
            state.pirate.position = action.payload.position;
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

export const { setPiratePosition } = docsSlice.actions;

export const { getPirate, hasAvailableMove } = docsSlice.selectors;

export default docsSlice.reducer;
