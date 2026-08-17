using cvAts;
using cvAts.Class;
using cvAts.DTO;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using OpenAI.Responses;
using Supabase.Gotrue;


namespace cvAts.Services
{

    public class TokenService
    {
        private readonly AppDbContext _context;
        public TokenService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Token>> GetYourTokens()
        {
            return await _context.Tokens.ToListAsync();
        }

        public async Task<TokenDto?> GetYourOwnToken(int userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            var token = await _context.Tokens
                .Where(t => t.UserId == userId)
                .Select(t => new TokenDto
                {
                    Id = t.id,
                    Token = t.TokenValue,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt
                })
                .FirstOrDefaultAsync();
            if (token == null)
            {
                return new TokenDto
                {
                    Id = 0,
                    Token = 0,
                    CreatedAt = DateTime.MinValue,
                    UpdatedAt = DateTime.MinValue
                };
            }

            return token;
        }

        public async Task AddTokens(int userId, int tokenValue)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            var token = await _context.Tokens.FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (token == null)
            {
                token = new Token
                {
                    UserId = user.Id,
                    TokenValue = tokenValue,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Tokens.Add(token);
            }
            else
            {
                token.TokenValue += tokenValue;
                token.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
        }

        public async Task UpdateTokens(int userId, int tokenValue)
        {
            var token = await _context.Tokens.FirstOrDefaultAsync(t => t.UserId == userId);

            if (token == null)
            {
                throw new KeyNotFoundException("Token not found.");
            }

            token.TokenValue = tokenValue;
            token.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

        }

        public async Task AddTokenAdminEmail(int tokenValue, string email)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            var token = await _context.Tokens
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (token == null)
            {
                token = new Token
                {
                    UserId = user.Id,
                    TokenValue = tokenValue,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Tokens.Add(token);
            }
            else
            {
                token.TokenValue += tokenValue;
                token.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }


        public async Task UpdateTokenAdminEmail(string email, int tokenValue)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }
            var token = await _context.Tokens.FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (token == null)
            {
                throw new KeyNotFoundException("Token not found.");
            }

            token.TokenValue = tokenValue;
            token.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

        }

        public async Task deleteOneToken(int userId)
        {
            var token = await _context.Tokens
               .FirstOrDefaultAsync(t => t.UserId == userId);
            if (token == null)
            {
                throw new KeyNotFoundException("User not found.");
            }
            if (token.TokenValue <= 0)
            {
                throw new InvalidOperationException("User has no tokens.");
            }
            token.TokenValue--;
            token.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

        }
    }
}
