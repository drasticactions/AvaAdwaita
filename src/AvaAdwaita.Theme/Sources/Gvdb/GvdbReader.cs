using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace AvaAdwaita.Sources.Gvdb;

/// <summary>
/// Reads a GVDB hash table file: GResource bundles, dconf databases and <c>gschemas.compiled</c>.
/// </summary>
/// <remarks>Port of the lookup half of GLib's gvdb-reader.c.</remarks>
internal sealed class GvdbReader
{
    private const uint Signature0 = 1918981703;
    private const uint Signature1 = 1953390953;
    private const int ItemSize = 24;

    private readonly ReadOnlyMemory<byte> _data;
    private readonly int _bloomStart;
    private readonly int _nBloomWords;
    private readonly int _bloomShift;
    private readonly int _bucketsStart;
    private readonly int _nBuckets;
    private readonly int _itemsStart;
    private readonly int _nItems;

    private GvdbReader(ReadOnlyMemory<byte> data, int start, int end, bool byteswapped)
    {
        _data = data;
        Byteswapped = byteswapped;
        var d = data.Span;
        if (end > d.Length || start + 8 > end)
        {
            return;
        }

        var bloomWord = U32(d, start);
        var nBloom = (int)(bloomWord & ((1u << 27) - 1));
        _bloomShift = (int)(bloomWord >> 27);
        var nBuckets = (int)U32(d, start + 4);
        var size = end - start - 8;
        if (nBloom * 4 > size)
        {
            return;
        }

        _bloomStart = start + 8;
        _nBloomWords = nBloom;
        size -= nBloom * 4;
        if (nBuckets < 0 || nBuckets * 4 > size)
        {
            return;
        }

        _bucketsStart = _bloomStart + (nBloom * 4);
        _nBuckets = nBuckets;
        size -= nBuckets * 4;
        if (size % ItemSize != 0)
        {
            return;
        }

        _itemsStart = _bucketsStart + (nBuckets * 4);
        _nItems = size / ItemSize;
    }

    /// <summary>True when the file's GVariant values are in the other byte order.</summary>
    public bool Byteswapped { get; }

    /// <summary>Opens the root table of a GVDB file, or returns null when it is not one.</summary>
    public static GvdbReader? Open(ReadOnlyMemory<byte> data)
    {
        var d = data.Span;
        if (d.Length < 24)
        {
            return null;
        }

        var s0 = BinaryPrimitives.ReadUInt32LittleEndian(d);
        var s1 = BinaryPrimitives.ReadUInt32LittleEndian(d[4..]);
        bool swapped;
        if (s0 == Signature0 && s1 == Signature1)
        {
            swapped = false;
        }
        else if (s0 == BinaryPrimitives.ReverseEndianness(Signature0) && s1 == BinaryPrimitives.ReverseEndianness(Signature1))
        {
            swapped = true;
        }
        else
        {
            return null;
        }

        var start = (int)U32(d, 16);
        var end = (int)U32(d, 20);
        return new GvdbReader(data, start, end, swapped);
    }

    private static uint U32(ReadOnlySpan<byte> d, int at) => BinaryPrimitives.ReadUInt32LittleEndian(d[at..]);

    private int ItemOffset(int i) => _itemsStart + (i * ItemSize);

    private uint ItemHash(int i) => U32(_data.Span, ItemOffset(i));

    private uint ItemParent(int i) => U32(_data.Span, ItemOffset(i) + 4);

    private char ItemType(int i) => (char)_data.Span[ItemOffset(i) + 14];

    private ReadOnlySpan<byte> ItemKey(int i)
    {
        var d = _data.Span;
        var o = ItemOffset(i);
        var start = (int)U32(d, o + 8);
        var size = BinaryPrimitives.ReadUInt16LittleEndian(d[(o + 12)..]);
        return start + size <= d.Length ? d.Slice(start, size) : default;
    }

    private (int Start, int End) ItemPointer(int i)
    {
        var d = _data.Span;
        var o = ItemOffset(i);
        return ((int)U32(d, o + 16), (int)U32(d, o + 20));
    }

    private bool BloomFilter(uint hash)
    {
        if (_nBloomWords == 0)
        {
            return true;
        }

        var word = (int)((hash / 32) % (uint)_nBloomWords);
        var mask = (1u << (int)(hash & 31)) | (1u << (int)((hash >> _bloomShift) & 31));
        return (U32(_data.Span, _bloomStart + (word * 4)) & mask) == mask;
    }

