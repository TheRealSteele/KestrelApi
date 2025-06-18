using System.Diagnostics.CodeAnalysis;

// CA2007: ConfigureAwait(false) should not be used in ASP.NET Core applications
[assembly: SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "ASP.NET Core doesn't use SynchronizationContext")]