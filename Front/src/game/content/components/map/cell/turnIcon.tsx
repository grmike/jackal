import { FaArrowAltCircleDown, FaArrowAltCircleUp, FaRegEye } from 'react-icons/fa';
import { GiFootprint } from 'react-icons/gi';

import { AvailableMove } from '/game/types';

const TurnIcon = ({ moves }: { moves: AvailableMove[] }) => {
    if (moves.length > 0) {
        if (moves[0].isQuakeBegin)
            return <FaArrowAltCircleUp size={20} style={{ position: 'absolute', color: 'gray' }} />;
        if (moves[0].isQuakeEnd)
            return <FaArrowAltCircleDown size={20} style={{ position: 'absolute', color: 'gray' }} />;
        if (moves[0].isLighthouse) return <FaRegEye size={20} style={{ position: 'absolute', color: 'dimGray' }} />;
    }
    return <GiFootprint size={20} style={{ position: 'absolute', color: 'dimGray' }} />;
};

export default TurnIcon;
