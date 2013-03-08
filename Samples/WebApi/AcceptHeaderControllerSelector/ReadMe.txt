AcceptHeaderControllerSelector
------------------------------

This ASP.NET Web API sample shows how to support multiple API controllers with
the same name in different namespaces. 

For example, you could define two controllers named "ValuesController":

	MyApplication.V1.ValuesController
	MyApplication.V2.ValuesController

and then invoke the controllers with one standard resource URL:

	/api/values

and specify the version of the API to call using Accept headers:

	Accept: application/vnd.api.v1+json
	Accept: application/vnd.api.v2+json

To make this work, the sample provides a custom implementation of the 
IHttpControllerSelector interface. The Web API pipeline uses this interface 
to select a controller during routing.

The custom IHttpControllerSelector looks for the Accepts header in the
incoming request. A lambda shouldbe given for parsing the header and
returning a namespace. For example:

	accept =>
    {
		var matches = Regex.Match(accept, @"application\/vnd.api.(.*)\+.*");

        if (matches.Groups.Count >= 2)
        {
			return matches.Groups[1].Value;
        }

        return "v2"; // default namespace, return null to throw 404 when namespace not given
    }

When you run the console application, it sends an HTTP request to the "v1"
controller and another to the "v2" controller, and displays the results.