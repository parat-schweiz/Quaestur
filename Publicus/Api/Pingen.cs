using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MimeKit;

namespace Publicus
{
    public enum PingenSpeed
    {
        Priority = 1,
        Economy = 2,
    }

    public class Pingen
    {
        private const string _apiurl = "https://api.pingen.com";
        private readonly string _token;

        public Pingen(string token)
        {
            _token = token;
        }

        public void Upload(byte[] file, bool send, PingenSpeed speed)
        {
            var data = new JObject(
                new JProperty("rightaddress", 1),
                new JProperty("color", 2),
                new JProperty("speed", (int)speed),
                new JProperty("send", send));

            var response = JObject.Parse(Request("document", "upload", data, file));

            var error = (bool)response.Property("error").Value;

            if (error)
            {
                var errormessage = (string)response.Property("errormessage").Value;
                throw new Exception(errormessage);
            }
        }

        private string Request(string endpoint, string method, JObject data, byte[] file)
        {
            var url = string.Join("/", _apiurl, endpoint, method, "token", _token);

            var content = new MultipartContent("form-data");
            var dataContent = new StringContent(data.ToString());
            dataContent.Headers.Add("Content-Disposition", "form-data; name=\"data\"");
            content.Add(dataContent);

            if (file != null)
            {
                var fileContent = new ByteArrayContent(file);
                fileContent.Headers.Add("Content-Disposition", "form-data; name=\"file\"; filename=\"file.pdf\"");
                fileContent.Headers.Add("Content-Type", "application/pdf");
                fileContent.Headers.Add("Content-Transfer-Encoding", "binary");
                content.Add(fileContent);
            }

            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri(url);
            request.Content = content;

            var client = new HttpClient();
            var waitResponse = client.SendAsync(request);
            waitResponse.Wait();
            var response = waitResponse.Result;

            var waitRead = response.Content.ReadAsByteArrayAsync();
            waitRead.Wait();
            var responseText = Encoding.UTF8.GetString(waitRead.Result);
            return responseText;
        }
    }
}
