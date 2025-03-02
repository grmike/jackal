﻿namespace JackalWebHost2.Models;

public class TileChange
{
    public string BackgroundImageSrc;
    public int Rotate;
    
    public bool IsUnknown;

    public LevelChange[] Levels;

    public int X;
    public int Y;
}