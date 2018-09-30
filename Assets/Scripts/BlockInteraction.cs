using UnityEngine;

public class BlockInteraction : MonoBehaviour
{
    public Game Game;
    public GameObject Camera;
    const float AttackRange = 4.0f;
    BlockTypes _buildBlockType = BlockTypes.Stone;
    
    void Update()
    {
        if (!Input.anyKey) return;

        //CheckForBuildBlockType();

        // left mouse click is going to destroy block and the right mouse click will add a block
        if (!(Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)))
            return;

        RaycastHit hit;
        //for cross hairs
        if (!Physics.Raycast(Camera.transform.position, Camera.transform.forward, out hit, AttackRange))
            return;

        Vector3 hitBlock = hit.point - hit.normal / 2.0f; // central point

        //Vector3 hitBlock = Input.GetMouseButtonDown(0)
        //    ? hit.point - hit.normal / 2.0f // central point
        //    : hit.point + hit.normal / 2.0f; // next to the one that we are pointing at

        Game.ProcessBlockHit(hitBlock);
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
}
