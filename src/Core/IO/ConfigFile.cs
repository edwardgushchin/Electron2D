using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Electron2D;

/// <summary>Identifies one strongly typed value in a <see cref="ConfigFile"/>.</summary>
/// <typeparam name="T">The value type used to serialize and deserialize the entry.</typeparam>
/// <remarks>
/// Reuse one key instance for each logical setting. The empty section addresses entries before the first section header.
/// Values are serialized with the declared type rather than a runtime-wide universal value container.
/// </remarks>
public sealed class ConfigKey<T>
{
    /// <summary>Initializes a typed configuration key.</summary>
    /// <param name="section">The case-sensitive section name, or an empty string for a sectionless entry.</param>
    /// <param name="name">The nonempty case-sensitive entry name.</param>
    /// <exception cref="ArgumentNullException"><paramref name="section"/> or <paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    /// <exception cref="NotSupportedException">
    /// <typeparamref name="T"/> is an untyped JSON DOM value, <see cref="object"/>, a delegate, or an engine object.
    /// </exception>
    public ConfigKey(string section, string name)
    {
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(name);

        if (name.Length == 0)
            throw new ArgumentException("A configuration key name cannot be empty.", nameof(name));

        ConfigFile.ValidateValueType(typeof(T));
        Section = section;
        Name = name;
    }

    /// <summary>Gets the case-sensitive section name.</summary>
    /// <value>The section name, or an empty string for a sectionless entry.</value>
    public string Section { get; }

    /// <summary>Gets the case-sensitive entry name.</summary>
    /// <value>The nonempty entry name.</value>
    public string Name { get; }

    /// <summary>Returns the section and entry name for diagnostics.</summary>
    /// <returns><c>section/name</c>, or only the entry name for a sectionless key.</returns>
    public override string ToString() => Section.Length == 0 ? Name : $"{Section}/{Name}";
}

/// <summary>Stores strongly typed values in a sectioned text configuration and loads or saves them as one document.</summary>
/// <remarks>
/// Values use compact JSON tokens inside an INI-style section layout. Keys remain strongly typed through
/// <see cref="ConfigKey{T}"/>; no untyped value getter or setter is exposed. All public operations are safe to invoke
/// concurrently and are serialized at document mutation boundaries. File operations use ordinary operating-system
/// paths and are unsuitable for a real-time frame callback.
/// </remarks>
public sealed class ConfigFile : ElectronObject
{
    private const byte EncryptionVersion = 1;
    private const byte RawKeyEncryption = 0;
    private const byte PasswordEncryption = 1;
    private const int EncryptionKeySize = 32;
    private const int EncryptionSaltSize = 16;
    private const int EncryptionNonceSize = 12;
    private const int EncryptionTagSize = 16;
    private const int PasswordIterations = 600_000;

    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private static readonly JsonSerializerOptions ValueJsonOptions = new()
    {
        IncludeFields = true,
        PropertyNameCaseInsensitive = false,
        WriteIndented = false,
        Converters =
        {
            new ColorJsonConverter(),
            new Vector2JsonConverter(),
            new Vector2IJsonConverter(),
            new Vector4JsonConverter(),
            new Vector4IJsonConverter(),
            new RectJsonConverter(),
            new TransformJsonConverter(),
        }
    };

    private static ReadOnlySpan<byte> EncryptionMagic => "E2DCFG"u8;

    private readonly object _gate = new();
    private readonly Dictionary<string, SectionState> _sections = new(StringComparer.Ordinal);
    private readonly List<string> _sectionOrder = [];

    /// <summary>Initializes an empty configuration document.</summary>
    public ConfigFile()
    {
    }

