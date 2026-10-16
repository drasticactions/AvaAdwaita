using System;
using System.Collections.Generic;
using System.Globalization;
using AvaAdwaita.CssEngine.Syntax;

namespace AvaAdwaita.CssEngine.Values;

/// <summary>
/// A specified numeric value: a dimension, or a <c>calc()</c>, <c>min()</c>, <c>max()</c> or
/// <c>clamp()</c> tree. In a relative color, channel keywords are leaves too.
/// </summary>
/// <remarks>Port of the computing half of gtkcssnumbervalue.c and gtkcssdimensionvalue.c.</remarks>
internal sealed class CssNumeric : IEquatable<CssNumeric>
{
    private readonly CssCalcOp _op;
    private readonly double _value;
    private readonly CssUnit _unit;
    private readonly CssNumeric[]? _args;

    private CssNumeric(CssCalcOp op, double value, CssUnit unit, CssNumeric[]? args)
    {
        _op = op;
        _value = value;
        _unit = unit;
        _args = args;
    }

    public static CssNumeric Dimension(double value, CssUnit unit) => new(CssCalcOp.Leaf, value, unit, null);

    public static CssNumeric Channel(int index) => new(CssCalcOp.Channel, index, CssUnit.Number, null);

    /// <summary>True for a plain dimension, not a calc tree.</summary>
    public bool IsSimple => _op == CssCalcOp.Leaf;

    public double SimpleValue => _value;

    public CssUnit SimpleUnit => _unit;

    /// <summary>The node kind, for printing the tree.</summary>
    public CssCalcOp Op => _op;

    /// <summary>The operands of a calc node; empty for a leaf or channel.</summary>
    public ReadOnlySpan<CssNumeric> Args => _args;

    /// <summary>True when computing needs the font size.</summary>
    public bool UsesFontSize => _op == CssCalcOp.Leaf ? _unit is CssUnit.Em or CssUnit.Ex : _args is { } a && Array.Exists(a, x => x.UsesFontSize);

    public bool HasPercent => _op == CssCalcOp.Leaf ? _unit == CssUnit.Percent : _args is { } a && Array.Exists(a, x => x.HasPercent);

    public static CssDimensionClass ClassOf(CssUnit unit) => unit switch
    {
        CssUnit.Number => CssDimensionClass.Number,
        CssUnit.Percent => CssDimensionClass.Percent,
        CssUnit.Deg or CssUnit.Rad or CssUnit.Grad or CssUnit.Turn => CssDimensionClass.Angle,
        CssUnit.S or CssUnit.Ms => CssDimensionClass.Time,
        _ => CssDimensionClass.Length,
    };

