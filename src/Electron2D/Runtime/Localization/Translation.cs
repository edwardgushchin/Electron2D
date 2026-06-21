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
/// Stores translated messages for one locale.
/// </summary>
///
/// <remarks>
/// <para>
/// A translation resource maps a source message key and optional context to a
/// translated message for <see cref="Locale" />. Register translation resources
/// with <see cref="TranslationServer.AddTranslation" /> so
/// <see cref="Object.Tr(string, string)" /> and
/// <see cref="TranslationServer.Translate" /> can resolve them.
/// </para>
/// <para>
/// Electron2D 0.1.0 Preview keeps this resource in memory. Loading translation
/// files from project resources is handled by later settings and import tasks.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate translation resources
/// before registering them, or coordinate access externally.
/// </threadsafety>
///
/// <since>
/// This class is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="TranslationServer" />
public sealed class Translation : Resource
{

    /// <summary>
    /// Initializes a new instance of the Translation type.
    /// </summary>
    ///
    /// <remarks>
    /// The new instance follows the lifetime and validation rules of its declaring type.
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
    /// <seealso cref="Translation" />
    ///
    public Translation()
    {
    }

    private readonly Dictionary<MessageKey, string> messages = new();
    private string locale = string.Empty;

    /// <summary>
    /// Gets or sets the locale represented by this translation.
    /// </summary>
    ///
    /// <remarks>
    /// The assigned value is normalized by replacing hyphens with underscores,
    /// trimming whitespace and normalizing the language part to lowercase and
    /// the region part to uppercase. For example, <c>pt-BR</c> is stored as
    /// <c>pt_BR</c>.
    /// </remarks>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when the assigned value is <c>null</c>.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <value>
    /// The current locale value.
    /// </value>
    ///
    /// <seealso cref="Translation" />
    ///
    public string Locale
    {
        get
        {
            ThrowIfFreed();
            return locale;
        }
        set
        {
            ThrowIfFreed();
            ArgumentNullException.ThrowIfNull(value);
            locale = TranslationServer.NormalizeLocaleName(value);
        }
    }

    /// <summary>
    /// Adds or replaces a translated message.
    /// </summary>
    ///
    /// <param name="srcMessage">The source message key.</param>
    /// <param name="xlatedMessage">The translated message text.</param>
    /// <param name="context">The optional message context.</param>
    ///
    /// <remarks>
    /// Re-adding the same <paramref name="srcMessage" /> and
    /// <paramref name="context" /> replaces the previous translated text.
    /// Contexts let different UI locations use the same source key with
    /// different translations.
    /// </remarks>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="srcMessage" />,
    /// <paramref name="xlatedMessage" /> or <paramref name="context" /> is
    /// <c>null</c>.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetMessage" />
    /// <seealso cref="EraseMessage" />
    public void AddMessage(string srcMessage, string xlatedMessage, string context = "")
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(srcMessage);
        ArgumentNullException.ThrowIfNull(xlatedMessage);
        ArgumentNullException.ThrowIfNull(context);

        messages[new MessageKey(srcMessage, context)] = xlatedMessage;
    }

    /// <summary>
    /// Removes a translated message if it exists.
    /// </summary>
    ///
    /// <param name="srcMessage">The source message key.</param>
    /// <param name="context">The optional message context.</param>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="srcMessage" /> or
    /// <paramref name="context" /> is <c>null</c>.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="AddMessage" />
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public void EraseMessage(string srcMessage, string context = "")
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(srcMessage);
        ArgumentNullException.ThrowIfNull(context);

        messages.Remove(new MessageKey(srcMessage, context));
    }

    /// <summary>
    /// Gets a translated message by source key and context.
    /// </summary>
    ///
    /// <param name="srcMessage">The source message key.</param>
    /// <param name="context">The optional message context.</param>
    /// <returns>
    /// The translated message, or an empty string when no matching message
    /// exists in this resource.
    /// </returns>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="srcMessage" /> or
    /// <paramref name="context" /> is <c>null</c>.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="AddMessage" />
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public string GetMessage(string srcMessage, string context = "")
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(srcMessage);
        ArgumentNullException.ThrowIfNull(context);

        return TryGetMessage(srcMessage, context, out var message) ? message : string.Empty;
    }

    /// <summary>
    /// Gets the source message keys stored by this translation.
    /// </summary>
    ///
    /// <returns>
    /// A stable, ordinally sorted array of unique source message keys.
    /// </returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="Translation" />
    ///
    public string[] GetMessageList()
    {
        ThrowIfFreed();
        return messages.Keys
            .Select(key => key.Source)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
    }

    internal bool TryGetMessage(string srcMessage, string context, out string message)
    {
        ThrowIfFreed();
        return messages.TryGetValue(new MessageKey(srcMessage, context), out message!);
    }

    private readonly record struct MessageKey(string Source, string Context);
}
