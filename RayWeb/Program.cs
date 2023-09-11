using CefSharp.DevTools.CacheStorage;
using Raylib_cs;
using System.Numerics;

namespace RayWeb
{
	internal class Program
	{
		static void Main(string[] args)
		{
			int screen_width = 1280;
			int screen_height = 720;

			// Initialize our Raylib Window
			Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
			Raylib.InitWindow(screen_width, screen_height, "RayWeb");
			Raylib.InitAudioDevice();
			Raylib.SetTargetFPS(60);

			// Initialize our webview to half the size of the screen
			int web_view_width = (int)(screen_width / 1.5f);
			int web_view_height = (int)(screen_height / 1.5f);
			RayWebView webview = new RayWebView(web_view_width, web_view_height);
			if (webview.InitializeAsync("www.google.com").Result == false)
			{
				Console.WriteLine("Failed to initialize raylib webview");
				return;
			}
			webview.X = web_view_width / 4;
			webview.Y = web_view_height / 4;

			// We're loading local files so this is currently commented out
			//webview.Browser.LoadUrl("google.com");

			// Our fun little background shader
			Shader background = Raylib.LoadShader(null, "resources/shaders/background.fs");
			int resolution_loc = Raylib.GetShaderLocation(background, "u_resolution");
			int time_loc = Raylib.GetShaderLocation(background, "Time");
			float time = 0;
			Vector2 resolution = new Vector2(screen_width, screen_height);
			Raylib.SetShaderValue(background, resolution_loc, resolution, ShaderUniformDataType.SHADER_UNIFORM_VEC2);

			// Main Game Loop
			while (Raylib.WindowShouldClose() == false)
			{
				// Moving the webview
				int horizontal = Raylib.IsKeyDown(KeyboardKey.KEY_LEFT) ? -1 : Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT) ? 1 : 0;
				int vertical = Raylib.IsKeyDown(KeyboardKey.KEY_UP) ? -1 : Raylib.IsKeyDown(KeyboardKey.KEY_DOWN) ? 1 : 0;
				if (horizontal != 0) webview.X += horizontal;
				if (vertical != 0) webview.Y += vertical;
				
				// Update shader
				time += Raylib.GetFrameTime();
				Raylib.SetShaderValue(background, time_loc, time, ShaderUniformDataType.SHADER_UNIFORM_FLOAT);

				// Drawing
				Raylib.BeginDrawing();
				Raylib.ClearBackground(Color.RAYWHITE);

				// Draw our background
				Raylib.BeginShaderMode(background);
				Raylib.DrawRectangle(0, 0, screen_width, screen_height, Color.BLACK);
				Raylib.EndShaderMode();

				// Draw our browser
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