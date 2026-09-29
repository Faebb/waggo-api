namespace Waggo.Application.Common.Interfaces;

/// <summary>Marker for use cases that change state.</summary>
#pragma warning disable CA1040 // Marker interface is intentional
public interface ICommand<TResponse>;
#pragma warning restore CA1040
