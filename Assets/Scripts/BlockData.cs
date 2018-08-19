using System;

public enum BlockType : byte
{
    Dirt, Stone, Diamond, Bedrock, Redstone, Sand, Leaves, Wood, Woodbase,
    Water,
    Grass, // types that have different textures on sides and bottom
    Air
}

[Flags]
public enum Cubeside : byte { Right = 1, Left = 2, Top = 4, Bottom = 8, Front = 16, Back = 32 }

public enum HealthLevel : byte { NoCrack, Crack1, Crack2, Crack3, Crack4 }

public struct BlockData
{
    public Cubeside Faces;
    public BlockType Type;
    public byte CurrentHealth;
    public HealthLevel HealthType;
}
