using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CounterAssistant.API.Controllers
{
    [ApiController]
    [Route("api/token")]
    public class TokenController : ControllerBase
    {
        private readonly static HashSet<string> _users = new HashSet<string> { "pashagrishyn@gmail.com" };

        [HttpPost]
        public IActionResult GetToken([FromQuery] string email)
        {
            if (_users.Contains(email))
            {
                var token = new JwtSecurityToken(
                    issuer: "MyAuthServer",
                    audience: "MyAuthClient",
                    notBefore: DateTime.UtcNow,
                    claims: new List<Claim> { new Claim(ClaimTypes.Email, email), new Claim(ClaimTypes.Name, email), new Claim(ClaimTypes.Role, "admin")},
                    expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(30)),
                    signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes("mysupersecret_secretkey!123!123")), SecurityAlgorithms.HmacSha256));

                var jwt = new JwtSecurityTokenHandler().WriteToken(token);

                return Ok(new {
                    access_token = jwt,
                    expire_in = TimeSpan.FromMinutes(30).TotalSeconds
                });
            }

            return Unauthorized();
        }
    }
}
