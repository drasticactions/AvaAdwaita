using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace AvaAdwaita.Sources.Gvdb;

/// <summary>
/// Decodes serialized GVariant data (the GVariant serialization format of GLib's gvariant-serializer.c)
/// into plain values: string, bool, byte, short, int, long, double, object[] for tuples and arrays.
/// </summary>
internal static class GVariantReader
{
    /// <summary>Splits a serialized <c>v</c> into its child data and type string.</summary>
    public static bool TryUnwrapVariant(ReadOnlySpan<byte> data, out ReadOnlySpan<byte> child, out string type)
    {
        child = default;
        type = string.Empty;
        var nul = data.LastIndexOf((byte)0);
        if (nul < 0)
        {
            return false;
        }

        type = Encoding.ASCII.GetString(data[(nul + 1)..]);
        child = data[..nul];
        return type.Length > 0;
    }

    /// <summary>Decodes a serialized <c>v</c>.</summary>
    public static object? DecodeVariant(ReadOnlySpan<byte> data, bool bigEndian = false) =>
        TryUnwrapVariant(data, out var child, out var type) ? Decode(child, type, bigEndian) : null;

    /// <summary>Decodes data of a known type.</summary>
    public static object? Decode(ReadOnlySpan<byte> data, string type, bool bigEndian = false)
    {
        var pos = 0;
        var t = ParseType(type, ref pos);
        return pos == type.Length ? Decode(data, t, bigEndian) : null;
    }

    private sealed record GType(char Kind, GType[] Children)
    {
        public int Alignment { get; init; }

        public int FixedSize { get; init; }
    }

    private static GType ParseType(string s, ref int pos)
    {
        var c = s[pos++];
        switch (c)
        {
            case 'b':
            case 'y':
                return new GType(c, []) { Alignment = 1, FixedSize = 1 };
            case 'n':
            case 'q':
                return new GType(c, []) { Alignment = 2, FixedSize = 2 };
            case 'i':
            case 'u':
            case 'h':
                return new GType(c, []) { Alignment = 4, FixedSize = 4 };
            case 'x':
            case 't':
            case 'd':
                return new GType(c, []) { Alignment = 8, FixedSize = 8 };
            case 's':
            case 'o':
            case 'g':
                return new GType(c, []) { Alignment = 1, FixedSize = 0 };
            case 'v':
                return new GType(c, []) { Alignment = 8, FixedSize = 0 };
            case 'a':
                {
                    var e = ParseType(s, ref pos);
                    return new GType('a', [e]) { Alignment = e.Alignment, FixedSize = 0 };
                }

            case 'm':
                {
                    var e = ParseType(s, ref pos);
                    return new GType('m', [e]) { Alignment = e.Alignment, FixedSize = 0 };
                }

            case '(':
            case '{':
                {
                    var close = c == '(' ? ')' : '}';
                    var items = new List<GType>();
                    while (s[pos] != close)
                    {
                        items.Add(ParseType(s, ref pos));
                    }

                    pos++;
                    var align = 1;
                    var fixedSize = 0;
                    var allFixed = true;
                    foreach (var i in items)
                    {
                        align = Math.Max(align, i.Alignment);
                        if (i.FixedSize == 0)
                        {
                            allFixed = false;
                        }
                        else if (allFixed)
                        {
                            fixedSize = Align(fixedSize, i.Alignment) + i.FixedSize;
                        }
                    }

                    if (allFixed)
                    {
                        fixedSize = items.Count == 0 ? 1 : Align(fixedSize, align);
                    }

                    return new GType('(', items.ToArray()) { Alignment = align, FixedSize = allFixed ? fixedSize : 0 };
                }

            default:
                throw new FormatException($"unsupported GVariant type '{c}'");
        }
    }

    private static int Align(int offset, int alignment) => (offset + alignment - 1) & ~(alignment - 1);

    private static int OffsetSize(int containerSize) => containerSize switch
    {
        0 => 0,
        <= 0xff => 1,
        <= 0xffff => 2,
        _ => 4,
    };

    private static int ReadOffset(ReadOnlySpan<byte> data, int at, int size, bool be) => size switch
    {
        1 => data[at],
        2 => be ? BinaryPrimitives.ReadUInt16BigEndian(data[at..]) : BinaryPrimitives.ReadUInt16LittleEndian(data[at..]),
        _ => (int)(be ? BinaryPrimitives.ReadUInt32BigEndian(data[at..]) : BinaryPrimitives.ReadUInt32LittleEndian(data[at..])),
    };

