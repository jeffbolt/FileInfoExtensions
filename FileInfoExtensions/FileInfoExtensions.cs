using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FileInfoExtensions
{
	/*  Platform Invoke (P/Invoke)
		http://www.pinvoke.net/default.aspx/shell32/SHGetFileInfo.html	
		https://docs.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke
		https://stackoverflow.com/questions/1437382/get-file-type-in-net#1437451
	*/

	public static class FileInfoExtensions
	{
		#region Private Constants

		/// <summary>Maximal Length of unmanaged Windows-Path-strings</summary>
		private const int MAX_PATH = 260;
		/// <summary>Maximal Length of unmanaged Typename</summary>
		private const int MAX_TYPE = 80;

		#endregion

		#region Private Enums

		[Flags]
		private enum SHGFI : uint
		{
			/// <summary>get icon</summary>
			Icon = 0x000000100,
			/// <summary>get display name</summary>
			DisplayName = 0x000000200,
			/// <summary>get type name</summary>
			TypeName = 0x000000400,
			/// <summary>get attributes</summary>
			Attributes = 0x000000800,
			/// <summary>get icon location</summary>
			IconLocation = 0x000001000,
			/// <summary>return exe type</summary>
			ExeType = 0x000002000,
			/// <summary>get system icon index</summary>
			SysIconIndex = 0x000004000,
			/// <summary>put a link overlay on icon</summary>
			LinkOverlay = 0x000008000,
			/// <summary>show icon in selected state</summary>
			Selected = 0x000010000,
			/// <summary>get only specified attributes</summary>
			Attr_Specified = 0x000020000,
			/// <summary>get large icon</summary>
			LargeIcon = 0x000000000,
			/// <summary>get small icon</summary>
			SmallIcon = 0x000000001,
			/// <summary>get open icon</summary>
			OpenIcon = 0x000000002,
			/// <summary>get shell size icon</summary>
			ShellIconSize = 0x000000004,
			/// <summary>pszPath is a pidl</summary>
			PIDL = 0x000000008,
			/// <summary>use passed dwFileAttribute</summary>
			UseFileAttributes = 0x000000010,
			/// <summary>apply the appropriate overlays</summary>
			AddOverlays = 0x000000020,
			/// <summary>Get the index of the overlay in the upper 8 bits of the iIcon</summary>
			OverlayIndex = 0x000000040,
		}

		private enum LoadLibraryFlags : uint
		{
			DONT_RESOLVE_DLL_REFERENCES = 0x00000001,
			LOAD_IGNORE_CODE_AUTHZ_LEVEL = 0x00000010,
			LOAD_LIBRARY_AS_DATAFILE = 0x00000002,
			LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE = 0x00000040,
			LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x00000020,
			LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008
		}

		#endregion

		#region Private Structs

		[StructLayout(LayoutKind.Sequential)]
		private struct SHFILEINFO
		{
			public IntPtr hIcon;
			public int iIcon;
			public uint dwAttributes;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
			public string szDisplayName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_TYPE)]
			public string szTypeName;
		};

		#endregion

		#region Private Classes

		private static class FILE_ATTRIBUTE
		{
			// https://docs.microsoft.com/en-us/windows/win32/fileio/file-attribute-constants
			public const uint FILE_ATTRIBUTE_ARCHIVE = 0x20;                    // 32
			public const uint FILE_ATTRIBUTE_COMPRESSED = 0x800;                // 2048
			public const uint FILE_ATTRIBUTE_DEVICE = 0x40;                     // 64 
			public const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;                  // 16
			public const uint FILE_ATTRIBUTE_ENCRYPTED = 0x4000;                // 16384
			public const uint FILE_ATTRIBUTE_NORMAL = 0x80;                     // 128
			public const uint FILE_ATTRIBUTE_NOT_CONTENT_INDEXED = 0x2000;      // 8192
			public const uint FILE_ATTRIBUTE_NO_SCRUB_DATA = 0x20000;           // 131072
			public const uint FILE_ATTRIBUTE_OFFLINE = 0x1000;                  // 4096
			public const uint FILE_ATTRIBUTE_READONLY = 0x1;                    // 1
			public const uint FILE_ATTRIBUTE_RECALL_ON_DATA_ACCESS = 0x400000;  // 14194304
			public const uint FILE_ATTRIBUTE_RECALL_ON_OPEN = 0x40000;          // 262144
			public const uint FILE_ATTRIBUTE_REPARSE_POINT = 0x400;             // 1024
			public const uint FILE_ATTRIBUTE_SPARSE_FILE = 0x200;               // 512
			public const uint FILE_ATTRIBUTE_SYSTEM = 0x4;                      // 3
			public const uint FILE_ATTRIBUTE_TEMPORARY = 0x100;                 // 256
			public const uint FILE_ATTRIBUTE_VIRTUAL = 0x10000;                 // 65536
		}

		#endregion

		#region Private DllImports

		[DllImport("shell32.dll")]
		private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);
		
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		private static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern int DestroyIcon(IntPtr hIcon);

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, LoadLibraryFlags dwFlags);

		#endregion

		#region Public Methods

		/// <summary>
		/// Extension method to get the file type of a FileInfo object
		/// </summary>
		/// <param name="fileInfo">FileInfo object</param>
		/// <returns></returns>
		public static string GetFileType(this FileInfo fileInfo)
		{
			if (SHGetFileInfo(fileInfo.Extension, (uint)SHGFI.Attributes, out SHFILEINFO shFileInfo, (uint)Marshal.SizeOf(typeof(SHFILEINFO)),
				(uint)(SHGFI.UseFileAttributes | SHGFI.TypeName)) != IntPtr.Zero)
				return shFileInfo.szTypeName;
			else
				return string.Empty;
		}

		/// <summary>
		/// Returns the associated Icon for a file or application.
		/// If the file path is invalid or there is no icon, the default icon is returned.
		/// </summary>
		/// <param name="filePath">Full path to the file</param>
		/// <param name="small">If true, the 16x16 icon is returned otherwise the 32x32</param>
		/// <returns></returns>
		public static Icon GetIcon(this FileInfo fileInfo, bool small)
		{
			SHFILEINFO info = new();
			int cbFileInfo = Marshal.SizeOf(info);
			SHGFI flags;
			if (small)
				flags = SHGFI.Icon | SHGFI.SmallIcon | SHGFI.UseFileAttributes;
			else
				flags = SHGFI.Icon | SHGFI.LargeIcon | SHGFI.UseFileAttributes;

			SHGetFileInfo(fileInfo.FullName, 256, out info, (uint)cbFileInfo, (uint)flags);
			return Icon.FromHandle(info.hIcon);
		}

		/// <summary>Returns an Icon of given size.</summary>
		/// <param name="path">Path to a file (.exe/.dll) that contains the icon(s). Skip it or use <c>null</c> to use current application's file.</param>
		/// <param name="resId">Name of the resource icon that should be loaded. Skip it to use the default <c>#32512</c> (value of <c>IDI_APPLICATION</c>)
		/// to use the application's icon.</param>
		/// <param name="size">Size of the icon to load. If there is no such size available, a larger or smaller-sized icon is scaled.</param>
		/// <returns>The Icon</returns>
		public static Icon? GetIconFromExe(this FileInfo fileInfo, string resId = "#32512", int size = 32)
		{
			// From http://pinvoke.net/default.aspx/user32/LoadImage.html
			// load module
			IntPtr h;
			if (fileInfo == null)
				h = Marshal.GetHINSTANCE(Assembly.GetEntryAssembly().GetModules()[0]);
			else
			{
				h = LoadLibraryEx(fileInfo.FullName, IntPtr.Zero, LoadLibraryFlags.LOAD_LIBRARY_AS_DATAFILE);
				if (h == IntPtr.Zero)
					return null;
			}

			// 1 is IMAGE_ICON
			IntPtr ptr = LoadImage(h, resId, 1, size, size, 0);
			if (ptr != IntPtr.Zero)
			{
				try
				{
					Icon icon = (Icon)Icon.FromHandle(ptr).Clone();
					return icon;
				}
				finally
				{
					_ = DestroyIcon(ptr);
				}
			}
			return null;
		}

		public static string GetBaseDirectory()
		{
			return Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase).Replace(@"file:\", "");
		}

		public static string GetTempDirectory()
		{
			return Path.Combine(Directory.GetParent(GetBaseDirectory()).Parent.FullName, "Temp");
		}

		public static string GetMimeContentType(string mimeType)
		{
			// Return the file extension part of the MIME string (i.e. "image/pjpeg")
			var arrKeys = mimeType.Split('/');
			if (arrKeys.Length == 2)
				return arrKeys[1].Trim();
			else
				return "";
		}

		/// <summary>Extension method for displaying the size of a FileInfo object</summary>
		/// <param name="fileInfo">FileInfo object</param>
		/// <param name="decimalPlaces">Number of decimal places to display</param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public static string GetFileSizeSuffix(this FileInfo fileInfo, int decimalPlaces = 0)
		{
			// From https://stackoverflow.com/questions/14488796/does-net-provide-an-easy-way-convert-bytes-to-kb-mb-gb-etc#14488941
			string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

			if (decimalPlaces < 0) throw new ArgumentOutOfRangeException(nameof(decimalPlaces));
			long value = fileInfo.Length;
			if (value <= 0) return string.Format("{0:n" + decimalPlaces + "} bytes", 0);

			// mag is 0 for bytes, 1 for KB, 2, for MB, etc.
			int mag = (int)Math.Log(value, 1024);

			// 1L << (mag * 10) == 2 ^ (10 * mag) 
			// [i.e. the number of bytes in the unit corresponding to mag]
			decimal adjustedSize = (decimal)value / (1L << (mag * 10));

			// make adjustment when the value is large enough that it would round up to 1000 or more
			if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
			{
				mag += 1;
				adjustedSize /= 1024;
			}

			return string.Format("{0:n" + decimalPlaces + "} {1}", adjustedSize, SizeSuffixes[mag]);
		}

		/// <summary>Returns a hexidecimal string representation of the file's binary contents</summary>
		/// <param name="fileInfo"></param>
		/// <returns></returns>
		/// <exception cref="FileNotFoundException"></exception>
		public static string ToHexString(this FileInfo fileInfo)
		{
			if (File.Exists(fileInfo.FullName))
			{
				byte[] bytes = File.ReadAllBytes(fileInfo.FullName);
				return BitConverter.ToString(bytes).Replace("-", string.Empty);
			}
			else
			{
				throw new FileNotFoundException($"Could not find the file '{fileInfo.FullName}'.");
			}
		}

		/// <summary>Creates a new file as a hexidecimal string representation from a file's binary contents.
		/// If the target file already exists, it is overwritten.</summary>
		/// <param name="fileInfo"></param>
		/// <param name="outputFilePath">The path to save the file</param>
		public static void SaveAsHex(this FileInfo fileInfo, string outputFilePath)
		{
			string fileContents = fileInfo.ToHexString();
			if (!string.IsNullOrEmpty(fileContents))
				File.WriteAllText(outputFilePath, fileContents);
		}

		/// <summary>Returns the base-64 string representation of an image file</summary>
		/// <param name="fileInfo"></param>
		/// <returns></returns>
		/// <exception cref="FileNotFoundException"></exception>
		public static string ConvertToBase64(this FileInfo fileInfo)
		{
			if (File.Exists(fileInfo.FullName))
			{
				byte[] bytes = File.ReadAllBytes(fileInfo.FullName);
				return Convert.ToBase64String(bytes);
			}
			else
			{
				throw new FileNotFoundException($"Could not find the file '{fileInfo.FullName}'.");
			}
		}

		#endregion
	}
}