    /// <summary>Removes every section and entry from memory.</summary>
    /// <remarks>This method does not modify any file previously loaded or saved.</remarks>
    /// <exception cref="ObjectDisposedException">The configuration file is disposing or disposed.</exception>
    public void Clear()
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            _sections.Clear();
            _sectionOrder.Clear();
        }
    }

    /// <summary>Encodes the current document as sectioned UTF-8-compatible text.</summary>
    /// <returns>
    /// A snapshot using LF line endings. Values are compact JSON tokens; unsafe section and key names are JSON-quoted.
    /// The empty document is encoded as an empty string.
    /// </returns>
    /// <remarks>Comments read by <see cref="Parse"/> are intentionally not retained.</remarks>
    /// <exception cref="ObjectDisposedException">The configuration file is disposing or disposed.</exception>
    public string EncodeToText()
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            return EncodeToTextLocked();
        }
    }

    /// <summary>Removes an existing section and all entries that it contains.</summary>
    /// <param name="section">The case-sensitive section name. An empty string identifies sectionless entries.</param>
    /// <exception cref="ArgumentNullException"><paramref name="section"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The section does not exist.</exception>
    /// <exception cref="ObjectDisposedException">The configuration file is disposing or disposed.</exception>
    public void EraseSection(string section)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(section);

        lock (_gate)
        {
            ThrowIfDisposed();
            if (!_sections.Remove(section))
                throw new KeyNotFoundException($"Configuration section '{section}' does not exist.");

            _sectionOrder.Remove(section);
        }
    }

    /// <summary>Removes an existing typed entry and removes its section when it becomes empty.</summary>
    /// <typeparam name="T">The declared entry value type.</typeparam>
    /// <param name="key">The typed section and entry identifier.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The section or entry does not exist.</exception>
    /// <exception cref="ObjectDisposedException">The configuration file is disposing or disposed.</exception>
    public void EraseSectionKey<T>(ConfigKey<T> key)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(key);

        lock (_gate)
        {
            ThrowIfDisposed();
            if (!_sections.TryGetValue(key.Section, out var section) || !section.Values.Remove(key.Name))
                throw new KeyNotFoundException($"Configuration entry '{key}' does not exist.");

            section.KeyOrder.Remove(key.Name);
            RemoveSectionIfEmptyLocked(key.Section, section);
        }
    }

    /// <summary>Gets the entry names currently stored in a section.</summary>
    /// <param name="section">The case-sensitive section name. An empty string identifies sectionless entries.</param>
    /// <returns>A read-only insertion-order snapshot of the entry names.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="section"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The section does not exist.</exception>
    /// <exception cref="ObjectDisposedException">The configuration file is disposing or disposed.</exception>
    public IReadOnlyList<string> GetSectionKeys(string section)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(section);

        lock (_gate)
        {
            ThrowIfDisposed();
            if (!_sections.TryGetValue(section, out var state))
                throw new KeyNotFoundException($"Configuration section '{section}' does not exist.");

            return Array.AsReadOnly(state.KeyOrder.ToArray());
        }
    }

    /// <summary>Gets all section names currently present in the document.</summary>
    /// <returns>
    /// A read-only insertion-order snapshot. The empty section, when present, is always the first item.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The configuration file is disposing or disposed.</exception>
    public IReadOnlyList<string> GetSections()
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            return Array.AsReadOnly(_sectionOrder.ToArray());
        }
    }

    /// <summary>Gets a required typed entry.</summary>
    /// <typeparam name="T">The declared entry value type.</typeparam>
    /// <param name="key">The typed section and entry identifier.</param>
    /// <returns>A newly deserialized value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The entry does not exist.</exception>
    /// <exception cref="InvalidDataException">The stored JSON token cannot be decoded as <typeparamref name="T"/>.</exception>
    /// <exception cref="ObjectDisposedException">The configuration file is disposing or disposed.</exception>
    public T GetValue<T>(ConfigKey<T> key)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(key);
        return DeserializeValue<T>(GetSerializedValue(key), key);
    }

    /// <summary>Gets a typed entry or a caller-provided fallback when the entry is absent.</summary>
    /// <typeparam name="T">The declared entry value type.</typeparam>
    /// <param name="key">The typed section and entry identifier.</param>
    /// <param name="defaultValue">The value returned without serialization when the entry is absent.</param>
    /// <returns>The deserialized stored value, or <paramref name="defaultValue"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidDataException">The stored JSON token exists but cannot be decoded as <typeparamref name="T"/>.</exception>
    /// <exception cref="ObjectDisposedException">The configuration file is disposing or disposed.</exception>
    public T GetValue<T>(ConfigKey<T> key, T defaultValue)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(key);

        return TryGetSerializedValue(key, out var serialized)
            ? DeserializeValue<T>(serialized, key)
            : defaultValue;
    }

    /// <summary>Determines whether a section exists.</summary>
    /// <param name="section">The case-sensitive section name. An empty string identifies sectionless entries.</param>
    /// <returns><see langword="true"/> when the section contains at least one entry; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="section"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The configuration file is disposing or disposed.</exception>
    public bool HasSection(string section)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(section);

        lock (_gate)
        {
            ThrowIfDisposed();
            return _sections.ContainsKey(section);
        }
    }

    /// <summary>Determines whether a typed entry exists.</summary>
    /// <typeparam name="T">The declared entry value type.</typeparam>
    /// <param name="key">The typed section and entry identifier.</param>
    /// <returns><see langword="true"/> when the entry exists; otherwise <see langword="false"/>.</returns>
    /// <remarks>The stored token is not deserialized by this method.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The configuration file is disposing or disposed.</exception>
    public bool HasSectionKey<T>(ConfigKey<T> key)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(key);

        lock (_gate)
        {
            ThrowIfDisposed();
            return _sections.TryGetValue(key.Section, out var section) && section.Values.ContainsKey(key.Name);
        }
    }

    /// <summary>Loads and merges an unencrypted configuration document from an operating-system path.</summary>
    /// <param name="path">The nonempty file path.</param>
    /// <remarks>
    /// Parsing is completed before mutation. Existing entries not present in the loaded document are retained, and
    /// matching entries are replaced atomically as one merge. Virtual resource paths are not resolved.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="IOException">The file cannot be read.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller cannot read the file.</exception>
    /// <exception cref="DecoderFallbackException">The file is not valid UTF-8.</exception>
    /// <exception cref="FormatException">The document is malformed.</exception>
    /// <exception cref="ObjectDisposedException">The configuration file is disposing or disposed.</exception>
    public void Load(string path)
    {
        ThrowIfDisposed();
        ValidatePath(path);
        var data = File.ReadAllText(path, StrictUtf8);
        MergeParsed(ParseDocument(data));
    }

    /// <summary>Loads and merges a configuration document encrypted with a 256-bit key.</summary>
    /// <param name="path">The nonempty encrypted file path.</param>
    /// <param name="key">Exactly 32 key bytes.</param>
    /// <remarks>The authenticated encryption envelope must have been produced by <see cref="SaveEncrypted"/>.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid, or <paramref name="key"/> is not 32 bytes.</exception>
    /// <exception cref="IOException">The file cannot be read.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller cannot read the file.</exception>
    /// <exception cref="InvalidDataException">The encryption envelope or decrypted UTF-8 document is malformed.</exception>
    /// <exception cref="CryptographicException">Authentication fails because the key is wrong or the file was modified.</exception>
    /// <exception cref="PlatformNotSupportedException">AES-GCM is unavailable on the current platform.</exception>
    /// <exception cref="FormatException">The decrypted configuration document is malformed.</exception>
    /// <exception cref="ObjectDisposedException">The configuration file is disposing or disposed.</exception>
    public void LoadEncrypted(string path, ReadOnlySpan<byte> key)
    {
        ThrowIfDisposed();
        ValidatePath(path);
        ValidateEncryptionKey(key);
        EnsureAuthenticatedEncryptionSupported();
        var envelope = File.ReadAllBytes(path);
        var plaintext = Decrypt(envelope, key, RawKeyEncryption);

        try
        {
            MergeParsed(ParseDocument(DecodePlaintext(plaintext)));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    /// <summary>Loads and merges a configuration document encrypted with a password-derived key.</summary>
    /// <param name="path">The nonempty encrypted file path.</param>
    /// <param name="password">The nonempty password.</param>
    /// <remarks>
    /// The authenticated envelope must have been produced by <see cref="SaveEncryptedPass"/>. A per-file random salt
    /// and PBKDF2-HMAC-SHA-256 are used before AES-256-GCM authentication and decryption.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> or <paramref name="password"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> or <paramref name="password"/> is empty, or the path is invalid.</exception>
    /// <exception cref="IOException">The file cannot be read.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller cannot read the file.</exception>
    /// <exception cref="InvalidDataException">The encryption envelope or decrypted UTF-8 document is malformed.</exception>
    /// <exception cref="CryptographicException">Authentication fails because the password is wrong or the file was modified.</exception>
    /// <exception cref="PlatformNotSupportedException">AES-GCM is unavailable on the current platform.</exception>
    /// <exception cref="FormatException">The decrypted configuration document is malformed.</exception>
    /// <exception cref="ObjectDisposedException">The configuration file is disposing or disposed.</exception>
    public void LoadEncryptedPass(string path, string password)
    {
        ThrowIfDisposed();
        ValidatePath(path);
        ValidatePassword(password);
        EnsureAuthenticatedEncryptionSupported();
        var envelope = File.ReadAllBytes(path);
        ParseEnvelope(envelope, PasswordEncryption, out _, out var saltOffset, out var saltLength, out _);
        var derivedKey = Rfc2898DeriveBytes.Pbkdf2(
            password,
            envelope.AsSpan(saltOffset, saltLength),
            PasswordIterations,
            HashAlgorithmName.SHA256,
            EncryptionKeySize);

        try
        {
            var plaintext = Decrypt(envelope, derivedKey, PasswordEncryption);
            try
            {
                MergeParsed(ParseDocument(DecodePlaintext(plaintext)));
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plaintext);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(derivedKey);
        }
    }

    /// <summary>Parses and merges an in-memory configuration document.</summary>
    /// <param name="data">The complete sectioned document.</param>
    /// <remarks>
    /// Blank lines and lines whose first non-whitespace character is a semicolon are ignored. Parsing is transactional:
    /// malformed input leaves the current document unchanged. Existing entries not mentioned by the input are retained.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">A section, assignment, identifier, or JSON value is malformed.</exception>
    /// <exception cref="ObjectDisposedException">The configuration file is disposing or disposed.</exception>
    public void Parse(string data)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(data);
        MergeParsed(ParseDocument(data));
    }

    /// <summary>Saves the current document to an unencrypted operating-system path.</summary>
    /// <param name="path">The nonempty destination path.</param>
    /// <remarks>
    /// A snapshot is written to a uniquely named file in the destination directory, flushed, and moved over the target.
    /// The destination directory must already exist. Virtual resource paths are not resolved.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid.</exception>
    /// <exception cref="IOException">The temporary or destination file cannot be written or replaced.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller cannot write the destination.</exception>
    /// <exception cref="ObjectDisposedException">The configuration file is disposing or disposed.</exception>
    public void Save(string path)
    {
        ThrowIfDisposed();
        ValidatePath(path);
        AtomicFile.Write(path, StrictUtf8.GetBytes(EncodeToText()));
    }

    /// <summary>Saves the current document using authenticated AES-256-GCM encryption.</summary>
    /// <param name="path">The nonempty destination path.</param>
    /// <param name="key">Exactly 32 key bytes.</param>
    /// <remarks>A fresh random nonce is generated for every save. The binary envelope is Electron2D-specific.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or invalid, or <paramref name="key"/> is not 32 bytes.</exception>
    /// <exception cref="IOException">The temporary or destination file cannot be written or replaced.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller cannot write the destination.</exception>
    /// <exception cref="CryptographicException">Encryption cannot be completed.</exception>
    /// <exception cref="PlatformNotSupportedException">AES-GCM is unavailable on the current platform.</exception>
    /// <exception cref="ObjectDisposedException">The configuration file is disposing or disposed.</exception>
    public void SaveEncrypted(string path, ReadOnlySpan<byte> key)
    {
        ThrowIfDisposed();
        ValidatePath(path);
        ValidateEncryptionKey(key);
        EnsureAuthenticatedEncryptionSupported();
        var plaintext = StrictUtf8.GetBytes(EncodeToText());

        try
        {
            AtomicFile.Write(path, Encrypt(plaintext, key, RawKeyEncryption, ReadOnlySpan<byte>.Empty));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    /// <summary>Saves the current document using password-derived authenticated encryption.</summary>
    /// <param name="path">The nonempty destination path.</param>
    /// <param name="password">The nonempty password.</param>
    /// <remarks>
    /// A fresh random salt and nonce are generated for every save. PBKDF2-HMAC-SHA-256 derives a 256-bit key before
    /// AES-256-GCM encryption. The binary envelope is Electron2D-specific.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> or <paramref name="password"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> or <paramref name="password"/> is empty, or the path is invalid.</exception>
    /// <exception cref="IOException">The temporary or destination file cannot be written or replaced.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller cannot write the destination.</exception>
    /// <exception cref="CryptographicException">Key derivation or encryption cannot be completed.</exception>
    /// <exception cref="PlatformNotSupportedException">AES-GCM is unavailable on the current platform.</exception>
    /// <exception cref="ObjectDisposedException">The configuration file is disposing or disposed.</exception>
    public void SaveEncryptedPass(string path, string password)
    {
        ThrowIfDisposed();
        ValidatePath(path);
        ValidatePassword(password);
        EnsureAuthenticatedEncryptionSupported();

        var salt = RandomNumberGenerator.GetBytes(EncryptionSaltSize);
        var derivedKey = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            PasswordIterations,
            HashAlgorithmName.SHA256,
            EncryptionKeySize);
        var plaintext = StrictUtf8.GetBytes(EncodeToText());

        try
        {
            AtomicFile.Write(path, Encrypt(plaintext, derivedKey, PasswordEncryption, salt));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
            CryptographicOperations.ZeroMemory(derivedKey);
            CryptographicOperations.ZeroMemory(salt);
        }
    }

    /// <summary>Assigns or removes a typed entry.</summary>
    /// <typeparam name="T">The declared entry value type.</typeparam>
    /// <param name="key">The typed section and entry identifier.</param>
    /// <param name="value">
    /// The value to serialize immediately. A <see langword="null"/> reference removes the entry without error when absent.
    /// </param>
    /// <remarks>
    /// Serialization uses compact, case-sensitive <see cref="System.Text.Json"/> semantics and includes public fields.
    /// The serialized snapshot is independent of later mutations to <paramref name="value"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="JsonException"><paramref name="value"/> cannot be serialized as <typeparamref name="T"/>.</exception>
    /// <exception cref="NotSupportedException"><typeparamref name="T"/> has no supported JSON representation.</exception>
    /// <exception cref="ObjectDisposedException">The configuration file is disposing or disposed.</exception>
    public void SetValue<T>(ConfigKey<T> key, T? value)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(key);

        if (value is null)
        {
            lock (_gate)
            {
                ThrowIfDisposed();
                RemoveValueIfPresentLocked(key.Section, key.Name);
            }

            return;
        }

        var serialized = SerializeSnapshot(value);
        lock (_gate)
        {
            ThrowIfDisposed();
            SetSerializedValueLocked(key.Section, key.Name, serialized);
        }
    }

    /// <summary>Attempts to get a typed entry.</summary>
    /// <typeparam name="T">The declared entry value type.</typeparam>
    /// <param name="key">The typed section and entry identifier.</param>
    /// <param name="value">The newly deserialized value when found; otherwise the default value of <typeparamref name="T"/>.</param>
    /// <returns><see langword="true"/> when the entry exists; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidDataException">The stored JSON token exists but cannot be decoded as <typeparamref name="T"/>.</exception>
    /// <exception cref="ObjectDisposedException">The configuration file is disposing or disposed.</exception>
    public bool TryGetValue<T>(ConfigKey<T> key, [MaybeNullWhen(false)] out T value)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(key);

        if (!TryGetSerializedValue(key, out var serialized))
        {
            value = default;
            return false;
        }

        value = DeserializeValue<T>(serialized, key);
        return true;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (_gate)
            {
                _sections.Clear();
                _sectionOrder.Clear();
            }
        }

        base.Dispose(disposing);
    }

    internal static void ValidateValueType(Type type)
    {
        if (type == typeof(object) || type == typeof(JsonElement) || type == typeof(JsonDocument) ||
            typeof(JsonNode).IsAssignableFrom(type) || typeof(Delegate).IsAssignableFrom(type) ||
            typeof(ElectronObject).IsAssignableFrom(type))
        {
            throw new NotSupportedException($"Configuration values cannot use the untyped or engine-owned type '{type}'.");
        }

        if (type.HasElementType && type.GetElementType() is { } elementType)
            ValidateValueType(elementType);

        foreach (var argument in type.GetGenericArguments())
            ValidateValueType(argument);
    }

    internal static T DeserializeSnapshot<T>(string serialized, string diagnosticName)
    {
        try
        {
            var value = JsonSerializer.Deserialize<T>(serialized, ValueJsonOptions);
            return value is null
                ? throw new InvalidDataException($"Configuration entry '{diagnosticName}' decoded to null, which is not a stored value.")
                : value;
        }
        catch (JsonException error)
        {
            throw new InvalidDataException(
                $"Configuration entry '{diagnosticName}' cannot be decoded as {typeof(T).FullName}.",
                error);
        }
        catch (NotSupportedException error)
        {
            throw new InvalidDataException(
                $"Configuration entry '{diagnosticName}' has no supported decoder for {typeof(T).FullName}.",
                error);
        }
    }

    internal static string SerializeSnapshot<T>(T value) => JsonSerializer.Serialize(value, ValueJsonOptions);

    private static T DeserializeValue<T>(string serialized, ConfigKey<T> key) =>
        DeserializeSnapshot<T>(serialized, key.ToString());

    private static string DecodeIdentifier(string token, bool allowEmpty, int lineNumber)
    {
        if (token.Length == 0)
        {
            if (allowEmpty)
                return string.Empty;

            throw new FormatException($"Configuration line {lineNumber} has an empty key name.");
        }

        if (token[0] != '"')
            return token;

        try
        {
            return JsonSerializer.Deserialize<string>(token) ??
                   throw new FormatException($"Configuration line {lineNumber} has a null identifier.");
        }
        catch (JsonException error)
        {
            throw new FormatException($"Configuration line {lineNumber} has an invalid quoted identifier.", error);
        }
    }

    private static string DecodePlaintext(byte[] plaintext)
    {
        try
        {
            return StrictUtf8.GetString(plaintext);
        }
        catch (DecoderFallbackException error)
        {
            throw new InvalidDataException("The decrypted configuration is not valid UTF-8.", error);
        }
    }

    private static byte[] Decrypt(byte[] envelope, ReadOnlySpan<byte> key, byte expectedMode)
    {
        ParseEnvelope(envelope, expectedMode, out var headerLength, out _, out _, out var nonceOffset);
        var ciphertextLength = envelope.Length - headerLength - EncryptionTagSize;
        var plaintext = new byte[ciphertextLength];

        try
        {
            using var cipher = new AesGcm(key, EncryptionTagSize);
            cipher.Decrypt(
                envelope.AsSpan(nonceOffset, EncryptionNonceSize),
                envelope.AsSpan(headerLength + EncryptionTagSize, ciphertextLength),
                envelope.AsSpan(headerLength, EncryptionTagSize),
                plaintext,
                envelope.AsSpan(0, headerLength));
            return plaintext;
        }
        catch
        {
            CryptographicOperations.ZeroMemory(plaintext);
            throw;
        }
    }

    private static string EncodeIdentifier(string identifier)
    {
        if (identifier.Length > 0 && identifier.All(IsSafeIdentifierCharacter))
            return identifier;

        return JsonSerializer.Serialize(identifier);
    }

    private static byte[] Encrypt(
        ReadOnlySpan<byte> plaintext,
        ReadOnlySpan<byte> key,
        byte mode,
        ReadOnlySpan<byte> salt)
    {
        var headerLength = EncryptionMagic.Length + 3 + salt.Length + EncryptionNonceSize;
        var envelope = new byte[headerLength + EncryptionTagSize + plaintext.Length];
        var offset = 0;

        EncryptionMagic.CopyTo(envelope);
        offset += EncryptionMagic.Length;
        envelope[offset++] = EncryptionVersion;
        envelope[offset++] = mode;
        envelope[offset++] = checked((byte)salt.Length);
        salt.CopyTo(envelope.AsSpan(offset));
        offset += salt.Length;

        var nonce = envelope.AsSpan(offset, EncryptionNonceSize);
        RandomNumberGenerator.Fill(nonce);

        using var cipher = new AesGcm(key, EncryptionTagSize);
        cipher.Encrypt(
            nonce,
            plaintext,
            envelope.AsSpan(headerLength + EncryptionTagSize, plaintext.Length),
            envelope.AsSpan(headerLength, EncryptionTagSize),
            envelope.AsSpan(0, headerLength));
        return envelope;
    }

    private static void EnsureAuthenticatedEncryptionSupported()
    {
        if (!AesGcm.IsSupported)
            throw new PlatformNotSupportedException("Authenticated configuration encryption requires AES-GCM support.");
    }

    private static int FindAssignmentSeparator(string line)
    {
        var quoted = false;
        var escaped = false;

        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (quoted)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == '"')
                {
                    quoted = false;
                }
            }
            else if (character == '"')
            {
                quoted = true;
            }
            else if (character == '=')
            {
                return index;
            }
        }

        return -1;
    }

    private static bool IsSafeIdentifierCharacter(char character) =>
        char.IsLetterOrDigit(character) || character is '_' or '-' or '.' or '/' or ':';

    private static List<ParsedEntry> ParseDocument(string data)
    {
        var entries = new List<ParsedEntry>();
        var section = string.Empty;
        using var reader = new StringReader(data);

        for (var lineNumber = 1; reader.ReadLine() is { } rawLine; lineNumber++)
        {
            if (lineNumber == 1)
                rawLine = rawLine.TrimStart('\uFEFF');

            var line = rawLine.Trim();
            if (line.Length == 0 || line[0] == ';')
                continue;

            if (line[0] == '[')
            {
                if (line[^1] != ']')
                    throw new FormatException($"Configuration line {lineNumber} has an unterminated section header.");

                section = DecodeIdentifier(line[1..^1].Trim(), allowEmpty: true, lineNumber);
                continue;
            }

            var separator = FindAssignmentSeparator(line);
            if (separator < 0)
                throw new FormatException($"Configuration line {lineNumber} is not a key-value assignment.");

            var name = DecodeIdentifier(line[..separator].Trim(), allowEmpty: false, lineNumber);
            var valueToken = line[(separator + 1)..].Trim();
            if (valueToken.Length == 0)
                throw new FormatException($"Configuration line {lineNumber} has no value.");

            try
            {
                using var document = JsonDocument.Parse(valueToken);
                entries.Add(new ParsedEntry(
                    section,
                    name,
                    document.RootElement.ValueKind == JsonValueKind.Null
                        ? null
                        : JsonSerializer.Serialize(document.RootElement, ValueJsonOptions)));
            }
            catch (JsonException error)
            {
                throw new FormatException($"Configuration line {lineNumber} has an invalid JSON value.", error);
            }
        }

        return entries;
    }

    private static void ParseEnvelope(
        byte[] envelope,
        byte expectedMode,
        out int headerLength,
        out int saltOffset,
        out int saltLength,
        out int nonceOffset)
    {
        var minimumLength = EncryptionMagic.Length + 3 + EncryptionNonceSize + EncryptionTagSize;
        if (envelope.Length < minimumLength || !envelope.AsSpan(0, EncryptionMagic.Length).SequenceEqual(EncryptionMagic))
            throw new InvalidDataException("The file is not an Electron2D encrypted configuration envelope.");

        var offset = EncryptionMagic.Length;
        var version = envelope[offset++];
        var mode = envelope[offset++];
        saltLength = envelope[offset++];
        saltOffset = offset;

        if (version != EncryptionVersion)
            throw new InvalidDataException($"Encrypted configuration version {version} is not supported.");

        if (mode != expectedMode)
            throw new InvalidDataException("The encrypted configuration uses a different key mode.");

        var requiredSaltLength = mode == PasswordEncryption ? EncryptionSaltSize : 0;
        if (saltLength != requiredSaltLength)
            throw new InvalidDataException("The encrypted configuration has an invalid salt length.");

        nonceOffset = saltOffset + saltLength;
        headerLength = nonceOffset + EncryptionNonceSize;
        if (envelope.Length < headerLength + EncryptionTagSize)
            throw new InvalidDataException("The encrypted configuration envelope is truncated.");
    }

    private static void ValidateEncryptionKey(ReadOnlySpan<byte> key)
    {
        if (key.Length != EncryptionKeySize)
            throw new ArgumentException("An encrypted configuration key must contain exactly 32 bytes.", nameof(key));
    }

    private static void ValidatePassword(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        if (password.Length == 0)
            throw new ArgumentException("An encrypted configuration password cannot be empty.", nameof(password));
    }

    private static void ValidatePath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (path.Length == 0)
            throw new ArgumentException("A configuration file path cannot be empty.", nameof(path));
    }

    private string EncodeToTextLocked()
    {
        var builder = new StringBuilder();
        var firstSection = true;

        foreach (var sectionName in _sectionOrder)
        {
            var section = _sections[sectionName];
            if (!firstSection)
                builder.Append('\n');

            firstSection = false;
            if (sectionName.Length > 0)
            {
                builder.Append('[').Append(EncodeIdentifier(sectionName)).Append("]\n\n");
            }

            foreach (var keyName in section.KeyOrder)
            {
                builder.Append(EncodeIdentifier(keyName))
                    .Append('=')
                    .Append(section.Values[keyName])
                    .Append('\n');
            }
        }

        return builder.ToString();
    }

    private string GetSerializedValue<T>(ConfigKey<T> key)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (!_sections.TryGetValue(key.Section, out var section) ||
                !section.Values.TryGetValue(key.Name, out var serialized))
            {
                throw new KeyNotFoundException($"Configuration entry '{key}' does not exist.");
            }

            return serialized;
        }
    }

    private void MergeParsed(List<ParsedEntry> entries)
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            foreach (var entry in entries)
            {
                if (entry.SerializedValue is null)
                    RemoveValueIfPresentLocked(entry.Section, entry.Name);
                else
                    SetSerializedValueLocked(entry.Section, entry.Name, entry.SerializedValue);
            }
        }
    }

    private void RemoveSectionIfEmptyLocked(string sectionName, SectionState section)
    {
        if (section.Values.Count != 0)
            return;

        _sections.Remove(sectionName);
        _sectionOrder.Remove(sectionName);
    }

    private void RemoveValueIfPresentLocked(string sectionName, string keyName)
    {
        if (!_sections.TryGetValue(sectionName, out var section) || !section.Values.Remove(keyName))
            return;

        section.KeyOrder.Remove(keyName);
        RemoveSectionIfEmptyLocked(sectionName, section);
    }

    private void SetSerializedValueLocked(string sectionName, string keyName, string serialized)
    {
        if (!_sections.TryGetValue(sectionName, out var section))
        {
            section = new SectionState();
            _sections.Add(sectionName, section);
            if (sectionName.Length == 0)
                _sectionOrder.Insert(0, sectionName);
            else
                _sectionOrder.Add(sectionName);
        }

        if (section.Values.TryAdd(keyName, serialized))
            section.KeyOrder.Add(keyName);
        else
            section.Values[keyName] = serialized;
    }

    internal void SetSerializedValue<T>(ConfigKey<T> key, string? serialized)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(key);

        lock (_gate)
        {
            ThrowIfDisposed();
            if (serialized is null)
                RemoveValueIfPresentLocked(key.Section, key.Name);
            else
                SetSerializedValueLocked(key.Section, key.Name, serialized);
        }
    }

    internal void ReorderEntries(IReadOnlyList<(string Section, string Name)> preferredOrder)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(preferredOrder);

        lock (_gate)
        {
            ThrowIfDisposed();
            var orderedSections = new List<string>();
            var orderedKeys = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var seen = new HashSet<(string Section, string Name)>();

            void Add(string sectionName, string keyName)
            {
                if (!seen.Add((sectionName, keyName)) ||
                    !_sections.TryGetValue(sectionName, out var section) ||
                    !section.Values.ContainsKey(keyName))
                {
                    return;
                }

                if (!orderedKeys.TryGetValue(sectionName, out var keys))
                {
                    keys = [];
                    orderedKeys.Add(sectionName, keys);
                    orderedSections.Add(sectionName);
                }

                keys.Add(keyName);
            }

            foreach (var item in preferredOrder)
                Add(item.Section, item.Name);

            foreach (var sectionName in _sectionOrder)
            {
                foreach (var keyName in _sections[sectionName].KeyOrder)
                    Add(sectionName, keyName);
            }

            _sectionOrder.Clear();
            _sectionOrder.AddRange(orderedSections);
            foreach (var pair in orderedKeys)
            {
                var keys = _sections[pair.Key].KeyOrder;
                keys.Clear();
                keys.AddRange(pair.Value);
            }
        }
    }

    internal bool TryGetSerializedValue<T>(ConfigKey<T> key, [NotNullWhen(true)] out string? serialized)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_sections.TryGetValue(key.Section, out var section) &&
                section.Values.TryGetValue(key.Name, out serialized))
            {
                return true;
            }

            serialized = null;
            return false;
        }
    }

    private readonly record struct ParsedEntry(string Section, string Name, string? SerializedValue);

    private sealed class SectionState
    {
        internal List<string> KeyOrder { get; } = [];

        internal Dictionary<string, string> Values { get; } = new(StringComparer.Ordinal);
    }
}

