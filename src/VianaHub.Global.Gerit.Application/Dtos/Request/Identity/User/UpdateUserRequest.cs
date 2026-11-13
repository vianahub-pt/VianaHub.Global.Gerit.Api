using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace VianaHub.Global.Gerit.Application.Dtos.Request.Identity.User;

public class UpdateUserRequest
{
    [Required(ErrorMessage = "Domain.User.NameRequired")]
    [MaxLength(150, ErrorMessage = "Api.Validator.User.Update.Name.MaximumLength")]
    public string? Name { get; set; }
    private string? _email;
    private string? _phoneNumber;
    private string? _urlImage;

    [MaxLength(256, ErrorMessage = "Domain.User.EmailMaxLength")]
    public string? Email { get => _email; set { _email = value; EmailProvided = true; } }
    [MaxLength(50, ErrorMessage = "Domain.User.PhoneNumberMaxLength")]
    public string? PhoneNumber { get => _phoneNumber; set { _phoneNumber = value; PhoneNumberProvided = true; } }
    [MaxLength(500, ErrorMessage = "Domain.User.UrlImageMaxLength")]
    public string? UrlImage { get => _urlImage; set { _urlImage = value; UrlImageProvided = true; } }

    [JsonIgnore]
    public bool EmailProvided { get; private set; }
    [JsonIgnore]
    public bool PhoneNumberProvided { get; private set; }
    [JsonIgnore]
    public bool UrlImageProvided { get; private set; }
}
