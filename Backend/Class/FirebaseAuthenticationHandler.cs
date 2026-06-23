using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Backend.Class
{
    public class FirebaseAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public FirebaseAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization"))
                return AuthenticateResult.Fail("Missing Authorization Header");

            string token = Request.Headers["Authorization"];
            if (token.StartsWith("Bearer "))
                token = token.Substring("Bearer ".Length);

            try
            {
                var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, decodedToken.Uid),
            new Claim(ClaimTypes.Name, decodedToken.Uid),
        };

                // Agregá custom claims si los usás
                var identity = new ClaimsIdentity(claims, nameof(FirebaseAuthenticationHandler));
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, this.Scheme.Name);
                return AuthenticateResult.Success(ticket);
            }
            catch (Exception ex)
            {
                return AuthenticateResult.Fail($"Token inválido: {ex.Message}");
            }
        }
    }
}
