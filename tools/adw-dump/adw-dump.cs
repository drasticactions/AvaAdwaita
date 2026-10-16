#:project ../../src/AvaAdwaita.Theme/AvaAdwaita.Theme.csproj
// Prints the computed style of a CSS node path, with the rules that match its last node.
//
//   dotnet run tools/adw-dump/adw-dump.cs -- "window.csd > headerbar > button.flat:hover" [OPTIONS]
//
//   --dark | --light          color scheme (default: the desktop's)
//   --hc                      high contrast
//   --accent NAME             blue, teal, green, yellow, orange, red, pink, purple or slate
//   --css installed|bundled   the stylesheet (default: installed when it parses, else bundled)
//   --all                     print every property, not only those that differ from the initial values
using System;
using System.Collections.Generic;
using System.Linq;
using AvaAdwaita;
using AvaAdwaita.CssEngine.Cascade;
using AvaAdwaita.CssEngine.Selectors;
using AvaAdwaita.Nodes;

var path = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
if (path is null)
{
    Console.Error.WriteLine("usage: adw-dump \"window.csd > button.flat:hover\" [--dark|--light] [--hc] [--accent NAME] [--css installed|bundled] [--all]");
    return 2;
}

string? Option(string name)
{
    var i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

var options = new AvaAdwaitaOptions
{
    CssSource = Option("--css") switch
    {
        "installed" => AdwCssSource.Installed,
        "bundled" => AdwCssSource.Bundled,
        _ => AdwCssSource.Auto,
    },
};
AdwaitaEnvironment.Preload(options);
AdwaitaEnvironment.EnsureInitialized();
var state = AdwaitaEnvironment.State;
if (args.Contains("--dark"))
{
    state.ColorScheme = AdwColorScheme.ForceDark;
}
else if (args.Contains("--light"))
{
    state.ColorScheme = AdwColorScheme.ForceLight;
}

if (args.Contains("--hc"))
{
    state.HighContrastOverride = AdwHighContrast.On;
}

if (Option("--accent") is { } accent)
{
    state.AccentOverride = Enum.Parse<AdwAccentColor>(accent, ignoreCase: true);
}

var engine = AdwaitaEnvironment.Engine;
CssNode? root = null;
CssNode? last = null;
foreach (var part in path.Split('>', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
{
    var node = Parse(part);
    node.MediaOverride = state.Media;
    if (last is null)
    {
        root = node;
    }
    else
    {
        last.InsertChild(last.Children.Count, node);
    }

    last = node;
}

engine.AddRoot(root!);
var style = engine.Resolve(last!);
Console.WriteLine($"# {AdwaitaEnvironment.Source.Description}; settings: {state.SourceName}; {(state.Dark ? "dark" : "light")}{(state.HighContrast ? ", high contrast" : string.Empty)}, accent {state.Accent}");
Console.WriteLine($"# {last!.Path}");
Console.WriteLine();
Console.WriteLine("/* rules, in cascade order */");
foreach (var rule in engine.DescribeMatches(last))
{
    Console.WriteLine("  " + rule);
}

Console.WriteLine();
Console.WriteLine("/* computed */");
CssNode initialNode = new("initial");
var initialEngine = new CssStyleEngine(new CssStyleSet());
initialEngine.AddRoot(initialNode);
Console.Write(style.Describe(args.Contains("--all") ? null : initialEngine.Resolve(initialNode)));
return 0;

static CssNode Parse(string compound)
{
    // name#id.class:state(...)
    var states = CssStates.None;
    var parts = compound.Split(':');
    foreach (var s in parts.Skip(1))
    {
        var name = s switch
        {
            "dir(rtl)" => "DirRtl",
            "dir(ltr)" => "DirLtr",
            _ => string.Concat(s.Split('-').Select(w => char.ToUpperInvariant(w[0]) + w[1..])),
        };
        if (!Enum.TryParse<CssStates>(name, out var flag))
        {
            throw new ArgumentException($"unknown state :{s}");
        }

        states |= flag;
    }

    if ((states & (CssStates.DirLtr | CssStates.DirRtl)) == 0)
    {
        states |= CssStates.DirLtr;
    }

    var head = parts[0];
    string? id = null;
    var hash = head.IndexOf('#');
    var classes = new List<string>();
    var segments = head.Split('.');
    var name0 = segments[0];
    if (hash >= 0)
    {
        var idPart = head[(hash + 1)..].Split('.')[0];
        id = idPart;
        name0 = name0.Split('#')[0];
    }

    classes.AddRange(segments.Skip(1));
    var node = new CssNode(name0.Length == 0 ? null : name0) { Id = id, States = states };
    node.SetClasses(classes);
    return node;
}
