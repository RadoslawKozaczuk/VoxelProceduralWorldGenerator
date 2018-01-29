using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour {

	public Material textureAtlas;
	public static int columnHeight = 6; // number of chunks
	public static int chunkSize = 6;
	public static Dictionary<string, Chunk> chunks;

	public static string BuildChunkName(Vector3 v)
	{
		return (int)v.x + "_" + (int)v.y + "_" + (int)v.z;
	}

	IEnumerator BuildChunkColumn()
	{
		for(var i = 0; i < columnHeight; i++)
		{
			var chunkPosition = new Vector3(transform.position.x, i*chunkSize, transform.position.z);
			var c = new Chunk(chunkPosition, textureAtlas);
			c.chunk.transform.parent = transform;
			chunks.Add(c.chunk.name, c);
		}

		foreach(var c in chunks)
		{
			c.Value.DrawChunk();
			yield return null;
		}
	}

	// Use this for initialization
	void Start () {
		chunks = new Dictionary<string, Chunk>();
		transform.position = Vector3.zero;
		transform.rotation = Quaternion.identity;
		StartCoroutine(BuildChunkColumn());
	}
}
