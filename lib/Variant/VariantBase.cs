public abstract class VariantBase : ICustomFormatting
{
    protected int index = -1;
    protected object? value = null;
    public int Index => index;
    public abstract object Value { get; }
    public abstract int ArgCount { get; }

    protected VariantBase(int index, object? value)
    {
        this.index = index;
        this.value = value;
    }
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
        return GetObjectType().Equals(typeof(T));
    }
    public T As<T>()
    {
        if (!Is<T>())
            throw new InvalidOperationException($"Can't get Variant ({GetType()}) as {typeof(T)} because it is of type {GetObjectType()}");
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
