using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ServerCore
{
    public class FileContainer
    {
        public static FileContainer Instance { get; } = new FileContainer();

        string root;
        Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();
        const int MAX_SEND_DATA = 65000;


        // C:\Users\mhoow\AppData\LocalLow\JWY\AssetBundles
        public FileContainer(string _root = "C:\\Users\\mhoow\\AppData\\LocalLow\\JWY\\AssetBundles")
        {
            root = _root;
            ReadChildrenFile();
        }

        public ArraySegment<byte> GetFile(int seek, string fileName)
        {
            if (files.TryGetValue(fileName, out _))
            {
                int seekLength = files[fileName].Length - seek > MAX_SEND_DATA ? MAX_SEND_DATA : files[fileName].Length - seek;
                
                return new ArraySegment<byte>(files[fileName], seek, seekLength);
            }

            return null;
        }

        void ReadChildrenFile()
        {
            if (Directory.Exists(root))
            {
                string[] filePaths = Directory.GetFiles(root);
                string separator = "\\";

                for (int i = 0; i < filePaths.Length; i++)
                {
                    string[] splitFileName = filePaths[i].Split(separator);
                    string fileName = splitFileName[splitFileName.Length - 1];

                    if (fileName != null)
                        files.Add(fileName, File.ReadAllBytes(filePaths[i]));
                }
            }                
        }
    }
}
