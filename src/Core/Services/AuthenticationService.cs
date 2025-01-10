using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using ListaCompras.Core.Models;
using ListaCompras.Core.Services;

namespace ListaCompras.Core.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserService _userService;
        private readonly ILogger<AuthenticationService> _logger;
        private readonly AuthenticationSettings _settings;
        private readonly ConcurrentDictionary<string, RefreshToken> _refreshTokens;
        private readonly RandomNumberGenerator _rng;

        public AuthenticationService(
            IUserService userService,
            ILogger<AuthenticationService> logger,
            IOptions<AuthenticationSettings> settings)
        {
            _userService = userService;
            _logger = logger;
            _settings = settings.Value;
            _refreshTokens = new ConcurrentDictionary<string, RefreshToken>();
            _rng = RandomNumberGenerator.Create();
        }

        public async Task<AuthenticationResult> AuthenticateAsync(string email, string password)
        {
            try
            {
                // Valida credenciais
                var user = await _userService.GetByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("Tentativa de login com email não encontrado: {Email}", email);
                    return AuthenticationResult.Failed("Credenciais inválidas");
                }

                // Verifica senha
                if (!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                {
                    _logger.LogWarning("Tentativa de login com senha incorreta para: {Email}", email);
                    await HandleFailedLoginAttemptAsync(user);
                    return AuthenticationResult.Failed("Credenciais inválidas");
                }

                // Verifica bloqueio
                if (user.IsLockedOut)
                {
                    _logger.LogWarning("Tentativa de login com conta bloqueada: {Email}", email);
                    return AuthenticationResult.Failed("Conta bloqueada. Tente novamente mais tarde.");
                }

                // Verifica 2FA
                if (user.TwoFactorEnabled && !await ValidateTwoFactorAsync(user))
                {
                    return AuthenticationResult.RequiresTwoFactor();
                }

                // Gera tokens
                var accessToken = GenerateAccessToken(user);
                var refreshToken = GenerateRefreshToken();
                _refreshTokens.TryAdd(refreshToken.Token, refreshToken);

                // Registra login bem-sucedido
                await _userService.UpdateLastLoginAsync(user.Id);

                return AuthenticationResult.Success(accessToken, refreshToken.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante autenticação para {Email}", email);
                throw;
            }
        }

        public async Task<AuthenticationResult> RefreshTokenAsync(string accessToken, string refreshToken)
        {
            try
            {
                var principal = GetPrincipalFromExpiredToken(accessToken);
                if (principal == null)
                {
                    return AuthenticationResult.Failed("Token inválido");
                }

                var userId = int.Parse(principal.FindFirst(ClaimTypes.NameIdentifier).Value);
                var user = await _userService.GetByIdAsync(userId);
                if (user == null)
                {
                    return AuthenticationResult.Failed("Usuário não encontrado");
                }

                if (!_refreshTokens.TryGetValue(refreshToken, out var savedToken) || 
                    savedToken.IsExpired)
                {
                    return AuthenticationResult.Failed("Refresh token inválido ou expirado");
                }

                var newAccessToken = GenerateAccessToken(user);
                var newRefreshToken = GenerateRefreshToken();

                _refreshTokens.TryRemove(refreshToken, out _);
                _refreshTokens.TryAdd(newRefreshToken.Token, newRefreshToken);

                return AuthenticationResult.Success(newAccessToken, newRefreshToken.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar token");
                throw;
            }
        }

        public async Task<bool> ValidateTwoFactorAsync(string email, string code)
        {
            var user = await _userService.GetByEmailAsync(email);
            if (user == null || !user.TwoFactorEnabled)
            {
                return false;
            }

            return ValidateTwoFactorCode(user.TwoFactorKey, code);
        }

        public async Task RevokeTokenAsync(string refreshToken)
        {
            _refreshTokens.TryRemove(refreshToken, out _);
        }

        public async Task RevokeAllTokensAsync(int userId)
        {
            var tokensToRemove = _refreshTokens.Where(t => 
                t.Value.UserId == userId).Select(t => t.Key);

            foreach (var token in tokensToRemove)
            {
                _refreshTokens.TryRemove(token, out _);
            }
        }

        private string GenerateAccessToken(UserModel user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_settings.SecretKey);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email)
            };

            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private RefreshToken GenerateRefreshToken()
        {
            var randomBytes = new byte[32];
            _rng.GetBytes(randomBytes);
            
            return new RefreshToken
            {
                Token = Convert.ToBase64String(randomBytes),
                ExpirationDate = DateTime.UtcNow.AddDays(_settings.RefreshTokenExpirationDays)
            };
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.ASCII.GetBytes(_settings.SecretKey)),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            
            try
            {
                var principal = tokenHandler.ValidateToken(token, 
                    tokenValidationParameters, out SecurityToken securityToken);

                if (!(securityToken is JwtSecurityToken jwtSecurityToken) || 
                    !jwtSecurityToken.Header.Alg.Equals(
                        SecurityAlgorithms.HmacSha256, 
                        StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }

        private bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
        {
            using (var hmac = new HMACSHA512(storedSalt))
            {
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                return computedHash.SequenceEqual(storedHash);
            }
        }

        private async Task HandleFailedLoginAttemptAsync(UserModel user)
        {
            user.FailedLoginAttempts++;
            
            if (user.FailedLoginAttempts >= _settings.MaxFailedLoginAttempts)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(_settings.LockoutDurationMinutes);
                user.IsLockedOut = true;
                _logger.LogWarning("Conta bloqueada após {Attempts} tentativas falhas: {Email}", 
                    user.FailedLoginAttempts, user.Email);
            }

            await _userService.UpdateAsync(user);
        }

        private bool ValidateTwoFactorCode(string secretKey, string code)
        {
            var validationWindow = _settings.TwoFactorValidationWindowMinutes;
            var validCodes = new List<string>();

            for (int i = -validationWindow; i <= validationWindow; i++)
            {
                var counterOffset = DateTime.UtcNow.AddMinutes(i);
                var counter = (long)(counterOffset - DateTime.UnixEpoch).TotalSeconds / 30;
                validCodes.Add(GenerateTwoFactorCode(secretKey, counter));
            }

            return validCodes.Contains(code);
        }

        private string GenerateTwoFactorCode(string secretKey, long counter)
        {
            var keyBytes = Convert.FromBase64String(secretKey);
            var counterBytes = BitConverter.GetBytes(counter).Reverse().ToArray();

            using (var hmac = new HMACSHA1(keyBytes))
            {
                var hash = hmac.ComputeHash(counterBytes);
                var offset = hash[^1] & 0x0F;
                var truncatedHash = new byte[4];
                Array.Copy(hash, offset, truncatedHash, 0, 4);
                var code = BitConverter.ToInt32(truncatedHash, 0) & 0x7FFFFFFF;
                return code.ToString("D6");
            }
        }
    }

    public class AuthenticationSettings
    {
        public string SecretKey { get; set; }
        public int AccessTokenExpirationMinutes { get; set; } = 60;
        public int RefreshTokenExpirationDays { get; set; } = 7;
        public int MaxFailedLoginAttempts { get; set; } = 5;
        public int LockoutDurationMinutes { get; set; } = 15;
        public int TwoFactorValidationWindowMinutes { get; set; } = 2;
        public bool RequireHttps { get; set; } = true;
    }

    public class AuthenticationResult
    {
        public bool Success { get; private set; }
        public bool RequiresTwoFactor { get; private set; }
        public string AccessToken { get; private set; }
        public string RefreshToken { get; private set; }
        public string Error { get; private set; }

        private AuthenticationResult() { }

        public static AuthenticationResult Success(string accessToken, string refreshToken)
        {
            return new AuthenticationResult
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }

        public static AuthenticationResult Failed(string error)
        {
            return new AuthenticationResult
            {
                Success = false,
                Error = error
            };
        }

        public static AuthenticationResult RequiresTwoFactor()
        {
            return new AuthenticationResult
            {
                Success = false,
                RequiresTwoFactor = true
            };
        }
    }

    public class RefreshToken
    {
        public string Token { get; set; }
        public int UserId { get; set; }
        public DateTime ExpirationDate { get; set; }
        public bool IsExpired => DateTime.UtcNow >= ExpirationDate;
    }
}