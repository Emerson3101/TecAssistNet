using System.Security.Claims;
using TecAssist.Application.Common;

namespace TecAssist.Api.Services;

public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid UserId
    {
        get
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                return Guid.Empty;
            }

            var subject = httpContext.User.FindFirstValue("sub")
                ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(subject, out var userId) ? userId : Guid.Empty;
        }
    }
}

public sealed class DevCurrentUser(Guid userId) : ICurrentUser
{
    public Guid UserId { get; } = userId;
}
