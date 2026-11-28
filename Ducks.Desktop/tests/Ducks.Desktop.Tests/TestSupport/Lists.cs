namespace Ducks.Desktop.Tests
{
    /// <summary>Atalho para devolver IReadOnlyList&lt;T&gt; nos setups dos mocks (evita problemas de inferência de tipos).</summary>
    public static class Lists
    {
        public static IReadOnlyList<T> Of<T>(params T[] items) => items;
    }
}
