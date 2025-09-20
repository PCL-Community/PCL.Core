using PCL.Core.Link.Scaffolding.Network;
using System;

namespace PCL.Core.Link.Scaffolding.Packets
{
    public class PacketNamespaceHandler : Attribute
    {
        public string Namespace { get; private set; }
        public string Method { get; private set; }
        public PacketNamespaceHandler(string @namespace, string method)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(@namespace);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(method);
            Namespace = @namespace;
            Method = method;
        }
    }
}
