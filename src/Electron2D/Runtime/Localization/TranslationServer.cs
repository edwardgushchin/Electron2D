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
using System.Globalization;

namespace Electron2D;

/// <summary>
/// Provides process-wide translation lookup and locale selection.
/// </summary>
///
/// <remarks>
/// <para>
/// The translation server stores in-memory <see cref="Translation"/> resources
/// and resolves messages for the current locale. It is the backing service for
/// <see cref="Object.Tr(string, string)" />.
/// </para>
/// <para>
/// The 0.1.0 Preview lookup policy checks the exact locale, the base language,
/// the <c>en</c> fallback locale and then returns the original message key.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// All methods synchronize access to the process-wide translation registry.
/// Returned arrays are snapshots and may be read from any thread.
/// </threadsafety>
///
/// <since>
/// This class is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="Translation" />
public static class TranslationServer
{
    private const string FallbackLocale = "en";
    private static readonly object SyncRoot = new();
    private static readonly List<Translation> Translations = new();
    private static string locale = DetectLocale();
    private static int version;

    internal static int Version
    {
        get
        {
            lock (SyncRoot)
            {
                return version;
            }
        }
    }

    /// <summary>
    /// Gets the current locale.
    /// </summary>
    ///
    /// <returns>
    /// The normalized locale name used for translation lookup.
    /// </returns>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="SetLocale" />
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public static string GetLocale()
    {
        lock (SyncRoot)
        {
            return locale;
        }
    }

    /// <summary>
    /// Sets the current locale used for translation lookup.
    /// </summary>
    ///
    /// <param name="locale">The locale name to use, such as <c>en</c> or <c>fr_CA</c>.</param>
    ///
    /// <remarks>
    /// The locale is normalized by replacing hyphens with underscores and
    /// applying stable casing to the language and region parts.
    /// </remarks>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="locale" /> is empty or whitespace.
    /// </exception>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="locale" /> is <c>null</c>.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetLocale" />
    public static void SetLocale(string locale)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(locale);
        var normalizedLocale = NormalizeLocaleName(locale);

        lock (SyncRoot)
        {
            if (string.Equals(TranslationServer.locale, normalizedLocale, StringComparison.Ordinal))
            {
                return;
            }

            TranslationServer.locale = normalizedLocale;
            version++;
        }
    }

    /// <summary>
    /// Registers a translation resource for lookup.
    /// </summary>
    ///
    /// <param name="translation">The translation resource to register.</param>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="translation" /> has an empty
    /// <see cref="Translation.Locale" />.
    /// </exception>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="translation" /> is <c>null</c>.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread. Mutating the translation
    /// resource while it is registered is not synchronized by the server.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="RemoveTranslation" />
    /// <seealso cref="Clear" />
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public static void AddTranslation(Translation translation)
    {
        ArgumentNullException.ThrowIfNull(translation);
        if (string.IsNullOrWhiteSpace(translation.Locale))
        {
            throw new ArgumentException("Translation locale must be set before registration.", nameof(translation));
        }

        lock (SyncRoot)
        {
            Translations.Add(translation);
            version++;
        }
    }

    /// <summary>
    /// Removes a translation resource from lookup.
    /// </summary>
    ///
    /// <param name="translation">The translation resource to remove.</param>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="translation" /> is <c>null</c>.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="AddTranslation" />
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public static void RemoveTranslation(Translation translation)
    {
        ArgumentNullException.ThrowIfNull(translation);

        lock (SyncRoot)
        {
            if (Translations.Remove(translation))
            {
                version++;
            }
        }
    }

    /// <summary>
    /// Removes all registered translation resources.
    /// </summary>
    ///
    /// <remarks>
    /// This method does not reset the current locale. It only clears the
    /// registered translations.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <seealso cref="TranslationServer" />
    ///
    public static void Clear()
    {
        lock (SyncRoot)
        {
            if (Translations.Count == 0)
            {
                return;
            }

            Translations.Clear();
            version++;
        }
    }

    /// <summary>
    /// Gets the locales currently provided by registered translations.
    /// </summary>
    ///
    /// <returns>
    /// A stable, ordinally sorted array of unique locale names.
    /// </returns>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="TranslationServer" />
    ///
    public static string[] GetLoadedLocales()
    {
        lock (SyncRoot)
        {
            return Translations
                .Select(translation => translation.Locale)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(item => item, StringComparer.Ordinal)
                .ToArray();
        }
    }

    /// <summary>
    /// Translates a message for the current locale.
    /// </summary>
    ///
    /// <param name="message">The source message key to translate.</param>
    /// <param name="context">The optional message context.</param>
    /// <returns>
    /// The translated message, or <paramref name="message" /> when no
    /// registered translation can resolve it.
    /// </returns>
    ///
    /// <remarks>
    /// Lookup checks the exact current locale, then its base language, then the
    /// <c>en</c> fallback locale. If all lookups miss, the original message is
    /// returned unchanged.
    /// </remarks>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="message" /> or <paramref name="context" />
    /// is <c>null</c>.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is safe to call from any thread as long as registered
    /// translation resources are not mutated concurrently.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Object.Tr(string, string)" />
    public static string Translate(string message, string context = "")
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(context);
        if (message.Length == 0)
        {
            return string.Empty;
        }

        lock (SyncRoot)
        {
            foreach (var candidateLocale in BuildLookupLocales(locale))
            {
                for (var index = Translations.Count - 1; index >= 0; index--)
                {
                    var translation = Translations[index];
                    if (!string.Equals(translation.Locale, candidateLocale, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (translation.TryGetMessage(message, context, out var translated))
                    {
                        return translated;
                    }
                }
            }
        }

        return message;
    }

    internal static string NormalizeLocaleName(string locale)
    {
        var normalized = locale.Trim().Replace('-', '_');
        if (normalized.Length == 0)
        {
            return string.Empty;
        }

        var parts = normalized.Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return string.Empty;
        }

        var result = parts[0].ToLowerInvariant();
        if (parts.Length > 1)
        {
            result += "_" + parts[1].ToUpperInvariant();
        }

        for (var index = 2; index < parts.Length; index++)
        {
            result += "_" + parts[index];
        }

        return result;
    }

    private static string DetectLocale()
    {
        var cultureName = CultureInfo.CurrentUICulture.Name;
        var detected = string.IsNullOrWhiteSpace(cultureName)
            ? FallbackLocale
            : NormalizeLocaleName(cultureName);

        return string.IsNullOrWhiteSpace(detected) ? FallbackLocale : detected;
    }

    private static string[] BuildLookupLocales(string currentLocale)
    {
        var candidates = new List<string>();
        AddCandidate(candidates, currentLocale);

        var separatorIndex = currentLocale.IndexOf('_', StringComparison.Ordinal);
        if (separatorIndex > 0)
        {
            AddCandidate(candidates, currentLocale[..separatorIndex]);
        }

        AddCandidate(candidates, FallbackLocale);
        return candidates.ToArray();
    }

    private static void AddCandidate(List<string> candidates, string candidate)
    {
        if (candidate.Length == 0 || candidates.Contains(candidate, StringComparer.Ordinal))
        {
            return;
        }

        candidates.Add(candidate);
    }
}
