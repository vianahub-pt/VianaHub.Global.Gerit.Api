using Microsoft.AspNetCore.Identity;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using VianaHub.Global.Gerit.Domain.Tools.Cryptography;
using FluentValidation;
using VianaHub.Global.Gerit.Domain.Tools.Notifications;

namespace VianaHub.Global.Gerit.Domain.Helpers;

public static class DomainExtensions
{
    // PasswordHasher para usar PBKDF2 (ASP.NET Core Identity)
    private static readonly PasswordHasher<object> _passwordHasher = new();

    // Limites de segurança para upload de CSV
    private const long MAX_CSV_FILE_SIZE = 10 * 1024 * 1024; // 10 MB
    private const int MAX_CSV_ROWS = 10000;
    private const int MAX_CSV_FIELD_LENGTH = 5000;

    #region CSV Security Extensions

    /// <summary>
    /// Sanitiza uma string contra CSV Injection (Formula Injection).
    /// Remove caracteres perigosos do início que poderiam executar fórmulas no Excel/LibreOffice.
    /// </summary>
    /// <param name="value">Valor a ser sanitizado</param>
    /// <returns>String segura para uso em CSV</returns>
    public static string SanitizeCsvValue(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // Remove espaços em branco do início e fim
        value = value.Trim();

        // Caracteres perigosos que podem iniciar fórmulas
        char[] dangerousChars = ['=', '+', '-', '@', '\t', '\r', '\n', '|'];

        // Remove caracteres perigosos do início
        while (value.Length > 0 && dangerousChars.Contains(value[0]))
        {
            value = value.Substring(1);
        }

        // Se o valor ainda começar com caractere perigoso após trim, adiciona aspas simples
        if (value.Length > 0 && dangerousChars.Contains(value[0]))
        {
            value = "'" + value;
        }

        // Remove caracteres de controle ASCII (0x00-0x1F exceto tab e newline já tratados)
        value = Regex.Replace(value, @"[\x00-\x08\x0B\x0C\x0E-\x1F]", "", RegexOptions.None, TimeSpan.FromMilliseconds(100));

        // Limita o tamanho do campo
        if (value.Length > MAX_CSV_FIELD_LENGTH)
        {
            value = value.Substring(0, MAX_CSV_FIELD_LENGTH);
        }

        return value;
    }

    /// <summary>
    /// Sanitiza uma string contra ataques de injeção e normaliza encoding.
    /// Combina proteção contra CSV Injection, XSS e garante UTF-8 correto.
    /// </summary>
    /// <param name="value">Valor a ser sanitizado</param>
    /// <returns>String segura e normalizada</returns>
    public static string SanitizeCsvInput(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        // 1. Sanitiza contra CSV Injection
        value = value.SanitizeCsvValue();

        // 2. Remove sequências de escape perigosas
        value = value.Replace("\0", ""); // Null byte

        // 3. Normaliza espaços em branco múltiplos
        value = Regex.Replace(value, @"\s+", " ", RegexOptions.None, TimeSpan.FromMilliseconds(100));

        // 4. Remove caracteres invisíveis Unicode (Zero-Width, Right-to-Left, etc)
        value = Regex.Replace(value, @"[\u200B-\u200F\u202A-\u202E\u2060-\u2069\uFEFF]", "", RegexOptions.None, TimeSpan.FromMilliseconds(100));

        return value.Trim();
    }

    /// <summary>
    /// Valida se uma string contém apenas caracteres seguros para CSV.
    /// Detecta tentativas de injeção de fórmulas ou scripts.
    /// </summary>
    /// <param name="value">Valor a ser validado</param>
    /// <returns>True se o valor é seguro, False caso contrário</returns>
    public static bool IsSafeCsvValue(this string? value)
    {
        if (string.IsNullOrEmpty(value))
            return true;

        // Verifica se começa com caracteres perigosos
        char[] dangerousStarts = ['=', '+', '-', '@'];
        if (dangerousStarts.Contains(value.TrimStart()[0]))
            return false;

        // Verifica se contém sequências perigosas
        string[] dangerousPatterns =
        [
            "javascript:",
            "data:",
            "vbscript:",
            "<script",
            "onclick=",
            "onerror=",
            "onload=",
            "cmd|",
            "powershell",
            "../../"
        ];

        string lowerValue = value.ToLowerInvariant();
        foreach (var pattern in dangerousPatterns)
        {
            if (lowerValue.Contains(pattern))
                return false;
        }

        // Verifica se contém caracteres de controle perigosos
        if (Regex.IsMatch(value, @"[\x00-\x08\x0B\x0C\x0E-\x1F]", RegexOptions.None, TimeSpan.FromMilliseconds(100)))
            return false;

        return true;
    }

