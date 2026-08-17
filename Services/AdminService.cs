using System.Security.Claims;
using cvAts.Class;
using cvAts.Enums;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace cvAts.Services
{
    public class AdminService
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwt;
        private readonly AuditService _auditService;
        public AdminService(AppDbContext context, AuditService auditService, JwtService jwt)
        {
            _context = context;
            _auditService = auditService;
            _jwt = jwt;
        }

        public async Task<string> LoginAsUser(int adminId, int targetUserId)
        {
            var admin = await _context.Users.FindAsync(adminId);

            if (admin == null)
                throw new Exception("Admin not found.");

            if (admin.Role != UserRole.Admin &&
                admin.Role != UserRole.SuperAdmin)
            {
                throw new UnauthorizedAccessException();
            }


            var targetUser = await _context.Users.FindAsync(targetUserId);

            if (targetUser == null)
                throw new Exception("User not found.");

            await _auditService.LogAsync(
                 action: "LoginAsUser",
                 description: "Admin started impersonation",
                 entityName: "User",
                 entityId: targetUser.Id
            );

            return "Bearer " + _jwt.GenerateToken(targetUser, impersonatedBy: admin.Id);
        }


        public async Task<string> StopImpersonation(int adminId)
        {
            var admin = await _context.Users.FindAsync(adminId);

            if (admin == null)
                throw new Exception("Admin not found.");

            if (admin.Role != UserRole.Admin &&
                admin.Role != UserRole.SuperAdmin)
            {
                throw new UnauthorizedAccessException();
            }

            return "Bearer " + _jwt.GenerateToken(admin);
        }


        public async Task<List<User>> GetAllUsers(int adminId)
        {
            var admin = await _context.Users.FindAsync(adminId);

            if (admin == null)
                throw new Exception("You don't have permission.");

            if (admin.Role != UserRole.Admin &&
                admin.Role != UserRole.SuperAdmin)
            {
                throw new UnauthorizedAccessException();
            }

            return await _context.Users.ToListAsync();
        }

        public async Task<string> AssignAdminRole(int adminId, string email)
        {
            var admin = await _context.Users.FindAsync(adminId);

            if (admin == null)
                throw new Exception("You don't have permission.");

            if (admin.Role != UserRole.Admin &&
                admin.Role != UserRole.SuperAdmin)
            {
                throw new UnauthorizedAccessException();
            }
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                throw new Exception("User not found.");

            user.Role = UserRole.Admin;

            await _context.SaveChangesAsync();

            return "Now this user is an admin.";
        }

        public async Task<string> AssignUserRole(int adminId, string email)
        {
            var admin = await _context.Users.FindAsync(adminId);

            if (admin == null)
                throw new Exception("You don't have permission.");

            if (admin.Role != UserRole.Admin &&
                admin.Role != UserRole.SuperAdmin)
            {
                throw new UnauthorizedAccessException();
            }
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                throw new Exception("User not found.");

            user.Role = UserRole.User;

            await _context.SaveChangesAsync();

            return "Now this admin is an user.";
        }

        public async Task<string> DeleteUser(int id)
        {
            var deletingUser = await _context.Users.FindAsync(id);

            if (deletingUser == null)
            {
                throw new Exception("User does not exist.");
            }

            _context.Users.Remove(deletingUser);
            await _context.SaveChangesAsync();

            return "User deleted.";
        }

        public async Task<string> DeleteUserWithEmail(string email)
        {
            var deletingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (deletingUser == null)
            {
                throw new Exception("User does not exist.");
            }

            _context.Users.Remove(deletingUser);
            await _context.SaveChangesAsync();

            return "User deleted.";
        }


        public async Task<List<AuditLog>> GetUserHistory()
        {
            var auditList = await _context.AuditLogs.ToListAsync();
            return auditList;
        }
    }
}
