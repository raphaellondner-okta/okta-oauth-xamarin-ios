using System;
using System.Text;
using IdentityModel.OidcClient;

using UIKit;
using Foundation;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace iOSClient
{
	public partial class ViewController : UIViewController
	{

        private string OAuthClientId = "79arVRKBcBEYMuMOXrYF";
        private string OAuthRedirectUrl = "com.oktapreview.example:/oauth";
        private string oktaTenantUrl = "https://example.oktapreview.com";


        SafariServices.SFSafariViewController safari;
		OidcClient _client;
		AuthorizeState _state;
		HttpClient _apiClient;

		public ViewController (IntPtr handle) : base (handle)
		{
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			LoginButton.TouchUpInside += LoginButton_TouchUpInside;
			CallApiButton.TouchUpInside += CallApiButton_TouchUpInside;
		}

		async void LoginButton_TouchUpInside (object sender, EventArgs e)
		{
            //var authority = "https://demo.identityserver.io";
            var authority = "https://example.oktapreview.com";

            var options = new OidcClientOptions (
				authority,
				OAuthClientId,
				"secret",
				"openid profile email offline_access groups",
				OAuthRedirectUrl);

			_client = new OidcClient(options);
			_state = await _client.PrepareLoginAsync();


			AppDelegate.CallbackHandler = HandleCallback;
			safari = new SafariServices.SFSafariViewController (new NSUrl (_state.StartUrl));

			this.PresentViewController (safari, true, null);
		}

		async void CallApiButton_TouchUpInside (object sender, EventArgs e)
		{
			if (_apiClient == null) 
			{
				return;
			}
			
			var result = await _apiClient.GetAsync("test");
			if (!result.IsSuccessStatusCode) 
			{
				OutputTextView.Text = result.ReasonPhrase;
				return;
			}

			var content = await result.Content.ReadAsStringAsync ();
			OutputTextView.Text = JArray.Parse (content).ToString ();
		}

		async void HandleCallback(string url)
		{
			await safari.DismissViewControllerAsync (true);

			var result = await _client.ValidateResponseAsync (url, _state);

			var sb = new StringBuilder (128);
			foreach (var claim in result.Claims) 
			{
				sb.AppendFormat ("{0}: {1}\n", claim.Type, claim.Value);
			}

            sb.AppendFormat("\n{0}: {1}\n", "id token", result.IdentityToken);
            sb.AppendFormat ("\n{0}: {1}\n", "refresh token", result.RefreshToken);
			sb.AppendFormat ("\n{0}: {1}\n", "access token", result.AccessToken);

			OutputTextView.Text = sb.ToString ();
			_apiClient = new HttpClient ();
			_apiClient.SetBearerToken (result.AccessToken);
			_apiClient.BaseAddress = new Uri ("https://demo.identityserver.io/api/");


		}

		public override void DidReceiveMemoryWarning ()
		{
			base.DidReceiveMemoryWarning ();
			// Release any cached data, images, etc that aren't in use.
		}
	}
}

