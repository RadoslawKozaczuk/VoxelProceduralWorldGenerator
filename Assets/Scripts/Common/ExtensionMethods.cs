using System;
using System.Collections.Generic;
using UnityEngine;

namespace Voxels.Common
{
    public static class ExtensionMethods
    {
        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
        {
            foreach (T item in enumeration)
                action(item);
        }

        public static Texture2D ToTexture2D(this RenderTexture renderTexture)
        {
            Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
            RenderTexture.active = renderTexture;
            tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            tex.Apply();
            return tex;
        }
    }
}
