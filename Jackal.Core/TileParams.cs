﻿namespace Jackal.Core
{
    public class TileParams : IClonable<TileParams>
    {
        public Position Position;
        public TileType Type;
        public int ArrowsCode;
		public int CanonDirection;
        public int SpinningCount;

        public TileParams()
        {
        }
        
        public TileParams(TileType type)
        {
            Type = type;
        }
        
        public TileParams(TileType type, int arrowsCode)
        {
            Type = type;
            ArrowsCode = arrowsCode;
        }
        
        public TileParams Clone()
        {
            return (TileParams)MemberwiseClone();
        }
    }
}