    /// <summary>
    /// Valida o tamanho de um arquivo CSV.
    /// </summary>
    /// <param name="fileSize">Tamanho do arquivo em bytes</param>
    /// <returns>True se o tamanho é válido, False caso contrário</returns>
    public static bool IsValidCsvFileSize(this long fileSize)
    {
        return fileSize > 0 && fileSize <= MAX_CSV_FILE_SIZE;
    }

    /// <summary>
    /// Valida a extensão de um arquivo.
    /// </summary>
    /// <param name="fileName">Nome do arquivo</param>
    /// <param name="allowedExtensions">Extensões permitidas (padrão: .csv)</param>
    /// <returns>True se a extensão é válida, False caso contrário</returns>
    public static bool HasValidCsvExtension(this string fileName, params string[] allowedExtensions)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        if (allowedExtensions == null || allowedExtensions.Length == 0)
            allowedExtensions = [".csv"];

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return allowedExtensions.Contains(extension);
    }

    /// <summary>
    /// Valida o nome do arquivo contra Path Traversal attacks.
    /// </summary>
    /// <param name="fileName">Nome do arquivo</param>
    /// <returns>True se o nome é seguro, False caso contrário</returns>
    public static bool IsSafeCsvFileName(this string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        // Verifica se contém caracteres perigosos
        char[] dangerousChars = ['/', '\\', ':', '*', '?', '"', '<', '>', '|', '\0'];
        if (fileName.IndexOfAny(dangerousChars) >= 0)
            return false;

        // Verifica Path Traversal
        if (fileName.Contains(".."))
            return false;

        // Verifica se o nome do arquivo é diferente do caminho completo
        var safeName = Path.GetFileName(fileName);
        if (safeName != fileName)
            return false;

        return true;
    }

    /// <summary>
    /// Normaliza uma string para UTF-8 correto, removendo BOM e garantindo encoding correto.
    /// </summary>
    /// <param name="value">Valor a ser normalizado</param>
    /// <returns>String com encoding UTF-8 correto</returns>
    public static string NormalizeUtf8(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // Remove BOM (Byte Order Mark) se existir
        value = value.TrimStart('\uFEFF', '\uFFFE');

        // Converte para bytes UTF-8 e volta para string
        // Isso garante que caracteres mal codificados sejam normalizados
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        string normalized = Encoding.UTF8.GetString(bytes);

        return normalized;
    }

    /// <summary>
    /// Cria um StreamReader com encoding UTF-8 forçado e sem detecção de BOM.
    /// </summary>
    /// <param name="stream">Stream a ser lido</param>
    /// <returns>StreamReader configurado com UTF-8</returns>
    public static StreamReader CreateUtf8StreamReader(this Stream stream)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        var encoding = new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true
        );

        return new StreamReader(
            stream,
            encoding,
            detectEncodingFromByteOrderMarks: true, // 🔥 ESSENCIAL
            bufferSize: 4096,
            leaveOpen: false
        );
    }


    /// <summary>
    /// Valida se um stream de arquivo está em encoding UTF-8 válido.
    /// </summary>
    /// <param name="stream">Stream do arquivo a ser validado</param>
    /// <returns>True se o arquivo está em UTF-8, False caso contrário</returns>
    public static bool IsValidUtf8Encoding(this Stream stream)
    {
        if (stream == null)
            return false;

        try
        {
            // Salva a posição original do stream
            long originalPosition = stream.Position;
            stream.Position = 0;

            // Tenta ler todo o stream como UTF-8 com validação rigorosa
            var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

            using (var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true))
            {
                // Lê todo o conteúdo para forçar a validação de encoding
                _ = reader.ReadToEnd();
            }

            // Restaura a posição original do stream
            stream.Position = originalPosition;
            return true;
        }
        catch (DecoderFallbackException)
        {
            // Exceção lançada quando há bytes inválidos para UTF-8
            // Restaura a posição original do stream antes de retornar
            try { stream.Position = 0; } catch { }
            return false;
        }
        catch
        {
            // Qualquer outro erro também indica problema com o encoding
            try { stream.Position = 0; } catch { }
            return false;
        }
    }

    /// <summary>
    /// Valida se o número de linhas do CSV está dentro do limite permitido.
    /// </summary>
    /// <param name="rowCount">Número de linhas</param>
    /// <returns>True se está dentro do limite, False caso contrário</returns>
    public static bool IsValidCsvRowCount(this int rowCount)
    {
        return rowCount > 0 && rowCount <= MAX_CSV_ROWS;
    }

    /// <summary>
    /// Obtém o limite máximo de linhas permitidas em um CSV.
    /// </summary>
    /// <returns>Número máximo de linhas</returns>
    public static int GetMaxCsvRows()
    {
        return MAX_CSV_ROWS;
    }

    /// <summary>
    /// Obtém o limite máximo de tamanho de arquivo CSV em bytes.
    /// </summary>
    /// <returns>Tamanho máximo em bytes</returns>
    public static long GetMaxCsvFileSize()
    {
        return MAX_CSV_FILE_SIZE;
    }

    /// <summary>
    /// Obtém o limite máximo de tamanho de arquivo CSV formatado (ex: "10 MB").
    /// </summary>
    /// <returns>Tamanho máximo formatado</returns>
    public static string GetMaxCsvFileSizeFormatted()
    {
        return $"{MAX_CSV_FILE_SIZE / (1024 * 1024)} MB";
    }

    /// <summary>
    /// Valida se um GUID em string é válido e não vazio.
    /// </summary>
    /// <param name="value">String a ser validada</param>
    /// <param name="guid">GUID resultante se válido</param>
    /// <returns>True se é um GUID válido e não vazio, False caso contrário</returns>
    public static bool IsValidNonEmptyGuid(this string value, out Guid guid)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            guid = Guid.Empty;
            return false;
        }

        if (Guid.TryParse(value, out guid))
        {
            return guid != Guid.Empty;
        }

        return false;
    }

    /// <summary>
    /// Sanitiza múltiplos campos de uma só vez.
    /// </summary>
    /// <param name="fields">Dicionário com campos a serem sanitizados</param>
    /// <returns>Dicionário com campos sanitizados</returns>
    public static Dictionary<string, string>? SanitizeCsvFields(this Dictionary<string, string>? fields)
    {
        if (fields == null || fields.Count == 0)
            return fields;

        var sanitized = new Dictionary<string, string>();
        foreach (var kvp in fields)
        {
            sanitized[kvp.Key] = kvp.Value.SanitizeCsvInput();
        }

        return sanitized;
    }

    /// <summary>
    /// Remove espaços em branco excessivos de uma string preservando estrutura.
    /// </summary>
    /// <param name="value">Valor a ser normalizado</param>
    /// <returns>String com espaços normalizados</returns>
    public static string NormalizeWhitespace(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        // Remove espaços múltiplos, tabs e quebras de linha no meio do texto
        value = Regex.Replace(value, @"\s+", " ", RegexOptions.None, TimeSpan.FromMilliseconds(100));

        return value.Trim();
    }

    /// <summary>
    /// Valida se uma string contém apenas caracteres alfanuméricos, espaços e pontuação básica.
    /// </summary>
    /// <param name="value">Valor a ser validado</param>
    /// <param name="allowSpecialChars">Permite caracteres especiais adicionais</param>
    /// <returns>True se contém apenas caracteres seguros, False caso contrário</returns>
    public static bool ContainsOnlySafeCharacters(this string value, bool allowSpecialChars = true)
    {
        if (string.IsNullOrEmpty(value))
            return true;

        if (allowSpecialChars)
        {
            // Permite letras, números, espaços e pontuação básica
            return Regex.IsMatch(value, @"^[\w\s\-.,;:!?()\[\]{}""']+$", RegexOptions.None, TimeSpan.FromMilliseconds(100));
        }
        else
        {
            // Apenas alfanuméricos e espaços
            return Regex.IsMatch(value, @"^[\w\s]+$", RegexOptions.None, TimeSpan.FromMilliseconds(100));
        }
    }

    #endregion

    public static string GetDescription<TEnum>(this int value)
    {
        // Verifica se TEnum é realmente um enum
        if (!typeof(TEnum).IsEnum)
            throw new ArgumentException("TEnum deve ser um enum.");

        if (Enum.IsDefined(typeof(TEnum), value))
        {
            var enumValue = (Enum)Enum.ToObject(typeof(TEnum), value);
            return enumValue.GetDescription(); // Usa o método para Enum abaixo
        }

        return value.ToString();
    }

    public static string GetDescription(this Enum input)
    {
        if (input == null)
            return string.Empty;

        var type = input.GetType();
        var memInfo = type.GetMember(input.ToString());

        if (memInfo.Length > 0)
        {
            var attrs = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attrs.Length > 0)
                return ((DescriptionAttribute)attrs[0]).Description;
        }

        return input.ToString();
    }

    public static TEnum GetEnumByDescription<TEnum>(string description)
    {
        Type type = typeof(TEnum);

        if (!type.IsEnum)
            throw new ArgumentException("TEnum deve ser um tipo enum.");

        foreach (var field in type.GetFields())
        {
            var attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;

            if (attribute != null)
            {
                if (attribute.Description.Equals(description, StringComparison.InvariantCultureIgnoreCase))
                    return (TEnum)(field.GetValue(null) ?? throw new InvalidOperationException("Enum field has no value."));
            }
            else
            {
                if (field.Name.Equals(description, StringComparison.InvariantCultureIgnoreCase))
                    return (TEnum)(field.GetValue(null) ?? throw new InvalidOperationException("Enum field has no value."));
            }
        }

        throw new ArgumentException(
            string.Format("Nenhum valor do enum {0} corresponde à descrição '{1}'.", type, description),
            nameof(description)
        );
    }

    public static string GenerateCode(this string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        // Remove espaços extras e separa palavras
        var words = name
            .Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 1)
            return words[0].ToLowerInvariant();

        string first = words.First().ToLowerInvariant();
        string last = words.Last().ToLowerInvariant();

        return $"{first}-{last}";
    }

    public static async Task<string> CreateShortId()
    {
        DateTime currentDate = DateTime.Now;
        string dayOfMonth = currentDate.Day.ToString("00");
        string monthAbbreviated = currentDate.ToString("MMM", new CultureInfo("pt-BR"))[..3].ToUpper();
        string hashRandom = GenerateHexadecimalSequence();
        string shortId = $"{dayOfMonth}{monthAbbreviated}-{hashRandom}";

        return await Task.Run(() => shortId);
    }

    public static string? FirstCharToUpper(this string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        string[] excecoes = ["e", "de", "da", "das", "do", "dos"];
        var palavras = new Queue<string>();
        foreach (var palavra in input.Split(' '))
        {
            if (!string.IsNullOrEmpty(palavra))
            {
                var emMinusculo = palavra.ToLower();
                var letras = emMinusculo.ToCharArray();
                if (!excecoes.Contains(emMinusculo)) letras[0] = char.ToUpper(letras[0]);
                palavras.Enqueue(new string(letras));
            }
        }
        return string.Join(" ", palavras);
    }

    public static async Task<bool> IsNumeric(this string input)
    {
        return await Task.Run(() => double.TryParse(input, out _));
    }
    public static async Task<bool> IsInt(this string input)
    {
        return await Task.Run(() => int.TryParse(input, out _));
    }

    public static async Task<bool> IsGuid(this string input)
    {
        return await Task.Run(() => Guid.TryParseExact(input, "D", out _));
    }

    public static async Task<bool> IsShortId(this string input)
    {
        var result = true;

        if (input.Length != 16) result = false;

        string dayOfMonth = input[..2];
        string monthAbbreviated = input.Substring(2, 3);
        string hashRandom = input[6..];

        if (!int.TryParse(dayOfMonth, out int dia) || dia < 1 || dia > 31) result = false;

        if (monthAbbreviated.Length != 3 || !monthAbbreviated.All(char.IsLetter) || !monthAbbreviated.All(char.IsUpper)) result = false;

        if (!ValidateHexadecimalSequence(hashRandom)) result = false;

        return await Task.FromResult(result);
    }

    public static async Task<bool> IsEmail(this string input)
    {
        return await Task.Run(() => new EmailAddressAttribute().IsValid(input));
    }

    public static string ToPhoneFormatPortugal(this string input)
    {
        var formattedPhone = string.Empty;

        if (!string.IsNullOrWhiteSpace(input))
        {
            string digits = Regex.Replace(input, @"\D", "");
            if (digits.Length != 9)
            {
                return input;
            }

            formattedPhone = Regex.Replace(digits, @"(\d{3})(\d{3})(\d{3})", "$1 $2 $3");
        }

        return formattedPhone;
    }

    public static DateTime LastTimeDay(this DateTime input)
    {
        return input.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
    }

    public static string StringShortDate(this DateTime input)
    {
        return input.ToString("dd/MM/yyyy");
    }

    public static string RemoveAccentsAndSpecialCharacters(this string input)
    {
        /** Troca os caracteres acentuados por não acentuados **/
        string[] acentos = ["ç", "Ç", "á", "é", "í", "ó", "ú", "ý", "Á", "É", "Í", "Ó", "Ú", "Ý", "à", "è", "ì", "ò", "ù", "À", "È", "Ì", "Ò", "Ù", "ã", "õ", "ñ", "ä", "ë", "ï", "ö", "ü", "ÿ", "Ä", "Ë", "Ï", "Ö", "Ü", "Ã", "Õ", "Ñ", "â", "ê", "î", "ô", "û", "Â", "Ê", "Î", "Ô", "Û"];
        string[] semAcento = ["c", "C", "a", "e", "i", "o", "u", "y", "A", "E", "I", "O", "U", "Y", "a", "e", "i", "o", "u", "A", "E", "I", "O", "U", "a", "o", "n", "a", "e", "i", "o", "u", "y", "A", "E", "I", "O", "U", "A", "O", "N", "a", "e", "i", "o", "u", "A", "E", "I", "O", "U"];

        for (int i = 0; i < acentos.Length; i++)
        {
            input = input.Replace(acentos[i], semAcento[i]);
        }
        /** Troca os caracteres especiais da string por "" **/
        string[] caracteresEspeciais = ["¹", "²", "³", "£", "¢", "¬", "º", "¨", "\"", "'", ".", ",", "-", ":", "(", ")", "ª", "|", "\\\\", "°", "_", "@", "#", "!", "$", "%", "&", "*", ";", "/", "<", ">", "?", "[", "]", "{", "}", "=", "+", "§", "´", "`", "^", "~"];

        for (int i = 0; i < caracteresEspeciais.Length; i++)
        {
            input = input.Replace(caracteresEspeciais[i], "");
        }

        /** Troca os caracteres especiais da string por " " **/
        input = Regex.Replace(input, @"[^\w\.@-]", " ", RegexOptions.None, TimeSpan.FromSeconds(1.5));

        return input.Trim();
    }

    public static string Encrypt(this string input)
    {
        return CryptoMD5.Encrypt(input);
    }

    public static bool DateMajorEqualNow(this DateTime input)
    {
        return input >= DateTime.Now;
    }

    public static bool NotEmpty(this string input)
    {
        return input != null;
    }

    public static byte[] ToByteArray(this string base64String)
    {
        if (string.IsNullOrEmpty(base64String))
        {
            throw new ArgumentException("A string Base64 não pode ser nula ou vazia.", nameof(base64String));
        }

        return Convert.FromBase64String(base64String);
    }

    public static string ToBase64(this byte[] byteArray)
    {
        if (byteArray == null || byteArray.Length == 0)
        {
            throw new ArgumentException("O array de bytes não pode ser nulo ou vazio.", nameof(byteArray));
        }

        return Convert.ToBase64String(byteArray);
    }

    public static byte[] ToByteArrayFileImage(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
        {
            throw new ArgumentException("Caminho da imagem inválido ou a imagem não existe.", nameof(imagePath));
        }

        return File.ReadAllBytes(imagePath);
    }

    public static string GenerateHexadecimalSequence()
    {
        // Gerar 4 bytes aleatórios
        byte[] bytesAleatorios = new byte[4];
        new Random().NextBytes(bytesAleatorios);

        // Calcular a soma dos bytes
        int soma = 0;
        foreach (byte b in bytesAleatorios)
        {
            soma += b;
        }

        // Calcular os dígitos verificadores
        byte digito1 = (byte)(soma % 16);
        byte digito2 = (byte)((soma + digito1) % 16);

        // Converter os bytes para uma sequência hexadecimal
        string sequenciaHex = BitConverter.ToString(bytesAleatorios).Replace("-", "");

        // Adicionar os dígitos verificadores aos últimos dois caracteres
        sequenciaHex = string.Concat(sequenciaHex, digito1.ToString("X"), digito2.ToString("X"));

        return sequenciaHex;
    }

    public static bool ValidateHexadecimalSequence(string sequencia)
    {
        // Verificar se a string possui o formato correto
        if (sequencia.Length != 10)
        {
            return false;
        }

        // Extrair os bytes e dígitos verificadores da sequência
        byte[] bytes = new byte[4];
        for (int i = 0; i < 4; i++)
        {
            int startIndex = i * 2;
            if (startIndex + 2 <= sequencia.Length)
            {
                bytes[i] = Convert.ToByte(sequencia.Substring(startIndex, 2), 16);
            }
            else
            {
                return false; // Sequência não possui bytes suficientes
            }
        }

        byte digito1 = Convert.ToByte(sequencia.Substring(8, 1), 16);
        byte digito2 = Convert.ToByte(sequencia.Substring(9, 1), 16);

        // Calcular a soma dos bytes
        int soma = 0;
        foreach (byte b in bytes)
        {
            soma += b;
        }

        // Verificar os dígitos verificadores
        return (soma % 16 == digito1) && ((soma + digito1) % 16 == digito2);
    }

    /// <summary>
    /// Gera um client secret seguro usando RNG e codifica em Base64 URL-safe.
    /// </summary>
    /// <param name="length">Tamanho em bytes do secret (padrão: 48 bytes = ~64 caracteres em Base64)</param>
    /// <returns>Client secret em Base64 URL-safe</returns>
    public static string GenerateClientSecret(int length = 48)
    {
        if (length <= 0) length = 48;

        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);

        string base64 = Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        return base64;
    }

    /// <summary>
    /// Faz hash de um client secret usando PBKDF2-HMAC-SHA256 (via ASP.NET Core Identity PasswordHasher).
    /// Usa 10.000 iterações por padrão, com salt único gerado automaticamente.
    /// Formato: PBKDF2-HMAC-SHA256, 256-bit hash, 128-bit salt, 10.000 iterações.
    /// </summary>
    /// <param name="secret">O client secret em texto claro</param>
    /// <returns>Hash no formato do ASP.NET Core Identity (V3: inclui versão, iterações e salt)</returns>
    public static string HashClientSecret(string? secret)
    {
        if (string.IsNullOrEmpty(secret))
            throw new ArgumentException("Secret cannot be null or empty.", nameof(secret));

        // ASP.NET Core Identity PasswordHasher usa PBKDF2-HMAC-SHA256 com 10.000 iterações
        // Formato do hash: AQAAAAEAACcQAAAAE...
        return _passwordHasher.HashPassword(new object(), secret);
    }

    /// <summary>
    /// Verifica se um client secret corresponde ao hash armazenado usando PBKDF2.
    /// Suporta rehashing automático se o formato do hash for de uma versão antiga.
    /// </summary>
    /// <param name="storedHash">O hash armazenado no banco de dados</param>
    /// <param name="providedSecret">O secret fornecido pelo cliente</param>
    /// <returns>True se o secret é válido, false caso contrário</returns>
    public static bool VerifyClientSecret(string? storedHash, string? providedSecret)
    {
        if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(providedSecret))
            return false;

        try
        {
            var result = _passwordHasher.VerifyHashedPassword(new object(), storedHash, providedSecret);
            return result == PasswordVerificationResult.Success ||
                   result == PasswordVerificationResult.SuccessRehashNeeded;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> NotifyValidationErrorsAsync<T>(this IValidator<T> validator, T request, INotify notify, CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                notify.Add(error.ErrorMessage, 400);
            }
            return false;
        }
        return true;
    }

    #region Private Methods

    private static string GetLayer()
    {
        return $"{GetCallingLayerName()}";
    }

    private static string GetMethod()
    {
        return $"{GetCallingMethodName()}";
    }

    public static string? GetCallingLayerName()
    {
        // Obtendo o método chamador
        MethodBase? callingMethod = new StackTrace().GetFrame(4)?.GetMethod();

        // Retornando o nome do método chamador
        return callingMethod?.DeclaringType?.Assembly.GetName().Name;

    }
    public static string? GetCallingMethodName()
    {
        // Obtendo o método chamador
        MethodBase? callingMethod = new StackTrace().GetFrame(4)?.GetMethod();

        // Retornando o nome do método chamador
        return callingMethod?.Name.Trim();
    }

    #endregion
}

