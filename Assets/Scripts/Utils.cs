using UnityEngine;

public class Utils
{
	private const int MaxHeight = 150;
	private const float Smooth = 0.01f; // bigger number increases sampling of the function
	private const int Octaves = 4;
	private const float Persistence = 0.5f;

	private const int MaxHeightStone = 145;
	private const float SmoothStone = 0.02f;
	private const int OctavesStone = 5;
	private const float PersistenceStone = 0.75f;

	// caves should be more erratic so has to be a higher number
	private const float SmoothCaves = 0.09f;
	private const int OctavesCaves = 3; // reduced a bit to lower workload but not to much to maintain randomness

	public static int GenerateStoneHeight(float x, float z)
	{
		float height = Map(0, MaxHeightStone, 0, 1, FractalBrownianMotion(x * SmoothStone, z * SmoothStone, OctavesStone, PersistenceStone));
		return (int)height;
	}

	public static int GenerateHeight(float x, float z)
	{
		float height = Map(0, MaxHeight, 0, 1, FractalBrownianMotion(x * Smooth, z * Smooth, Octaves, Persistence));
		return (int)height;
	}

	static float Map(float newmin, float newmax, float origmin, float origmax, float value)
	{
		return Mathf.Lerp(newmin, newmax, Mathf.InverseLerp(origmin, origmax, value));
	}

	// good noise generator
	// persistence - if < 1 each function is less powerful than the previous one, for > 1 each is more important
	// octaves - number of functions that we sum up
	static float FractalBrownianMotion(float x, float z, int oct, float pers)
	{
		float total = 0;
		float frequency = 1;
		float amplitude = 1;
		float maxValue = 0;
		for (int i = 0; i < oct; i++)
		{
			total += Mathf.PerlinNoise(x * frequency, z * frequency) * amplitude;

			maxValue += amplitude;

			amplitude *= pers;
			frequency *= 2;
		}

		return total / maxValue;
	}

	public static float FractalBrownianMotion3D(float x, float y, int z)
	{
		// this is obviously more computational heavy
		float xy = FractalBrownianMotion(x * SmoothCaves, y * SmoothCaves, OctavesCaves, 0.5f);
		float yz = FractalBrownianMotion(y * SmoothCaves, z * SmoothCaves, OctavesCaves, 0.5f);
		float xz = FractalBrownianMotion(x * SmoothCaves, z * SmoothCaves, OctavesCaves, 0.5f);

		float yx = FractalBrownianMotion(y * SmoothCaves, x * SmoothCaves, OctavesCaves, 0.5f);
		float zy = FractalBrownianMotion(z * SmoothCaves, y * SmoothCaves, OctavesCaves, 0.5f);
		float zx = FractalBrownianMotion(z * SmoothCaves, x * SmoothCaves, OctavesCaves, 0.5f);

		return (xy + yz + xz + yx + zy + zx) / 6.0f;
	}
}