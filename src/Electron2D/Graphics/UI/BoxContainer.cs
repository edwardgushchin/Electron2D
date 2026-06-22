/*
    Electron2D
    MIT License
    Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
    SPDX-License-Identifier: MIT

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
namespace Electron2D;

/// <summary>
/// Arranges direct child controls in a single horizontal or vertical line.
/// </summary>
///
/// <remarks>
/// <para>
/// <c>BoxContainer</c> is the shared implementation behind
/// <see cref="HBoxContainer"/> and <see cref="VBoxContainer"/>. It measures
/// child minimum sizes, applies the <c>separation</c> theme constant and
/// distributes extra space to children whose size flags expand on the main
/// axis.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate it on the main scene
/// thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="Container"/>
/// <seealso cref="HBoxContainer"/>
/// <seealso cref="VBoxContainer"/>
public class BoxContainer : Container
{
    private const int DefaultSeparation = 4;

    /// <summary>
    /// Initializes a new instance of the <see cref="BoxContainer"/> type.
    /// </summary>
    ///
    /// <remarks>
    /// The new container uses horizontal layout until <see cref="Vertical"/> is
    /// set to <c>true</c>.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This constructor is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This constructor is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Vertical"/>
    public BoxContainer()
    {
    }

    /// <summary>
    /// Gets or sets how non-expanded children are aligned along the box axis.
    /// </summary>
    ///
    /// <value>
    /// The current <see cref="BoxContainerAlignmentMode"/>. The default is
    /// <see cref="BoxContainerAlignmentMode.Begin"/>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Alignment only affects free space that was not consumed by expanded
    /// children.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="BoxContainerAlignmentMode"/>
    public BoxContainerAlignmentMode Alignment { get; set; } = BoxContainerAlignmentMode.Begin;

    /// <summary>
    /// Gets or sets whether this box lays out children vertically.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> for vertical layout; <c>false</c> for horizontal layout.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Specialized <see cref="HBoxContainer"/> and <see cref="VBoxContainer"/>
    /// types set this value in their constructors.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="HBoxContainer"/>
    /// <seealso cref="VBoxContainer"/>
    public bool Vertical { get; set; }

    /// <summary>
    /// Adds a spacer control at the beginning or end of this box.
    /// </summary>
    ///
    /// <param name="begin">
    /// <c>true</c> to place the spacer before existing children; <c>false</c>
    /// to place it after existing children.
    /// </param>
    ///
    /// <returns>
    /// The created spacer control.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// The spacer expands and fills on the main axis. It is a normal
    /// <see cref="Control"/> child and can be removed or moved by regular node
    /// hierarchy APIs.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Node.AddChild(Node)"/>
    /// <seealso cref="Node.MoveChild(Node, int)"/>
    public Control AddSpacer(bool begin)
    {
        ThrowIfFreed();
        var spacer = new Control
        {
            SizeFlagsHorizontal = Vertical ? SizeFlags.Fill : SizeFlags.ExpandFill,
            SizeFlagsVertical = Vertical ? SizeFlags.ExpandFill : SizeFlags.Fill
        };

        AddChild(spacer);
        if (begin)
        {
            MoveChild(spacer, 0);
        }

        QueueSort();
        return spacer;
    }

    /// <summary>
    /// Gets the minimum size required by this box container.
    /// </summary>
    ///
    /// <returns>
    /// The minimum size produced by direct visible child controls and
    /// separation.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// The main axis sums child minimum sizes. The cross axis uses the largest
    /// child minimum size.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Control.GetMinimumSize"/>
    public override Vector2 _GetMinimumSize()
    {
        var children = GetLayoutChildren();
        if (children.Length == 0)
        {
            return Vector2.Zero;
        }

        var separation = GetThemeConstantOrDefault("separation", DefaultSeparation);
        var main = 0f;
        var cross = 0f;
        foreach (var child in children)
        {
            var minimum = child.GetCombinedMinimumSize();
            main += Main(minimum);
            cross = MathF.Max(cross, Cross(minimum));
        }

        main += separation * (children.Length - 1);
        return MakeVector(main, cross);
    }

    protected override void SortChildren()
    {
        var children = GetLayoutChildren();
        if (children.Length == 0)
        {
            return;
        }

        var separation = GetThemeConstantOrDefault("separation", DefaultSeparation);
        var minimumSizes = children.Select(child => child.GetCombinedMinimumSize()).ToArray();
        var mainAvailable = Main(Size);
        var crossAvailable = Cross(Size);
        var minimumMain = minimumSizes.Sum(Main) + (separation * (children.Length - 1));
        var freeMain = MathF.Max(0f, mainAvailable - minimumMain);
        var totalExpandRatio = 0f;
        for (var index = 0; index < children.Length; index++)
        {
            if (UsesExpand(MainFlags(children[index])))
            {
                totalExpandRatio += children[index].SizeFlagsStretchRatio;
            }
        }

        var cursor = totalExpandRatio > 0f ? 0f : Alignment switch
        {
            BoxContainerAlignmentMode.Center => freeMain / 2f,
            BoxContainerAlignmentMode.End => freeMain,
            _ => 0f
        };

        for (var index = 0; index < children.Length; index++)
        {
            var child = children[index];
            var minimum = minimumSizes[index];
            var mainFlags = MainFlags(child);
            var crossFlags = CrossFlags(child);
            var slotMain = Main(minimum);
            if (totalExpandRatio > 0f && UsesExpand(mainFlags))
            {
                slotMain += freeMain * (child.SizeFlagsStretchRatio / totalExpandRatio);
            }

            var childMain = UsesFill(mainFlags) ? slotMain : Main(minimum);
            var childCross = UsesFill(crossFlags) ? MathF.Max(crossAvailable, Cross(minimum)) : Cross(minimum);
            var childMainPosition = cursor + AlignOffset(slotMain, childMain, mainFlags);
            var childCrossPosition = AlignOffset(crossAvailable, childCross, crossFlags);
            FitChildInRect(
                child,
                MakeRect(childMainPosition, childCrossPosition, childMain, childCross));

            cursor += slotMain + separation;
        }
    }

    private SizeFlags MainFlags(Control control)
    {
        return Vertical ? control.SizeFlagsVertical : control.SizeFlagsHorizontal;
    }

    private SizeFlags CrossFlags(Control control)
    {
        return Vertical ? control.SizeFlagsHorizontal : control.SizeFlagsVertical;
    }

    private float Main(Vector2 value)
    {
        return Vertical ? value.Y : value.X;
    }

    private float Cross(Vector2 value)
    {
        return Vertical ? value.X : value.Y;
    }

    private Vector2 MakeVector(float main, float cross)
    {
        return Vertical ? new Vector2(cross, main) : new Vector2(main, cross);
    }

    private Rect2 MakeRect(float mainPosition, float crossPosition, float mainSize, float crossSize)
    {
        return Vertical
            ? new Rect2(crossPosition, mainPosition, crossSize, mainSize)
            : new Rect2(mainPosition, crossPosition, mainSize, crossSize);
    }
}
