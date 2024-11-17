import axios from 'axios';
import { call, put } from 'redux-saga/effects';
import { ErrorInfo } from '../redux/commonSlice.types';
import { showError } from '/redux/commonSlice';
import { debugLog } from '/app/global';
import { setAuth } from '/redux/authSlice';
import config from '/app/config';

export const sagaActions = {
    GAME_RESET: 'GAME_RESET',
    GAME_START: 'GAME_START',
    GAME_TURN: 'GAME_TURN',
    GET_TILES_PACK_NAMES: 'GET_TILES_PACK_NAMES',
    CHECK_MAP: 'CHECK_MAP',

    LOBBY_CREATE: 'LOBBY_CREATE',
    LOBBY_JOIN: 'LOBBY_JOIN',
    LOBBY_GET: 'LOBBY_GET',
    LOBBY_DO_POLLING: 'LOBBY_DO_POLLING',
    LOBBY_STOP_POLLING: 'LOBBY_STOP_POLLING',

    AUTH_CHECK: 'AUTH_CHECK',
    AUTH_LOGIN: 'AUTH_LOGIN',
    AUTH_LOGOUT: 'AUTH_LOGOUT',
};

export const axiosInstance = axios.create({
    withCredentials: true,
    baseURL: config.BaseApi,
});

export const errorsWrapper = (saga: (action: any) => void) =>
    function* (action: any) {
        try {
            yield call(saga, action);
        } catch (err) {
            if (axios.isAxiosError(err)) {
                let error = err.response?.data as ErrorInfo;

                debugLog(error, err);

                if (error) {
                    yield put(showError(error));
                } else if (err.response?.status == 401) {
                    yield put(
                        setAuth({
                            isAuthorised: false,
                        }),
                    );
                    yield put(
                        showError({
                            error: true,
                            errorCode: err.response?.statusText,
                            errorMessage: 'Не авторизован',
                        }),
                    );
                } else {
                    yield put(
                        showError({
                            error: true,
                            errorCode: 'InternalServerError',
                            errorMessage: 'Ошибка сервера',
                        }),
                    );
                }
            } else {
                debugLog(err);
            }
        }
    };
