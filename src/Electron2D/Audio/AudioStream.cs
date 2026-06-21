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
/// Provides the base resource for audio data that can be played by audio nodes.
/// </summary>
///
/// <remarks>
/// <para>
/// `AudioStream` describes reusable sound or music data. Concrete stream
/// instances are created by the resource import pipeline and are consumed by
/// audio playback nodes such as <see cref="AudioStreamPlayer" /> and
/// <see cref="AudioStreamPlayer2D" />.
/// </para>
///
/// <para>
/// Electron2D 0.1.0 Preview exposes stream metadata queries only. Device
/// lifecycle, user bus routing and public stream playback handle objects are
/// separate audio runtime tasks.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate stream resources on the
/// main thread that owns the scene tree. Immutable metadata queries may be
/// called from any thread by concrete implementations that document that
/// behavior.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="Resource" />
/// <seealso cref="AudioStreamPlayer" />
/// <seealso cref="AudioStreamPlayer2D" />
public abstract class AudioStream : Resource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AudioStream" /> type.
    /// </summary>
    ///
    /// <remarks>
    /// The new stream has no playback state. Concrete implementations provide
    /// metadata through overridden query methods.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This constructor is not synchronized. Call it from the thread that owns
    /// the resource being created.
    /// </threadsafety>
    ///
    /// <since>
    /// This constructor is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="AudioStream" />
    public AudioStream()
    {
    }

    /// <summary>
    /// Returns the stream length in seconds.
    /// </summary>
    ///
    /// <returns>
    /// The stream length in seconds, or <c>0</c> when the stream length is not
    /// finite or not known.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// Imported WAV and OGG streams return the duration discovered during
    /// import. Generated, microphone or indefinite future stream types should
    /// return <c>0</c>.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread when the concrete stream is
    /// immutable.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="CanBeSampled" />
    public abstract float GetLength();

    /// <summary>
    /// Checks whether this stream is a collection of other streams.
    /// </summary>
    ///
    /// <returns>
    /// <c>true</c> when the stream selects or combines child streams;
    /// otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// The base implementation returns <c>false</c>. Composite stream resources
    /// may override this method in later audio runtime tasks.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread when the concrete stream is
    /// immutable.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="AudioStream" />
    public virtual bool IsMetaStream()
    {
        ThrowIfFreed();
        return false;
    }

    /// <summary>
    /// Checks whether this stream contains exactly one audio channel.
    /// </summary>
    ///
    /// <returns>
    /// <c>true</c> when the stream is monophonic; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// The base implementation returns <c>false</c>. Imported streams override
    /// this method using the channel count discovered during import.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread when the concrete stream is
    /// immutable.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetLength" />
    public virtual bool IsMonophonic()
    {
        ThrowIfFreed();
        return false;
    }

    /// <summary>
    /// Checks whether the stream can be converted into a static audio sample.
    /// </summary>
    ///
    /// <returns>
    /// <c>true</c> when the stream represents finite static data that can be
    /// sampled in memory; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// The base implementation returns <c>false</c>. Imported static streams
    /// override this method and return <c>true</c>; imported streaming streams
    /// keep returning <c>false</c>.
    /// </para>
    ///
    /// <para>
    /// This preview method is a metadata query only. It does not decode audio
    /// data and does not allocate sample buffers.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread when the concrete stream is
    /// immutable.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetLength" />
    public virtual bool CanBeSampled()
    {
        ThrowIfFreed();
        return false;
    }
}
