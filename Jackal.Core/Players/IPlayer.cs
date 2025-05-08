﻿using System;

namespace Jackal.Core.Players;

public interface IPlayer
{
    long UserId { get; }
    
    void OnNewGame();

    /// <summary>
    /// Насильный выбор хода, для HumanPlayer
    /// </summary>
    void SetHumanMove(int moveNum, Guid? pirateId);

    (int moveNum, Guid? pirateId) OnMove(GameState gameState);
}