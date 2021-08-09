using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ServerCore
{
    public class FileReader
    {
        string root;
        Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();

        // C:\Users\mhoow\AppData\LocalLow\JWY\AssetBundles
        public FileReader(string _root = "C:\\Users\\mhoow\\AppData\\LocalLow\\JWY\\AssetBundles")
        {
            root = _root;
            ReadChildrenFile();
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