internal sealed class ColorJsonConverter : JsonConverter<Color>
{
    private const int Red = 1;
    private const int Green = 2;
    private const int Blue = 4;
    private const int Alpha = 8;
    private const int Complete = Red | Green | Blue | Alpha;

    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("A color must be a JSON object.");

        var color = default(Color);
        var fields = 0;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("A color contains an invalid JSON token.");

            var propertyName = reader.GetString();
            var field = propertyName switch
            {
                nameof(Color.R) => Red,
                nameof(Color.G) => Green,
                nameof(Color.B) => Blue,
                nameof(Color.A) => Alpha,
                _ => throw new JsonException($"A color contains unknown field '{propertyName}'."),
            };
            if ((fields & field) != 0)
                throw new JsonException($"A color contains duplicate field '{propertyName}'.");
            if (!reader.Read() || reader.TokenType != JsonTokenType.Number || !reader.TryGetSingle(out var value) || !float.IsFinite(value))
                throw new JsonException($"Color field '{propertyName}' must be a finite number.");

            fields |= field;
            switch (field)
            {
                case Red:
                    color.R = value;
                    break;
                case Green:
                    color.G = value;
                    break;
                case Blue:
                    color.B = value;
                    break;
                case Alpha:
                    color.A = value;
                    break;
            }
        }

        if (reader.TokenType != JsonTokenType.EndObject)
            throw new JsonException("A color JSON object is incomplete.");
        if (fields != Complete)
            throw new JsonException("A color must contain exactly R, G, B, and A fields.");

        return color;
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        if (!float.IsFinite(value.R) || !float.IsFinite(value.G) || !float.IsFinite(value.B) || !float.IsFinite(value.A))
            throw new JsonException("Configuration colors require finite components.");

        writer.WriteStartObject();
        writer.WriteNumber(nameof(Color.R), value.R);
        writer.WriteNumber(nameof(Color.G), value.G);
        writer.WriteNumber(nameof(Color.B), value.B);
        writer.WriteNumber(nameof(Color.A), value.A);
        writer.WriteEndObject();
    }
}

