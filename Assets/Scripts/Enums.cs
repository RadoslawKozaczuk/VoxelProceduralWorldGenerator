using System;

// whenever you change these values you also have to change MeshGenerator constants
public enum BlockType : byte
{
    Air, // air need to be the default value (zero) to allow additional performance optimizations
    Dirt, Stone, Diamond, Bedrock, Redstone, Sand, Leaves, Wood, Woodbase,
	Water,
	Grass // types that have different textures on sides and bottom
}

[Flags]
public enum Cubeside : byte { Right = 1, Left = 2, Top = 4, Bottom = 8, Front = 16, Back = 32 }

public enum WorldGeneratorStatus { NotReady, TerrainReady, FacesReady, AllReady }

public enum ChunkStatus { NotReady, NeedToBeRedrawn, NeedToBeRecreated, Ready }

public enum TreeProbability { None = 0, Some = 1, Lots = 2 }

public enum ComputingAcceleration { UnityJobSystem, PureCSParallelisation }