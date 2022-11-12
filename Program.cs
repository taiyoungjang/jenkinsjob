using System.Net.Http.Headers;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var jenkinsUrl = builder.Configuration.GetValue<string>("jenkins:url");
var jenkinsUser = builder.Configuration.GetValue<string>("jenkins:user");
var jenkinsToken = builder.Configuration.GetValue<string>("jenkins:token");

app.MapGet("/{job}", (string job) => JobCall(job) );

app.Run();

string JobCall(string job)
{
    if (string.IsNullOrEmpty(job))
    {
        return "invalid job";
    }
    using HttpClient httpClient = new ();
    var credentials = Encoding.ASCII.GetBytes($"{jenkinsUser}:{jenkinsToken}");
    AuthenticationHeaderValue header = new ("Basic", Convert.ToBase64String(credentials));
    httpClient.DefaultRequestHeaders.Authorization = header;
    using HttpRequestMessage requestMessage = new HttpRequestMessage();
    requestMessage.Method = HttpMethod.Post;
    requestMessage.RequestUri = new Uri($"{jenkinsUrl}/job/{job}/build");
    using var response =  httpClient.Send(requestMessage);
    return response.StatusCode.ToString();
}