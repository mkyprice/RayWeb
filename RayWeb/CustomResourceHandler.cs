//using CefSharp.DevTools.Network;
//using CefSharp;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace RayWeb
//{
//	internal class CustomResourceHandler : ResourceHandler
//	{
//		private string FolderPath;

//		public CustomResourceHandler()
//		{

//		}

//		public override bool ProcessRequestAsync(IRequest request, ICallback callback)
//		{
//			var uri = new Uri(request.Url);
//			var fileName = uri.AbsolutePath;

//			var requestedFilePath = FolderPath + fileName;

//			if (File.Exists(requestedFilePath))
//			{
//				byte[] bytes = File.ReadAllBytes(requestedFilePath);
//				Stream = new MemoryStream(bytes);

//				var fileExtension = Path.GetExtension(fileName);
//				MimeType = GetMimeType(fileExtension);

//				callback.Continue();
//				return true;
//			}

//			callback.Dispose();
//			return false;
//		}
//	}
//}
