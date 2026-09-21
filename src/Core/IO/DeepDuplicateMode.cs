namespace Electron2D;

/// <summary>Controls which nested resources are copied during deep resource duplication.</summary>
public enum DeepDuplicateMode
{
    /// <summary>Shares every nested resource while still allowing derived resources to copy their collection containers.</summary>
    None = 0,

    /// <summary>Duplicates only nested resources that are embedded or do not have an external path.</summary>
    Internal = 1,

    /// <summary>Duplicates every nested resource, including resources with external paths.</summary>
    All = 2
}
