namespace ChatWithPdf.Api.Contracts.Requests;

public sealed record ChatRequest(string Question, Guid? DocumentId = null);