    /// <summary>Computes the value. Lengths become px, angles deg, times ms.</summary>
    public CssNumber Compute(in CssNumericContext ctx)
    {
        switch (_op)
        {
            case CssCalcOp.Leaf:
                return ComputeLeaf(_value, _unit, ctx);
            case CssCalcOp.Channel:
                {
                    var ch = ctx.Channels;
                    return CssNumber.Plain(ch is null ? 0 : ch[(int)_value]);
                }

            case CssCalcOp.Negate:
                return _args![0].Compute(ctx) * -1;
            case CssCalcOp.Invert:
                {
                    var v = _args![0].Compute(ctx);
                    return CssNumber.Plain(v.Value == 0 ? double.PositiveInfinity : 1.0 / v.Value);
                }

            case CssCalcOp.Sum:
                {
                    var acc = _args![0].Compute(ctx);
                    for (var i = 1; i < _args.Length; i++)
                    {
                        acc += _args[i].Compute(ctx);
                    }

                    return acc;
                }

            case CssCalcOp.Product:
                {
                    var acc = _args![0].Compute(ctx);
                    for (var i = 1; i < _args.Length; i++)
                    {
                        var v = _args[i].Compute(ctx);
                        if (v.Class == CssDimensionClass.Number)
                        {
                            acc *= v.Value;
                        }
                        else if (acc.Class == CssDimensionClass.Number)
                        {
                            acc = v * acc.Value;
                        }
                        else if (acc.Class == CssDimensionClass.Percent && v.Class == CssDimensionClass.Percent)
                        {
                            acc = CssNumber.Pct(acc.Percent * v.Percent / 100);
                        }
                        else
                        {
                            acc = new CssNumber(acc.Value * v.Value, acc.Percent * v.Value, acc.Class);
                        }
                    }

                    return acc;
                }

            case CssCalcOp.Min:
            case CssCalcOp.Max:
                {
                    var best = _args![0].Compute(ctx);
                    for (var i = 1; i < _args.Length; i++)
                    {
                        var v = _args[i].Compute(ctx);
                        var better = _op == CssCalcOp.Min ? Key(v) < Key(best) : Key(v) > Key(best);
                        if (better)
                        {
                            best = v;
                        }
                    }

                    return best;
                }

            case CssCalcOp.Clamp:
                {
                    var lo = _args![0].Compute(ctx);
                    var v = _args[1].Compute(ctx);
                    var hi = _args[2].Compute(ctx);
                    if (Key(v) > Key(hi))
                    {
                        v = hi;
                    }

                    if (Key(v) < Key(lo))
                    {
                        v = lo;
                    }

                    return v;
                }

            default:
                throw new InvalidOperationException();
        }
    }

    // min() and max() over a percentage compare against a 100px basis; gtk.css never mixes them.
    private static double Key(CssNumber n) => n.Value + n.Percent;

    private static CssNumber ComputeLeaf(double v, CssUnit unit, in CssNumericContext ctx) => unit switch
    {
        CssUnit.Number => CssNumber.Plain(v),
        CssUnit.Percent => CssNumber.Pct(v),
        CssUnit.Px => CssNumber.Px(v),
        CssUnit.Pt => CssNumber.Px(v * ctx.Dpi / 72.0),
        CssUnit.Pc => CssNumber.Px(v * ctx.Dpi / 72.0 * 12.0),
        CssUnit.In => CssNumber.Px(v * ctx.Dpi),
        CssUnit.Cm => CssNumber.Px(v * ctx.Dpi * 0.39370078740157477),
        CssUnit.Mm => CssNumber.Px(v * ctx.Dpi * 0.039370078740157477),
        CssUnit.Em => CssNumber.Px(v * ctx.EmPx),
        CssUnit.Ex => CssNumber.Px(v * 0.5 * ctx.EmPx),
        CssUnit.Rem => CssNumber.Px(v * ctx.RemPx),
        CssUnit.Deg => new CssNumber(v, 0, CssDimensionClass.Angle),
        CssUnit.Rad => new CssNumber(v * 360.0 / (2 * Math.PI), 0, CssDimensionClass.Angle),
        CssUnit.Grad => new CssNumber(v * 360.0 / 400.0, 0, CssDimensionClass.Angle),
        CssUnit.Turn => new CssNumber(v * 360.0, 0, CssDimensionClass.Angle),
        CssUnit.S => new CssNumber(v * 1000.0, 0, CssDimensionClass.Time),
        CssUnit.Ms => new CssNumber(v, 0, CssDimensionClass.Time),
        _ => CssNumber.Plain(v),
    };

