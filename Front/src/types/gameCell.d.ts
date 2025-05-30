interface GameLevel {
    level: number;
    coins: number;
    piratesWithCoinsCount?: number;
    freeCoinGirlId?: string;
    features?: GameLevelFeature[];
}

interface GameLevelFeature {
    backgroundColor: string;
    photo: string;
}
