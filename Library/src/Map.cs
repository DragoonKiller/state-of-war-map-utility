using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;

namespace StateOfWarUtility
{
    public enum TileGround : uint
    {
        Passed = 0x00000000,
        Blocked = 0x00FFFFFD,
    }
    
    public enum TileAir : uint
    {
        Passed = 0x00000000,
        Blocked = 0x00FFFFFD,
    }
    
    public enum TileTurret : uint
    {
        Passed = 0x00000000,
        Blocked = 0x00010000,
    }
    
    
    public class Tile
    {
        public static readonly List<byte> template = new List<byte>() {
            0x00,0x00,0xFD,0xFF,0xFF,0x00,0xFD,0xFF,0xFF,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00 };
        internal static int length { get => template.Count; }
        [Location(0x0)] public byte x;
        [Location(0x1)] public byte y;
        [Location(0x2)] public TileGround ground;
        [Location(0x6)] public TileAir air;
        [Location(0xA)] public TileTurret turret;
        internal Tile() => Access(0, template);
        internal void Access(int begin, List<byte> arr) => arr.GrabData(begin, this);
        internal void AppendTo(List<byte> arr)
        {
            arr.AddRange(template);
            arr.Set(arr.Count-length, this);
        }
    }
    
    
    public class MapInfo
    {
        internal static readonly List<byte> mapHeader = new List<byte>() {
            0x04,0x56,0x45,0x52,0x37,0x00,0x03,0x00,0x00,0xC0,0x04,0x00,0x00,0x40,0x00,0x00,0x00,0x40,0x00,0x00,0x00 };
        public static int length { get => mapHeader.Count; }
        [Location(0x05)] public uint initViewX;
        [Location(0x09)] public uint initViewY;
        [Location(0x0D)] public uint width;
        [Location(0x11)] public uint height;
        internal MapInfo() => Access(0, mapHeader);
        internal void Access(int begin, List<byte> arr) => arr.GrabData(begin, this);
        internal void AppendTo(List<byte> arr)
        {
            arr.AddRange(mapHeader);
            arr.Set(arr.Count-length, this);
        }
    }
    
    public sealed class Map : IEnumerable<Tile>
    {
        public readonly MapInfo headerInfo = new MapInfo();
        public Tile[,] tiles = null;
        
        public Tile this[int x, int y] { get => tiles[x, y]; }
        
        public int width { get => (int)headerInfo.width; }
        public int height { get => (int)headerInfo.height; }
        
        public Map(string path) : this(File.ReadAllBytes(path)) { }
        public Map(byte[] data) : this(new List<byte>(data)) { }
        public Map(List<byte> data) => FromBytes(data);
        
        public IEnumerator<Tile> GetEnumerator() => tiles.Cast<Tile>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException();
        
        public void FromBytes(List<byte> arr)
        {
            headerInfo.Access(0, arr);
            tiles = new Tile[height, width];
            
            for(int y = 0; y < height; y++) for(int x = 0; x < width; x++)
            {
                int index = (y * width + x) * Tile.length + MapInfo.length;
                tiles[y, x] = new Tile();
                tiles[y, x].Access(index, arr);
            }
        }
        
        public List<byte> ToBytes()
        {
            var data = new List<byte>();
            headerInfo.AppendTo(data);
            for(int y = 0; y < height; y++) for(int x = 0; x < width; x++)
            {
                tiles[y, x].AppendTo(data);
            }
            // Append 5 bytes empty.
            // This is necessary for map reading.
            for(int i=0; i<5; i++) data.Add(0x00);
            return data;
        }
        
        public void Save(string path) => File.WriteAllBytes(path, ToBytes().ToArray());
        
        public static bool Validate(string path)
        {
            try
            {
                var data = File.ReadAllBytes(path);
                var head = data.Slice(0, MapInfo.mapHeader.Count);
                return MapInfo.mapHeader.SameAs(head);
            }
            catch(FileNotFoundException) { return false; }
            catch(FieldAccessException) { return false; }
            catch(AccessViolationException) { return false; }
        }
    }
    
}