internal sealed class Vector2JsonConverter : JsonConverter<Vector2>
{
    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        Vector2JsonFields.Read(ref reader, "Vector2", "value");

    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
    {
        if (!value.IsFinite())
            throw new JsonException("Configuration vectors require finite components.");

        writer.WriteStartObject();
        writer.WriteNumber(nameof(Vector2.X), value.X);
        writer.WriteNumber(nameof(Vector2.Y), value.Y);
        writer.WriteEndObject();
    }
}

internal sealed class Vector2IJsonConverter : JsonConverter<Vector2I>
{
    private const int XField = 1;
    private const int YField = 2;

    public override Vector2I Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("An integer vector must be a JSON object.");

        var fields = 0;
        var value = default(Vector2I);
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("An integer vector contains an invalid JSON token.");

            var propertyName = reader.GetString();
            var field = propertyName switch
            {
                nameof(Vector2I.X) => XField,
                nameof(Vector2I.Y) => YField,
                _ => throw new JsonException($"An integer vector contains unknown field '{propertyName}'."),
            };
            if ((fields & field) != 0)
                throw new JsonException($"An integer vector contains duplicate field '{propertyName}'.");
            if (!reader.Read() || reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out var component))
                throw new JsonException($"Integer vector field '{propertyName}' must be a 32-bit integer.");

            fields |= field;
            if (field == XField)
                value.X = component;
            else
                value.Y = component;
        }

        if (reader.TokenType != JsonTokenType.EndObject)
            throw new JsonException("An integer vector JSON object is incomplete.");
        if (fields != (XField | YField))
            throw new JsonException("An integer vector must contain exactly X and Y fields.");

        return value;
    }

    public override void Write(Utf8JsonWriter writer, Vector2I value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber(nameof(Vector2I.X), value.X);
        writer.WriteNumber(nameof(Vector2I.Y), value.Y);
        writer.WriteEndObject();
    }
}

