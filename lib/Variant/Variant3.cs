public class Variant<T1, T2, T3> : VariantBase
{
    public override int ArgCount => 3;
    public override object Value => index switch
    {
        1 => (T1)value!,
        2 => (T2)value!,
        3 => (T3)value!,
        _ => throw new InvalidOperationException("Unexpected index"),
    };
    public Variant() : base(-1, null) { }
    public Variant(T1 value) : base(1, value) { }
    public Variant(T2 value) : base(2, value) { }
    public Variant(T3 value) : base(3, value) { }
    public static implicit operator Variant<T1, T2, T3>(T1 value) { return new(value); }
    public static implicit operator Variant<T1, T2, T3>(T2 value) { return new(value); }
    public static implicit operator Variant<T1, T2, T3>(T3 value) { return new(value); }
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
        if (index == 1) t1Handler(As<T1>());
        else if (index == 2) t2Handler(As<T2>());
        else if (index == 3) t3Handler(As<T3>());
        else throw new InvalidOperationException("Unexpected index");
    }
}
