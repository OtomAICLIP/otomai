using System.Diagnostics;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using Bubble.Core.Unity;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Bubble.Core.Assets;

public static class AssetService
{
    private static string? DofusPath;
    
    public static void Initialize(string dofusPath)
    {
        DofusPath = dofusPath;
    }
    
    private static readonly Lock Lock = new Lock();

    public static void Export(string filePath, string outputPath)
    {
        if (DofusPath == null)
        {
            throw new InvalidOperationException("Dofus path is not set.");
        }
        
        if (!filePath.StartsWith(DofusPath))
            filePath = Path.Combine(DofusPath, filePath);

        Log.Information("Loading {File}...", filePath);

        var ressourcesFiles = new Dictionary<string, byte[]>();
        
        var bonesId = filePath.Split("_")[^1].Split(".")[0];
        
        var sw = Stopwatch.StartNew();
        try
        {
            var bundle = AssetBundleReader.ReadAssetBundle(filePath);
            var assetManager = UnityLib.GetAssetsManager();
            
            var workspace = new AssetWorkspace
            {
                AssetsManager = assetManager
            };
            
            outputPath = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(filePath));
            Directory.CreateDirectory(outputPath);
            for (var index = 0; index < bundle.BlockAndDirInfo.DirectoryInfos.Count; index++)
            {
                if(!bundle.BlockAndDirInfo.DirectoryInfos[index].Name.EndsWith(".resS"))
                    continue;
                
                var data = BundleHelper.LoadAssetDataFromBundle(bundle, index);
                if (data == null)
                {
                    Log.Warning("Failed to load asset {Index} from bundle {File}.", index, filePath);
                    continue;
                }
                
                ressourcesFiles[bundle.BlockAndDirInfo.DirectoryInfos[index].Name] = data;
            }
            
            for (var index = 0; index < bundle.BlockAndDirInfo.DirectoryInfos.Count; index++)
            {
                var aSw = Stopwatch.StartNew();
                var data = BundleHelper.LoadAssetDataFromBundle(bundle, index);

                if (data == null)
                {
                    Log.Warning("Failed to load asset {Index} from bundle {File}.", index, filePath);
                    continue;
                }

                lock (Lock)
                {
                    using var unpackedStream = new MemoryStream(data);
                    var fileInst = assetManager.LoadAssetsFile(unpackedStream, filePath, true);
                    workspace.LoadedFiles.Add(fileInst);
                    
                    foreach (var info in fileInst.file.AssetInfos)
                    {
                        var cont = new AssetContainer(info, fileInst);
                        workspace.LoadedAssets[cont.AssetPPtr] = cont;

                        if(cont.FileReader == null || cont.FileReader.BaseStream.CanRead == false)
                            continue;
                        
                        workspace.GetDisplayNameFast(cont, false, out var assetName, out var type);

                        switch (type)
                        {
                            case "MonoBehaviour":
                            {
                                var id = filePath.Split("_")[^1].Split(".")[0];
                                var baseField = workspace.GetBaseField(cont);

                                if (baseField == null)
                                    continue;

                                var fileName = assetName;

                                if (baseField.Any(x => x.FieldName == "animations"))
                                {
                                    fileName = $"{id}-AnimatedObjectDefinition";
                                }
                                else if (baseField.Any(x => x.FieldName == "vertices"))
                                {
                                    fileName = $"{id}-{info.PathId}-SkinAsset";
                                }
                            
                                using var sw2 = new FileStream(Path.Combine(outputPath, $"{fileName}.json"), FileMode.Create);
                                using var streamWriter = new StreamWriter(sw2);
                                UnityLib.DumpJsonAsset(streamWriter, baseField, false);
                            
                                Log.Information("Exported {Asset} from {File}.", assetName, filePath);
                                break;
                            }
                            case "TextAsset":
                            {
                                var baseField = workspace.GetBaseField(cont);

                                if (baseField == null)
                                    continue;
                            
                                using var sw2 = new FileStream(Path.Combine(outputPath, $"{assetName}.dat"), FileMode.Create);
                                UnityLib.DumpRawAsset(sw2, cont.FileReader, cont.FilePosition, cont.Size);
                            
                                Log.Information("Exported {Asset} from {File}.", assetName, filePath);
                                break;
                            }
                            case "Texture2D":
                            {
                                var texBaseField = UnityLib.GetByteArrayTexture(workspace, cont);
                            
                                if (texBaseField == null)
                                {
                                    Log.Warning("Failed to load texture {Asset} from {File}.", assetName, filePath);
                                    continue;
                                }               
                            
                                var texFile = TextureFile.ReadTextureFile(texBaseField);
                                //0x0 texture, usually called like Font Texture or smth
                                if (texFile.m_Width == 0 && texFile.m_Height == 0)
                                    continue;
                            
                                if (!AssetService.GetResSTexture(texFile, cont.FileInstance, ressourcesFiles))
                                {
                                    var resSName = Path.GetFileName(texFile.m_StreamData.path);
                                    Log.Warning($"[{assetName}]: resS was detected but {resSName} was not found on disk");
                                    continue;
                                }
                            
                                var rawTextureBytes = UnityLib.GetRawTextureBytes(texFile, cont.FileInstance);

                                if (rawTextureBytes == null)
                                {
                                    var resSName = Path.GetFileName(texFile.m_StreamData.path);
                                    Log.Warning($"[{assetName}]: resS was detected but {resSName} was not found on disk");
                                    continue;
                                }
                            
                                var platformBlob = UnityLib.GetPlatformBlob(texBaseField);
                                var platform = cont.FileInstance.file.Metadata.TargetPlatform;
                            
                                using var sw2 = new FileStream(Path.Combine(outputPath, $"{assetName}.png"), FileMode.Create);
                                AssetService.ExportTexture(rawTextureBytes, sw2, texFile.m_Width, texFile.m_Height, (TextureFormat)texFile.m_TextureFormat, platform, platformBlob);
                                break;
                            }
                            default:
                                Log.Information("Skipped {Asset} from {File}.", assetName, filePath);
                                break;
                        }
                    }
                }
                
                aSw.Stop();
                Log.Information("Processed {Index} in {Elapsed}ms.", index, aSw.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while loading");
        }
        finally
        {
            Log.Information("Loaded in {Elapsed}.", sw.Elapsed);
        }
    }

    public static void ExportTexture(
        byte[] encData,
        Stream stream,
        int width,
        int height,
        TextureFormat format,
        uint platform = 0,
        byte[]? platformBlob = null)
    {
        using var image = AssetService.ExportTexture(encData, width, height, format, platform, platformBlob);
        if (image == null)
            return;

        image.Save(stream, new PngEncoder());
    }
    
    public static Image<Rgba32>? ExportTexture(
        byte[] encData, int width, int height,
        TextureFormat format, uint platform = 0, byte[]? platformBlob = null)
    {
        var decData = DecodeTexture(encData, width, height, format);
        if (decData == null)
            return null;

        var image = Image.LoadPixelData<Rgba32>(decData, width, height);
        image.Mutate(i => i.Flip(FlipMode.Vertical));
        
        return image;
    }

    private static byte[]? DecodeTexture(byte[] data, int width, int height, TextureFormat format)
    {
        switch (format)
        {
            //crunch
            case TextureFormat.DXT1Crunched:
            case TextureFormat.DXT5Crunched:
            case TextureFormat.ETC_RGB4Crunched:
            case TextureFormat.ETC2_RGBA8Crunched:
            {
                var uncrunch = DecodeCrunch(data, width, height, format);
                if (uncrunch == null)
                    return null;

                format = format switch
                {
                    TextureFormat.DXT1Crunched => TextureFormat.DXT1,
                    TextureFormat.DXT5Crunched => TextureFormat.DXT5,
                    TextureFormat.ETC_RGB4Crunched => TextureFormat.ETC_RGB4,
                    TextureFormat.ETC2_RGBA8Crunched => TextureFormat.ETC2_RGBA8,
                    _ => 0 //can't happen
                };

                byte[] res = [];
                if (format is TextureFormat.DXT1 or TextureFormat.DXT5)
                    res = DecodeAssetRipperTex(uncrunch, width, height, format);

                return res;
            }
            default:
                throw new NotSupportedException($"Texture format {format} not supported!");
        }

    }
    
    private static byte[] DecodeAssetRipperTex(byte[] data, int width, int height, TextureFormat format)
    {
        var dest = TextureFile.DecodeManaged(data, format, width, height);

        for (var i = 0; i < dest.Length; i += 4)
        {
            (dest[i], dest[i + 2]) = (dest[i + 2], dest[i]);
        }
        return dest;
    }
    private static byte[]? DecodeCrunch(byte[] data, int width, int height, TextureFormat format)
    {
        byte[] dest = new byte[width * height * 4];
        uint size = 0;
        unsafe
        {
            fixed (byte* dataPtr = data)
            fixed (byte* destPtr = dest)
            {
                var dataIntPtr = (IntPtr)dataPtr;
                var destIntPtr = (IntPtr)destPtr;
                size = PInvoke.DecodeByCrunchUnity(dataIntPtr, destIntPtr, (int)format, (uint)width, (uint)height, (uint)data.Length);
            }
        }
        if (size > 0)
            return dest; //big size is fine for now
        return null;
    }
    
    public static bool GetResSTexture(TextureFile texFile, AssetsFileInstance fileInst, Dictionary<string, byte[]> ressourcesFiles)
    {
        var streamInfo = texFile.m_StreamData;
        
        if(string.IsNullOrEmpty(streamInfo.path))
            return false;
        
        var searchPath = streamInfo.path;
        if (searchPath.StartsWith("archive:/"))
            searchPath = searchPath[9..].Split('/').Last();

        if (!ressourcesFiles.TryGetValue(searchPath, out var resSData))
            return false;
        
        texFile.pictureData = resSData;
        texFile.m_StreamData.offset = 0;
        texFile.m_StreamData.size = 0;
        texFile.m_StreamData.path = "";
        return true;

    }
}