internal sealed class Vector4JsonConverter : JsonConverter<Vector4>
{
    public override Vector4 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("A four-component vector must be a JSON object.");

        var fields = 0;
        var value = default(Vector4);
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("A four-component vector contains an invalid JSON token.");

            var propertyName = reader.GetString();
            var field = propertyName switch
            {
                nameof(Vector4.X) => 1,
                nameof(Vector4.Y) => 2,
                nameof(Vector4.Z) => 4,
                nameof(Vector4.W) => 8,
                _ => throw new JsonException($"A four-component vector contains unknown field '{propertyName}'."),
            };
            if ((fields & field) != 0)
                throw new JsonException($"A four-component vector contains duplicate field '{propertyName}'.");
            if (!reader.Read() || reader.TokenType != JsonTokenType.Number || !reader.TryGetSingle(out var component) || !float.IsFinite(component))
                throw new JsonException($"Four-component vector field '{propertyName}' must be a finite number.");

            fields |= field;
            switch (field)
            {
                case 1:
                    value.X = component;
                    break;
                case 2:
                    value.Y = component;
                    break;
                case 4:
                    value.Z = component;
                    break;
                case 8:
                    value.W = component;
                    break;
            }
        }

        if (reader.TokenType != JsonTokenType.EndObject)
            throw new JsonException("A four-component vector JSON object is incomplete.");
        if (fields != 15)
            throw new JsonException("A four-component vector must contain exactly X, Y, Z, and W fields.");
        return value;
    }

    public override void Write(Utf8JsonWriter writer, Vector4 value, JsonSerializerOptions options)
    {
        if (!value.IsFinite())
            throw new JsonException("Configuration vectors require finite components.");

        writer.WriteStartObject();
        writer.WriteNumber(nameof(Vector4.X), value.X);
        writer.WriteNumber(nameof(Vector4.Y), value.Y);
        writer.WriteNumber(nameof(Vector4.Z), value.Z);
        writer.WriteNumber(nameof(Vector4.W), value.W);
        writer.WriteEndObject();
    }
}

