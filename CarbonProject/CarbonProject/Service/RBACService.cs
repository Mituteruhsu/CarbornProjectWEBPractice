using CarbonProject.Data;
using CarbonProject.Models.EFModels.RBAC;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Mysqlx.Expr;
using Org.BouncyCastle.Pqc.Crypto.Lms;
using Org.BouncyCastle.Utilities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static Azure.Core.HttpHeader;

// 這裡架構 RBACService 使用服務

namespace CarbonProject.Services
{
    public class RBACService
    {
        private readonly RbacDbContext _context;

        public RBACService(RbacDbContext context)
        {
            _context = context;
        }
        public async Task<List<User>> GetActiveUsers()
        {
            return await _context.Users.Where(u => u.IsActive).ToListAsync();
        }
    }
}