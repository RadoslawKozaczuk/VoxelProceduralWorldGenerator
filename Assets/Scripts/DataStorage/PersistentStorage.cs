using System.Collections.Generic;
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

    public void SaveGame(Vector3 playerPosition, Vector3 playerRotation, World world)
    {
        _writer = new BinaryWriter(File.Open(_savePath, FileMode.Create));

        // player
        Write(playerPosition);
        Write(playerRotation);

        // world parameters
        _writer.Write(world.ChunkSize);
        _writer.Write(world.WorldSizeX);
        _writer.Write(world.WorldSizeY);
        _writer.Write(world.WorldSizeZ);

        // chunk data
        for (int x = 0; x < world.WorldSizeX; x++)
            for (int z = 0; z < world.WorldSizeZ; z++)
                for (int y = 0; y < world.WorldSizeY; y++)
                    Write(new Chunk()
                    {
                        Blocks = world.Chunks[x, y, z].Blocks
                    });

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
            PlayerPosition = ReadVector3(),
            PlayerRotation = ReadVector3(),

            // world data
            ChunkSize = _reader.ReadByte(),
            WorldSizeX = _reader.ReadByte(),
            WorldSizeY = _reader.ReadByte(),
            WorldSizeZ = _reader.ReadByte()
        };

        int sizeX = loadGameData.WorldSizeX,
            sizeY = loadGameData.WorldSizeY,
            sizeZ = loadGameData.WorldSizeX;

        var chunks = new Chunk[sizeX, sizeY, sizeZ];

        // chunks data
        for (int x = 0; x < sizeX; x++)
            for (int z = 0; z < sizeZ; z++)
                for (int y = 0; y < sizeY; y++)
                    chunks[x, y, z] = ReadChunkData();


        loadGameData.Chunks = chunks;

        _reader.Close();
        _reader.Dispose();

        return loadGameData;
    }

    #region Reading Methods
    Chunk ReadChunkData() => new Chunk()
    {
        Blocks = ReadBlockDataArray(),
        Coord = ReadVector3Int(),
        Status = ChunkStatus.NeedToBeRedrawn
    };

    Block[,,] ReadBlockDataArray()
    {
        var blocks = new Block[_chunkSize, _chunkSize, _chunkSize];
        for (var z = 0; z < _chunkSize; z++)
            for (var y = 0; y < _chunkSize; y++)
                for (var x = 0; x < _chunkSize; x++)
                    blocks[x, y, z] = ReadBlock();

        return blocks;
    }

    Block ReadBlock() => new Block
    {
        Faces = (Cubesides)_reader.ReadByte(),
        Type = (BlockTypes)_reader.ReadByte(),
        Hp = _reader.ReadByte(),
        HealthLevel = _reader.ReadByte()
    };

    Quaternion ReadQuaternion() => new Quaternion
    {
        x = _reader.ReadSingle(),
        y = _reader.ReadSingle(),
        z = _reader.ReadSingle(),
        w = _reader.ReadSingle()
    };

    Vector3[] ReadArrayVector3(int size)
    {
        var array = new Vector3[size];
        for (int i = 0; i < size; i++)
            array[i] = ReadVector2();
        return array;
    }

    Vector3 ReadVector3() => new Vector3
    {
        x = _reader.ReadSingle(),
        y = _reader.ReadSingle(),
        z = _reader.ReadSingle()
    };

    Vector3Int ReadVector3Int() => new Vector3Int
    {
        x = _reader.ReadInt32(),
        y = _reader.ReadInt32(),
        z = _reader.ReadInt32()
    };

    List<Vector2> ReadListVector2(int size)
    {
        var list = new List<Vector2>(size);
        for (int i = 0; i < size; i++)
            list.Add(ReadVector2());
        return list;
    }

    Vector2[] ReadArrayVector2(int size)
    {
        var array = new Vector2[size];
        for (int i = 0; i < size; i++)
            array[i] = ReadVector2();
        return array;
    }

    Vector2 ReadVector2() => new Vector2
    {
        x = _reader.ReadSingle(),
        y = _reader.ReadSingle()
    };

    int[] ReadArrayInt32(int size)
    {
        var array = new int[size];
        for (int i = 0; i < size; i++)
            array[i] = _reader.ReadInt32();
        return array;
    }
    #endregion

    #region Writing Methods
    void Write(Chunk chunkData)
    {
        Write(chunkData.Blocks);
        Write(chunkData.Coord);
    }

    void Write(Block[,,] blocks)
    {
        for (var z = 0; z < _chunkSize; z++)
            for (var y = 0; y < _chunkSize; y++)
                for (var x = 0; x < _chunkSize; x++)
                    Write(blocks[x, y, z]);
    }

    void Write(Block value)
    {
        _writer.Write((byte)value.Faces);
        _writer.Write((byte)value.Type);
        _writer.Write(value.Hp);
        _writer.Write(value.HealthLevel);
    }

    void Write(Quaternion value)
    {
        _writer.Write(value.x);
        _writer.Write(value.y);
        _writer.Write(value.z);
        _writer.Write(value.w);
    }

    void Write(Vector3[] array)
    {
        for (int i = 0; i < array.Length; i++)
            Write(array[i]);
    }

    void Write(Vector3 value)
    {
        _writer.Write(value.x);
        _writer.Write(value.y);
        _writer.Write(value.z);
    }

    void Write(Vector3Int value)
    {
        _writer.Write(value.x);
        _writer.Write(value.y);
        _writer.Write(value.z);
    }

    void Write(List<Vector2> list)
    {
        foreach (var v in list)
            Write(v);
    }

    void Write(Vector2[] array)
    {
        for (int i = 0; i < array.Length; i++)
            Write(array[i]);
    }

    void Write(Vector2 value)
    {
        _writer.Write(value.x);
        _writer.Write(value.y);
    }

    void Write(int[] array)
    {
        for (int i = 0; i < array.Length; i++)
            _writer.Write(array[i]);
    }
    #endregion
}