import { Button, Container, Form, Row } from 'react-bootstrap';
import classes from './lobbyCard.module.less';
import { useDispatch, useSelector } from 'react-redux';
import { LobbyInfo, ReduxState, StorageState } from '/redux/types';
import { useNavigate, useParams } from 'react-router-dom';
import { sagaActions } from '/sagas/constants';
import Players from '/content/components/players';
import { useState } from 'react';
import { PlayersInfo } from '/content/components/types';

const LobbyCard = () => {
    let { id } = useParams();
    const navigate = useNavigate();
    const dispatch = useDispatch();

    const lobby = useSelector<ReduxState, LobbyInfo | undefined>((state) =>
        state.lobby.lobbies.find((it) => it.id === id),
    );

    const userSettings = useSelector<ReduxState, StorageState>((state) => state.game.userSettings);

    const [players, setPlayers] = useState<PlayersInfo>({
        count: 4,
        members: ['human', 'human', 'human', 'human'],
        groups: userSettings.groups,
    });

    const joinLobby = () => {
        dispatch({
            type: sagaActions.LOBBY_JOIN,
            payload: {
                lobbyId: id,
            },
        });
    };

    const exitLobby = () => {
        navigate('/newgame');
    };

    return (
        <Container>
            <Row className="justify-content-center">
                <Form className={classes.lobbyCard} onSubmit={(event) => event.preventDefault()}>
                    {lobby && (
                        <>
                            <Form.Group as={Row} className="mb-2">
                                <Form.Label column xs="4">
                                    Номер:
                                </Form.Label>
                                <Form.Label column xs="8">
                                    {lobby.id}
                                </Form.Label>
                            </Form.Group>
                            <Form.Group as={Row} className="mb-2">
                                <Form.Label column xs="4">
                                    Владелец:
                                </Form.Label>
                                <Form.Label column xs="8">
                                    {lobby.ownerId}
                                </Form.Label>
                            </Form.Group>
                            <Form.Group className="mb-3">
                                <Form.Label>Участники:</Form.Label>
                                {lobby.lobbyMembers && Object.keys(lobby.lobbyMembers).map((key) => <div>{key}</div>)}
                            </Form.Group>
                            <Players players={players} setPlayers={setPlayers} />
                        </>
                    )}
                    <Button variant="primary" type="submit" onClick={joinLobby}>
                        Присоединиться
                    </Button>
                    <Button className="float-end" variant="outline-primary" type="submit" onClick={exitLobby}>
                        Вернуться
                    </Button>
                </Form>
            </Row>
        </Container>
    );
};

export default LobbyCard;