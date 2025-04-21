using System;

namespace Nimbus;

internal class NimbusException : Exception
{
    public NimbusException(string Message) : base($"Nimbus: {Message}") { }
}
