
public abstract class VariantBase : ICustomFormatting
{
    public abstract int Index { get; }
    public abstract int ArgCount { get; }
    public abstract object Value { get; }
    public abstract Type GetObjectType();
    public bool HasValue()
    {
        return Index > 0;
    }
    public override bool Equals(object? obj)
    {
        if (obj == null) return false;
        if (obj.GetType() != GetType()) return false;
        var other = (VariantBase)obj;
        if (!HasValue() && !other.HasValue()) return true;
        if (other.Index != Index) return false;
        return Value.Equals(other.Value);
    }
    public bool Is<T>()
    {
        return typeof(T) == GetObjectType();
    }
    public T As<T>()
    {
        if (!Is<T>())
            throw new InvalidOperationException($"Can't get Variant as {typeof(T)} because it is of type {GetObjectType()}");
        return (T)Value;
    }
    public bool TryAs<T>(out T value)
    {
        if (Is<T>())
        {
            value = As<T>();
            return true;
        }
        value = default!;
        return false;
    }
    public override string ToString()
    {
        return Value.ToString()!;
    }
    public override int GetHashCode()
    {
        int num = Value.GetHashCode();
        return (num * 397) ^ Index;
    }

    public string Format(string prefix)
    {
        if (HasValue())
            return $"{prefix}[{GetObjectType()}]\n{Formatter.Format(Value, prefix)}";
        else
            return $"{prefix}[No value]\n";
    }
}

public class Variant<T1, T2> : VariantBase
{
    private readonly int index;
    private readonly T1? t1;
    private readonly T2? t2;
    public override int Index => index;
    public override int ArgCount => 2;
    public override object Value => index switch
    {
        1 => t1!,
        2 => t2!,
        _ => throw new InvalidOperationException("Unexpected index"),
    };
    public Variant()
    {
        index = 0;
        t1 = default;
        t2 = default;
    }
    public Variant(T1 value)
    {
        index = 1;
        t1 = value;
        t2 = default;
    }
    public Variant(T2 value)
    {
        index = 2;
        t1 = default;
        t2 = value;
    }
    public static implicit operator Variant<T1, T2>(T1 value)
    {
        return new(value);
    }
    public static implicit operator Variant<T1, T2>(T2 value)
    {
        return new(value);
    }
    public override Type GetObjectType()
    {
        return index switch
        {
            1 => typeof(T1),
            2 => typeof(T2),
            _ => throw new InvalidOperationException("Unexpected index"),
        };
    }
    public T Match<T>(Func<T1, T> t1Handler, Func<T2, T> t2Handler)
    {
        return index switch
        {
            1 => t1Handler(As<T1>()),
            2 => t2Handler(As<T2>()),
            _ => throw new InvalidOperationException("Unexpected index"),
        };
    }
    public void Switch(Action<T1> t1Handler, Action<T2> t2Handler)
    {
        switch (index)
        {
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
}

public class Variant<T1, T2, T3> : VariantBase
{
    private readonly int index;
    private readonly T1? t1;
    private readonly T2? t2;
    private readonly T3? t3;
    public override int Index => index;
    public override int ArgCount => 3;
    public override object Value => index switch
    {
        1 => t1!,
        2 => t2!,
        3 => t3!,
        _ => throw new InvalidOperationException("Unexpected index"),
    };
    public Variant()
    {
        index = 0;
        t1 = default;
        t2 = default;
        t3 = default;
    }
    public Variant(T1 value)
    {
        index = 1;
        t1 = value;
        t2 = default;
        t3 = default;
    }
    public Variant(T2 value)
    {
        index = 2;
        t1 = default;
        t2 = value;
        t3 = default;
    }
    public Variant(T3 value)
    {
        index = 3;
        t1 = default;
        t2 = default;
        t3 = value;
    }
    public static implicit operator Variant<T1, T2, T3>(T1 value)
    {
        return new(value);
    }
    public static implicit operator Variant<T1, T2, T3>(T2 value)
    {
        return new(value);
    }
    public static implicit operator Variant<T1, T2, T3>(T3 value)
    {
        return new(value);
    }
    public override Type GetObjectType()
    {
        return index switch
        {
            1 => typeof(T1),
            2 => typeof(T2),
            3 => typeof(T3),
            _ => throw new InvalidOperationException("Unexpected index"),
        };
    }
    public T Match<T>(Func<T1, T> t1Handler, Func<T2, T> t2Handler, Func<T3, T> t3Handler)
    {
        return index switch
        {
            1 => t1Handler(As<T1>()),
            2 => t2Handler(As<T2>()),
            3 => t3Handler(As<T3>()),
            _ => throw new InvalidOperationException("Unexpected index"),
        };
    }
    public void Switch(Action<T1> t1Handler, Action<T2> t2Handler, Action<T3> t3Handler)
    {
        switch (index)
        {
            case 1:
                t1Handler(As<T1>());
                return;
            case 2:
                t2Handler(As<T2>());
                return;
            case 3:
                t3Handler(As<T3>());
                return;
            default:
                throw new InvalidOperationException();
        }
    }
}
