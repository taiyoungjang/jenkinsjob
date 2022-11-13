using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var jenkinsUrl = builder.Configuration.GetValue<string>("jenkins:url");
var jenkinsUser = builder.Configuration.GetValue<string>("jenkins:user");
var jenkinsToken = builder.Configuration.GetValue<string>("jenkins:token");

HttpClient httpClient = new();
var credentials = Encoding.ASCII.GetBytes($"{jenkinsUser}:{jenkinsToken}");
AuthenticationHeaderValue header = new("Basic", Convert.ToBase64String(credentials));
httpClient.DefaultRequestHeaders.Authorization = header;

app.MapGet("/build/{job}", Build);
app.MapGet("/listjob", List);
app.MapPost("/request_modal", RequestModal);
app.MapPost("/submission", Submission);
app.MapPost("/", PostJson);

app.Run();

async Task<IResult> PostJson(HttpRequest request)
{
    Stream stream = new MemoryStream();
    StreamReader reader = new StreamReader(stream);
    await request.BodyReader.CopyToAsync(stream);
    stream.Position = 0;
    string json = reader.ReadToEnd();
    JsonNode jsonNode = JsonNode.Parse(json);
    var type = jsonNode!["type"];
    switch (type.GetValue<string>())
    {
        case "request_modal":
            var requestModalDto = System.Text.Json.JsonSerializer.Deserialize<RequestModalDto>(json);
            return RequestModal(requestModalDto);
        case "submission":
            var submissionDto = System.Text.Json.JsonSerializer.Deserialize<SubmissionDto>(json);
            return Submission(submissionDto);
    }

    return Results.NotFound();
}

IResult RequestModal(RequestModalDto dto)
{
    return Results.Json(dto.message);
}

IResult Submission(SubmissionDto dto)
{
    return Results.Json(dto.message);
}

IResult Build(string job)
{
    if (string.IsNullOrEmpty(job))
    {
        return Results.Json(new { message = "invalid job" });
    }

    using HttpRequestMessage requestMessage = new HttpRequestMessage();
    requestMessage.Method = HttpMethod.Post;
    requestMessage.RequestUri = new Uri($"{jenkinsUrl}/job/{job}/build");
    using var response = httpClient.Send(requestMessage);
    var statusCode = new { response.StatusCode };
    return Results.Json(statusCode);
}

IResult List()
{
    using HttpRequestMessage requestMessage = new HttpRequestMessage();
    requestMessage.Method = HttpMethod.Post;
    requestMessage.RequestUri =
        new Uri($"{jenkinsUrl}/api/json?tree=jobs[name,buildable,jobs[name,buildable,jobs[name,buildable]]]&pretty");
    using var response = httpClient.Send(requestMessage);
    var jsonNodes = JsonNode.Parse(response.Content.ReadAsStringAsync().Result);
    List<string> jobs = new();
    StringBuilder sb = new StringBuilder();
    foreach (JsonObject job in jsonNodes!["jobs"]!.AsArray())
    {
        string name = job!["name"]!.ToString();
        jobs.Add(name);
        sb.AppendLine($"<div>");
        sb.AppendLine($"<button class=\"button\" type=\"button\" onclick=\"location.href='/build/{name}'\">{name}</button>");
        sb.AppendLine($"</div>");
    }

    StringBuilder html = new StringBuilder();
    html.AppendLine("<!doctype html>");
    html.AppendLine("<html>");
    html.AppendLine("<head>");
    html.AppendLine("<style>");
    html.AppendLine("  .div {");
    html.AppendLine("background-color: #04AA6D;");
    html.AppendLine("border: none;");
    html.AppendLine("color: white;");
    html.AppendLine("padding: 10px;");
    html.AppendLine("text-align: center;");
    html.AppendLine("text-decoration: none;");
    html.AppendLine("display: inline-block;");
    html.AppendLine("font-size: 10px;");
    html.AppendLine("margin: 4px 2px;");
    html.AppendLine("border-radius: 12px;");
    html.AppendLine("}");
    html.AppendLine("</style>");
    html.AppendLine("</head>");
    html.AppendLine("<body>");
    html.AppendLine(sb.ToString());
    html.AppendLine("</body></html>");
    return Results.Content(html.ToString(), "text/html");
}
