using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ServerCore
{
    public class FileRoom
    {
        byte[] file;
        string root = "C:\\Users\\mhoow\\AppData\\LocalLow\\JWY\\Knight\\Resources";

        public int seek
        {
            get;
            private set;
        }
        public int fileSize { get => file.Length; }

        public bool Alloc(int fileSize)
        {
            if (file == null)
            {
                file = new byte[fileSize];
                return true;
            }
            
            if (fileSize > file.Length)
            {
                file = new byte[fileSize];
                return true;
            }

            return false;
        }

        public void AddData(S_FileResponse response)
        {
            Array.Copy(response.file, 0, file, seek, response.file.Length);
            seek += response.file.Length;
        }

        public bool IsFull()
        {
            return seek == file.Length ? true : false;
        }

        public void MakeFile(string fileName)
        {
            if (!Directory.Exists(root))
                Directory.CreateDirectory(root);
            
            File.WriteAllBytes(root + "\\" + fileName, file);
            file = null;
        }
    }
}
