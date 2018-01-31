using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
	public class WorldController : MonoBehaviour
	{
		// Stats
		// batches - how many things need to be processed before you can an image on the screen
		// tris - number of triangles 
		// verts - number of vertexes
		public GameObject block;
		public int worldSize = 5;

		public IEnumerator BuildWorld()
		{
			for (int z = 0; z < worldSize; z++)
			{
				for (int y = 0; y < worldSize; y++)
				{
					for (int x = 0; x < worldSize; x++)
					{
						Vector3 pos = new Vector3(x,y,z);
						GameObject cube = GameObject.Instantiate(block, pos, Quaternion.identity);
						cube.name = x + "_" + y + "_" + z;
						cube.GetComponent<Renderer>().material = new Material(Shader.Find("Standard")); // this time each cube will have a different material
						// normally Unity does it best to batch together all the object with the same material
					}
					yield return null; // one row at a time 
				}
			}
		}

		// Use this for initialization
		void Start ()
		{
			StartCoroutine(BuildWorld());
		}
	}
}
