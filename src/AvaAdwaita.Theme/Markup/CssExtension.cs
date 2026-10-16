using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;
using Avalonia.Data.Core.Plugins;
using AvaAdwaita.CssEngine.Computed;

namespace AvaAdwaita.Markup;

/// <summary>
/// <c>{adw:Css Color}</c> binds to a computed value of the nearest CSS node at or above the target,
/// as <c>Color</c>, <c>Font.Weight</c>, <c>Padding</c> or <c>BorderRadius</c>.
/// </summary>
public sealed class CssExtension : MarkupExtension
{
    /// <summary>Creates the extension.</summary>
    public CssExtension()
    {
    }

    /// <summary>Creates the extension for a path.</summary>
    public CssExtension(string path)
    {
        Path = path;
    }

    /// <summary>A computed-style path, flat (<c>FontWeight</c>) or grouped (<c>Font.Weight</c>).</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>The value used before the node has a style.</summary>
    public object? Fallback { get; set; }

    /// <summary>The name of another element in the template whose node to read, instead of the nearest one.</summary>
    public string? Node { get; set; }

    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var scope = Node is null ? null : serviceProvider.GetService(typeof(INameScope)) as INameScope;
        return Create(Path, Fallback, Node, scope);
    }

    /// <summary>Builds the binding.</summary>
    public static CompiledBinding Create(string path, object? fallback = null, string? node = null, INameScope? scope = null)
    {
        var p = CssPaths.Get(path) ?? throw new ArgumentException($"'{path}' is not a computed style path", nameof(path));
        var builder = new CompiledBindingPathBuilder();
        builder = node is not null && scope is not null ? builder.ElementName(scope, node) : builder.Self();
        var bindingPath = builder
            .Property(Nodes.Css.StyleProperty, PropertyInfoAccessorFactory.CreateAvaloniaPropertyAccessor)
            .Build();
        return new CompiledBinding(bindingPath)
        {
            Mode = BindingMode.OneWay,
            Converter = new PathConverter(p),
            FallbackValue = fallback ?? AvaloniaProperty.UnsetValue,
            TargetNullValue = fallback ?? AvaloniaProperty.UnsetValue,
        };
    }

    private sealed class PathConverter(CssPath path) : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not ComputedStyle s)
            {
                return AvaloniaProperty.UnsetValue;
            }

            var target = Nullable.GetUnderlyingType(targetType) ?? targetType;
            return CssConvert.To(path.Get(s), target) ?? AvaloniaProperty.UnsetValue;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => BindingOperations.DoNothing;
    }
}