    private bool CheckName(int item, ReadOnlySpan<byte> key)
    {
        var thisKey = ItemKey(item);
        if (thisKey.Length > key.Length)
        {
            return false;
        }

        var rest = key.Length - thisKey.Length;
        if (!key[rest..].SequenceEqual(thisKey))
        {
            return false;
        }

        var parent = ItemParent(item);
        if (rest == 0 && parent == 0xffffffffu)
        {
            return true;
        }

        return parent < _nItems && thisKey.Length > 0 && CheckName((int)parent, key[..rest]);
    }

    private int Lookup(string key, char type)
    {
        if (_nBuckets == 0 || _nItems == 0)
        {
            return -1;
        }

        var bytes = Encoding.UTF8.GetBytes(key);
        uint hash = 5381;
        foreach (var b in bytes)
        {
            hash = (hash * 33) + (uint)(sbyte)b;
        }

        if (!BloomFilter(hash))
        {
            return -1;
        }

        var bucket = (int)(hash % (uint)_nBuckets);
        var itemno = (int)U32(_data.Span, _bucketsStart + (bucket * 4));
        int lastno;
        if (bucket == _nBuckets - 1 || (lastno = (int)U32(_data.Span, _bucketsStart + ((bucket + 1) * 4))) > _nItems)
        {
            lastno = _nItems;
        }

        for (; itemno < lastno; itemno++)
        {
            if (ItemHash(itemno) == hash && CheckName(itemno, bytes) && ItemType(itemno) == type)
            {
                return itemno;
            }
        }

        return -1;
    }

    /// <summary>The raw serialized <c>v</c> value of a key, or null.</summary>
    public ReadOnlyMemory<byte>? GetRawValue(string key)
    {
        var i = Lookup(key, 'v');
        if (i < 0)
        {
            return null;
        }

        var (start, end) = ItemPointer(i);
        if (start > end || end > _data.Length)
        {
            return null;
        }

        return _data[start..end];
    }

    /// <summary>The decoded value of a key, or null.</summary>
    public object? GetValue(string key) =>
        GetRawValue(key) is { } raw ? GVariantReader.DecodeVariant(raw.Span, Byteswapped) : null;

    /// <summary>A nested hash table, as the schemas in <c>gschemas.compiled</c>.</summary>
    public GvdbReader? GetTable(string key)
    {
        var i = Lookup(key, 'H');
        if (i < 0)
        {
            return null;
        }

        var (start, end) = ItemPointer(i);
        return new GvdbReader(_data, start, end, Byteswapped);
    }

    /// <summary>Every full key in the table with its item type.</summary>
    public IEnumerable<(string Key, char Type)> Entries()
    {
        for (var i = 0; i < _nItems; i++)
        {
            yield return (FullKey(i), ItemType(i));
        }
    }

    private string FullKey(int i)
    {
        var parts = new List<byte[]>();
        var guard = 0;
        for (var n = i; n >= 0 && n < _nItems && guard++ < 64;)
        {
            parts.Add(ItemKey(n).ToArray());
            var p = ItemParent(n);
            n = p == 0xffffffffu ? -1 : (int)p;
        }

        parts.Reverse();
        var sb = new StringBuilder();
        foreach (var p in parts)
        {
            sb.Append(Encoding.UTF8.GetString(p));
        }

        return sb.ToString();
    }

    /// <summary>Reads a file from a GResource bundle, inflating it when it is compressed.</summary>
    /// <remarks>Each resource is a <c>(uuay)</c>: size, flags, data. Flag 1 is G_RESOURCE_FLAGS_COMPRESSED.</remarks>
    public byte[]? ReadResource(string path)
    {
        if (GetRawValue(path) is not { } raw || !GVariantReader.TryUnwrapVariant(raw.Span, out var child, out var type) || type != "(uuay)" || child.Length < 8)
        {
            return null;
        }

        var size = Byteswapped ? BinaryPrimitives.ReadUInt32BigEndian(child) : BinaryPrimitives.ReadUInt32LittleEndian(child);
        var flags = Byteswapped ? BinaryPrimitives.ReadUInt32BigEndian(child[4..]) : BinaryPrimitives.ReadUInt32LittleEndian(child[4..]);
        var content = child[8..];
        if ((flags & 1) != 0)
        {
            using var input = new MemoryStream(content.ToArray());
            using var z = new ZLibStream(input, CompressionMode.Decompress);
            var output = new byte[size];
            z.ReadExactly(output);
            return output;
        }

        // GResource stores a trailing nul after the data.
        return content[..(int)Math.Min(size, (uint)content.Length)].ToArray();
    }
}
