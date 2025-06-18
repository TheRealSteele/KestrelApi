using System.Diagnostics.CodeAnalysis;

// CA1707: Test method names can have underscores for readability
[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test method names should be descriptive")]

// CA2007: ConfigureAwait(false) is not needed in test projects
[assembly: SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Test projects don't need ConfigureAwait")]

// CA1825: Use Array.Empty<T>() is not critical in tests
[assembly: SuppressMessage("Performance", "CA1825:Avoid zero-length array allocations", Justification = "Not critical in tests")]

// CA1861: Prefer static readonly fields - not critical in tests
[assembly: SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments", Justification = "Not critical in tests")]