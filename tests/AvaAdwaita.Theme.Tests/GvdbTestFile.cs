using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace AvaAdwaita.Theme.Tests;

/// <summary>
/// Writes a one-item GVDB file, as gvdb-builder.c lays it out. The tables are little-endian in every file; a
/// byteswapped file swaps the signature words and the serialized values (gvdb-format.h).
/// </summary>
internal static class GvdbTestFile
{
    public static byte[] Build(string key, byte[] content, bool compressed, bool bigEndian = false)
    {
        var payload = content;
        if (compressed)
        {
            using var m = new MemoryStream();
            using (var z = new ZLibStream(m, CompressionLevel.Optimal, leaveOpen: true))
            {
                z.Write(content);
            }

            payload = m.ToArray();
        }

        var keyBytes = Encoding.UTF8.GetBytes(key);
        using var f = new MemoryStream();
        var w = new EndianWriter(f, false);
        const int RootStart = 24;
        const int RootSize = 8 + 4 + 24;
        var keyStart = RootStart + RootSize;
        var valueStart = (keyStart + keyBytes.Length + 7) & ~7;
        var value = new MemoryStream();
        var vw = new EndianWriter(value, bigEndian);
        vw.Write((uint)content.Length);
        vw.Write(compressed ? 1u : 0u);
        vw.Write(payload);
        if (!compressed)
        {
            vw.Write((byte)0);
        }

        vw.Write((byte)0);
        vw.Write(Encoding.ASCII.GetBytes("(uuay)"));
        var valueBytes = value.ToArray();

        uint hash = 5381;
        foreach (var b in keyBytes)
        {
            hash = (hash * 33) + (uint)(sbyte)b;
        }

        // In a byteswapped file the signature words are swapped, which is how readers detect it.
        new EndianWriter(f, bigEndian).Write(1918981703u);
        new EndianWriter(f, bigEndian).Write(1953390953u);
        w.Write(0u);
        w.Write(0u);
        w.Write((uint)RootStart);
        w.Write((uint)(RootStart + RootSize));
        w.Write(0u);
        w.Write(1u);
        w.Write(0u);
        w.Write(hash);
        w.Write(0xffffffffu);
        w.Write((uint)keyStart);
        w.Write((ushort)keyBytes.Length);
        w.Write((byte)'v');
        w.Write((byte)0);
        w.Write((uint)valueStart);
        w.Write((uint)(valueStart + valueBytes.Length));
        w.Write(keyBytes);
        while (f.Length < valueStart)
        {
            w.Write((byte)0);
        }

        w.Write(valueBytes);
        w.Flush();
        return f.ToArray();
    }

    private sealed class EndianWriter(Stream stream, bool bigEndian)
    {
        public void Write(uint v)
        {
            Span<byte> b = stackalloc byte[4];
            if (bigEndian)
            {
                BinaryPrimitives.WriteUInt32BigEndian(b, v);
            }
            else
            {
                BinaryPrimitives.WriteUInt32LittleEndian(b, v);
            }

            stream.Write(b);
        }

        public void Write(ushort v)
        {
            Span<byte> b = stackalloc byte[2];
            if (bigEndian)
            {
                BinaryPrimitives.WriteUInt16BigEndian(b, v);
            }
            else
            {
                BinaryPrimitives.WriteUInt16LittleEndian(b, v);
            }

            stream.Write(b);
        }

        public void Write(byte v) => stream.WriteByte(v);

        public void Write(byte[] v) => stream.Write(v);

        public void Flush() => stream.Flush();
    }
}
