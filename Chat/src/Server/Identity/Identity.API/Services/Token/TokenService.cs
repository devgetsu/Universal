﻿using Identity.API.Data;
using Identity.API.DataTransferObjects.Auth;
using Identity.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Identity.API.Services.Token
{
    public class TokenService : ITokenService
    {
        private IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public TokenService(IConfiguration configuration, ApplicationDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public string GenerateJWT(IEnumerable<Claim> additionalClaims = null)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var expireInMinutes = Convert.ToInt32(_configuration["JWT:ExpireInMinutes"] ?? "1440");

            var claims = new List<Claim>();

            if (additionalClaims?.Any() == true)
                claims.AddRange(additionalClaims);

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:Issuer"],
                audience: _configuration["JWT:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(expireInMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateJWT(User user)
        {
            var claims = new List<Claim>();

            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
            claims.Add(new Claim(ClaimTypes.Name, user.FirstName.ToString()));
            claims.Add(new Claim(ClaimTypes.MobilePhone, user.PhoneNumber.ToString()));

            return GenerateJWT(claims);
        }

        public async ValueTask<ClaimsPrincipal> GetClaimsFromExpiredTokenAsync(string token)
        {
            var validationParametrs = new TokenValidationParameters()
            {
                ValidateIssuer = true,
                ValidIssuer = _configuration["JWT:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["JWT:Audience"],
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Key"])),
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var claimsPrincipal = tokenHandler.ValidateToken(token, validationParametrs, out SecurityToken securityToken);

            var jwtsecurityToken = securityToken as JwtSecurityToken;

            if (jwtsecurityToken == null)
                throw new Exception("Invalid token");

            return claimsPrincipal;
        }

        public async ValueTask<TokenDTO> RefreshToken(RefreshTokenDTO refreshTokenDTO)
        {
            var claims = await GetClaimsFromExpiredTokenAsync(refreshTokenDTO.AccessToken);

            var id = Convert.ToInt32(claims.FindFirst(ClaimTypes.NameIdentifier).Value);

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id);

            if (user.RefreshToken != refreshTokenDTO.RefreshToken)
                throw new Exception("Refresh token is not valid");

            /*
            if (user.RefreshTokenExpireDate <= DateTime.Now)
                throw new Exception("Refresh token has already been expired");*/

            var newAccessToken = GenerateJWT(user);

            return new TokenDTO(
                AccessToken: newAccessToken,
                RefreshToken: user.RefreshToken,
                ExpireDate: user.RefreshTokenExpireDate ?? DateTime.Now.AddMinutes(1440));
        }
    }
}
