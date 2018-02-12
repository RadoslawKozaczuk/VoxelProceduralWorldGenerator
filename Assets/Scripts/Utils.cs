using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utils
{

	static int maxHeight = 150;
	static float smooth = 0.01f;
	static int octaves = 4;
	static float persistence = 0.5f;

	public static int GenerateHeight(float x, float z)
	{
		float height = Map(0, maxHeight, 0, 1, FractalBrownianMotion(x * smooth, z * smooth, octaves, persistence));
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
}
