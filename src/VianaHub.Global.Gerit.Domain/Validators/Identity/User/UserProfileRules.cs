namespace VianaHub.Global.Gerit.Domain.Validators.Identity.User;

public static class UserProfileRules
{
    public static bool IsValidImageUrl(string? value) => value is null ||
        (value == value.Trim() && !value.Any(char.IsControl) &&
         Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
         (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp) &&
         !string.IsNullOrEmpty(uri.Host) && string.IsNullOrEmpty(uri.UserInfo));
}
