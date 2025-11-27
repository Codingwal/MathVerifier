public static class Extensions
{
    public static void SetCount<T>(this List<T> list, int count)
    {
        while (list.Count > count)
            list.RemoveAt(list.Count - 1); // Remove last element
    }
}