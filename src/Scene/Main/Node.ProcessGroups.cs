namespace Electron2D;

public partial class Node
{
    /// <summary>Selects the processing owner inherited by this node and its descendants.</summary>
    public enum ProcessThreadGroupMode
    {
        /// <summary>Uses the nearest explicit ancestor group or the default main group.</summary>
        Inherit = 0,
        /// <summary>Establishes a group processed on the scene owner thread.</summary>
        MainThread = 1,
        /// <summary>Establishes a group processed by a retained worker and joined at its order barrier.</summary>
        SubThread = 2
    }
    /// <summary>Permits message-only processing when a group has no callbacks in a frame lane.</summary>
    [Flags]
    public enum ProcessThreadMessageFlags
    {
        /// <summary>Permits message-only idle processing.</summary>
        Process = 1,
        /// <summary>Permits message-only physics processing.</summary>
        Physics = 2,
        /// <summary>Permits message-only processing in both lanes.</summary>
        All = 3
    }
    private ProcessThreadGroupMode _processThreadGroup;
    private int _processThreadGroupOrder;
    private ProcessThreadMessageFlags _processThreadMessages;
    internal SceneTree.ProcessingGroup? ActiveProcessingGroup { get; set; }
    internal int ProcessingTreeOrder { get; set; }

    /// <summary>Gets or sets this node's explicit processing group role.</summary>
    /// <value>Inherit initially; an explicit MainThread or SubThread establishes a boundary for inherited descendants.</value>
    /// <remarks>Configuration requires the scene owner. An attached role change rebinds inherited members before synchronously
    /// raising <see cref="ElectronObject.PropertyListChanged"/>. Detached and equal writes do not raise that event.
    /// Observer failures leave the new role committed; a nested change determines the final role.
    /// New group membership starts at a later captured frame.
    /// Worker callbacks retain ordinary backend/render/hierarchy guards; publish those changes to a main group.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The mode is not defined.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off the owner thread or scene capture is active.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposed.</exception>
    /// <exception cref="Exception">A property-list observer throws after the role and group membership have changed.</exception>
    public ProcessThreadGroupMode ProcessThreadGroup
    {
        get { ThrowIfDisposed(); return _processThreadGroup; }
        set
        {
            EnsureMutable(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (_processThreadGroup == value) return;
            _processThreadGroup = value;
            if (Tree is { } tree)
            {
                tree.RebindProcessingGroups(this);
                NotifyPropertyListChanged();
            }
        }
    }
    /// <summary>Gets or sets the ascending order of this node's explicit processing group.</summary>
    /// <value>Any integer, initially zero; inherited nodes retain the value without defining a group order.</value>
    /// <remarks>Subthread stages complete before main groups at equal order. The scene owner or current group may write this value,
    /// including on inherited descendants. Writes apply to the next frame capture. Reads require caller coordination.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is outside the owner/current processing group or scene capture is active.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposed.</exception>
    public int ProcessThreadGroupOrder
    {
        get { ThrowIfDisposed(); return _processThreadGroupOrder; }
        set { EnsureProcessingAccess(); _processThreadGroupOrder = value; }
    }
    /// <summary>Gets or sets lanes permitted to process this explicit group's messages without active callbacks.</summary>
    /// <value>Zero initially; Process, Physics or All permit the corresponding empty-group lane.</value>
    /// <remarks>A group with enabled callbacks already services messages in that lane. The implicit main group
    /// always permits both lanes. Inherited nodes retain this stored value without overriding their owner.
    /// The scene owner or current group may write this value. Changes affect later stage eligibility without
    /// interrupting an active drain. Reads require caller coordination.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The flags contain unknown bits.</exception>
    /// <exception cref="InvalidOperationException">Mutation is outside the owner/current processing group or scene capture is active.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposed.</exception>
    public ProcessThreadMessageFlags ProcessThreadMessages
    {
        get { ThrowIfDisposed(); return _processThreadMessages; }
        set
        {
            EnsureProcessingAccess(); if ((value & ~ProcessThreadMessageFlags.All) != 0) throw new ArgumentOutOfRangeException(nameof(value));
            _processThreadMessages = value;
        }
    }
    private static readonly PropertyDescriptor[] ProcessingGroupProperties =
    [
        new PropertyDescriptor<Node, ProcessThreadGroupMode>(nameof(ProcessThreadGroup), n => n.ProcessThreadGroup,
            (n, v) => n.ProcessThreadGroup = v, _ => ProcessThreadGroupMode.Inherit, (_, v) => Enum.IsDefined(v), stored: true),
        new PropertyDescriptor<Node, int>(nameof(ProcessThreadGroupOrder), n => n.ProcessThreadGroupOrder,
            (n, v) => n.ProcessThreadGroupOrder = v, _ => 0, stored: true),
        new PropertyDescriptor<Node, ProcessThreadMessageFlags>(nameof(ProcessThreadMessages), n => n.ProcessThreadMessages,
            (n, v) => n.ProcessThreadMessages = v, _ => 0, (_, v) => (v & ~ProcessThreadMessageFlags.All) == 0, stored: true)
    ];
    internal void EnsureProcessingAccess()
    {
        ThrowIfDisposed(); EnsureNotSceneCapture(); Tree?.EnsureProcessingAccess(this); ValidateMutation();
    }
    internal bool ProcessingAccessible => Tree is not { } tree || tree.IsOwnerThread || SceneTree.CurrentProcessingGroup is { } group && ReferenceEquals(group, ActiveProcessingGroup);
}
