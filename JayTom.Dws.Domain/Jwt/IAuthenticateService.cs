using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.Domain.Jwt {

    public interface IAuthenticateService {

        bool IsAuthenticated(LoginRequestDto request, out string token);
    }

    public class LoginRequestDto {

        [Required]
        public string? UserCode { get; set; }

        [Required]
        public string? PassWord { get; set; }
    }
}