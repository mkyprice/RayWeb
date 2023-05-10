using CefSharp;
using CefSharp.OffScreen;
using CefSharp.SchemeHandler;
using Raylib_cs;
using System.Diagnostics;

namespace RayWeb
{
	public class RayWebView : IDisposable
	{
		public static int MAX_INITIALIZE_WAIT_MS = 5000;
		public RaycefBrowser Browser { get; private set; }

		public int X, Y;
		private int _width;
		public int Width
		{
			get { return _width; }
			set
			{
				_width = value;
				Browser?.Resize(Width, Height);
			}
		}
		public int _height;
		public int Height
		{
			get { return _height; }
			set
			{
				_height = value;
				Browser?.Resize(Width, Height);
			}
		}

		public RayWebView(int width, int height)
		{
			Width = width;
			Height = height;
		}

		public void Render()
		{
			if (Browser?.IsBrowserInitialized == false) return;

			int x = Raylib.GetMouseX() - X;
			int y = Raylib.GetMouseY() - Y;
			Browser.SetMousePosition(x, y);

			if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT))
			{
				Browser.HandleMouseButtonDown(ConvertMouseButton(MouseButton.MOUSE_BUTTON_LEFT), x, y);
			}

			if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_RIGHT))
			{
				Browser.HandleMouseButtonDown(ConvertMouseButton(MouseButton.MOUSE_BUTTON_RIGHT), x, y);
			}

			if (Raylib.IsMouseButtonReleased(MouseButton.MOUSE_BUTTON_LEFT))
			{
				Browser.HandleMouseButtonUp(ConvertMouseButton(MouseButton.MOUSE_BUTTON_LEFT), x, y);
			}

			if (Raylib.IsMouseButtonReleased(MouseButton.MOUSE_BUTTON_RIGHT))
			{
				Browser.HandleMouseButtonUp(ConvertMouseButton(MouseButton.MOUSE_BUTTON_RIGHT), x, y);
			}

			int key_code;
			do
			{
				key_code = Raylib.GetKeyPressed();
				if (key_code != 0)
				{
					KeyEvent key = new KeyEvent()
					{
						Type = KeyEventType.RawKeyDown,
						NativeKeyCode = key_code,
						Modifiers = CefEventFlags.None,
						WindowsKeyCode = key_code,
					};

					Browser.HandleKeyEvent(key);
				}
			}
			while (key_code != 0);

			Browser.Render(X, Y);
		}

		public async Task<bool> InitializeAsync(string localfiles = null)
		{
			CefSettings settings = new CefSettings();
			settings.CachePath = Directory.GetCurrentDirectory() + "/cache";

			settings.EnableAudio();

			settings.SetOffScreenRenderingBestPerformanceArgs();

			settings.WindowlessRenderingEnabled = true;

			settings.MultiThreadedMessageLoop = true;

			settings.RemoteDebuggingPort = 9001;

#if DEBUG
			// Allows debugging
			settings.CefCommandLineArgs.Add("remote-allow-origins", "*");
			settings.CefCommandLineArgs.Add("disable-features", "BlockInsecurePrivateNetworkRequests");
#endif

			string default_address = null;

			// Loads all custom files. Only necessary if loading websites off your local drive
			if (string.IsNullOrEmpty(localfiles) == false)
			{
				settings.RegisterScheme(new CefCustomScheme()
				{
					SchemeName = "LocalFolder",
					DomainName = "cefSharp",
					SchemeHandlerFactory = new FolderSchemeHandlerFactory(
						rootFolder: localfiles,
						hostName: "cefSharp",
						defaultPage: "index.html"
						)
				});
				default_address = "localFolder://cefSharp/";
			}

			// Initialize Cef with our custom settings
			Cef.Initialize(settings);

			// Set browser to local file directory or null for external sites
			Browser = new RaycefBrowser(default_address);

			unsafe
			{
				Browser.CreateBrowser(new WindowInfo()
				{
					WindowlessRenderingEnabled = true,
					//ParentWindowHandle = (IntPtr)Raylib.GetWindowHandle()
				}, new BrowserSettings(true)
				{
					WindowlessFrameRate = 30,
				});
			}

			Stopwatch sw = Stopwatch.StartNew();
			while (Browser.IsBrowserInitialized == false && sw.ElapsedMilliseconds < MAX_INITIALIZE_WAIT_MS)
			{
				await Task.Delay(1);
				Console.WriteLine("Waiting for Cef initialization... {0}s", sw.Elapsed.TotalSeconds);
			}
			Browser.Resize(Width, Height);

			return Browser.IsBrowserInitialized;
		}

		public void Dispose()
		{
			Browser.Dispose();
		}

		private MouseButtonType ConvertMouseButton(MouseButton button)
		{
			switch (button)
			{
				case MouseButton.MOUSE_BUTTON_LEFT:
					return MouseButtonType.Left;
				case MouseButton.MOUSE_BUTTON_RIGHT:
					return MouseButtonType.Right;
				case MouseButton.MOUSE_BUTTON_MIDDLE:
					return MouseButtonType.Middle;
				default:
					Console.WriteLine("Mouse button {0} has no CEF conversion", button);
					break;
			}
			return (MouseButtonType)button;
		}
	}
}
