namespace AvaAdwaita.Sources.Elf;

/// <summary>One ELF section header: its name and where its bytes are in the file.</summary>
internal sealed record ElfSection(string Name, uint Type, long Offset, long Size);
