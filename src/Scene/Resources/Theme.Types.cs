namespace Electron2D;

public partial class Theme
{
    /// <summary>Tests a typed category without resolving database fallbacks.</summary>
    /// <param name="dataType">The typed category.</param><param name="name">The item key.</param><param name="themeType">The exact type key.</param>
    /// <returns>The category's presence result, including the local default for font sizes.</returns>
    public bool HasThemeItem(DataType dataType, string name, string themeType)
    {
        CheckCategory(dataType);
        return dataType switch { DataType.Color => HasColor(name, themeType), DataType.Constant => HasConstant(name, themeType), DataType.Font => HasFont(name, themeType), DataType.FontSize => HasFontSize(name, themeType), DataType.Icon => HasIcon(name, themeType), _ => HasStyleBox(name, themeType) };
    }
    /// <summary>Renames a stored entry in a typed category.</summary>
    /// <param name="dataType">The supported category.</param><param name="oldName">The existing key.</param><param name="name">The unused new key.</param><param name="themeType">The exact type key.</param>
    public void RenameThemeItem(DataType dataType, string oldName, string name, string themeType)
    {
        CheckCategory(dataType);
        switch (dataType)
        {
            case DataType.Color: RenameColor(oldName, name, themeType); break;
            case DataType.Constant: RenameConstant(oldName, name, themeType); break;
            case DataType.Font: RenameFont(oldName, name, themeType); break;
            case DataType.FontSize: RenameFontSize(oldName, name, themeType); break;
            case DataType.Icon: RenameIcon(oldName, name, themeType); break;
            case DataType.StyleBox: RenameStyleBox(oldName, name, themeType); break;
        }
    }
    /// <summary>Removes an existing typed entry, including an explicit placeholder.</summary>
    /// <param name="dataType">The supported category.</param><param name="name">The item key.</param><param name="themeType">The exact type key.</param>
    public void ClearThemeItem(DataType dataType, string name, string themeType)
    {
        CheckCategory(dataType);
        switch (dataType)
        {
            case DataType.Color: ClearColor(name, themeType); break;
            case DataType.Constant: ClearConstant(name, themeType); break;
            case DataType.Font: ClearFont(name, themeType); break;
            case DataType.FontSize: ClearFontSize(name, themeType); break;
            case DataType.Icon: ClearIcon(name, themeType); break;
            case DataType.StyleBox: ClearStyleBox(name, themeType); break;
        }
    }
    /// <summary>Lists all stored keys in one typed category, including placeholders.</summary>
    /// <param name="dataType">The supported category.</param><param name="themeType">The exact type key.</param><returns>An item-name snapshot.</returns>
    public string[] GetThemeItemList(DataType dataType, string themeType)
    {
        CheckCategory(dataType);
        return dataType switch { DataType.Color => GetColorList(themeType), DataType.Constant => GetConstantList(themeType), DataType.Font => GetFontList(themeType), DataType.FontSize => GetFontSizeList(themeType), DataType.Icon => GetIconList(themeType), _ => GetStyleBoxList(themeType) };
    }
    /// <summary>Lists types with a record in the selected category.</summary>
    /// <param name="dataType">The supported category.</param><returns>A type-name snapshot.</returns>
    public string[] GetThemeItemTypeList(DataType dataType)
    {
        CheckCategory(dataType);
        return dataType switch { DataType.Color => GetColorTypeList(), DataType.Constant => GetConstantTypeList(), DataType.Font => GetFontTypeList(), DataType.FontSize => GetFontSizeTypeList(), DataType.Icon => GetIconTypeList(), _ => GetStyleBoxTypeList() };
    }
    private void CheckCategory(DataType dataType)
    {
        ThrowIfDisposed();
        if (dataType is < DataType.Color or >= DataType.Max) throw new ArgumentOutOfRangeException(nameof(dataType));
    }

