using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var jenkinsUrl = builder.Configuration.GetValue<string>("jenkins:url");
var jenkinsUser = builder.Configuration.GetValue<string>("jenkins:user");
var jenkinsToken = builder.Configuration.GetValue<string>("jenkins:token");
var requestKey = builder.Configuration.GetValue<string>("jenkins:request_key");

HttpClient httpClient = new();
var credentials = Encoding.ASCII.GetBytes($"{jenkinsUser}:{jenkinsToken}");
AuthenticationHeaderValue header = new("Basic", Convert.ToBase64String(credentials));
httpClient.DefaultRequestHeaders.Authorization = header;

string buttonName = "button-29";
var css = System.IO.File.ReadAllText($"css/{buttonName}.css");

app.MapGet("/build", Build);
app.MapGet("/listjob", List);
// app.MapPost("/request_modal", RequestModal);
// app.MapPost("/submission", Submission);
app.MapGet("/",  List);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "css")),
    RequestPath = "/css"
});

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

IResult Build(HttpRequest request)
{
    var job = request.Query["job"].ToString();
    if (string.IsNullOrEmpty(job))
    {
        return Results.NotFound();
    }
    {
        var key = request.Query["key"].ToString();
        if (key != requestKey)
        {
            return Results.NotFound();
        }
    }
    
    using HttpRequestMessage requestMessage = new HttpRequestMessage();
    requestMessage.Method = HttpMethod.Post;
    requestMessage.RequestUri = new Uri($"{jenkinsUrl}/job/{job}/build");
    using var response = httpClient.Send(requestMessage);
    var statusCode = new { response.StatusCode };
    return Results.Json(statusCode);
}

IResult List(HttpRequest request)
{
    {
        var key = request.Query["key"].ToString();
        if (key != requestKey)
        {
            return Results.NotFound();
        }
    }
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
        string jobName = job!["name"]!.ToString();
        jobs.Add(jobName);
        sb.AppendLine($"<div><p>");
        sb.AppendLine($"<button class=\"{buttonName}\" role=\"button\" onclick=\"location.href='/build?job={jobName}&key={requestKey}'\">{jobName}</button>");
        sb.AppendLine($"</p></div>");
    }

    var html = new StringBuilder();
    html.AppendLine("<!doctype html>");
    html.AppendLine("<html>");
    html.AppendLine("<meta http-equiv=\"Cache-Control\" content=\"no-cache, no-store, must-revalidate\">");
    html.AppendLine("<head>");
    html.AppendLine($"<link href=\"/css/{buttonName}.css\" rel=\"stylesheet\" type=\"text/css\" />");
    html.AppendLine("</head>");
    html.AppendLine("<body>");
    html.AppendLine(sb.ToString());
    html.AppendLine("</body></html>");
    return Results.Content(html.ToString(), "text/html");
}
