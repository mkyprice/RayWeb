using Raylib_cs;

namespace RayWeb
{
	internal class Program
	{
		static void Main(string[] args)
		{
			const int width = 1280;
			const int height = 720;

			// Initialize our Raylib Window
			Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
			Raylib.InitWindow(width, height, "RayWeb");
			Raylib.InitAudioDevice();
			Raylib.SetTargetFPS(60);

			// Initialize our webview to half the size of the screen
			var webview = new RayWebView(width / 2, height / 2);
			if (webview.InitializeAsync("raywebUI").Result == false)
			{
				Console.WriteLine("Failed to initialize raylib webview");
				return;
			}

			// We're loading local files so this is currently commented out
			//webview.Browser.LoadUrl("youtube.com");

			// Main Game Loop
			while (Raylib.WindowShouldClose() == false)
			{
				// Moving the webview
				int horizontal = Raylib.IsKeyDown(KeyboardKey.KEY_LEFT) ? -1 : Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT) ? 1 : 0;
				int vertical = Raylib.IsKeyDown(KeyboardKey.KEY_UP) ? -1 : Raylib.IsKeyDown(KeyboardKey.KEY_DOWN) ? 1 : 0;

				if (horizontal != 0) webview.X += horizontal;
				if (vertical != 0) webview.Y += vertical;

				// Drawing
				Raylib.BeginDrawing();
				Raylib.ClearBackground(Color.RAYWHITE);

				webview.Render();

				Raylib.DrawFPS(32, 32);

				Raylib.EndDrawing();
			}

			// Cleanup
			Raylib.CloseWindow();
			webview.Dispose();
		}
	}
}