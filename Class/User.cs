using System.ComponentModel.DataAnnotations;
using cvAts.Enums;

namespace cvAts.Class
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be less 6  not more 50 character")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Incorrect email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be less 6 character")]
        public string Password { get; set; }


        public Boolean IsEmailConfirmed { get; set; } = false;

        public UserRole Role { get; set; } = UserRole.User;
        public ICollection<Mycv> Mycvs { get; set; } = new List<Mycv>();
        public Token Token { get; set; } = null!;

    }
}
