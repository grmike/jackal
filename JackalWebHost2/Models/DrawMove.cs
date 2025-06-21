﻿namespace JackalWebHost2.Models;

public class DrawMove
{
    public int MoveNum;
    public PiratePosition From;
    public PiratePosition To;
    public DrawPosition? Prev;
    public bool WithCoin;
    public bool WithBigCoin;
    public bool WithRespawn;
    public bool WithLighthouse;
    public bool WithQuake;
}