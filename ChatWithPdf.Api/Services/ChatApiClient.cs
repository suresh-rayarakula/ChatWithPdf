using System.Net.Http.Json;
using ChatWithPdf.Application.Contracts;
using Microsoft.AspNetCore.Components;

namespace ChatWithPdf.Api.Services;

public sealed class ChatApiClient(HttpClient http, NavigationManager navigation)
{
  private readonly HttpClient _http = http;
  private readonly NavigationManager _navigation = navigation;

  private HttpClient Client
  {
    get
    {
      if (_http.BaseAddress is null)
      {
        _http.BaseAddress = new Uri(_navigation.BaseUri);
      }

      return _http;
    }
  }

  public async Task<List<DocumentSummary>> GetDocumentsAsync(CancellationToken cancellationToken = default)
  {
    var result = await Client.GetFromJsonAsync<List<DocumentSummary>>("api/documents", cancellationToken);
    return result ?? [];
  }

  public async Task<DocumentIngestionResult> UploadAsync(
    Stream stream,
    string fileName,
    CancellationToken cancellationToken = default)
  {
    using var content = new MultipartFormDataContent();
    var fileContent = new StreamContent(stream);
    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
    content.Add(fileContent, "file", fileName);

    var response = await Client.PostAsync("api/documents/upload", content, cancellationToken);
    response.EnsureSuccessStatusCode();

    return (await response.Content.ReadFromJsonAsync<DocumentIngestionResult>(cancellationToken))!;
  }

  public async Task<ChatAnswer> AskAsync(
    string question,
    Guid? documentId,
    CancellationToken cancellationToken = default)
  {
    var response = await Client.PostAsJsonAsync(
      "api/chat",
      new { question, documentId },
      cancellationToken);

    response.EnsureSuccessStatusCode();

    return (await response.Content.ReadFromJsonAsync<ChatAnswer>(cancellationToken))!;
  }
}
