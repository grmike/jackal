const docsPirateMap = {
    CalcTopOffset: function (girl: GamePiratePosition, mapSize: number, cellSize: number): number {
        return (mapSize - 1 - girl.position.y) * (cellSize + 1);
    },
    CalcLeftOffset: function (girl: GamePiratePosition, cellSize: number): number {
        return girl.position.x * (cellSize + 1);
    },
};

export default docsPirateMap;
