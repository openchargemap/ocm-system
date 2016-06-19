using System;
using System.Collections.Generic;
using System.Configuration;
using System.IdentityModel.Tokens;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace OCM.API.Security
{
    public class JWTAuth
    {
        private const string ISSUER = "Open Charge Map";
        private const string AUDIENCE = "api.openchargemap.io";

        /// <summary>
        /// Generate user specific JWT string
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static string GenerateEncodedJWT(OCM.API.Common.Model.User user)
        {
            var claims = new List<System.Security.Claims.Claim>();
            claims.Add(new Claim("UserID", user.ID.ToString()));
            claims.Add(new Claim("nonce", user.CurrentSessionToken.ToString()));

            var signingKey = new InMemorySymmetricSecurityKey(Encoding.ASCII.GetBytes(user.CurrentSessionToken));
            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256Signature, SecurityAlgorithms.Sha256Digest);
            var token = new JwtSecurityToken(ISSUER, AUDIENCE, claims, DateTime.UtcNow, DateTime.UtcNow.AddMonths(1), signingCredentials);

            var handler = new JwtSecurityTokenHandler();

            var jwt = handler.WriteToken(token);

            return jwt;
        }

        public static JwtSecurityToken ParseEncodedJWT(string ticket)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();

                var jwt = (JwtSecurityToken)handler.ReadToken(ticket);
                return jwt;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static ClaimsPrincipal ValidateJWTForUser(string token, Common.Model.User user)
        {
            var handler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidIssuer = ISSUER,
                ValidAudience = AUDIENCE,
                IssuerSigningKey = new InMemorySymmetricSecurityKey(Encoding.ASCII.GetBytes(user.CurrentSessionToken)),
                RequireExpirationTime = true,
                ValidateIssuer = true
            };

            try
            {
                SecurityToken validatedToken = null;
                var claims = handler.ValidateToken(token, validationParameters, out validatedToken);
              
                return claims;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("{0}\n {1}", e.Message, e.StackTrace);
                return null;
            }
        }
    }
}