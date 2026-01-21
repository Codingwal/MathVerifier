public class Variant<T1, T2, T3, T4, T5, T6, T7, T8> : VariantBase
{
    public override int ArgCount => 8;
    public override object Value => index switch
    {
        1 => (T1)value!,
        2 => (T2)value!,
        3 => (T3)value!,
        4 => (T4)value!,
        5 => (T5)value!,
        6 => (T6)value!,
        7 => (T7)value!,
        8 => (T8)value!,
        _ => throw new InvalidOperationException("Unexpected index"),
    };
    public Variant() : base(-1, null) { }
    public Variant(T1 value) : base(1, value) { }
    public Variant(T2 value) : base(2, value) { }
    public Variant(T3 value) : base(3, value) { }
    public Variant(T4 value) : base(4, value) { }
    public Variant(T5 value) : base(5, value) { }
    public Variant(T6 value) : base(6, value) { }
    public Variant(T7 value) : base(7, value) { }
    public Variant(T8 value) : base(8, value) { }
    public static implicit operator Variant<T1, T2, T3, T4, T5, T6, T7, T8>(T1 value) { return new(value); }
    public static implicit operator Variant<T1, T2, T3, T4, T5, T6, T7, T8>(T2 value) { return new(value); }
    public static implicit operator Variant<T1, T2, T3, T4, T5, T6, T7, T8>(T3 value) { return new(value); }
    public static implicit operator Variant<T1, T2, T3, T4, T5, T6, T7, T8>(T4 value) { return new(value); }
    public static implicit operator Variant<T1, T2, T3, T4, T5, T6, T7, T8>(T5 value) { return new(value); }
    public static implicit operator Variant<T1, T2, T3, T4, T5, T6, T7, T8>(T6 value) { return new(value); }
    public static implicit operator Variant<T1, T2, T3, T4, T5, T6, T7, T8>(T7 value) { return new(value); }
    public static implicit operator Variant<T1, T2, T3, T4, T5, T6, T7, T8>(T8 value) { return new(value); }
    public override Type GetObjectType()
    {
        return index switch
        {
            1 => typeof(T1),
            2 => typeof(T2),
            3 => typeof(T3),
            4 => typeof(T4),
            5 => typeof(T5),
            6 => typeof(T6),
            7 => typeof(T7),
            8 => typeof(T8),
            _ => throw new InvalidOperationException("Unexpected index"),
        };
    }
    public T Match<T>(Func<T1, T> t1Handler, Func<T2, T> t2Handler, Func<T3, T> t3Handler, Func<T4, T> t4Handler,
        Func<T5, T> t5Handler, Func<T6, T> t6Handler, Func<T7, T> t7Handler, Func<T8, T> t8Handler)
    {
        return index switch
        {
            1 => t1Handler(As<T1>()),
            2 => t2Handler(As<T2>()),
            3 => t3Handler(As<T3>()),
            4 => t4Handler(As<T4>()),
            5 => t5Handler(As<T5>()),
            6 => t6Handler(As<T6>()),
            7 => t7Handler(As<T7>()),
            8 => t8Handler(As<T8>()),
            _ => throw new InvalidOperationException("Unexpected index"),
        };
    }
    public void Switch(Action<T1> t1Handler, Action<T2> t2Handler, Action<T3> t3Handler, Action<T4> t4Handler,
        Action<T5> t5Handler, Action<T6> t6Handler, Action<T7> t7Handler, Action<T8> t8Handler)
    {
        if (index == 1) t1Handler(As<T1>());
        else if (index == 2) t2Handler(As<T2>());
        else if (index == 3) t3Handler(As<T3>());
        else if (index == 4) t4Handler(As<T4>());
        else if (index == 5) t5Handler(As<T5>());
        else if (index == 6) t6Handler(As<T6>());
        else if (index == 7) t7Handler(As<T7>());
        else if (index == 8) t8Handler(As<T8>());
        else throw new InvalidOperationException("Unexpected index");
    }
}
