using CefSharp.OffScreen;
using CefSharp;
using Raylib_cs;
using System.Drawing;
using CefSharp.Structs;
using System.Drawing.Imaging;
using System.Text;

namespace RayWeb
{
	public class RayCefBrowser : ChromiumWebBrowser
	{
		public int MouseX { get; private set; }
		public int MouseY { get; private set; }
		private object ImageLock = new object();
		private Raylib_cs.Image? CurrentImage = null;
		private Texture2D? Frame = null;
		private readonly byte[] PNG_CACHE = Encoding.ASCII.GetBytes(".png");
		private readonly Dictionary<MouseButtonType, int> _mouseDownCount = new Dictionary<MouseButtonType, int>()
		{
			{ MouseButtonType.Left, 0 },
			{ MouseButtonType.Right, 0 },
			{ MouseButtonType.Middle, 0 }
		};

		public RayCefBrowser(string address = "", IBrowserSettings browserSettings = null,
			IRequestContext requestContext = null, bool automaticallyCreateBrowser = false,
			Action<IBrowser> onAfterBrowserCreated = null, bool useLegacyRenderHandler = false)
			: base(address, browserSettings, requestContext, automaticallyCreateBrowser, onAfterBrowserCreated, useLegacyRenderHandler)
		{
			Initialize();
		}

		/// <summary>
		/// Resize the browser
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public void Resize(int width, int height)
		{
			this.Size = new System.Drawing.Size(width, height);
		}

		/// <summary>
		/// Render in 2D at the given coordinates
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		public void Render(int x, int y)
		{
			if (TryGetTexture(out Texture2D texture))
			{
				Raylib.DrawTexture(texture, x, y, Raylib_cs.Color.WHITE);
			}
		}

		/// <summary>
		/// Get browser Texture
		/// </summary>
		/// <param name="texture"></param>
		/// <returns></returns>
		public bool TryGetTexture(out Texture2D texture)
		{
			lock (ImageLock)
			{
				if (CurrentImage != null)
				{
					if (Frame != null) Raylib.UnloadTexture((Texture2D)Frame);
					Frame = Raylib.LoadTextureFromImage((Raylib_cs.Image)CurrentImage);
					Raylib.UnloadImage((Raylib_cs.Image)CurrentImage);
					CurrentImage = null;
				}
			}

			if (Frame != null)
			{
				texture = (Texture2D)Frame;
			}
			else
			{
				texture = default;
			}
			return Frame != null;
		}

		public void SetMousePosition(int x, int y)
		{
			MouseX = x;
			MouseY = y;
			GetBrowser().GetHost().SendMouseMoveEvent(MouseX, MouseY, false, CefEventFlags.None);
		}
		
		public void HandleMouseButtonDown(MouseButtonType but)
		{
			if (IsBrowserInitialized)
			{
				_mouseDownCount[but]++;
				GetBrowser().GetHost().SendMouseClickEvent(MouseX, MouseY, but, false, _mouseDownCount[but], CefEventFlags.None);
				//GetBrowser().GetHost().Invalidate(PaintElementType.View);
			}
		}

		public void HandleMouseButtonUp(MouseButtonType but)
		{
			if (IsBrowserInitialized)
			{
				_mouseDownCount[but] = 0;
				GetBrowser().GetHost().SendMouseClickEvent(MouseX, MouseY, but, true, 1, CefEventFlags.None);
				//GetBrowser().GetHost().Invalidate(PaintElementType.View);
			}
		}

		/// <summary>
		/// Handling of key events
		/// </summary>
		/// <param name="keyCode"></param>
		public void HandleKeyEvent(KeyEvent key)
		{
			if (IsBrowserInitialized)
			{
				GetBrowser().GetHost().SendKeyEvent(key);
				GetBrowser().GetHost().Invalidate(PaintElementType.View);
			}
		}

		/// <summary>
		/// Load a local html file
		/// Sets up a file watcher that will reload it if there are any changes
		/// </summary>
		/// <param name="file"></param>
		public void LoadHtmlFile(string file)
		{
			string? name = Path.GetDirectoryName(file);
			if (string.IsNullOrEmpty(name))
			{
				Console.WriteLine("Failed to find file {0}", file);
				return;
			}
			FileSystemWatcher watcher = new FileSystemWatcher(file);
			watcher.Filter = Path.GetFileName(file);
			watcher.Changed += FileChanged;
			watcher.EnableRaisingEvents = true;
			this.LoadHtml(File.ReadAllText(file));
		}

		public new void Dispose()
		{
			base.Dispose();
			Cef.Shutdown();
		}

		#region Private Functions
		
		private void Initialize()
		{
			this.Paint += OnCefPaint;
		}

		/// <summary>
		/// Callback for new screen image from chromium
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnCefPaint(object? sender, OnPaintEventArgs e)
		{
			if (e.DirtyRect.Width == 0 || e.DirtyRect.Height == 0) return;

			Bitmap bmp = new Bitmap(e.Width, e.Height, 4 * e.Width, System.Drawing.Imaging.PixelFormat.Format32bppArgb, e.BufferHandle);
			ConvertTexture(bmp, e.DirtyRect);
		}

		/// <summary>
		/// Converts a Bitmap into our Raylib render Texture
		/// </summary>
		/// <param name="bmp"></param>
		/// <param name="rect"></param>
		private unsafe void ConvertTexture(Bitmap bmp, Rect rect)
		{
			byte[] data;
			using (MemoryStream ms = new MemoryStream())
			{
				bmp.Save(ms, ImageFormat.Png);
				data = ms.ToArray();
			}

			fixed (byte* ptr = data)
			{
				lock (ImageLock)
				{
					if (CurrentImage != null)
					{
						Raylib.UnloadImage((Raylib_cs.Image)CurrentImage);
						CurrentImage = null;
					}

					fixed (byte* png = PNG_CACHE)
					{
						CurrentImage = Raylib.LoadImageFromMemory((sbyte*)png, ptr, data.Length);
					}
				}
			}
		}

		/// <summary>
		/// Callback for when local files are updated. Loads new changes to watched file
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void FileChanged(object sender, FileSystemEventArgs e)
		{
			Console.WriteLine(e.FullPath);
			using (var fs = File.Open(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			using (var sr = new StreamReader(fs, Encoding.Default))
				this.LoadHtml(sr.ReadToEnd());
		}

		#endregion
	}
}