internal sealed class Vector4IJsonConverter : JsonConverter<Vector4I>
{
    public override Vector4I Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("A four-component integer vector must be a JSON object.");

        var fields = 0;
        var value = default(Vector4I);
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("A four-component integer vector contains an invalid JSON token.");

            var propertyName = reader.GetString();
            var field = propertyName switch
            {
                nameof(Vector4I.X) => 1,
                nameof(Vector4I.Y) => 2,
                nameof(Vector4I.Z) => 4,
                nameof(Vector4I.W) => 8,
                _ => throw new JsonException($"A four-component integer vector contains unknown field '{propertyName}'."),
            };
            if ((fields & field) != 0)
                throw new JsonException($"A four-component integer vector contains duplicate field '{propertyName}'.");
            if (!reader.Read() || reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out var component))
                throw new JsonException($"Four-component integer vector field '{propertyName}' must be a 32-bit integer.");

            fields |= field;
            switch (field)
            {
                case 1:
                    value.X = component;
                    break;
                case 2:
                    value.Y = component;
                    break;
                case 4:
                    value.Z = component;
                    break;
                case 8:
                    value.W = component;
                    break;
            }
        }

        if (reader.TokenType != JsonTokenType.EndObject)
            throw new JsonException("A four-component integer vector JSON object is incomplete.");
        if (fields != 15)
            throw new JsonException("A four-component integer vector must contain exactly X, Y, Z, and W fields.");
        return value;
    }

    public override void Write(Utf8JsonWriter writer, Vector4I value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber(nameof(Vector4I.X), value.X);
        writer.WriteNumber(nameof(Vector4I.Y), value.Y);
        writer.WriteNumber(nameof(Vector4I.Z), value.Z);
        writer.WriteNumber(nameof(Vector4I.W), value.W);
        writer.WriteEndObject();
    }
}

