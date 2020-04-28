using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Voxels.Common;
using Voxels.Common.DataModels;

namespace Voxels.SaveLoad
{
    public static class SaveLoadController
    {
        readonly static string _saveFileName = "VoxelsSaveGame.sav";
        readonly static string _savePath = Path.Combine(Application.persistentDataPath, _saveFileName);

        static BinaryWriter _writer;
        static BinaryReader _reader;

        /// <summary>
        /// Saves the current state of the game to a file.
        /// World data is taken from <see cref="GlobalVariables"/>.
        /// </summary>
        public static void SaveGame(Vector3 playerPosition, Vector3 playerRotation)
        {
            _writer = new BinaryWriter(File.Open(_savePath, FileMode.Create));

            // player
            Write(playerPosition);
            Write(playerRotation);

            int worldSizeX = GlobalVariables.Settings.WorldSizeX,
                worldSizeZ = GlobalVariables.Settings.WorldSizeZ;

            // world parameters
            _writer.Write((byte)worldSizeX);
            _writer.Write((byte)worldSizeZ);

            // chunk data
            int x, y, z;
            for (x = 0; x < GlobalVariables.Settings.WorldSizeX; x++)
                for (z = 0; z < GlobalVariables.Settings.WorldSizeZ; z++)
                    for (y = 0; y < Constants.WORLD_SIZE_Y; y++)
                        Write(GlobalVariables.Chunks[x, y, z]);

            int blockNumberX = worldSizeX * Constants.CHUNK_SIZE,
                blockNumberY = Constants.WORLD_SIZE_Y * Constants.CHUNK_SIZE,
                blockNumberZ = worldSizeZ * Constants.CHUNK_SIZE;

            for (x = 0; x < blockNumberX; x++)
                for (z = 0; z < blockNumberZ; z++)
                    for (y = 0; y < blockNumberY; y++)
                        Write(GlobalVariables.Blocks[x, y, z]);

            _writer.Close();
            _writer.Dispose();
        }

        /// <summary>
        /// Load the game from a file.
        /// World data stored in <see cref="GlobalVariables"/> is populated automatically.
        /// The method returns only player's data.
        /// </summary>
        public static void LoadGame(out Vector3 playerPosition, out Vector3 playerRotation)
        {
            byte[] data = File.ReadAllBytes(_savePath);
            _reader = new BinaryReader(new MemoryStream(data));

            // player data
            playerPosition = ReadVector3();
            playerRotation = ReadVector3();

            // world data
            int worldSizeX = GlobalVariables.Settings.WorldSizeX = _reader.ReadByte(),
                worldSizeZ = GlobalVariables.Settings.WorldSizeZ = _reader.ReadByte(),
                blockNumberX = worldSizeX * Constants.CHUNK_SIZE,
                blockNumberY = Constants.WORLD_SIZE_Y * Constants.CHUNK_SIZE,
                blockNumberZ = worldSizeZ * Constants.CHUNK_SIZE;

            GlobalVariables.Chunks = new ChunkData[worldSizeX, Constants.WORLD_SIZE_Y, worldSizeZ];
            int x, y, z;
            for (x = 0; x < worldSizeX; x++)
                for (z = 0; z < worldSizeZ; z++)
                    for (y = 0; y < Constants.WORLD_SIZE_Y; y++)
                        GlobalVariables.Chunks[x, y, z] = ReadChunk();

            GlobalVariables.Blocks = new BlockData[blockNumberX, blockNumberY, blockNumberZ];
            for (x = 0; x < blockNumberX; x++)
                for (z = 0; z < blockNumberZ; z++)
                    for (y = 0; y < blockNumberY; y++)
                        GlobalVariables.Blocks[x, y, z] = ReadBlock();

            _reader.Close();
            _reader.Dispose();
        }

        #region Reading Methods
        static ChunkData ReadChunk() => new ChunkData(ReadReadonlyVector3Int(), ReadReadonlyVector3Int()) { Status = ChunkStatus.NeedToBeRedrawn };

