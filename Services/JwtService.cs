using cvAts.Class;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class JwtService
{
    private readonly string _key;

    public JwtService(string key)
    {
        _key = key;
    }

    public string GenerateToken(User user, int? impersonatedBy = null)
    {
        var claims = new List<Claim>
        {
            new Claim("userId", user.Id.ToString()),
            new Claim("username", user.Username ?? ""),
            new Claim("email", user.Email ?? ""),
            new Claim("confirm", user.IsEmailConfirmed.ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("isImpersonated", (impersonatedBy != null).ToString())


        

        //new Claim(ClaimTypes.Name, user.Username),
        //new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        //new Claim("email", user.Email ?? "")
        // new Claim(ClaimTypes.Email, user.Email ?? "")
    };
        
        if (impersonatedBy != null)
        {
            claims.Add(new Claim("impersonatedBy", impersonatedBy.Value.ToString()));
        }

        // Используем ключ из конструктора
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        //var token = new JwtSecurityToken(
        //    claims: claims,
        //    expires: DateTime.Now.AddHours(1),
        //    signingCredentials: creds);
        var token = new JwtSecurityToken(
            issuer: "yourdomain.com",
            audience: "yourdomain.com",
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

//public class User
//{
//    public int Id { get; set; }
//    public string Username { get; set; } = "";
//}
