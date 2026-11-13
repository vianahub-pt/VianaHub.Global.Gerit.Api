using System.ComponentModel.DataAnnotations;

namespace VianaHub.Global.Gerit.Application.Dtos.Request.Identity.User;

public class CreateUserRequest
{
    [Required(ErrorMessage = "Api.Validator.User.Create.Name")]
    [MaxLength(150, ErrorMessage = "Api.Validator.User.Create.Name.MaximumLength")]
    public string? Name { get; set; }
    [Required(ErrorMessage = "Api.Validator.User.Create.Email")]
    [MaxLength(256, ErrorMessage = "Api.Validator.User.Create.Email.MaximumLength")]
    public string? Email { get; set; }
    [MaxLength(50, ErrorMessage = "Domain.User.PhoneNumberMaxLength")]
    public string? PhoneNumber { get; set; }
    [MaxLength(500, ErrorMessage = "Domain.User.UrlImageMaxLength")]
    public string? UrlImage { get; set; }
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }
}