    /// <summary>Defines or replaces a non-native type's variation base.</summary>
    /// <param name="themeType">A nonempty non-native type key.</param><param name="baseType">The nonempty base key.</param>
    /// <remarks>Equal assignments move the variation to the end of its base's child order and notify.
    /// Self-links and cycles are retained; queries use visited sets to terminate safely.</remarks>
    public void SetTypeVariation(string themeType, string baseType)
    {
        ThrowIfDisposed(); ValidateName(themeType, false); ValidateName(baseType, false);
        if (ThemeDB.IsNativeType(themeType)) throw new ArgumentException("A native engine type cannot be a theme variation.", nameof(themeType));
        lock (_gate) { ThrowIfDisposed(); SetVariationCore(themeType, baseType); }
        Publish(true);
    }
    /// <summary>Tests the direct variation relationship, without following ancestors.</summary>
    /// <param name="themeType">The variation key.</param><param name="baseType">The proposed direct base.</param><returns>True for a matching direct link.</returns>
    public bool IsTypeVariation(string themeType, string baseType)
    {
        lock (_gate) { CheckQuery(themeType, baseType); return _variations.TryGetValue(themeType, out var value) && value == baseType; }
    }
    /// <summary>Removes an existing direct variation link while preserving its theme items and descendants.</summary>
    /// <param name="themeType">The variation key.</param><exception cref="ArgumentException">The variation is absent.</exception>
    public void ClearTypeVariation(string themeType)
    {
        lock (_gate) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(themeType); if (!ClearVariationCore(themeType)) throw new ArgumentException("The theme variation does not exist.", nameof(themeType)); }
        Publish(true);
    }
    /// <summary>Gets the direct variation base.</summary>
    /// <param name="themeType">The variation key.</param><returns>The direct base or an empty string.</returns>
    public string GetTypeVariationBase(string themeType)
    {
        lock (_gate) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(themeType); return _variations.TryGetValue(themeType, out var value) ? value : string.Empty; }
    }
    /// <summary>Lists direct and indirect variations in depth-first child order, suppressing repeats.</summary>
    /// <param name="baseType">The starting base.</param><returns>A finite snapshot even when links contain cycles.</returns>
    public string[] GetTypeVariationList(string baseType)
    {
        lock (_gate)
        {
            ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(baseType);
            var result = new List<string>(); var seen = new HashSet<string>(StringComparer.Ordinal); var pending = new Stack<string>();
            PushChildren(baseType, pending);
            while (pending.TryPop(out var name)) if (seen.Add(name)) { result.Add(name); PushChildren(name, pending); }
            return [.. result];
        }
    }
    private void PushChildren(string name, Stack<string> pending)
    {
        if (_variationChildren.TryGetValue(name, out var children)) for (var index = children.Count - 1; index >= 0; index--) pending.Push(children[index]);
    }
    private void SetVariationCore(string themeType, string baseType)
    {
        if (_variations.TryGetValue(themeType, out var previous)) _variationChildren[previous].Remove(themeType);
        _variations[themeType] = baseType;
        if (!_variationChildren.TryGetValue(baseType, out var children)) _variationChildren.Add(baseType, children = []);
        children.Add(themeType);
    }
    private bool ClearVariationCore(string themeType)
    {
        if (!_variations.Remove(themeType, out var baseType)) return false;
        _variationChildren[baseType].Remove(themeType); return true;
    }

    /// <summary>Adds a type record to every supported category and emits structural notifications, even if it already exists.</summary>
    /// <param name="themeType">The valid type key, including the empty key.</param>
    public void AddType(string themeType)
    {
        lock (_gate)
        {
            ThrowIfDisposed(); ValidateName(themeType, true);
            EnsureType(_colors, themeType); EnsureType(_constants, themeType); EnsureType(_fontSizes, themeType); EnsureType(_fonts, themeType); EnsureType(_icons, themeType); EnsureType(_styles, themeType);
        }
        Publish(true);
    }
    /// <summary>Removes all records of a type and its direct or indirect variation links.</summary>
    /// <param name="themeType">The type to remove.</param>
    /// <remarks>Descendant types retain their items. Resource-category removals and each variation removal notify
    /// as separate committed phases before the final notification. Observer errors do not skip required phases.</remarks>
    public void RemoveType(string themeType)
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(themeType); List<Exception>? errors = null;
        RunPhase(() => RemoveResourceType(_icons, themeType), ref errors);
        RunPhase(() => RemoveResourceType(_styles, themeType), ref errors);
        RunPhase(() => RemoveResourceType(_fonts, themeType), ref errors);
        lock (_gate) { ThrowIfDisposed(); _colors.Remove(themeType); _constants.Remove(themeType); _fontSizes.Remove(themeType); }
        RunPhase(() => RemoveVariationIfPresent(themeType), ref errors);
        foreach (var variation in GetTypeVariationList(themeType)) RunPhase(() => RemoveVariationIfPresent(variation), ref errors);
        RunPhase(() => Publish(true), ref errors); ThrowPhases(errors);
    }
    /// <summary>Renames a type independently in each category and retargets its variation relationships.</summary>
    /// <param name="oldThemeType">The previous type key.</param><param name="themeType">The valid destination key.</param>
    /// <remarks>A destination already present in one category preserves both category records. All descendant
    /// variations are retargeted directly to the destination. An empty destination clears variation links.</remarks>
    public void RenameType(string oldThemeType, string themeType)
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(oldThemeType); ValidateName(themeType, true);
        lock (_gate)
        {
            ThrowIfDisposed(); RenameTypeCore(_colors, oldThemeType, themeType); RenameTypeCore(_constants, oldThemeType, themeType);
            RenameTypeCore(_fontSizes, oldThemeType, themeType); RenameTypeCore(_fonts, oldThemeType, themeType); RenameTypeCore(_icons, oldThemeType, themeType); RenameTypeCore(_styles, oldThemeType, themeType);
        }
        List<Exception>? errors = null; var baseType = GetTypeVariationBase(oldThemeType);
        if (baseType.Length != 0)
        {
            RunPhase(() => RemoveVariationIfPresent(oldThemeType), ref errors);
            if (themeType.Length != 0 && !ThemeDB.IsNativeType(themeType)) RunPhase(() => SetTypeVariation(themeType, baseType), ref errors);
        }
        foreach (var variation in GetTypeVariationList(oldThemeType))
        {
            RunPhase(() => RemoveVariationIfPresent(variation), ref errors);
            if (themeType.Length != 0) RunPhase(() => SetTypeVariation(variation, themeType), ref errors);
        }
        RunPhase(() => Publish(true), ref errors); ThrowPhases(errors);
    }
    /// <summary>Lists unique types represented by a category record or a variation key.</summary><returns>A type-name snapshot.</returns>
    public string[] GetTypeList()
    {
        lock (_gate)
        {
            ThrowIfDisposed(); var names = new HashSet<string>(StringComparer.Ordinal);
            names.UnionWith(_icons.Keys); names.UnionWith(_styles.Keys); names.UnionWith(_fontSizes.Keys); names.UnionWith(_fonts.Keys); names.UnionWith(_colors.Keys); names.UnionWith(_constants.Keys); names.UnionWith(_variations.Keys);
            return [.. names];
        }
    }
    private void RemoveResourceType<T>(Dictionary<string, Dictionary<string, T?>> map, string themeType) where T : Resource
    {
        lock (_gate) { ThrowIfDisposed(); if (!map.Remove(themeType, out var items)) return; foreach (var item in items.Values) Release(item); }
        Publish(true);
    }
    private void RemoveVariationIfPresent(string themeType) { lock (_gate) { ThrowIfDisposed(); if (!ClearVariationCore(themeType)) return; } Publish(true); }
    private static void RenameTypeCore<T>(Dictionary<string, Dictionary<string, T>> map, string oldName, string name)
    {
        if (!map.ContainsKey(name) && map.Remove(oldName, out var items)) map.Add(name, items);
    }
    private static void RunPhase(Action action, ref List<Exception>? errors) { try { action(); } catch (Exception error) { (errors ??= []).Add(error); } }
    private static void ThrowPhases(List<Exception>? errors)
    {
        if (errors is { Count: 1 }) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(errors[0]).Throw();
        if (errors is { Count: > 1 }) throw new AggregateException("Theme mutation observers failed.", errors);
    }
}
