namespace VianaHub.Global.Gerit.Domain.Vocabularies;

public sealed record CountryCode(string Code, string Region, string LocalizationKey);

/// <summary>
/// Países ISO alpha-2 das regiões Europa e Américas da ONU M49.
/// Lista versionada: membros da ONU e Santa Sé, sem territórios dependentes.
/// </summary>
public static class ClientNationalityCatalog
{
    public const string Version = "2026-11-21";

    public static IReadOnlyList<CountryCode> Entries { get; } = Array.AsReadOnly<CountryCode>(
    [
        new("AD", "Europe", "Client.Nationality.AD"),
        new("AG", "Americas", "Client.Nationality.AG"),
        new("AL", "Europe", "Client.Nationality.AL"),
        new("AR", "Americas", "Client.Nationality.AR"),
        new("AT", "Europe", "Client.Nationality.AT"),
        new("BA", "Europe", "Client.Nationality.BA"),
        new("BB", "Americas", "Client.Nationality.BB"),
        new("BE", "Europe", "Client.Nationality.BE"),
        new("BG", "Europe", "Client.Nationality.BG"),
        new("BO", "Americas", "Client.Nationality.BO"),
        new("BR", "Americas", "Client.Nationality.BR"),
        new("BS", "Americas", "Client.Nationality.BS"),
        new("BY", "Europe", "Client.Nationality.BY"),
        new("BZ", "Americas", "Client.Nationality.BZ"),
        new("CA", "Americas", "Client.Nationality.CA"),
        new("CH", "Europe", "Client.Nationality.CH"),
        new("CL", "Americas", "Client.Nationality.CL"),
        new("CO", "Americas", "Client.Nationality.CO"),
        new("CR", "Americas", "Client.Nationality.CR"),
        new("CU", "Americas", "Client.Nationality.CU"),
        new("CZ", "Europe", "Client.Nationality.CZ"),
        new("DE", "Europe", "Client.Nationality.DE"),
        new("DK", "Europe", "Client.Nationality.DK"),
        new("DM", "Americas", "Client.Nationality.DM"),
        new("DO", "Americas", "Client.Nationality.DO"),
        new("EC", "Americas", "Client.Nationality.EC"),
        new("EE", "Europe", "Client.Nationality.EE"),
        new("ES", "Europe", "Client.Nationality.ES"),
        new("FI", "Europe", "Client.Nationality.FI"),
        new("FR", "Europe", "Client.Nationality.FR"),
        new("GB", "Europe", "Client.Nationality.GB"),
        new("GD", "Americas", "Client.Nationality.GD"),
        new("GR", "Europe", "Client.Nationality.GR"),
        new("GT", "Americas", "Client.Nationality.GT"),
        new("GY", "Americas", "Client.Nationality.GY"),
        new("HN", "Americas", "Client.Nationality.HN"),
        new("HR", "Europe", "Client.Nationality.HR"),
        new("HT", "Americas", "Client.Nationality.HT"),
        new("HU", "Europe", "Client.Nationality.HU"),
        new("IE", "Europe", "Client.Nationality.IE"),
        new("IS", "Europe", "Client.Nationality.IS"),
        new("IT", "Europe", "Client.Nationality.IT"),
        new("JM", "Americas", "Client.Nationality.JM"),
        new("KN", "Americas", "Client.Nationality.KN"),
        new("LC", "Americas", "Client.Nationality.LC"),
        new("LI", "Europe", "Client.Nationality.LI"),
        new("LT", "Europe", "Client.Nationality.LT"),
        new("LU", "Europe", "Client.Nationality.LU"),
        new("LV", "Europe", "Client.Nationality.LV"),
        new("MC", "Europe", "Client.Nationality.MC"),
        new("MD", "Europe", "Client.Nationality.MD"),
        new("ME", "Europe", "Client.Nationality.ME"),
        new("MK", "Europe", "Client.Nationality.MK"),
        new("MT", "Europe", "Client.Nationality.MT"),
        new("MX", "Americas", "Client.Nationality.MX"),
        new("NI", "Americas", "Client.Nationality.NI"),
        new("NL", "Europe", "Client.Nationality.NL"),
        new("NO", "Europe", "Client.Nationality.NO"),
        new("PA", "Americas", "Client.Nationality.PA"),
        new("PE", "Americas", "Client.Nationality.PE"),
        new("PL", "Europe", "Client.Nationality.PL"),
        new("PT", "Europe", "Client.Nationality.PT"),
        new("PY", "Americas", "Client.Nationality.PY"),
        new("RO", "Europe", "Client.Nationality.RO"),
        new("RS", "Europe", "Client.Nationality.RS"),
        new("RU", "Europe", "Client.Nationality.RU"),
        new("SE", "Europe", "Client.Nationality.SE"),
        new("SI", "Europe", "Client.Nationality.SI"),
        new("SK", "Europe", "Client.Nationality.SK"),
        new("SM", "Europe", "Client.Nationality.SM"),
        new("SR", "Americas", "Client.Nationality.SR"),
        new("SV", "Americas", "Client.Nationality.SV"),
        new("TT", "Americas", "Client.Nationality.TT"),
        new("UA", "Europe", "Client.Nationality.UA"),
        new("US", "Americas", "Client.Nationality.US"),
        new("UY", "Americas", "Client.Nationality.UY"),
        new("VA", "Europe", "Client.Nationality.VA"),
        new("VC", "Americas", "Client.Nationality.VC"),
        new("VE", "Americas", "Client.Nationality.VE"),
    ]);

    public static IReadOnlyList<string> Codes { get; } =
        Array.AsReadOnly(Entries.Select(entry => entry.Code).ToArray());

    private static readonly HashSet<string> CanonicalCodes = new(Codes, StringComparer.Ordinal);

    public static bool IsCanonical(string? value) => value is null || CanonicalCodes.Contains(value);
}