    /// <summary>Parses one numeric value of the allowed kinds.</summary>
    /// <param name="r">The reader.</param>
    /// <param name="flags">The kinds allowed.</param>
    /// <param name="channels">The channel keywords of a relative color, or null.</param>
    public static CssNumeric? Parse(CssReader r, CssNumberFlags flags, string[]? channels = null)
    {
        var c = r.Peek();
        if (c is null)
        {
            r.Fail("expected a number");
            return null;
        }

        CssNumeric? result;
        if (c.IsFunction && IsMathFunction(c.Token.Text))
        {
            r.Next();
            result = ParseMathFunction(c, r, channels);
            if (result is null)
            {
                return null;
            }

            if (!CheckClass(result.Classify(), flags))
            {
                r.Fail("calc() has the wrong type");
                return null;
            }

            return result;
        }

        if (c.Type == CssTokenType.Ident && channels is not null)
        {
            var idx = Array.FindIndex(channels, n => string.Equals(n, c.Token.Text, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0 && (flags & CssNumberFlags.Number) != 0)
            {
                r.Next();
                return Channel(idx);
            }
        }

        result = ParseToken(c.Token, flags, r);
        if (result is not null)
        {
            r.Next();
        }

        return result;
    }

    public static bool CanParse(CssReader r)
    {
        var c = r.Peek();
        return c is not null && (c.Token.IsNumeric || (c.IsFunction && IsMathFunction(c.Token.Text)));
    }

    private static bool IsMathFunction(string name) =>
        name.Equals("calc", StringComparison.OrdinalIgnoreCase) || name.Equals("min", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("max", StringComparison.OrdinalIgnoreCase) || name.Equals("clamp", StringComparison.OrdinalIgnoreCase);

    private static bool CheckClass(CssDimensionClass? cls, CssNumberFlags flags)
    {
        if (cls is null)
        {
            return false;
        }

        return cls.Value switch
        {
            CssDimensionClass.Number => (flags & CssNumberFlags.Number) != 0,
            CssDimensionClass.Percent => (flags & CssNumberFlags.Percent) != 0,
            CssDimensionClass.Length => (flags & CssNumberFlags.Length) != 0,
            CssDimensionClass.Angle => (flags & CssNumberFlags.Angle) != 0,
            CssDimensionClass.Time => (flags & CssNumberFlags.Time) != 0,
            _ => false,
        };
    }

    /// <summary>The dimension class of the tree, or null if it mixes incompatible types.</summary>
    private CssDimensionClass? Classify()
    {
        switch (_op)
        {
            case CssCalcOp.Leaf:
                return ClassOf(_unit);
            case CssCalcOp.Channel:
            case CssCalcOp.Invert:
                return CssDimensionClass.Number;
            case CssCalcOp.Negate:
                return _args![0].Classify();
            case CssCalcOp.Product:
                {
                    CssDimensionClass? acc = CssDimensionClass.Number;
                    foreach (var a in _args!)
                    {
                        var c = a.Classify();
                        if (c is null)
                        {
                            return null;
                        }

                        if (c == CssDimensionClass.Number)
                        {
                            continue;
                        }

                        if (acc == CssDimensionClass.Number)
                        {
                            acc = c;
                        }
                        else
                        {
                            return null;
                        }
                    }

                    return acc;
                }

            default:
                {
                    CssDimensionClass? acc = null;
                    foreach (var a in _args!)
                    {
                        var c = a.Classify();
                        if (c is null)
                        {
                            return null;
                        }

                        if (acc is null || acc == c)
                        {
                            acc = c;
                        }
                        else if (acc == CssDimensionClass.Percent && c is CssDimensionClass.Length or CssDimensionClass.Angle)
                        {
                            acc = c;
                        }
                        else if (c == CssDimensionClass.Percent && acc is CssDimensionClass.Length or CssDimensionClass.Angle)
                        {
                        }
                        else
                        {
                            return null;
                        }
                    }

                    return acc;
                }
        }
    }

    private static CssNumeric? ParseToken(CssToken t, CssNumberFlags flags, CssReader r)
    {
        switch (t.Type)
        {
            case CssTokenType.Number:
                if ((flags & CssNumberFlags.Number) == 0)
                {
                    // GTK accepts a unitless 0 wherever a length is allowed.
                    if (t.Number == 0 && (flags & CssNumberFlags.Length) != 0)
                    {
                        return Dimension(0, CssUnit.Px);
                    }

                    r.Fail("unexpected number");
                    return null;
                }

                if ((flags & CssNumberFlags.PositiveOnly) != 0 && t.Number < 0)
                {
                    r.Fail("negative values are not allowed");
                    return null;
                }

                return Dimension(t.Number, CssUnit.Number);
            case CssTokenType.Percentage:
                if ((flags & CssNumberFlags.Percent) == 0)
                {
                    r.Fail("percentages are not allowed here");
                    return null;
                }

                if ((flags & CssNumberFlags.PositiveOnly) != 0 && t.Number < 0)
                {
                    r.Fail("negative values are not allowed");
                    return null;
                }

                return Dimension(t.Number, CssUnit.Percent);
            case CssTokenType.Dimension:
                {
                    var unit = ParseUnit(t.Text);
                    if (unit is null)
                    {
                        r.Fail($"'{t.Text}' is not a valid unit");
                        return null;
                    }

                    if (!CheckClass(ClassOf(unit.Value), flags))
                    {
                        r.Fail($"unit '{t.Text}' is not allowed here");
                        return null;
                    }

                    if ((flags & CssNumberFlags.PositiveOnly) != 0 && t.Number < 0)
                    {
                        r.Fail("negative values are not allowed");
                        return null;
                    }

                    return Dimension(t.Number, unit.Value);
                }

            default:
                r.Fail("expected a number");
                return null;
        }
    }

    public static CssUnit? ParseUnit(string unit) => unit.ToLowerInvariant() switch
    {
        "px" => CssUnit.Px,
        "pt" => CssUnit.Pt,
        "pc" => CssUnit.Pc,
        "in" => CssUnit.In,
        "cm" => CssUnit.Cm,
        "mm" => CssUnit.Mm,
        "em" => CssUnit.Em,
        "ex" => CssUnit.Ex,
        "rem" => CssUnit.Rem,
        "deg" => CssUnit.Deg,
        "rad" => CssUnit.Rad,
        "grad" => CssUnit.Grad,
        "turn" => CssUnit.Turn,
        "s" => CssUnit.S,
        "ms" => CssUnit.Ms,
        _ => null,
    };

    private static CssNumeric? ParseMathFunction(CssComponent fn, CssReader outer, string[]? channels)
    {
        var name = fn.Token.Text.ToLowerInvariant();
        if (name == "calc")
        {
            var inner = new CssReader(fn.Children);
            var v = ParseSum(inner, channels);
            if (v is null || !inner.AtEnd)
            {
                outer.Absorb(inner);
                outer.Fail("bad calc()");
                return null;
            }

            return v;
        }

        var parts = CssReader.SplitCommas(fn.Children);
        var args = new List<CssNumeric>();
        foreach (var p in parts)
        {
            var inner = new CssReader(p);
            var v = ParseSum(inner, channels);
            if (v is null || !inner.AtEnd)
            {
                outer.Absorb(inner);
                outer.Fail($"bad {name}()");
                return null;
            }

            args.Add(v);
        }

        if (name == "clamp")
        {
            if (args.Count != 3)
            {
                outer.Fail("clamp() needs 3 arguments");
                return null;
            }

            return new CssNumeric(CssCalcOp.Clamp, 0, CssUnit.Number, args.ToArray());
        }

        if (args.Count == 0)
        {
            outer.Fail($"{name}() needs arguments");
            return null;
        }

        return args.Count == 1 ? args[0] : new CssNumeric(name == "min" ? CssCalcOp.Min : CssCalcOp.Max, 0, CssUnit.Number, args.ToArray());
    }

    private static CssNumeric? ParseSum(CssReader r, string[]? channels)
    {
        var first = ParseProduct(r, channels);
        if (first is null)
        {
            return null;
        }

        List<CssNumeric>? terms = null;
        while (!r.AtEnd)
        {
            var c = r.Peek()!;
            bool negate;
            if (c.IsDelim('+'))
            {
                negate = false;
            }
            else if (c.IsDelim('-'))
            {
                negate = true;
            }
            else if (c.Token.IsNumeric && c.Token.HasSign && r.PrecededByWhitespace)
            {
                // "a -5px" tokenizes the sign into the number; GTK still reads it as a subtraction.
                terms ??= new List<CssNumeric> { first };
                var signed = ParseProduct(r, channels);
                if (signed is null)
                {
                    return null;
                }

                terms.Add(signed);
                continue;
            }
            else
            {
                break;
            }

            r.Next();
            var next = ParseProduct(r, channels);
            if (next is null)
            {
                return null;
            }

            terms ??= new List<CssNumeric> { first };
            terms.Add(negate ? new CssNumeric(CssCalcOp.Negate, 0, CssUnit.Number, [next]) : next);
        }

        return terms is null ? first : new CssNumeric(CssCalcOp.Sum, 0, CssUnit.Number, terms.ToArray());
    }

    private static CssNumeric? ParseProduct(CssReader r, string[]? channels)
    {
        var first = ParseCalcValue(r, channels);
        if (first is null)
        {
            return null;
        }

        List<CssNumeric>? factors = null;
        while (!r.AtEnd)
        {
            var c = r.Peek()!;
            bool divide;
            if (c.IsDelim('*'))
            {
                divide = false;
            }
            else if (c.IsDelim('/'))
            {
                divide = true;
            }
            else
            {
                break;
            }

            r.Next();
            var next = ParseCalcValue(r, channels);
            if (next is null)
            {
                return null;
            }

            factors ??= new List<CssNumeric> { first };
            factors.Add(divide ? new CssNumeric(CssCalcOp.Invert, 0, CssUnit.Number, [next]) : next);
        }

        return factors is null ? first : new CssNumeric(CssCalcOp.Product, 0, CssUnit.Number, factors.ToArray());
    }

    private static CssNumeric? ParseCalcValue(CssReader r, string[]? channels)
    {
        var c = r.Peek();
        if (c is null)
        {
            r.Fail("unexpected end of calc()");
            return null;
        }

        if (c.Type == CssTokenType.OpenParen)
        {
            r.Next();
            var inner = new CssReader(c.Children);
            var v = ParseSum(inner, channels);
            if (v is null || !inner.AtEnd)
            {
                r.Absorb(inner);
                return null;
            }

            return v;
        }

        if (c.Type == CssTokenType.Ident)
        {
            if (c.IsIdent("pi"))
            {
                r.Next();
                return Dimension(Math.PI, CssUnit.Number);
            }

            if (c.IsIdent("e"))
            {
                r.Next();
                return Dimension(Math.E, CssUnit.Number);
            }
        }

        return Parse(r, CssNumberFlags.Number | CssNumberFlags.Percent | CssNumberFlags.Length | CssNumberFlags.Angle | CssNumberFlags.Time, channels);
    }

    public bool Equals(CssNumeric? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (_op != other._op || _value != other._value || _unit != other._unit)
        {
            return false;
        }

        if (_args is null || other._args is null)
        {
            return _args is null && other._args is null;
        }

        if (_args.Length != other._args.Length)
        {
            return false;
        }

        for (var i = 0; i < _args.Length; i++)
        {
            if (!_args[i].Equals(other._args[i]))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is CssNumeric n && Equals(n);

    public override int GetHashCode() => HashCode.Combine(_op, _value, _unit, _args?.Length ?? 0);

    public override string ToString() => _op switch
    {
        CssCalcOp.Leaf => _value.ToString(CultureInfo.InvariantCulture) + _unit switch
        {
            CssUnit.Number => string.Empty,
            CssUnit.Percent => "%",
            _ => _unit.ToString().ToLowerInvariant(),
        },
        CssCalcOp.Channel => "channel" + _value.ToString(CultureInfo.InvariantCulture),
        _ => _op.ToString().ToLowerInvariant() + "(" + string.Join(", ", (IEnumerable<CssNumeric>)_args!) + ")",
    };
}
