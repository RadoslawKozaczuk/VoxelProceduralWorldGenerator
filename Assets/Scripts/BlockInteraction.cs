using UnityEngine;

public class BlockInteraction : MonoBehaviour
{
    public GameObject Cam;
    const float AttackRange = 4.0f;
    BlockTypes _buildBlockType = BlockTypes.Stone;

    // Update is called once per frame
    void Update()
    {
        if (!Input.anyKey) return;

        CheckForBuildBlockType();

        // left mouse click is going to destroy block and the right mouse click will add a block
        if (!(Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)))
            return;

        RaycastHit hit;

        //for mouse clicking
        //Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); 
        //if ( Physics.Raycast (ray,out hit,10)) 
        //{

        // DEBUG - visible ray
        //Debug.DrawRay(Cam.transform.position, Cam.transform.forward, Color.red, 1000, true);

        var sw = new System.Diagnostics.Stopwatch();

        sw.Start();
        //for cross hairs
        if (!Physics.Raycast(Cam.transform.position, Cam.transform.forward, out hit, AttackRange))
        {
            sw.Stop();
            Debug.Log("Raycast missed. " + sw.ElapsedTicks + " ticks");
            return;
        }
        sw.Stop();
        Debug.Log("Raycast hit. " + sw.ElapsedTicks + " ticks");

        Vector3 hitBlock = Input.GetMouseButtonDown(0)
            ? hit.point - hit.normal / 2.0f // central point
            : hit.point + hit.normal / 2.0f; // next to the one that we are pointing at

        //int chunkX = hitBlock.x < 0 ? 0 : (int)(hitBlock.x / World.ChunkSize);
        //int chunkY = hitBlock.y < 0 ? 0 : (int)(hitBlock.y / World.ChunkSize);
        //int chunkZ = hitBlock.z < 0 ? 0 : (int)(hitBlock.z / World.ChunkSize);

        //Chunk hitc = World.Chunks[chunkX, chunkY, chunkZ];

        //int blx = (int)hitBlock.x - chunkX * World.ChunkSize;
        //int bly = (int)hitBlock.y - chunkY * World.ChunkSize;
        //int blz = (int)hitBlock.z - chunkZ * World.ChunkSize;

        //Block b = hitc.Blocks[blx, bly, blz];

        // temporary commented out
        //bool update = Input.GetMouseButtonDown(0) 
        //	? b.HitBlock() // returns true if destroyed
        //	: b.BuildBlock(_buildBlockType); // always returns true

        //if (!update) return;

        //hitc.Changed = true;
        //RedrawNeighbours(b.LocalPosition, chunkX, chunkY, chunkZ);
    }

    void CheckForBuildBlockType()
    {
        if (Input.GetKeyDown("1"))
        {
            _buildBlockType = BlockTypes.Grass;
            Debug.Log("Change build block type to Grass");
        }
        else if (Input.GetKeyDown("2"))
        {
            _buildBlockType = BlockTypes.Dirt;
            Debug.Log("Change build block type to Dirt");
        }
        else if (Input.GetKeyDown("3"))
        {
            _buildBlockType = BlockTypes.Stone;
            Debug.Log("Change build block type to Stone");
        }
        else if (Input.GetKeyDown("4"))
        {
            _buildBlockType = BlockTypes.Diamond;
            Debug.Log("Change build block type to Diamond");
        }
        else if (Input.GetKeyDown("5"))
        {
            _buildBlockType = BlockTypes.Bedrock;
            Debug.Log("Change build block type to Bedrock");
        }
        else if (Input.GetKeyDown("6"))
        {
            _buildBlockType = BlockTypes.Redstone;
            Debug.Log("Change build block type to Redstone");
        }
        else if (Input.GetKeyDown("7"))
        {
            _buildBlockType = BlockTypes.Sand;
            Debug.Log("Change build block type to Sand");
        }
        else if (Input.GetKeyDown("8"))
        {
            _buildBlockType = BlockTypes.Water;
            Debug.Log("Change build block type to Water");
        }
    }

    private void RedrawNeighbours(Vector3 blockPosition, int chunkX, int chunkY, int chunkZ)
    {
        // if the block is on the edge of the chunk we need to inform the neighbor chunk
        //         if (blockPosition.x == 0 && chunkX - 1 >= 0)
        //             World.Chunks[chunkX - 1, chunkY, chunkZ].RecreateMeshAndCollider();
        //if (blockPosition.x == World.ChunkSize - 1 && chunkX + 1 < World.WorldSizeX)
        //             World.Chunks[chunkX + 1, chunkY, chunkZ].RecreateMeshAndCollider();

        //if (blockPosition.y == 0 && chunkY - 1 >= 0)
        //             World.Chunks[chunkX, chunkY - 1, chunkZ].RecreateMeshAndCollider();
        //if (blockPosition.y == World.ChunkSize - 1 && chunkY + 1 < World.WorldSizeY)
        //             World.Chunks[chunkX, chunkY + 1, chunkZ].RecreateMeshAndCollider();

        //if (blockPosition.z == 0 && chunkX - 1 >= 0)
        //             World.Chunks[chunkX, chunkY, chunkZ - 1].RecreateMeshAndCollider();
        //if (blockPosition.z == World.ChunkSize - 1 && chunkZ + 1 < World.WorldSizeZ)
        //             World.Chunks[chunkX, chunkY, chunkZ + 1].RecreateMeshAndCollider();
    }
}
