using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using Mysqlx.Expr;
using Org.BouncyCastle.Pqc.Crypto.Lms;
using Org.BouncyCastle.Utilities;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static Azure.Core.HttpHeader;

// 這裡架構 JwtService JWT（JSON Web Token）使用服務
// 須將以下這些規則加入 Controller
namespace CarbonProject.Services
{
    public class JWTService
    {
        private readonly IConfiguration _config;

        public JWTService(IConfiguration config)
        {
            _config = config;
        }

        // ===== 生成 JWT Token =====
        public string GenerateToken(string username,
                                    IEnumerable<string> roles,
                                    IEnumerable<string> permissions,
                                    IEnumerable<string> capabilities,
                                    int memberId,
                                    bool rememberMe = false)
        {
            // A 建立一個 JWT（有 claims、過期時間、使用 HMAC-SHA256 簽章）
            // A-01 建立 Claim 列表（Username、Role、MemberId、RememberMe）
            Debug.WriteLine("===== Service/JWTService.cs =====");
            var claims = new List<Claim>
            {
                // JWT 標準欄位常數表（Registered Claim Names）
                // sub → Subject（主體）
                // iss → Issuer（簽發者）
                // aud → Audience（接收者）
                // exp → Expiration time（過期時間）
                // nbf → Not before（何時開始有效）
                // iat → Issued at（簽發時間）
                // jti → JWT ID（唯一編號）
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("Username", username),
                new Claim("MemberId", memberId.ToString()),
                new Claim("RememberMe", rememberMe.ToString()),
            };
            // 多角色
            foreach (var role in roles)
                claims.Add(new Claim("Role", role));

            // 多 Permission
            foreach (var permission in permissions)
                claims.Add(new Claim("Permission", permission));

            // 多 Capability
            foreach (var capability in capabilities)
                claims.Add(new Claim("Capability", capability));

            // A-02 用 _secretKey 產生 SymmetricSecurityKey 與 SigningCredentials
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
           
            // A-03 設定 expires（根據 rememberMe 選擇 24 小時 或 30 天）
            // 根據「記住我」選項設定過期時間
            var expires = rememberMe
                ? DateTime.UtcNow.AddDays(7)  // 記住我：7天
                : DateTime.UtcNow.AddHours(2); // 一般登入：2小時


            // A-04 建 JwtSecurityToken 並用 JwtSecurityTokenHandler.WriteToken 回傳字串
            // 改用標準 claim 名稱（例如 JwtRegisteredClaimNames.Sub、JwtRegisteredClaimNames.Jti、ClaimTypes.Role），提高相容性
            // 加入 iat（issued at）、jti（唯一 token id，用於撤銷/追蹤）
            // 若為生產環境，建議改用 非對稱簽章（RS256, 用 RSA 私鑰簽），或把對稱 key 放在安全機制（例如 Azure Key Vault）
            var token = new JwtSecurityToken(
                issuer: _config["JwtSettings:Issuer"],
                audience: _config["JwtSettings:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );
            Debug.WriteLine($"建立 Token : {token}");

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // ===== 驗證並解析 JWT Token =====
        // <- **重要**：回傳 ClaimsPrincipal 或 null（不要用 out）
        public ClaimsPrincipal ValidateToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]);

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = _config["JwtSettings:Issuer"],
                    ValidAudience = _config["JwtSettings:Audience"],
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                }, out _);

                return principal;
            }
            catch
            {
                return null;
            }
        }

        // 方便取 Session 資訊
        public static string GetUsername(ClaimsPrincipal principal) =>
            principal?.Claims.FirstOrDefault(c => c.Type == "Username")?.Value;
        public static int GetMemberId(ClaimsPrincipal principal)
        {
            var idClaim = principal?.Claims.FirstOrDefault(c => c.Type == "MemberId")?.Value;
            return int.TryParse(idClaim, out var memberId) ? memberId : 0;
        }
        public static List<string> GetRoles(ClaimsPrincipal principal) =>
            principal?.Claims
                .Where(c => c.Type == "Role")
                .Select(c => c.Value)
                .ToList() ?? new List<string>();

        public static List<string> GetPermissions(ClaimsPrincipal principal) =>
            principal?.Claims
                .Where(c => c.Type == "Permission")
                .Select(c => c.Value)
                .ToList() ?? new List<string>();

        public static List<string> GetCapabilities(ClaimsPrincipal principal) =>
            principal?.Claims
                .Where(c => c.Type == "Capability")
                .Select(c => c.Value)
                .ToList() ?? new List<string>();
    }
}