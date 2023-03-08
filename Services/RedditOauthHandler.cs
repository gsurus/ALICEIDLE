using System.Diagnostics;



namespace ALICEIDLE.Services
{

    public class RedditToken
    {
        public static string AuthorizeUser(string appId, string appSecret = null, int port = 8080)
        {
            // Create a new instance of the auth token retrieval library.  --Kris
            //AuthTokenRetrieverLib authTokenRetrieverLib = new AuthTokenRetrieverLib(appId, port, appSecret: appSecret);

            // Start the callback listener.  --Kris
            // Note - Ignore the logging exception message if you see it.  You can use Console.Clear() after this call to get rid of it if you're running a console app.
            //authTokenRetrieverLib.AwaitCallback();

            // Open the browser to the Reddit authentication page.  Once the user clicks "accept", Reddit will redirect the browser to localhost:8080, where AwaitCallback will take over.  --Kris
            //string authUrl = authTokenRetrieverLib.AuthURL();
            //Console.WriteLine(authUrl);
            //OpenBrowser(authUrl);
            //Console.WriteLine(authTokenRetrieverLib.AuthURL());
            // Replace this with whatever you want the app to do while it waits for the user to load the auth page and click Accept.  --Kris
            //Console.ReadKey();
            // Cleanup.  --Kris
            //authTokenRetrieverLib.StopListening();

            //return authTokenRetrieverLib.RefreshToken;
            return null;
        }

        public static async void OpenBrowser(string authUrl, string browserPath = @"C:\Program Files\Google\Chrome\Application\chrome.exe")
        {
            try
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo(authUrl);
                var process = Process.Start(processStartInfo);
                process.WaitForExitAsync();
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // This typically occurs if the runtime doesn't know where your browser is.  Use BrowserPath for when this happens.  --Kris
                ProcessStartInfo processStartInfo = new ProcessStartInfo(browserPath)
                {
                    Arguments = authUrl
                };
                Process.Start(processStartInfo);
            }
        }
    }
}
