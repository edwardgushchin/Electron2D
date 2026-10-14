namespace Electron2D;

/// <summary>Controls which nested resources are copied during deep resource duplication.</summary>
/// <remarks>The policy applies to ordinary resource edges. Explicit force/never-copy fields override it;
/// ordinary nested <see cref="Script"/> assets stay shared. Derived hooks copy typed containers independently.</remarks>
public enum DeepDuplicateMode
{
    /// <summary>Shares ordinary nested resources while derived hooks copy their collection containers.</summary>
    None = 0,

    /// <summary>Duplicates ordinary built-in nested resources except script assets.</summary>
    Internal = 1,

    /// <summary>Duplicates ordinary nested resources, including external resources, except script assets.</summary>
    All = 2
}