internal sealed class RectJsonConverter : JsonConverter<Rect>
{
    private const int Position = 1;
    private const int Size = 2;
    private const int Complete = Position | Size;

    public override Rect Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("A rectangle must be a JSON object.");

        var position = default(Vector2);
        var size = default(Vector2);
        var fields = 0;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("A rectangle contains an invalid JSON token.");

            var propertyName = reader.GetString();
            var field = propertyName switch
            {
                nameof(Rect.Position) => Position,
                nameof(Rect.Size) => Size,
                _ => throw new JsonException($"A rectangle contains unknown field '{propertyName}'."),
            };
            if ((fields & field) != 0)
                throw new JsonException($"A rectangle contains duplicate field '{propertyName}'.");
            if (!reader.Read())
                throw new JsonException($"Rectangle field '{propertyName}' is incomplete.");

            fields |= field;
            if (field == Position)
                position = Vector2JsonFields.Read(ref reader, "Rectangle", propertyName!);
            else
                size = Vector2JsonFields.Read(ref reader, "Rectangle", propertyName!);
        }

        if (reader.TokenType != JsonTokenType.EndObject)
            throw new JsonException("A rectangle JSON object is incomplete.");
        if (fields != Complete)
            throw new JsonException("A rectangle must contain exactly Position and Size fields.");

        return new Rect(position, size);
    }

    public override void Write(Utf8JsonWriter writer, Rect value, JsonSerializerOptions options)
    {
        if (!value.IsFinite())
            throw new JsonException("Configuration rectangles require finite components.");

        writer.WriteStartObject();
        Vector2JsonFields.Write(writer, nameof(Rect.Position), value.Position);
        Vector2JsonFields.Write(writer, nameof(Rect.Size), value.Size);
        writer.WriteEndObject();
    }
}

