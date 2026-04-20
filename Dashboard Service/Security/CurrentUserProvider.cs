using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Dashboard_Service.Security
{
    public class CurrentUserProvider : ICurrentUserProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public CurrentUser GetCurrentUser()
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (user == null || !user.Identity.IsAuthenticated)
                return null;

            var userIdRaw = user.FindFirst("userId")?.Value;
            var role = user.FindFirst(ClaimTypes.Role)?.Value;

            Console.WriteLine($"✔ FIXED ROLE: {role}");

            return new CurrentUser
            {
                Id = long.Parse(userIdRaw ?? "0"),
                Role = role
            };
        }
    }
}