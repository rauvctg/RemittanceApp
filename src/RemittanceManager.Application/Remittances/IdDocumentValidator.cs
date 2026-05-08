using System.Text.RegularExpressions;
using RemittanceManager.Application.Common.Exceptions;
using RemittanceManager.Application.Common.Constants;
using RemittanceManager.Domain.Enums;

namespace RemittanceManager.Application.Remittances;

internal static class IdDocumentValidator
{
    private sealed class Rule
    {
        public int MinLength { get; init; }
        public int MaxLength { get; init; }
        public Regex Pattern { get; init; } = default!;
        public string Hint { get; init; } = string.Empty;
    }

    private static readonly Regex Digits = new("^\\d+$", RegexOptions.Compiled);
    private static readonly Regex Alphanumeric = new("^[A-Za-z0-9]+$", RegexOptions.Compiled);

    private static readonly Dictionary<IdDocumentType, Rule> Rules = new()
    {
        [IdDocumentType.CC]       = new Rule { MinLength = 6,  MaxLength = 10, Pattern = Digits,           Hint = "6 to 10 digits" },
        [IdDocumentType.CE]       = new Rule { MinLength = 6,  MaxLength = 7,  Pattern = Digits,           Hint = "6 to 7 digits" },
        [IdDocumentType.Passport] = new Rule { MinLength = 6,  MaxLength = 12, Pattern = Alphanumeric,     Hint = "6 to 12 alphanumeric characters" },
        [IdDocumentType.DNI]      = new Rule { MinLength = 7,  MaxLength = 9,  Pattern = Digits,           Hint = "7 to 9 digits" }
    };

    public static void Validate(string fieldName, IdDocumentType type, string value)
    {
        var rule = Rules[type];
        var v = value ?? string.Empty;

        var errors = new List<string>();
        if (v.Length < rule.MinLength || v.Length > rule.MaxLength)
            errors.Add(string.Format(ValidationMessages.LengthRange, rule.MinLength, rule.MaxLength));
        if (!rule.Pattern.IsMatch(v))
            errors.Add(string.Format(ValidationMessages.InvalidFormat, rule.Hint));

        if (errors.Count > 0)
        {
            var dict = new Dictionary<string, string[]> { [fieldName] = errors.ToArray() };
            throw new ValidationException(string.Format(ValidationMessages.InvalidIdFormat, fieldName, type), dict);
        }
    }
}
