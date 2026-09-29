namespace TecAssist.Domain.Conversations;

public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public string? Title { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Message> Messages { get; set; } = [];
}
