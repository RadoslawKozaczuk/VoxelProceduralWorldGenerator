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

public enum WorldGeneratorStatus { NotReady, TerrainReady, FacesReady, AllReady }

public enum ChunkStatus { NotReady, NeedToBeRedrawn, NeedToBeRecreated, Ready }

public enum TreeProbability { None = 0, Some = 1, Lots = 2 }

public enum ComputingAcceleration { UnityJobSystem, PureCSParallelisation }