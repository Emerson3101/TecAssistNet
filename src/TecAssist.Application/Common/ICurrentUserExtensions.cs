namespace TecAssist.Application.Common;

public static class ICurrentUserExtensions
{
    public static Guid RequireUserId(this ICurrentUser currentUser)
    {
        var userId = currentUser.UserId;
        if (userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("The caller could not be identified. A valid bearer token is required.");
        }

        return userId;
    }
}
