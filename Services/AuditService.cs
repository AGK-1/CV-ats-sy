using System.Security.Claims;
using cvAts;
using cvAts.Class;

public class AuditService
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(
        AppDbContext context,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(
        string action,
        string? description = null,
        string? entityName = null,
        int? entityId = null)
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user == null)
            throw new Exception("HttpContext is null.");

        var userIdClaim = user.FindFirst("userId");

        if (userIdClaim == null)
            throw new Exception("userId claim not found.");

        var userId = int.Parse(userIdClaim.Value);

        var impersonatedByClaim = user.FindFirst("impersonatedBy");

        int? impersonatedBy = null;

        if (impersonatedByClaim != null)
        {
            impersonatedBy = int.Parse(impersonatedByClaim.Value);
        }

        var log = new AuditLog
        {
            UserId = userId,
            Action = action,
            Description = description,
            ImpersonatedBy = impersonatedBy,
            EntityName = entityName,
            EntityId = entityId,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }


    public async Task LogLoginAsync(int userId, bool success, string? description = null)
    {
        var log = new AuditLog
        {
            UserId = userId,
            Action = success ? "User logged in" : "Login failed",
            Description = description,
            EntityName = "User",
            EntityId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}