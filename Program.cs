using System.Net.Http.Headers;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var jenkinsUrl = builder.Configuration.GetValue<string>("jenkins:url");
var jenkinsUser = builder.Configuration.GetValue<string>("jenkins:user");
var jenkinsToken = builder.Configuration.GetValue<string>("jenkins:token");

HttpClient httpClient = new ();
var credentials = Encoding.ASCII.GetBytes($"{jenkinsUser}:{jenkinsToken}");
AuthenticationHeaderValue header = new ("Basic", Convert.ToBase64String(credentials));
httpClient.DefaultRequestHeaders.Authorization = header;

app.MapGet("/build/{job}", (string job) => Build(job) );
app.MapGet("/listjob", () => List() );

app.Run();

string Build(string job)
{
    if (string.IsNullOrEmpty(job))
    {
        return "invalid job";
    }
    using HttpRequestMessage requestMessage = new HttpRequestMessage();
    requestMessage.Method = HttpMethod.Post;
    requestMessage.RequestUri = new Uri($"{jenkinsUrl}/job/{job}/build");
    using var response =  httpClient.Send(requestMessage);
    var statusCode = new { response.StatusCode };
    return statusCode.ToString()!;
}

string List()
{
    using HttpRequestMessage requestMessage = new HttpRequestMessage();
    requestMessage.Method = HttpMethod.Post;
    requestMessage.RequestUri = new Uri($"{jenkinsUrl}/api/json/json?tree=jobs[name,buildable,jobs[name,buildable,jobs[name,buildable]]]&pretty");
    using var response =  httpClient.Send(requestMessage);
    var result = response.Content.ReadAsStringAsync().Result;
    return result;
}
