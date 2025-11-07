using System.Collections;
using System.Reflection;

public interface ICustomFormatting
{
    public string Format(string prefix);
}

public class Formatter
{
    public static string Format(object obj, string prefix = "")
    {
        // Console.WriteLine($"Formating obj of type {obj.GetType()}");
        if (obj is ICustomFormatting customFormatter)
        {
            return customFormatter.Format(prefix);
        }
        else if (obj is string s)
        {
            return $"{prefix}\"{s}\"\n";
        }
        else if (Convert.GetTypeCode(obj) != TypeCode.Object)
        {
            return $"{prefix}{Convert.ToString(obj)}\n";
        }
        else if (obj is IEnumerable enumerable)
        {
            string str = "";
            foreach (var e in enumerable)
            {
                str += $"{prefix}Element:\n";
                str += Format(e, AddToPrefix(prefix));
            }
            return str;
        }
        else
        {
            string str = "";
            foreach (FieldInfo field in obj.GetType().GetFields())
            {
                if (field.IsStatic)
                    continue;
                str += prefix + field.Name + ":\n";
                str += Format(field.GetValue(obj)!, AddToPrefix(prefix));
            }
            return str;
        }
    }
    private static string AddToPrefix(string prefix)
    {
        if (prefix.Length / 3 % 2 == 0)
            return prefix + " | ";
        else
            return prefix + " : ";
    }
}