internal sealed class TransformJsonConverter : JsonConverter<Transform>
{
    private const int XAxis = 1;
    private const int YAxis = 2;
    private const int Origin = 4;
    private const int Complete = XAxis | YAxis | Origin;

    public override Transform Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("A transform must be a JSON object.");

        var transform = default(Transform);
        var fields = 0;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("A transform contains an invalid JSON token.");

            var propertyName = reader.GetString();
            var field = propertyName switch
            {
                nameof(Transform.X) => XAxis,
                nameof(Transform.Y) => YAxis,
                nameof(Transform.Origin) => Origin,
                _ => throw new JsonException($"A transform contains unknown field '{propertyName}'."),
            };
            if ((fields & field) != 0)
                throw new JsonException($"A transform contains duplicate field '{propertyName}'.");
            if (!reader.Read())
                throw new JsonException($"Transform field '{propertyName}' is incomplete.");

            fields |= field;
            transform[field switch
            {
                XAxis => 0,
                YAxis => 1,
                _ => 2,
            }] = Vector2JsonFields.Read(ref reader, "Transform", propertyName!);
        }

        if (reader.TokenType != JsonTokenType.EndObject)
            throw new JsonException("A transform JSON object is incomplete.");
        if (fields != Complete)
            throw new JsonException("A transform must contain exactly X, Y, and Origin fields.");

        return transform;
    }

    public override void Write(Utf8JsonWriter writer, Transform value, JsonSerializerOptions options)
    {
        if (!value.IsFinite())
            throw new JsonException("Configuration transforms require finite components.");

        writer.WriteStartObject();
        Vector2JsonFields.Write(writer, nameof(Transform.X), value.X);
        Vector2JsonFields.Write(writer, nameof(Transform.Y), value.Y);
        Vector2JsonFields.Write(writer, nameof(Transform.Origin), value.Origin);
        writer.WriteEndObject();
    }
}

internal static class Vector2JsonFields
{
    internal static Vector2 Read(ref Utf8JsonReader reader, string valueName, string fieldName)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"{valueName} field '{fieldName}' must be a vector object.");

        const int xField = 1;
        const int yField = 2;
        var fields = 0;
        var value = default(Vector2);
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException($"{valueName} field '{fieldName}' contains an invalid JSON token.");

            var componentName = reader.GetString();
            var component = componentName switch
            {
                nameof(Vector2.X) => xField,
                nameof(Vector2.Y) => yField,
                _ => throw new JsonException($"{valueName} field '{fieldName}' contains unknown component '{componentName}'."),
            };
            if ((fields & component) != 0)
                throw new JsonException($"{valueName} field '{fieldName}' contains duplicate component '{componentName}'.");
            if (!reader.Read() || reader.TokenType != JsonTokenType.Number || !reader.TryGetSingle(out var number) || !float.IsFinite(number))
                throw new JsonException($"{valueName} component '{fieldName}.{componentName}' must be a finite number.");

            fields |= component;
            if (component == xField)
                value.X = number;
            else
                value.Y = number;
        }

        if (reader.TokenType != JsonTokenType.EndObject)
            throw new JsonException($"{valueName} field '{fieldName}' is incomplete.");
        if (fields != (xField | yField))
            throw new JsonException($"{valueName} field '{fieldName}' must contain exactly X and Y components.");

        return value;
    }

    internal static void Write(Utf8JsonWriter writer, string propertyName, Vector2 value)
    {
        writer.WriteStartObject(propertyName);
        writer.WriteNumber(nameof(Vector2.X), value.X);
        writer.WriteNumber(nameof(Vector2.Y), value.Y);
        writer.WriteEndObject();
    }
}
