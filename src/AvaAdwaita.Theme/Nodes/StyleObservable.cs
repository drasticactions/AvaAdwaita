using System;
using System.Collections.Generic;
using Avalonia.Data;

namespace AvaAdwaita.Nodes;

/// <summary>A minimal observable that publishes the latest value, for binding a property at style priority.</summary>
/// <typeparam name="T">The value type.</typeparam>
internal sealed class StyleObservable<T> : IObservable<BindingValue<T>>
{
    private readonly List<IObserver<BindingValue<T>>> _observers = new();
    private BindingValue<T> _value = BindingValue<T>.Unset;
    private bool _hasValue;

    public void Push(T value)
    {
        if (_hasValue && EqualityComparer<T>.Default.Equals(_value.Value, value))
        {
            return;
        }

        _value = value;
        _hasValue = true;
        foreach (var o in _observers.ToArray())
        {
            o.OnNext(_value);
        }
    }

    public IDisposable Subscribe(IObserver<BindingValue<T>> observer)
    {
        _observers.Add(observer);
        if (_hasValue)
        {
            observer.OnNext(_value);
        }

        return new Unsubscriber(this, observer);
    }

    private sealed class Unsubscriber(StyleObservable<T> owner, IObserver<BindingValue<T>> observer) : IDisposable
    {
        public void Dispose() => owner._observers.Remove(observer);
    }
}
