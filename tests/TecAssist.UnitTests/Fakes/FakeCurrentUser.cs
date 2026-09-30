using TecAssist.Application.Common;

namespace TecAssist.UnitTests.Fakes;

public sealed class FakeCurrentUser(Guid userId) : ICurrentUser
{
    public Guid UserId { get; set; } = userId;
}
