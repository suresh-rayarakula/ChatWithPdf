namespace ChatWithPdf.Application.Contracts;

public interface IRagChatService
{
  Task<ChatAnswer> AskAsync(ChatQuestion question, CancellationToken cancellationToken = default);
}

public sealed record ChatQuestion(string Question, Guid? DocumentId = null);

public sealed record ChatAnswer(
  string Answer,
  IReadOnlyList<SourceCitation> Sources);

public sealed record SourceCitation(
  Guid DocumentId,
  string FileName,
  int PageNumber,
  int ChunkIndex,
  string Excerpt,
  double Similarity);
