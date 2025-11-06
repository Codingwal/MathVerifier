public interface IVariant
{
    public object Value { get; }
    public bool Is<T>();
    public Type GetObjectType();
    public T As<T>();
    public bool Equals(object other);
    public string ToString();
    public int GetHashCode();
}

public readonly struct Variant<T0, T1> : IVariant
{
    private readonly int index;
    private readonly T0? t0;
    private readonly T1? t1;
    public readonly object Value => index switch
    {
        0 => t0!,
        1 => t1!,
        _ => throw new InvalidOperationException("Unexpected index"),
    };
    public readonly int Index => index;
    public Variant(T0 value)
    {
        t0 = value;
        index = 0;
    }
    public Variant(T1 value)
    {
        t1 = value;
        index = 1;
    }
    public static implicit operator Variant<T0, T1>(T0 value)
    {
        return new(value);
    }
    public static implicit operator Variant<T0, T1>(T1 value)
    {
        return new(value);
    }
    public readonly bool Is<T>()
    {
        return typeof(T) == GetObjectType();
    }
    public readonly Type GetObjectType()
    {
        return index switch
        {
            0 => typeof(T0),
            1 => typeof(T1),
            _ => throw new InvalidOperationException("Unexpected index"),
        };
    }
    public readonly T As<T>()
    {
        if (!Is<T>())
            throw new InvalidOperationException($"Can't get Variant as {typeof(T)} because it is of type {GetObjectType()}");
        return (T)Value;
    }
    public readonly bool TryAs<T>(out T value)
    {
        if (Is<T>())
        {
            value = As<T>();
            return true;
        }
        value = default!;
        return false;
    }
    public readonly T Match<T>(Func<T0, T> t0Handler, Func<T1, T> t1Handler)
    {
        return index switch
        {
            0 => t0Handler(As<T0>()),
            1 => t1Handler(As<T1>()),
            _ => throw new InvalidOperationException("Unexpected index"),
        };
    }
    public readonly void Switch(Action<T0> t0Handler, Action<T1> t1Handler)
    {
        switch (index)
        {
            case 0:
                t0Handler(As<T0>());
                return;
            case 1:
                t1Handler(As<T1>());
                return;
            default:
                throw new InvalidOperationException();
        }
    }
    public bool Equals(Variant<T0, T1> other)
    {
        if (index != other.index) return false;
        return Value.Equals(other.Value);
    }
    public override bool Equals(object? obj)
    {
        if (obj == null)
            return false;
        if (obj is Variant<T0, T1> other)
            return Equals(other);
        return false;
    }
    public override string ToString()
    {
        return Value.ToString()!;
    }
    public override int GetHashCode()
    {
        int num = Value.GetHashCode();
        return (num * 397) ^ index;
    }
}

public readonly struct Variant<T0, T1, T2> : IVariant
{
    private readonly int index;
    private readonly T0? t0;
    private readonly T1? t1;
    private readonly T2? t2;
    public readonly object Value => index switch
    {
        0 => t0!,
        1 => t1!,
        2 => t2!,
        _ => throw new InvalidOperationException("Unexpected index"),
    };
    public readonly int Index => index;
    public Variant()
    {
        index = -1;
        t0 = default;
        t1 = default;
        t2 = default;
    }
    public Variant(T0 value)
    {
        index = 0;
        t0 = value;
        t1 = default;
        t2 = default;
    }
    public Variant(T1 value)
    {
        index = 1;
        t0 = default;
        t1 = value;
        t2 = default;
    }
    public Variant(T2 value)
    {
        index = 2;
        t0 = default;
        t1 = default;
        t2 = value;
    }
    public static implicit operator Variant<T0, T1, T2>(T0 value)
    {
        return new(value);
    }
    public static implicit operator Variant<T0, T1, T2>(T1 value)
    {
        return new(value);
    }
    public static implicit operator Variant<T0, T1, T2>(T2 value)
    {
        return new(value);
    }
    public readonly bool Is<T>()
    {
        return typeof(T) == GetObjectType();
    }
    public readonly Type GetObjectType()
    {
        return index switch
        {
            0 => typeof(T0),
            1 => typeof(T1),
            2 => typeof(T2),
            _ => throw new InvalidOperationException("Unexpected index"),
        };
    }
    public readonly T As<T>()
    {
        if (!Is<T>())
            throw new InvalidOperationException($"Can't get Variant as {typeof(T)} because it is of type {GetObjectType()}");
        return (T)Value;
    }
    public readonly bool TryAs<T>(out T value)
    {
        if (Is<T>())
        {
            value = As<T>();
            return true;
        }
        value = default!;
        return false;
    }
    public readonly T Match<T>(Func<T0, T> t0Handler, Func<T1, T> t1Handler, Func<T2, T> t2Handler)
    {
        return index switch
        {
            0 => t0Handler(As<T0>()),
            1 => t1Handler(As<T1>()),
            2 => t2Handler(As<T2>()),
            _ => throw new InvalidOperationException("Unexpected index"),
        };
    }
    public readonly void Switch(Action<T0> t0Handler, Action<T1> t1Handler, Action<T2> t2Handler)
    {
        switch (index)
        {
            case 0:
                t0Handler(As<T0>());
                return;
            case 1:
                t1Handler(As<T1>());
                return;
            case 2:
                t2Handler(As<T2>());
                return;
            default:
                throw new InvalidOperationException();
        }
    }
    public bool Equals(Variant<T0, T1, T2> other)
    {
        if (index != other.index) return false;
        return Value.Equals(other.Value);
    }
    public override bool Equals(object? obj)
    {
        if (obj == null)
            return false;
        if (obj is Variant<T0, T1, T2> other)
            return Equals(other);
        return false;
    }
    public override string ToString()
    {
        return Value.ToString()!;
    }
    public override int GetHashCode()
    {
        int num = Value.GetHashCode();
        return (num * 397) ^ index;
    }
}
