using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Harjoitustyö.Models;
using Harjoitustyö.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Harjoitustyö.Middleware
{
    public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "BasicAuthentication";

        private readonly IUserAuthenticationService _authenticationService;

        public BasicAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IUserAuthenticationService authenticationService)
            : base(options, logger, encoder)
        {
            _authenticationService = authenticationService;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization"))
            {
                return AuthenticateResult.NoResult();
            }

            string userName;
            string password;
            try
            {
                var authHeader = AuthenticationHeaderValue.Parse(Request.Headers.Authorization!);
                if (!"Basic".Equals(authHeader.Scheme, StringComparison.OrdinalIgnoreCase) || authHeader.Parameter == null)
                {
                    return AuthenticateResult.NoResult();
                }

                string credentials = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader.Parameter));
                int separator = credentials.IndexOf(':');
                if (separator < 0)
                {
                    return AuthenticateResult.Fail("Virheellinen Authorization-otsake");
                }
                userName = credentials[..separator];
                password = credentials[(separator + 1)..];
            }
            catch (FormatException)
            {
                return AuthenticateResult.Fail("Virheellinen Authorization-otsake");
            }

            User? user = await _authenticationService.Authenticate(userName, password);
            if (user == null)
            {
                return AuthenticateResult.Fail("Väärä tunnus tai salasana");
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName)
            };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.Headers.WWWAuthenticate = "Basic realm=\"Harjoitustyo\", charset=\"UTF-8\"";
            return base.HandleChallengeAsync(properties);
        }
    }
}
