using AssetsTools.NET;
using Serilog;

namespace Bubble.Core.Unity;

public static class AssetBundleReader
{
    public static AssetBundleFile ReadAssetBundle(string path)
    {
        //Log.Information("Reading asset bundle {File}...", path);

        return AssetBundleReader.ReadAssetBundleFile(path);
    }

    private static AssetBundleFile ReadAssetBundleFile(string path)
    {
        Stream fs = File.OpenRead(path);
        var reader = new AssetsFileReader(fs);

        var fileType = FileTypeDetector.DetectFileType(reader, 0);

        if (fileType != DetectedFileType.BundleFile)
        {
            Log.Error("File {File} is not a UnityFS bundle file.", path);
            throw new NotSupportedException("File is not a UnityFS bundle file.");
        }

        //Log.Debug("Decompressing asset bundle {File}...", path);

        var assetBundle = new AssetBundleFile();
        assetBundle.Read(reader);

        if (assetBundle.Header.GetCompressionType() == 0)
            return assetBundle;

        var nfs = new MemoryStream();
        var writer = new AssetsFileWriter(nfs);
        assetBundle.Unpack(writer);

        nfs.Position = 0;
        fs.Close();

        fs.Dispose();
        reader.Dispose();

        fs = nfs;
        reader = new AssetsFileReader(fs);

        assetBundle = new AssetBundleFile();
        assetBundle.Read(reader);

        return assetBundle;
    }
}