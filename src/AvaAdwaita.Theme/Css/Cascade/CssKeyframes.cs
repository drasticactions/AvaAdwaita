using System.Collections.Generic;

namespace AvaAdwaita.CssEngine.Cascade;

/// <summary>A <c>@keyframes</c> rule: offsets from 0 to 1 and their declarations.</summary>
internal sealed record CssKeyframes(string Name, List<(double Offset, CssDeclaration[] Declarations)> Frames, CssMediaQuery? Media);
