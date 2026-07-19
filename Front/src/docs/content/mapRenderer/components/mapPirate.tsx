import { useSelector } from 'react-redux';

import { getPirate } from '/docs/redux/docsSlice';
import { PiratePhotoMemoized } from '/game/content/components/mapPirates/piratePhotoMemoized';
import girlsMap from '/game/logic/components/girlsMap';

interface MapPirateProps {
    mapSize: number;
    cellSize: number;
}

const MapPirate = ({ mapSize, cellSize }: MapPirateProps) => {
    const pirate = useSelector(getPirate);

    const pirateSize = cellSize * 0.55;

    return (
        <div
            className="level"
            style={{
                top: girlsMap.CalcTopOffset(pirate, mapSize, cellSize, pirateSize) - mapSize * (cellSize + 1),
                left: girlsMap.CalcLeftOffset(pirate, cellSize, pirateSize),
                zIndex: 10,
                pointerEvents: 'auto',
            }}
        >
            <PiratePhotoMemoized
                pirate={pirate}
                pirateSize={pirateSize}
                isCurrentPlayerGirl
                onTeamPirateClick={() => {}}
            />
        </div>
    );
};

export default MapPirate;
