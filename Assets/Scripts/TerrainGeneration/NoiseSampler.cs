using System.Runtime.CompilerServices;
using UnityEngine;

#if UNITY_EDITOR || UNITY_DEVELOPMENT
[assembly: InternalsVisibleTo("Editor")]
#endif

namespace Voxels.TerrainGeneration
{
    internal static class NoiseSampler
    {
        static int _width, _height;
        static int _resolution;
        static float[,] _array; // reference type so it is not possible to be used using Burst compiler

        /// <summary>
        /// Only red color matters.
        /// <paramref name="resolution"/> means how many samples in very from x to x + 1 range.
        /// </summary>
        internal static void Initialize(Texture2D noiseTexture, int resolution)
        {
            _width = noiseTexture.width;
            _height = noiseTexture.height;
            _resolution = resolution;

            Color[] colors = noiseTexture.GetPixels(0, 0, _width, _height);
            _array = new float[_width, _height];
            for (int x = 0; x < _width; x++)
                for (int y = 0; y < _height; y++)
                    _array[x, y] = colors[y * _width + x].r;
        }

        /// <summary>
        /// Samples the texture.
        /// Value returned is from 0 to 1 (both inclusive).
        /// </summary>
        internal static float Sample(int x, int y)
        {
            // assertions
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (x < 0 || x > _width / _resolution)
                throw new System.ArgumentOutOfRangeException("x", "x cannot be lower than 0 or greater than texture's width multiplied by sampling step value.");
            if (y < 0 || y > _height / _resolution)
                throw new System.ArgumentOutOfRangeException("y", "y cannot be lower than 0 or greater than texture's height multiplied by sampling step value.");
#endif

            return _array[x * _resolution, y * _resolution];
        }

        /// <summary>
        /// Samples the texture using linear approximation for non-integer input.
        /// Value returned is from 0 to 1 (both inclusive).
        /// </summary>
        internal static float Sample(float x, float y)
        {
            // assertions
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (x < 0 || x > (float)_width / _resolution)
                throw new System.ArgumentOutOfRangeException("x", "x cannot be lower than 0 or greater than texture's width multiplied by sampling step value.");
            if (y < 0 || y > (float)_height / _resolution)
                throw new System.ArgumentOutOfRangeException("y", "y cannot be lower than 0 or greater than texture's height multiplied by sampling step value.");
#endif

            float internalX = x * _resolution;
            float internalY = y * _resolution;

            int lowerX = Mathf.FloorToInt(internalX);
            int lowerY = Mathf.FloorToInt(internalY);
            int higherX = Mathf.CeilToInt(internalX);
            int higherY = Mathf.CeilToInt(internalY);

            // no approximation needed
            if (lowerX == higherX && lowerY == higherY)
                return _array[lowerX, lowerY];

            float lowerPixel = _array[lowerX, lowerY];
            float higherPixel = _array[higherX, higherY];

            float t = (higherX - lowerX + higherY - lowerY) / 2;
            return Mathf.Lerp(lowerPixel, higherPixel, t);
        }

        /// <summary>
        /// Samples the texture using linear approximation for non-integer input.
        /// Value returned is from 0 to 1 (both inclusive).
        /// Accepts value from outside of the sampled area (coordinates will be wrapped to be in the sampled area).
        /// </summary>
        internal static float SampleWithWrap(float x, float y)
        {
            float internalX = x * _resolution;
            float internalY = y * _resolution;

            // mapping
            float mappedX = internalX - Mathf.Floor(internalX / _width) * _width;
            float mappedY = internalY - Mathf.Floor(internalY / _height) * _height;

            int lowerX = Mathf.FloorToInt(mappedX);
            int lowerY = Mathf.FloorToInt(mappedY);
            int higherX = Mathf.CeilToInt(mappedX);
            int higherY = Mathf.CeilToInt(mappedY);

            // no approximation needed
            if (lowerX == higherX && lowerY == higherY)
                return _array[lowerX, lowerY];

            float lowerPixel = _array[lowerX, lowerY];
            float higherPixel = _array[higherX, higherY];

            float t = (higherX - lowerX + higherY - lowerY) / 2;
            return Mathf.Lerp(lowerPixel, higherPixel, t);
        }
    }
}
