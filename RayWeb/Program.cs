using Raylib_cs;
using CefSharp;
using CefSharp.OffScreen;

namespace RayWeb
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			const string testUrl = "https://www.youtube.com/";

			Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
			Raylib.InitWindow(1280, 720, "Webbo");
			Raylib.InitAudioDevice();
			Raylib.SetTargetFPS(60);

			var webview = new RayWebView();
			if (webview.InitializeAsync("raywebUI").Result == false)
			{
				Console.WriteLine("Failed to initialize raylib webview");
				return;
			}

			//webview.Browser.LoadUrl(testUrl);

			//webview.Browser.LoadHtmlFile(Directory.GetCurrentDirectory() + "/raywebUI/index.html");

			while (Raylib.WindowShouldClose() == false)
			{
				Raylib.BeginDrawing();
				Raylib.ClearBackground(Color.WHITE);

				webview.Render();

				Raylib.DrawFPS(32, 32);

				Raylib.EndDrawing();

			}
			Raylib.CloseWindow();
			webview.Dispose();
		}
	}
}