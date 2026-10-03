namespace GeXingzhou.DomainChecks;
public static class Check
{
    public static void True(bool value) { if (!value) throw new Exception("Expected true"); }
    public static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}"); }
    public static void Throws<T>(Action action) where T: Exception { try { action(); } catch(T) { return; } throw new Exception($"Expected {typeof(T).Name}"); }
}
