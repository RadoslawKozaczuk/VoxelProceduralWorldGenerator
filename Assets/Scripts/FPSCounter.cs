using System;
using UnityEngine;
using UnityEngine.UI;

public class FPSCounter : MonoBehaviour
{
    [Serializable]
    struct FPSColor
    {
        public Color color;
        public int minimumFPS;
    }

    // strings are pre-prepered to avoid coountless string concatenation and memory pollution
    static readonly string[] stringsFrom00To99 = {
        "00", "01", "02", "03", "04", "05", "06", "07", "08", "09",
        "10", "11", "12", "13", "14", "15", "16", "17", "18", "19",
        "20", "21", "22", "23", "24", "25", "26", "27", "28", "29",
        "30", "31", "32", "33", "34", "35", "36", "37", "38", "39",
        "40", "41", "42", "43", "44", "45", "46", "47", "48", "49",
        "50", "51", "52", "53", "54", "55", "56", "57", "58", "59",
        "60", "61", "62", "63", "64", "65", "66", "67", "68", "69",
        "70", "71", "72", "73", "74", "75", "76", "77", "78", "79",
        "80", "81", "82", "83", "84", "85", "86", "87", "88", "89",
        "90", "91", "92", "93", "94", "95", "96", "97", "98", "99"
    };

    public Text HighestFPSLabel, AverageFPSLabel, LowestFPSLabel;

    [Tooltip("The number of frames the calculation is based on.")]
    public int FrameRange = 60;
    public int HighestFPS { get; private set; }
    public int AverageFPS { get; private set; }
    public int LowestFPS { get; private set; }

    [SerializeField] FPSColor[] _coloring;
    int[] _fpsBuffer; // we store all values from the last second
    int _fpsBufferIndex; // index of the curretly stored value

    void Update()
    {
        Display(HighestFPSLabel, HighestFPS);
        Display(AverageFPSLabel, AverageFPS);
        Display(LowestFPSLabel, LowestFPS);

        if (_fpsBuffer == null || _fpsBuffer.Length != FrameRange)
            InitializeBuffer();

        UpdateBuffer();
        CalculateFPS();
    }

    void Display(Text label, int fps)
    {
        // find appriopraite color
        for (int i = 0; i < _coloring.Length; i++)
        {
            if (fps >= _coloring[i].minimumFPS)
            {
                label.color = _coloring[i].color;
                break;
            }
        }

        label.text = stringsFrom00To99[Mathf.Clamp(fps, 0, 99)];
    }

    void UpdateBuffer()
    {
        _fpsBufferIndex++;
        if (_fpsBufferIndex >= FrameRange)
            _fpsBufferIndex = 0;

        // it is better to use unscaled delta time because it always gives the time that took to process 
        // the last frame delta time on the other hand is affected by the time settings
        _fpsBuffer[_fpsBufferIndex] = (int)(1f / Time.unscaledDeltaTime);
    }

    void InitializeBuffer()
    {
        if (FrameRange <= 0)
            FrameRange = 1;

        _fpsBuffer = new int[FrameRange];
        _fpsBufferIndex = 0;
    }

    void CalculateFPS()
    {
        int sum = 0;
        int highest = 0;
        int lowest = int.MaxValue;

        for (int i = 0; i < FrameRange; i++)
        {
            int fps = _fpsBuffer[i];
            sum += fps;
            if (fps > highest)
                highest = fps;
            if (fps < lowest)
                lowest = fps;
        }

        HighestFPS = highest;
        AverageFPS = sum / FrameRange;
        LowestFPS = lowest;
    }
}