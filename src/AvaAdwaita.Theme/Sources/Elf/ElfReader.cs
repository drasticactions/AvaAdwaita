using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AvaAdwaita.Sources.Elf;

/// <summary>
/// Reads the section header table of an ELF file: ELF64 and ELF32, little- and big-endian. The file is
/// read as data; it is never mapped or loaded.
/// </summary>
internal sealed class ElfReader
{
    private const uint SectionTypeNoBits = 8;

    private ElfReader(List<ElfSection> sections, bool is64, bool bigEndian)
    {
        Sections = sections;
        Is64 = is64;
        BigEndian = bigEndian;
    }

    public IReadOnlyList<ElfSection> Sections { get; }

    public bool Is64 { get; }

    public bool BigEndian { get; }

    public ElfSection? Find(string name) => ((List<ElfSection>)Sections).Find(s => s.Name == name);

    /// <summary>Reads the section headers of the file at <paramref name="path"/>.</summary>
    public static ElfReader Open(string path)
    {
        using var f = File.OpenRead(path);
        return Read(f);
    }

    /// <summary>Reads the section headers from a seekable stream.</summary>
    public static ElfReader Read(Stream f)
    {
        Span<byte> ident = stackalloc byte[16];
        ReadExactly(f, 0, ident);
        if (ident[0] != 0x7f || ident[1] != (byte)'E' || ident[2] != (byte)'L' || ident[3] != (byte)'F')
        {
            throw new InvalidDataException("not an ELF file");
        }

        var is64 = ident[4] switch
        {
            1 => false,
            2 => true,
            _ => throw new InvalidDataException("bad ELF class"),
        };
        var be = ident[5] switch
        {
            1 => false,
            2 => true,
            _ => throw new InvalidDataException("bad ELF data encoding"),
        };

        var header = new byte[is64 ? 64 : 52];
        ReadExactly(f, 0, header);
        long shoff;
        int shentsize, shnum, shstrndx;
        if (is64)
        {
            shoff = (long)U64(header, 0x28, be);
            shentsize = U16(header, 0x3A, be);
            shnum = U16(header, 0x3C, be);
            shstrndx = U16(header, 0x3E, be);
        }
        else
        {
            shoff = U32(header, 0x20, be);
            shentsize = U16(header, 0x2E, be);
            shnum = U16(header, 0x30, be);
            shstrndx = U16(header, 0x32, be);
        }

        if (shoff <= 0 || shnum == 0 || shentsize < (is64 ? 64 : 40) || shoff + ((long)shnum * shentsize) > f.Length)
        {
            throw new InvalidDataException("ELF section header table is missing or truncated");
        }

        var table = new byte[shnum * shentsize];
        ReadExactly(f, shoff, table);
        var raw = new (uint Name, uint Type, long Offset, long Size)[shnum];
        for (var i = 0; i < shnum; i++)
        {
            var o = i * shentsize;
            if (is64)
            {
                raw[i] = (U32(table, o, be), U32(table, o + 4, be), (long)U64(table, o + 0x18, be), (long)U64(table, o + 0x20, be));
            }
            else
            {
                raw[i] = (U32(table, o, be), U32(table, o + 4, be), U32(table, o + 0x10, be), U32(table, o + 0x14, be));
            }
        }

        if (shstrndx >= shnum)
        {
            throw new InvalidDataException("bad section name table index");
        }

        var strtab = raw[shstrndx];
        if (strtab.Offset + strtab.Size > f.Length)
        {
            throw new InvalidDataException("section name table is truncated");
        }

        var names = new byte[strtab.Size];
        ReadExactly(f, strtab.Offset, names);
        var sections = new List<ElfSection>(shnum);
        foreach (var s in raw)
        {
            var name = CString(names, (int)s.Name);
            var size = s.Type == SectionTypeNoBits ? 0 : s.Size;
            sections.Add(new ElfSection(name, s.Type, s.Offset, size));
        }

        return new ElfReader(sections, is64, be);
    }

    /// <summary>Reads a section's bytes.</summary>
    public static byte[] ReadSection(string path, ElfSection section)
    {
        using var f = File.OpenRead(path);
        if (section.Offset < 0 || section.Offset + section.Size > f.Length)
        {
            throw new InvalidDataException($"section {section.Name} is truncated");
        }

        var data = new byte[section.Size];
        ReadExactly(f, section.Offset, data);
        return data;
    }

    private static void ReadExactly(Stream f, long offset, Span<byte> buffer)
    {
        f.Seek(offset, SeekOrigin.Begin);
        f.ReadExactly(buffer);
    }

    private static string CString(byte[] b, int start)
    {
        if (start < 0 || start >= b.Length)
        {
            return string.Empty;
        }

        var end = Array.IndexOf(b, (byte)0, start);
        if (end < 0)
        {
            end = b.Length;
        }

        return Encoding.ASCII.GetString(b, start, end - start);
    }

    private static ushort U16(byte[] b, int o, bool be) => be ? BinaryPrimitives.ReadUInt16BigEndian(b.AsSpan(o)) : BinaryPrimitives.ReadUInt16LittleEndian(b.AsSpan(o));

    private static uint U32(byte[] b, int o, bool be) => be ? BinaryPrimitives.ReadUInt32BigEndian(b.AsSpan(o)) : BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(o));

    private static ulong U64(byte[] b, int o, bool be) => be ? BinaryPrimitives.ReadUInt64BigEndian(b.AsSpan(o)) : BinaryPrimitives.ReadUInt64LittleEndian(b.AsSpan(o));
}
