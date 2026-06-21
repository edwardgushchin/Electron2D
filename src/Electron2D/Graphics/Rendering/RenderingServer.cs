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
/// Provides the Electron2D server boundary for rendering capabilities.
/// </summary>
///
/// <remarks>
/// <para>
/// `RenderingServer` is a singleton-style facade. It exposes the active renderer
/// profile and feature flags while keeping concrete backend implementations
/// internal to the runtime.
/// </para>
///
/// <para>
/// Electron2D 0.1.0 Preview keeps the native GPU backend device lifecycle behind internal
/// backend types. This public server remains the stable Electron2D query
/// boundary for the active renderer profile and feature flags.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// Reading the current profile and feature flags is safe from any thread.
/// Backend replacement is an internal startup/test operation and should not be
/// performed while a frame is being submitted.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
public static class RenderingServer
{
    private static readonly object BackendLock = new();
    private static IRenderingBackend _backend = new CompatibilityRenderingBackend();

    /// <summary>
    /// Identifies the active renderer profile.
    /// </summary>
    ///
    /// <since>
    /// This enum is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <remarks>
    /// This type is part of the Electron2D 0.1.0 Preview public API.
    /// </remarks>
    ///
    public enum RenderingProfile
    {
        /// <summary>
        /// Guaranteed minimum renderer profile for supported desktop platforms.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept RenderingProfile.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="RenderingProfile" />
        ///
        Compatibility = 0,

        /// <summary>
        /// Full the native GPU backend-oriented renderer profile.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept RenderingProfile.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="RenderingProfile" />
        ///
        Standard = 1
    }

    /// <summary>
    /// Identifies a renderer feature that can be queried at runtime.
    /// </summary>
    ///
    /// <since>
    /// This enum is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <remarks>
    /// This type is part of the Electron2D 0.1.0 Preview public API.
    /// </remarks>
    ///
    public enum RenderingFeature
    {
        /// <summary>
        /// Basic sprite drawing.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept RenderingFeature.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="RenderingFeature" />
        ///
        Sprites = 0,

        /// <summary>
        /// Sprite or UI animation support.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept RenderingFeature.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="RenderingFeature" />
        ///
        Animation = 1,

        /// <summary>
        /// Tile map drawing support.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept RenderingFeature.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="RenderingFeature" />
        ///
        TileMap = 2,

        /// <summary>
        /// User interface drawing support.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept RenderingFeature.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="RenderingFeature" />
        ///
        Ui = 3,

        /// <summary>
        /// Text drawing support.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept RenderingFeature.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="RenderingFeature" />
        ///
        Text = 4,

        /// <summary>
        /// 2D primitive drawing support.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept RenderingFeature.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="RenderingFeature" />
        ///
        Primitives = 5,

        /// <summary>
        /// 2D camera support.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept RenderingFeature.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="RenderingFeature" />
        ///
        Camera = 6,

        /// <summary>
        /// Canvas clipping support.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept RenderingFeature.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="RenderingFeature" />
        ///
        Clipping = 7,

        /// <summary>
        /// Standard blend mode support.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept RenderingFeature.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="RenderingFeature" />
        ///
        StandardBlendModes = 8,

        /// <summary>
        /// Render target support.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept RenderingFeature.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="RenderingFeature" />
        ///
        RenderTargets = 9,

        /// <summary>
        /// Custom canvas shader support.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept RenderingFeature.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="RenderingFeature" />
        ///
        CustomShaders = 10,

        /// <summary>
        /// Shader material support.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept RenderingFeature.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="RenderingFeature" />
        ///
        ShaderMaterial = 11,

        /// <summary>
        /// Multi-pass rendering support.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept RenderingFeature.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="RenderingFeature" />
        ///
        MultiPass = 12,

        /// <summary>
        /// Advanced blending support.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept RenderingFeature.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="RenderingFeature" />
        ///
        AdvancedBlending = 13,

        /// <summary>
        /// Post-processing support.
        /// </summary>
        /// <remarks>
        /// Use this value with APIs that accept RenderingFeature.
        /// </remarks>
        ///
        /// <since>
        /// This API is available since Electron2D 0.1.0 Preview.
        /// </since>
        ///
        /// <seealso cref="RenderingFeature" />
        ///
        PostProcessing = 14
    }

    /// <summary>
    /// Gets the renderer profile currently selected by the runtime.
    /// </summary>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current current profile value.
    /// </value>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="RenderingServer" />
    ///
    public static RenderingProfile CurrentProfile
    {
        get
        {
            lock (BackendLock)
            {
                return _backend.Profile;
            }
        }
    }

    /// <summary>
    /// Checks whether the active renderer backend supports a feature.
    /// </summary>
    ///
    /// <param name="feature">The feature to query.</param>
    ///
    /// <returns><c>true</c> when the active backend supports the feature; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="RenderingServer" />
    ///
    public static bool HasFeature(RenderingFeature feature)
    {
        lock (BackendLock)
        {
            return _backend.HasFeature(feature);
        }
    }

    internal static string CurrentBackendName
    {
        get
        {
            lock (BackendLock)
            {
                return _backend.Name;
            }
        }
    }

    internal static void SetBackend(IRenderingBackend backend)
    {
        ArgumentNullException.ThrowIfNull(backend);

        lock (BackendLock)
        {
            _backend = backend;
        }
    }
}
