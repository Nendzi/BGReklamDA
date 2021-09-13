using System.IO;
using System.IO.Compression;

namespace WebEdgeClassification.Builders
{
    public class DataSetBuilder
    {
        private readonly string _path;
        private readonly string _folder;

        public DataSetBuilder(string path, string folder)
        {
            _path = path;
            _folder = folder;
        }

        // ne heroku serveru kotisti se / za direktorijume a na localhostu \\
        public void SaveJsonData(string jsonData, string fileName)
        {
            File.WriteAllText(_path + "/" + _folder + "/" + fileName, jsonData);
        }

        public void ZipFolder(string zipFileName)
        {
            string fullZipFileName = _path + "/" + zipFileName;
            if (File.Exists(fullZipFileName))
            {
                File.Delete(fullZipFileName);
            }
            ZipFile.CreateFromDirectory(_path + "/" + _folder, fullZipFileName);
        }
    }
}