        static BlockData ReadBlock() => new BlockData
        {
            Faces = (Cubeside)_reader.ReadByte(),
            Type = (BlockType)_reader.ReadByte(),
            Hp = _reader.ReadByte(),
            HealthLevel = _reader.ReadByte()
        };

        static Quaternion ReadQuaternion() => new Quaternion(_reader.ReadSingle(), _reader.ReadSingle(), _reader.ReadSingle(), _reader.ReadSingle());

        static Vector3[] ReadArrayVector3(int size)
        {
            var array = new Vector3[size];
            for (int i = 0; i < size; i++)
                array[i] = ReadVector2();
            return array;
        }

        static Vector3 ReadVector3() => new Vector3(_reader.ReadSingle(), _reader.ReadSingle(), _reader.ReadSingle());

        static Vector3Int ReadVector3Int() => new Vector3Int(_reader.ReadInt32(), _reader.ReadInt32(), _reader.ReadInt32());

        static ReadonlyVector3Int ReadReadonlyVector3Int() => new ReadonlyVector3Int(_reader.ReadInt32(), _reader.ReadInt32(), _reader.ReadInt32());

        static List<Vector2> ReadListVector2(int size)
        {
            var list = new List<Vector2>(size);
            for (int i = 0; i < size; i++)
                list.Add(ReadVector2());
            return list;
        }

        static Vector2[] ReadArrayVector2(int size)
        {
            var array = new Vector2[size];
            for (int i = 0; i < size; i++)
                array[i] = ReadVector2();
            return array;
        }

        static Vector2 ReadVector2() => new Vector2(_reader.ReadSingle(), _reader.ReadSingle());

        static int[] ReadArrayInt32(int size)
        {
            var array = new int[size];
            for (int i = 0; i < size; i++)
                array[i] = _reader.ReadInt32();
            return array;
        }
        #endregion

        #region Writing Methods
        static void Write(ChunkData chunk)
        {
            Write(chunk.Coord);
            Write(chunk.Position);
        }

        static void Write(BlockData[,,] blocks)
        {
            for (int z = 0; z < Constants.CHUNK_SIZE; z++)
                for (int y = 0; y < Constants.CHUNK_SIZE; y++)
                    for (int x = 0; x < Constants.CHUNK_SIZE; x++)
                        Write(blocks[x, y, z]);
        }

        static void Write(BlockData value)
        {
            _writer.Write((byte)value.Faces);
            _writer.Write((byte)value.Type);
            _writer.Write(value.Hp);
            _writer.Write(value.HealthLevel);
        }

        static void Write(Quaternion value)
        {
            _writer.Write(value.x);
            _writer.Write(value.y);
            _writer.Write(value.z);
            _writer.Write(value.w);
        }

        static void Write(Vector3[] array)
        {
            for (int i = 0; i < array.Length; i++)
                Write(array[i]);
        }

        static void Write(Vector3 value)
        {
            _writer.Write(value.x);
            _writer.Write(value.y);
            _writer.Write(value.z);
        }

        static void Write(Vector3Int value)
        {
            _writer.Write(value.x);
            _writer.Write(value.y);
            _writer.Write(value.z);
        }

        static void Write(ReadonlyVector3Int value)
        {
            _writer.Write(value.X);
            _writer.Write(value.Y);
            _writer.Write(value.Z);
        }

        static void Write(List<Vector2> list)
        {
            foreach (Vector2 v in list)
                Write(v);
        }

        static void Write(Vector2[] array)
        {
            for (int i = 0; i < array.Length; i++)
                Write(array[i]);
        }

        static void Write(Vector2 value)
        {
            _writer.Write(value.x);
            _writer.Write(value.y);
        }

        static void Write(int[] array)
        {
            for (int i = 0; i < array.Length; i++)
                _writer.Write(array[i]);
        }
        #endregion
    }
}
