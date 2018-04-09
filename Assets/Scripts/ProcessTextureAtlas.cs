using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts
{
	// PostTextureAtlas extends the Unity class AssetPostprocessor. 
	// Just being in the project will have them run at the appropriate times.  
	// In this case, this file will run when you change the Texture type in the Inspector from one thing to another and then click on apply.
	public class ProcessTextureAtlas : AssetPostprocessor
	{
		static readonly List<string> _uvs = new List<string>();

		void OnPreprocessTexture()
		{
			TextureImporter textureImporter = (TextureImporter)assetImporter;
			textureImporter.textureType = TextureImporterType.Sprite;
			textureImporter.spriteImportMode = SpriteImportMode.Multiple;
			textureImporter.mipmapEnabled = false;
			textureImporter.filterMode = FilterMode.Point;
			textureImporter.npotScale = TextureImporterNPOTScale.None;
		}

		static void WriteUVs()
		{
			string path = "Assets/blockuvs.txt";

			var writer = new StreamWriter(path);
			foreach (string s in _uvs)
			{
				writer.WriteLine(s);
			}
			writer.Close();

			//Re-import the file to update the reference in the editor
			AssetDatabase.ImportAsset(path);
		}

		public void OnPostprocessTexture(Texture2D texture)
		{
			const int colCount = 16;
			const int rowCount = 16;
			int sw = texture.width / colCount;
			int sh = texture.height / rowCount;

			List<SpriteMetaData> metas = new List<SpriteMetaData>();

			for (int r = 0; r < rowCount; r++)
			{
				for (int c = 0; c < colCount; c++)
				{
					var meta = new SpriteMetaData();
					meta.rect = new Rect(c * sw, r * sh, sw, sh);
					float uv1x = (c * sw) / (float)texture.width;
					float uv1y = (r * sh) / (float)texture.height;
					float uv2x = (c * sw + sw) / (float)texture.width;
					float uv2y = (r * sh) / (float)texture.height;
					float uv3x = (c * sw) / (float)texture.width;
					float uv3y = (r * sh + sh) / (float)texture.height;
					float uv4x = (c * sw + sw) / (float)texture.width;
					float uv4y = (r * sh + sh) / (float)texture.height;
					meta.name = uv1x + "," + uv1y + "|" +
								uv2x + "," + uv2y + "|" +
								uv3x + "," + uv3y + "|" +
								uv4x + "," + uv4y;
					metas.Add(meta);
					string n = "Row: " + r + " Col: " + c + " { new Vector2(" + uv1x + "f," + uv1y + "f)," +
							   " new Vector2(" + uv2x + "f," + uv2y + "f),\n" +
							   " new Vector2(" + uv3x + "f," + uv3y + "f)," +
							   " new Vector2(" + uv4x + "f," + uv4y + "f)}";
					_uvs.Add(n);
				}
			}

			TextureImporter textureImporter = (TextureImporter)assetImporter;
			textureImporter.spritesheet = metas.ToArray();
			WriteUVs();
		}
	}
}
