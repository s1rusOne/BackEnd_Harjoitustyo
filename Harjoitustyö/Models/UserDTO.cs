using System.ComponentModel.DataAnnotations;

namespace Harjoitustyö.Models
{
    public class UserDTO
    {
        public string UserName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime? JoinDate { get; set; }
        public DateTime? LastLogin { get; set; }
    }

    public class RegisterUserDTO
    {
        [Required, StringLength(50, MinimumLength = 3)]
        [RegularExpression(@"^[a-zA-Z0-9_.-]+$", ErrorMessage = "Tunnus saa sisältää vain kirjaimia a-z, numeroita sekä merkkejä _ . -")]
        public string UserName { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string LastName { get; set; } = string.Empty;
    }

    public class UpdateUserDTO
    {
        [StringLength(100, MinimumLength = 8)]
        public string? Password { get; set; }

        [StringLength(50)]
        public string? FirstName { get; set; }

        [StringLength(50)]
        public string? LastName { get; set; }
    }
}
