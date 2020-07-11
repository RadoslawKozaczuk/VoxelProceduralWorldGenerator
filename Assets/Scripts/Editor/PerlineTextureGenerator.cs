using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Voxels.Editor
{
    public class PerlineTextureGenerator : MonoBehaviour
    {
        const int TEXTURE_WIDTH = 512;
        const int TEXTURE_HEIGHT = 512;
        const float SAMPLE_STEP = 0.02f;
        const int RESOLUTION = 50; // how many samples we have for every x to x + 1 range - always an inverse of the sample step

        /// <summary>
        /// Creates <see cref="TEXTURE_WIDTH"/> x <see cref="TEXTURE_HEIGHT"/> perlin noise function 
        /// by sampling perlin noise function in range 0 to <see cref="TEXTURE_WIDTH"/> / <see cref="RESOLUTION"/>.
        /// </summary>
        [MenuItem("Assets/Create/Perlin Noise Texture")]
        static void CreatePerlinNoiseTexture()
        {
            Texture2D texture = CreateTextrue();

            // save
            byte[] bytes = texture.EncodeToPNG();
            var dirPath = Application.dataPath + "/";
            File.WriteAllBytes(dirPath + "PerlinNoise.png", bytes);
            Debug.Log("Noise texture saved to: " + dirPath);
        }

        [MenuItem("Assets/Create/TEST TEST")]
        static void TEST_TEST()
        {
            //float result0 = Test0_TexturePrecisionLoss(); // texture precision loss for RBG24: 0.001021004
            // texture precision loss for R16:   0.000024536

            // 0.001 straty dla rozdziałki 8
            // 0.001 stratu dla rozdizalki 16
            float result1 = Test1_PointSampling();
            // 0.03 straty dla rozdizalki 8
            // 0.015 straty dla rozdzialki 16
            // 0.01 straty dla rozdzialki 25
            float result2 = Test2_ApproximationSampling(); 

            int hfghf = 5;
        }

        static float Test0_TexturePrecisionLoss()
        {
            //var texture = new Texture2D(1, 1, TextureFormat.RGB24, false) { filterMode = FilterMode.Point };
            var texture = new Texture2D(1, 1, TextureFormat.R16, false) { filterMode = FilterMode.Point };
            var pixels = new Color32[1];

            float differenceSum = 0;
            int differenceSumCounter = 0;

            for (int x = 0; x < TEXTURE_WIDTH; x++)
                for (int y = 0; y < TEXTURE_HEIGHT; y++)
                {
                    float realX = x * SAMPLE_STEP;
                    float realY = y * SAMPLE_STEP;

                    float realValue = Mathf.PerlinNoise(realX, realY);
                    //pixels[0] = new Color(realValue, 0, 0, 0);
                    //texture.SetPixels32(pixels);
                    texture.SetPixel(0, 0, new Color(realValue, 0, 0)); // tu mogą być straty na zgrywaniu danych do tekstury

                    float retrievedValue = texture.GetPixel(0, 0).r;

                    if (retrievedValue != realValue)
                    {
                        differenceSum += Math.Abs(retrievedValue - realValue);
                        differenceSumCounter++;
                    }
                }

            if (differenceSumCounter > 0)
            {
                float avgDifference = differenceSum / differenceSumCounter;
                return avgDifference;
            }

            return 0;
        }

        // should give the same results as the precision loss test
        static float Test1_PointSampling()
        {
            // zadaniem tej metody tstujacej jest
            // a) stworzyc teksture
            //TerrainGeneration.NoiseSampler noiseSampler = new TerrainGeneration.NoiseSampler(CreateTextrue(), RESOLUTION);
            TerrainGeneration.NoiseSampler.Initialize(CreateTextrue(), RESOLUTION);

            // b) nastepnie porównać wartości pixeli z samplami funkcji analitycznej
            float differenceSum = 0;
            int differenceSumCounter = 0;

            // full sampler
            for (float x = 0; x < TEXTURE_WIDTH / RESOLUTION; x += SAMPLE_STEP)
                for (float y = 0; y < TEXTURE_HEIGHT / RESOLUTION; y+= SAMPLE_STEP)
                {
                    //float texVal = noiseSampler.Sample(x, y);
                    float texVal = TerrainGeneration.NoiseSampler.Sample(x, y);
                    float perVal = Mathf.PerlinNoise(x, y);
                    
                    if (texVal != perVal)
                    {
                        differenceSum += Math.Abs(texVal - perVal);
                        differenceSumCounter++;
                    }
                }

            if (differenceSumCounter > 0)
            {
                // zakłada się że rozrzut nie powinien być większy niż 1%
                // mamy lekko powyżej 0.1% więc jest dobrze
                float avgDifference = differenceSum / differenceSumCounter; // 0.001354937
                return avgDifference;
            }

            return 0;
        }

        static float Test2_ApproximationSampling()
        {
            // zadaniem tej metody tstujacej jest
            // a) stworzyc teksture
            //TerrainGeneration.NoiseSampler noiseSampler = new TerrainGeneration.NoiseSampler(CreateTextrue(), RESOLUTION);
            TerrainGeneration.NoiseSampler.Initialize(CreateTextrue(), RESOLUTION);

            // b) nastepnie porównać wartości pixeli z samplami funkcji analitycznej
            float differenceSum = 0;
            int differenceSumCounter = 0;

            // approximation sampler
            // pick random values in between samples
            for (int x = 0; x < TEXTURE_WIDTH - 1; x++)
                for (int y = 0; y < TEXTURE_HEIGHT - 1; y++)
                {
                    // trololo always pick something in between (but the sample it self lololo)
                    float xOffset = UnityEngine.Random.Range(0 + float.Epsilon, SAMPLE_STEP - float.Epsilon); // from zero to 0.25 both sides non inclusive
                    float yOffset = UnityEngine.Random.Range(0 + float.Epsilon, SAMPLE_STEP - float.Epsilon);

                    float realX = x * SAMPLE_STEP + xOffset;
                    float realY = y * SAMPLE_STEP + yOffset;
                    //float texVal = noiseSampler.Sample(realX, realY);
                    float texVal = TerrainGeneration.NoiseSampler.Sample(realX, realY);
                    float perVal = Mathf.PerlinNoise(realX, realY);
                    if (texVal != perVal)
                    {
                        differenceSum += Math.Abs(texVal - perVal); // 0.00139
                        differenceSumCounter++;
                    }
                }

            if (differenceSumCounter > 0)
            {
                // zakłada się że rozrzut nie powinien być większy niż 1%
                // potrzebne testy zeby wiedziec ile mamy
                float avgDifference = differenceSum / differenceSumCounter;
                return avgDifference;
            }

            return 0;
        }

        static Texture2D CreateTextrue()
        {
            // create texture
            var texture = new Texture2D(TEXTURE_WIDTH, TEXTURE_HEIGHT, TextureFormat.R16, false)
            {
                filterMode = FilterMode.Point,
                name = "PerlinNoise"
            };

            // sample noise
            var pixels = new Color32[TEXTURE_WIDTH * TEXTURE_HEIGHT];
            for (int x = 0; x < TEXTURE_WIDTH; x++)
                for (int y = 0; y < TEXTURE_HEIGHT; y++)
                {
                    float sample = Mathf.PerlinNoise(x * SAMPLE_STEP, y * SAMPLE_STEP);
                    texture.SetPixel(x, y, new Color(sample, 0, 0));
                    //pixels[y * TEXTURE_HEIGHT + x] = new Color(sample, 0, 0);
                }

            

            //texture.SetPixels32(pixels);

            return texture;
        }
    }
}