    private static object? Decode(ReadOnlySpan<byte> d, GType t, bool be)
    {
        if (t.FixedSize > 0 && d.Length != t.FixedSize)
        {
            // GVariant treats wrongly sized fixed data as the default value.
            return null;
        }

        switch (t.Kind)
        {
            case 'b':
                return d[0] != 0;
            case 'y':
                return d[0];
            case 'n':
                return be ? BinaryPrimitives.ReadInt16BigEndian(d) : BinaryPrimitives.ReadInt16LittleEndian(d);
            case 'q':
                return be ? BinaryPrimitives.ReadUInt16BigEndian(d) : BinaryPrimitives.ReadUInt16LittleEndian(d);
            case 'i':
            case 'h':
                return be ? BinaryPrimitives.ReadInt32BigEndian(d) : BinaryPrimitives.ReadInt32LittleEndian(d);
            case 'u':
                return be ? BinaryPrimitives.ReadUInt32BigEndian(d) : BinaryPrimitives.ReadUInt32LittleEndian(d);
            case 'x':
                return be ? BinaryPrimitives.ReadInt64BigEndian(d) : BinaryPrimitives.ReadInt64LittleEndian(d);
            case 't':
                return be ? BinaryPrimitives.ReadUInt64BigEndian(d) : BinaryPrimitives.ReadUInt64LittleEndian(d);
            case 'd':
                return BitConverter.Int64BitsToDouble(be ? BinaryPrimitives.ReadInt64BigEndian(d) : BinaryPrimitives.ReadInt64LittleEndian(d));
            case 's':
            case 'o':
            case 'g':
                {
                    var end = d.IndexOf((byte)0);
                    return Encoding.UTF8.GetString(end < 0 ? d : d[..end]);
                }

            case 'v':
                return DecodeVariant(d, be);
            case 'm':
                {
                    if (d.Length == 0)
                    {
                        return null;
                    }

                    var e = t.Children[0];
                    return Decode(e.FixedSize > 0 ? d : d[..^1], e, be);
                }

            case 'a':
                return DecodeArray(d, t.Children[0], be);
            case '(':
                return DecodeTuple(d, t.Children, be);
            default:
                return null;
        }
    }

    private static object?[] DecodeArray(ReadOnlySpan<byte> d, GType e, bool be)
    {
        if (d.Length == 0)
        {
            return [];
        }

        if (e.FixedSize > 0)
        {
            var n = d.Length / e.FixedSize;
            var a = new object?[n];
            for (var i = 0; i < n; i++)
            {
                a[i] = Decode(d.Slice(i * e.FixedSize, e.FixedSize), e, be);
            }

            return a;
        }

        var osize = OffsetSize(d.Length);
        var tableStart = ReadOffset(d, d.Length - osize, osize, be);
        if (tableStart > d.Length || (d.Length - tableStart) % osize != 0)
        {
            return [];
        }

        var count = (d.Length - tableStart) / osize;
        var result = new object?[count];
        var start = 0;
        for (var i = 0; i < count; i++)
        {
            var end = ReadOffset(d, tableStart + (i * osize), osize, be);
            start = Align(start, e.Alignment);
            if (start > end || end > tableStart)
            {
                return [];
            }

            result[i] = Decode(d[start..end], e, be);
            start = end;
        }

        return result;
    }

    private static object?[] DecodeTuple(ReadOnlySpan<byte> d, GType[] items, bool be)
    {
        var result = new object?[items.Length];
        var osize = OffsetSize(d.Length);
        var offsetIndex = 0;
        var pos = 0;
        for (var i = 0; i < items.Length; i++)
        {
            var it = items[i];
            pos = Align(pos, it.Alignment);
            int end;
            if (it.FixedSize > 0)
            {
                end = pos + it.FixedSize;
            }
            else if (i == items.Length - 1)
            {
                end = d.Length - (offsetIndex * osize);
            }
            else
            {
                offsetIndex++;
                end = ReadOffset(d, d.Length - (offsetIndex * osize), osize, be);
            }

            if (pos > end || end > d.Length)
            {
                return result;
            }

            result[i] = Decode(d[pos..end], it, be);
            pos = end;
        }

        return result;
    }
}
