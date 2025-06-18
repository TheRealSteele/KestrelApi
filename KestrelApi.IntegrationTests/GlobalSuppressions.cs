using System.Diagnostics.CodeAnalysis;

// Test naming conventions use underscores
[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test naming convention")]

// Using string paths for relative URIs is acceptable in tests
[assembly: SuppressMessage("Usage", "CA2234:Pass system uri objects instead of strings", Justification = "Relative paths as strings are cleaner in tests")]

// Low priority warnings that are acceptable in test code
[assembly: SuppressMessage("Usage", "CA2263:Prefer generic overload when type is known", Justification = "Minor performance impact in tests")]
[assembly: SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments", Justification = "Acceptable in test code")]