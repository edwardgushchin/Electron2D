using System.Collections.Generic;
using Electron2D.Binding.Box2D.Common;

namespace Electron2D.Binding.Box2D.Dynamics.Contacts
{
    /// <summary>
    /// A contact edge is used to connect bodies and contacts together
    /// in a contact graph where each body is a node and each contact
    /// is an edge. A contact edge belongs to a doubly linked list
    /// maintained in each attached body. Each contact has two contact
    /// nodes, one for each attached body.
    /// </summary>
    public class ContactEdge
    {
        /// <summary>
        /// provides quick access to the other body attached.
        /// </summary>
        public Body Other;

        /// <summary>
        /// the contact
        /// </summary>
        public Contact Contact;

        public readonly LinkedListNode<ContactEdge> Node = new LinkedListNode<ContactEdge>(default);
    }
}