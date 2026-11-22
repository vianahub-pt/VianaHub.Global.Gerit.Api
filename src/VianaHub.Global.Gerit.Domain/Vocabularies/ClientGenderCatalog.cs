using VianaHub.Global.Gerit.Domain.Enums;

namespace VianaHub.Global.Gerit.Domain.Vocabularies;

public static class ClientGenderCatalog
{
    public static IReadOnlyList<string> Codes { get; } = Array.AsReadOnly(new[]
    {
        "male", "female", "non-binary", "prefer-not-to-say"
    });

    public static bool IsCanonical(string? value) => value is null || TryParse(value, out _);

    public static bool TryParse(string? value, out ClientGender gender)
    {
        gender = value switch
        {
            "male" => ClientGender.Male,
            "female" => ClientGender.Female,
            "non-binary" => ClientGender.NonBinary,
            "prefer-not-to-say" => ClientGender.PreferNotToSay,
            _ => default
        };
        return gender != default;
    }

    public static string? ToCode(ClientGender gender) => gender switch
    {
        ClientGender.Male => "male",
        ClientGender.Female => "female",
        ClientGender.NonBinary => "non-binary",
        ClientGender.PreferNotToSay => "prefer-not-to-say",
        _ => null
    };
}
