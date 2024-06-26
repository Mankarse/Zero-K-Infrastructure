#region using

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using PlasmaShared;

#endregion

namespace ZkData
{
	public class Pool
	{
		readonly Dictionary<Hash, bool> files = new Dictionary<Hash, bool>();
		readonly List<string> readPaths = new List<string>();
		readonly string tempPath;
		readonly string writePath;

		public Pool(SpringPaths paths)
		{
			foreach (var p in paths.DataDirectories)
			{
				var readPath = Utils.MakePath(p, "pool");
				Utils.CheckPath(readPath);
				readPaths.Add(readPath);
			}

			writePath = Utils.MakePath(paths.WritableDirectory, "pool");
			Utils.CheckPath(writePath);

			tempPath = Utils.MakePath(paths.WritableDirectory, "temp");
			Utils.CheckPath(tempPath);
		}

		public bool Exists(Hash hash)
		{
			if (files.ContainsKey(hash)) return true;

			if (CheckAndAddOneFile(hash)) return true;
			return false;
		}


		public void PutToStorage(byte[] data, Hash hash)
		{
			var strhash = hash.ToString();
			var folder = strhash.Substring(0, 2);
			var name = strhash.Substring(2) + ".gz";

			folder = Path.Combine(writePath, folder);
			if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

			var gotHash = Hash.HashBytes(data.Decompress());
			if (gotHash != hash) throw new ApplicationException("File hash invalid, cannot add to pool");
			var tempFile = Path.Combine(tempPath, name);
			var targetFile = Path.Combine(folder, name);
			File.WriteAllBytes(tempFile, data);
            if (File.Exists(targetFile)) File.Delete(targetFile);
			File.Move(tempFile, targetFile);
		}

        public byte[] ReadFromStorage(Hash hash)
        {
            var strhash = hash.ToString();
            var folder = strhash.Substring(0, 2);
            var name = strhash.Substring(2) + ".gz";

            folder = Path.Combine(writePath, folder);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var targetFile = Path.Combine(folder, name);
            return File.ReadAllBytes(targetFile);
        }


        public MemoryStream ReadFromStorageDecompressed(Hash hash)
        {
            var strhash = hash.ToString();
            var folder = strhash.Substring(0, 2);
            var name = strhash.Substring(2) + ".gz";

            folder = Path.Combine(writePath, folder);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var targetFile = Path.Combine(folder, name);
            var ms = new MemoryStream();
            using (var fs = File.OpenRead(targetFile))
            {
                using (var gz = new GZipStream(fs, CompressionMode.Decompress))
                {
                    gz.CopyTo(ms);
                }
            }
            ms.Seek(0, SeekOrigin.Begin);

            return ms;
        }


        bool CheckAndAddOneFile(Hash hash)
		{
			if (readPaths.Any(x => File.Exists(GetStorageFileName(x, hash))))
			{
				files.Add(hash, true);
				return true;
			}
			return false;
		}


		string GetStorageFileName(string path, Hash hash)
		{
			var strHash = hash.ToString();
			return string.Format("{0}/{1}/{2}.gz", path, strHash.Substring(0, 2), strHash.Substring(2));
		}
	}
}