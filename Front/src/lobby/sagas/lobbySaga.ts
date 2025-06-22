import { PayloadAction } from '@reduxjs/toolkit';
import { call, put, select, takeEvery } from 'redux-saga/effects';

import { NetGameInfoResponse, NetGameListResponse } from '../../common/redux.types';
import { applyGamesList, applyLeaderBoard, applyNetGame, applyNetGamesList } from '../redux/lobbySlice';
import { LeaderBoardItemResponse } from '../types/sagaContracts';
import { getAuth } from '/auth/redux/authSlice';
import { AuthState } from '/auth/types/auth';
import { axiosInstance, errorsWrapper, sagaActions } from '/common/sagas';
import gameHub from '/game/hub/gameHub';

export function* applyActiveGamesData(action: PayloadAction<NetGameListResponse>) {
    const auth: AuthState = yield select(getAuth);
    const data = action.payload;
    yield put(applyGamesList({ currentUserId: auth.user?.id, gamesEntries: data.gamesEntries }));
}

export function* applyNetGamesData(action: PayloadAction<NetGameListResponse>) {
    const auth: AuthState = yield select(getAuth);
    const data = action.payload;
    yield put(applyNetGamesList({ currentUserId: auth.user?.id, gamesEntries: data.gamesEntries }));
}

export function* applyNetGameData(action: PayloadAction<NetGameInfoResponse>) {
    const auth: AuthState = yield select(getAuth);
    const data = action.payload;
    yield put(applyNetGame({ currentUserId: auth.user?.id, gameInfo: data }));
    if (data.gameId) {
        gameHub.loadGame(data.gameId);
    }
}

export function* getLeaderBoardData() {
    const result: { data: { leaderboard: LeaderBoardItemResponse[] } } = yield call(
        async () =>
            await axiosInstance({
                url: 'v1/leaderboard',
                method: 'get',
            }),
    );
    yield put(applyLeaderBoard(result.data.leaderboard));
}

export default function* rootSaga() {
    yield takeEvery(sagaActions.ACTIVE_GAMES_APPLY_DATA, errorsWrapper(applyActiveGamesData));
    yield takeEvery(sagaActions.NET_GAMES_APPLY_DATA, errorsWrapper(applyNetGamesData));
    yield takeEvery(sagaActions.NET_GAME_APPLY_DATA, errorsWrapper(applyNetGameData));
    yield takeEvery(sagaActions.LOBBY_GET_LEADERBOARD, errorsWrapper(getLeaderBoardData));
}
