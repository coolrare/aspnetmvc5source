using System;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.ServiceModel;
using System.Text.RegularExpressions;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using System.Web.Http.SelfHost;

namespace AcceptHeaderControllerSelectorSample
{
    class Program
    {
        static readonly Uri _baseAddress = new Uri("http://localhost:50231/");

        static void Main(string[] args)
        {
            HttpSelfHostServer server = null;
            try
            {
                // Set up server configuration
                HttpSelfHostConfiguration config = new HttpSelfHostConfiguration(_baseAddress);
                config.HostNameComparisonMode = HostNameComparisonMode.Exact;

                // Register default route
                config.Routes.MapHttpRoute(
                    name: "DefaultApi",
                    routeTemplate: "api/{controller}/{id}",
                    defaults: new { id = RouteParameter.Optional }
                );

                config.Services.Replace(typeof(IHttpControllerSelector),
                    new AcceptHeaderControllerSelector(config, accept =>
                        {
                            var matches = Regex.Match(accept, @"application\/vnd.api.(.*)\+.*");

                            if (matches.Groups.Count >= 2)
                            {
                                return matches.Groups[1].Value;
                            }

                            return "v2"; // default namespace, return null to throw 404 when namespace not given
                        }));

                // Add custom media types as supported to their default formatters
                config.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api.v1+json"));
                config.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api.v2+json"));

                config.Formatters.XmlFormatter.SupportedMediaTypes.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api.v1+xml"));
                config.Formatters.XmlFormatter.SupportedMediaTypes.Add(new MediaTypeWithQualityHeaderValue("application/vnd.api.v2+xml"));

                // Create server
                server = new HttpSelfHostServer(config);

                // Start listening
                server.OpenAsync().Wait();
                Console.WriteLine("Listening on " + _baseAddress);

                // Run HttpClient issuing requests
                RunClient();

                Console.WriteLine("Hit ENTER to exit...");
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not start server: {0}", e.GetBaseException().Message);
                Console.WriteLine("Hit ENTER to exit...");
                Console.ReadLine();
            }
            finally
            {
                if (server != null)
                {
                    // Stop listening
                    server.CloseAsync().Wait();
                }
            }
        }

        static void RunClient()
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = _baseAddress;

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.api.v1+json");
            using (HttpResponseMessage response = client.GetAsync("api/values").Result)
            {
                response.EnsureSuccessStatusCode();
                string content = response.Content.ReadAsStringAsync().Result;
                Console.WriteLine("Version 1 response: '{0}'\n", content);
            }

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.api.v2+json");
            using (HttpResponseMessage response = client.GetAsync("api/values").Result)
            {
                response.EnsureSuccessStatusCode();
                string content = response.Content.ReadAsStringAsync().Result;
                Console.WriteLine("Version 2 response: '{0}'\n", content);
            }
        }
    }
}
