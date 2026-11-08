using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using QuizMasterApplication.Services.Interfaces;
using QuizMasterDomain.DTOs.AuthDtos;
using QuizMasterDomain.Identity;
using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace QuizMasterApplication.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public AuthService(UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        public async Task<AuthResponseDto> LoginUserAsync(LoginDto dto, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password)) 
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }
            if (!user.EmailConfirmed)
            {
                throw new InvalidOperationException("Email not verified.");
            }
            return await GenerateJwtTokenAsync(user);
        }

        private async Task<AuthResponseDto> GenerateJwtTokenAsync(ApplicationUser user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key is not configured.");
            var issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT issuer is not configured");
            var audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt audience is not configured.");
            if (!int.TryParse(_configuration["Jwt:ExpirationInMinutes"], out var expirationMinutes))
            {
                throw new InvalidOperationException("JWT expiration is not configured correctly.");
            }

            var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);
            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ClaimTypes.Name, user.UserName ?? string.Empty)
            };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken
                (
                issuer: issuer, 
                audience: audience, 
                claims: claims, 
                expires: expiresAt, 
                signingCredentials: credentials
                );
            return new AuthResponseDto
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAtUtc = expiresAt
            };
        } 


    }
}
