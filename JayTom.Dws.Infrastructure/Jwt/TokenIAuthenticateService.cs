using System.Text;
using JayTom.Dws.Domain.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace JayTom.Dws.Infrastructure.Jwt {

    /// <summary>使用集中式安全参数签发 JWT。</summary>
    public class TokenIAuthenticateService : IAuthenticateService {
        private readonly TokenManagement _tokenManagement;
        private readonly TimeProvider _timeProvider;

        /// <summary>创建令牌签发服务。</summary>
        public TokenIAuthenticateService(
            IOptions<TokenManagement> tokenManagement,
            TimeProvider timeProvider) {
            _tokenManagement = tokenManagement.Value;
            _tokenManagement.Validate();
            _timeProvider = timeProvider;
        }

        /// <summary>验证登录请求并签发具备严格过期时间的令牌。</summary>
        public bool IsAuthenticated(LoginRequestDto request, out string token) {
            token = string.Empty;
            var claim = new Claim[] {
                new(ClaimTypes.Name,request.UserCode??string.Empty)
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tokenManagement.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var issuedAt = _timeProvider.GetUtcNow();
            var jwtSecurityToken = new JwtSecurityToken(
                _tokenManagement.Issuer,// Issuer 颁发者，通常为STS服务器地址
                _tokenManagement.Audience,// Audience Token的作用对象，也就是被访问的资源服务器授权标识
                claim,
                issuedAt.UtcDateTime,
                issuedAt.AddMinutes(_tokenManagement.AccessExpiration).UtcDateTime,
                creds);
            token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
            return true;
        }
    }

    /// <summary>集中保存 JWT 签发与验证所需的安全参数。</summary>
    public class TokenManagement {

        /// <summary>
        /// 用于加密的key
        /// </summary>
        public string Secret { get; set; } = string.Empty;

        /// <summary>
        /// Token是谁颁发的
        /// </summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// Token给那些客户端去使用
        /// </summary>
        public string Audience { get; set; } = string.Empty;

        public int AccessExpiration { get; set; }

        public int RefreshExpiration { get; set; }

        /// <summary>创建禁止降级的令牌验证参数。</summary>
        public TokenValidationParameters CreateValidationParameters() {
            Validate();
            return new TokenValidationParameters {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)),
                RequireSignedTokens = true,
                ValidateIssuer = true,
                ValidIssuer = Issuer,
                ValidateAudience = true,
                ValidAudience = Audience,
                RequireExpirationTime = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        }

        /// <summary>拒绝空标识、弱密钥和无效有效期配置。</summary>
        public void Validate() {
            ArgumentException.ThrowIfNullOrWhiteSpace(Issuer);
            ArgumentException.ThrowIfNullOrWhiteSpace(Audience);
            if (Encoding.UTF8.GetByteCount(Secret) < 32) {
                throw new InvalidOperationException("JWT 对称密钥至少需要 32 个 UTF-8 字节。");
            }
            if (AccessExpiration <= 0 || RefreshExpiration <= 0) {
                throw new InvalidOperationException("JWT 有效期必须为正数分钟。");
            }
        }
    }
}
