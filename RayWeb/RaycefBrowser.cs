using CefSharp.OffScreen;
using CefSharp;
using Raylib_cs;
using System.Drawing;
using CefSharp.Structs;
using System.Drawing.Imaging;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace RayWeb
{
	public class RaycefBrowser : ChromiumWebBrowser
	{
		private object ImageLock = new object();
		private Raylib_cs.Image? CurrentImage = null;
		private Texture2D? Frame = null;

		public RaycefBrowser(string address = "", IBrowserSettings browserSettings = null,
			IRequestContext requestContext = null, bool automaticallyCreateBrowser = false,
			Action<IBrowser> onAfterBrowserCreated = null, bool useLegacyRenderHandler = false)
			: base(address, browserSettings, requestContext, automaticallyCreateBrowser, onAfterBrowserCreated, useLegacyRenderHandler)
		{
			Initialize();
		}

		private void Initialize()
		{
			this.Paint += OnCefPaint;
		}

		public void Resize(int width, int height)
		{
			this.Size = new System.Drawing.Size(width, height);
		}

		public new void Dispose()
		{
			base.Dispose();
			Cef.Shutdown();
		}

		public void Render(int x, int y)
		{
			if (TryGetRenderTexture(out Texture2D texture))
			{
				Raylib.DrawTexture(texture, x, y, Raylib_cs.Color.WHITE);
			}
		}

		public bool TryGetRenderTexture(out Texture2D texture)
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
			GetBrowser().GetHost().SendMouseMoveEvent(x, y, false, CefEventFlags.None);
		}

		private readonly Dictionary<MouseButtonType, int> DownCount = new Dictionary<MouseButtonType, int>()
		{
			{ MouseButtonType.Left, 0 },
			{ MouseButtonType.Right, 0 },
			{ MouseButtonType.Middle, 0 }
		};
		public void HandleMouseButtonDown(MouseButtonType but, int x, int y)
		{
			if (IsBrowserInitialized)
			{
				DownCount[but]++;
				GetBrowser().GetHost().SendMouseClickEvent(x, y, but, false, DownCount[but], CefEventFlags.None);
				//GetBrowser().GetHost().Invalidate(PaintElementType.View);
			}
		}

		public void HandleMouseButtonUp(MouseButtonType but, int x, int y)
		{
			if (IsBrowserInitialized)
			{
				DownCount[but] = 0;
				GetBrowser().GetHost().SendMouseClickEvent(x, y, but, true, 1, CefEventFlags.None);
				//GetBrowser().GetHost().Invalidate(PaintElementType.View);
			}
		}

		public void HandleKeyEvent(KeyEvent key)
		{
			if (IsBrowserInitialized)
			{
				GetBrowser().GetHost().SendKeyEvent(key);
				GetBrowser().GetHost().Invalidate(PaintElementType.View);
			}
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
			if (bmp != null)
			{
				ConvertTexture(bmp, e.DirtyRect);
			}
		}

		/// <summary>
		/// Converts a Bitmap into our Raylib render Texture
		/// </summary>
		/// <param name="bmp"></param>
		/// <param name="rect"></param>
		private void ConvertTexture(Bitmap bmp, Rect rect)
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();
			byte[] data;

			unsafe
			{
				using (MemoryStream ms = new MemoryStream())
				{
					bmp.Save(ms, ImageFormat.Png);
					data = ms.ToArray();
				}

				byte[] pngbytes = Encoding.ASCII.GetBytes(".png");
				fixed (byte* ptr = data)
				{
					lock (ImageLock)
					{
						if (CurrentImage != null)
						{
							Raylib.UnloadImage((Raylib_cs.Image)CurrentImage);
							CurrentImage = null;
						}

						fixed (byte* ptr2 = pngbytes)
						{
							CurrentImage = Raylib.LoadImageFromMemory((sbyte*)ptr2, ptr, data.Length);
						}
					}

					sw.Stop();
					Console.WriteLine("Total image load time: {0}", sw.ElapsedMilliseconds);
				}
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

		/// <summary>
		/// Loads new changes to watched file
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
	}
}
