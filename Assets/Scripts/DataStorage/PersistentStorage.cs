using System.IO;
using UnityEngine;

public class PersistentStorage
{
    [SerializeField] string _saveFileName = "VoxelsSaveGame.sav";
    readonly string _savePath;
    readonly int _chunkSize;

    BinaryWriter _writer;
    BinaryReader _reader;

    public PersistentStorage(int chunkSize)
    {
        _chunkSize = chunkSize;
        _savePath = Path.Combine(Application.persistentDataPath, _saveFileName);
    }
    
    public void SaveGame(Transform playerTransform, World world)
    {
        _writer = new BinaryWriter(File.Open(_savePath, FileMode.Create));

        // player
        Write(playerTransform.position);
        Write(playerTransform.rotation);

        // world parameters
        _writer.Write(world.ChunkSize);
        _writer.Write(world.WorldSizeX);
        _writer.Write(world.WorldSizeY);
        _writer.Write(world.WorldSizeZ);

        // terrain data
        for (int x = 0; x < world.WorldSizeX; x++)
            for (int z = 0; z < world.WorldSizeZ; z++)
                for (int y = 0; y < world.WorldSizeY; y++)
                    Write(world.Chunks[x, y, z].Blocks);

        _writer.Close();
        _writer.Dispose();
    }

    public SaveGameData LoadGame()
    {
        byte[] data = File.ReadAllBytes(_savePath);
        _reader = new BinaryReader(new MemoryStream(data));

        var loadGameData = new SaveGameData()
        {
            // player data
            Position = ReadVector3(),
            Rotation = ReadQuaternion(),

            // world data
            ChunkSize = _reader.ReadByte(),
            WorldSizeX =_reader.ReadByte(),
            WorldSizeY =_reader.ReadByte(),
            WorldSizeZ =_reader.ReadByte()
        };

        int sizeX = loadGameData.WorldSizeX, 
            sizeY = loadGameData.WorldSizeY, 
            sizeZ = loadGameData.WorldSizeX;

        var chunks = new ChunkData[sizeX, sizeY, sizeZ];

        // chunks data
        for (int x = 0; x < sizeX; x++)
            for (int z = 0; z < sizeZ; z++)
                for (int y = 0; y < sizeY; y++)
                    chunks[x, y, z] = new ChunkData() { Blocks = ReadChunkData() };

        loadGameData.Chunks = chunks;

        _reader.Close();
        _reader.Dispose();

        return loadGameData;
    }
    
    BlockData[,,] ReadChunkData()
    {
        var blocks = new BlockData[_chunkSize, _chunkSize, _chunkSize];
        for (var z = 0; z < _chunkSize; z++)
            for (var y = 0; y < _chunkSize; y++)
                for (var x = 0; x < _chunkSize; x++)
                    blocks[x, y, z] = ReadBlock();

        return blocks;
    }

    BlockData ReadBlock() => new BlockData
    {
        Faces = (Cubesides)_reader.ReadByte(),
        Type = (BlockTypes)_reader.ReadByte()
    };
    
    Quaternion ReadQuaternion() => new Quaternion
    {
        x = _reader.ReadSingle(),
        y = _reader.ReadSingle(),
        z = _reader.ReadSingle(),
        w = _reader.ReadSingle()
    };

    Vector3 ReadVector3() => new Vector3
    {
        x = _reader.ReadSingle(),
        y = _reader.ReadSingle(),
        z = _reader.ReadSingle()
    };
    
    void Write(BlockData[,,] blocks)
    {
        for (var z = 0; z < _chunkSize; z++)
            for (var y = 0; y < _chunkSize; y++)
                for (var x = 0; x < _chunkSize; x++)
                {
                    var block = blocks[x, y, z];
                    _writer.Write((byte)block.Faces);
                    _writer.Write((byte)block.Type);
                }
    }
    
    void Write(Quaternion value)
    {
        _writer.Write(value.x);
        _writer.Write(value.y);
        _writer.Write(value.z);
        _writer.Write(value.w);
    }

    void Write(Vector3 value)
    {
        _writer.Write(value.x);
        _writer.Write(value.y);
        _writer.Write(value.z);
    }
}