namespace Waggo.Application.Common.Interfaces;

/// <summary>Marker for read-only use cases.</summary>
#pragma warning disable CA1040 // Marker interface is intentional
public interface IQuery<TResponse>;
#pragma warning restore CA1040
