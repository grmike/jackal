import { Animate } from 'react-move';

import { girlsMap } from '../../../logic/gameLogic';
import { PiratePhotoMemoized } from './piratePhotoMemoized';

interface AnimatePirateProps {
    pirate: GamePirate;
    pirateSize: number;
    speed: number;
    left: number;
    top: number;
    isCurrentPlayerGirl: boolean;
    onTeamPirateClick: (girl: GamePirate, allowChoosing: boolean) => void;
}

const AnimatePirate = ({
    pirate,
    pirateSize,
    speed,
    left,
    top,
    isCurrentPlayerGirl,
    onTeamPirateClick,
}: AnimatePirateProps) => {
    const mapLevel = girlsMap.GetPosition(pirate);

    return (
        <Animate
            start={{
                x: left,
                y: top,
            }}
            update={{
                x: [left],
                y: [top],
                timing: { duration: speed * 100 },
            }}
        >
            {(state) => {
                const { x, y } = state;

                return (
                    <div
                        className="level"
                        style={{
                            top: y,
                            left: x,
                            zIndex: pirate.isActive ? 10 : (mapLevel?.girls?.indexOf(pirate.id) || 0) + 3,
                            pointerEvents: isCurrentPlayerGirl ? 'auto' : 'none',
                        }}
                    >
                        <PiratePhotoMemoized
                            pirate={pirate}
                            pirateSize={pirateSize}
                            isCurrentPlayerGirl={isCurrentPlayerGirl}
                            onTeamPirateClick={onTeamPirateClick}
                        />
                    </div>
                );
            }}
        </Animate>
    );
};
export default AnimatePirate;
