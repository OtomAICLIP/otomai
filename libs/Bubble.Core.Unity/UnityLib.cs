using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bubble.Core.Unity;

public static class UnityLib
{

    static UnityLib()
    {
    }
    
    public static AssetsManager GetAssetsManager()
    {
        var assetsManager = new AssetsManager();
        
        var classDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "classdata.tpk");
        if (File.Exists(classDataPath))
            assetsManager.LoadClassPackage(classDataPath);
        
        assetsManager.LoadClassDatabaseFromPackage("0.0.0");
        
        return assetsManager;
    }

    public static void Reset(AssetsManager assetsManager)
    {
        assetsManager.UnloadAll();
    }
    
    public static void DumpRawAsset(FileStream wfs, AssetsFileReader reader, long position, uint size)
    {
        var assetFs = reader.BaseStream;
        assetFs.Position = position;
        var buf = new byte[4096];
        var bytesLeft = (int)size;
        while (bytesLeft > 0)
        {
            var readSize = assetFs.Read(buf, 0, Math.Min(bytesLeft, buf.Length));
            wfs.Write(buf, 0, readSize);
            bytesLeft -= readSize;
        }
    }

    public static void DumpJsonAsset(StreamWriter sw, AssetTypeValueField baseField, bool allowByteArrays = true)
    {
        var jBaseField = RecurseJsonDump(baseField, allowByteArrays);
        sw.Write(jBaseField.ToString(Formatting.None));
    }
    
    private static JToken RecurseJsonDump(AssetTypeValueField field, bool allowByteArrays)
    {
        var template = field.TemplateField;
        var isArray = template.IsArray;

        if (isArray)
        {
            var jArray = new JArray();

            if (template.ValueType != AssetValueType.ByteArray)
            {
                foreach (var t in field.Children)
                {
                    jArray.Add(RecurseJsonDump(t, allowByteArrays));
                }
            }
            else
            {
                if (!allowByteArrays)
                    return jArray;
                
                var byteArrayData = field.AsByteArray;
                foreach (var t in byteArrayData)
                {
                    jArray.Add(t);
                }
            }

            return jArray;
        }
        
        if (field.Value != null)
        {
            var evt = field.Value.ValueType;

            if (field.Value.ValueType != AssetValueType.ManagedReferencesRegistry)
            {
                object value = evt switch
                {
                    AssetValueType.Bool => field.AsBool,
                    AssetValueType.Int8 or
                        AssetValueType.Int16 or
                        AssetValueType.Int32 => field.AsInt,
                    AssetValueType.Int64 => field.AsLong,
                    AssetValueType.UInt8 or
                        AssetValueType.UInt16 or
                        AssetValueType.UInt32 => field.AsUInt,
                    AssetValueType.UInt64 => field.AsULong,
                    AssetValueType.String => field.AsString,
                    AssetValueType.Float => field.AsFloat,
                    AssetValueType.Double => field.AsDouble,
                    _ => "invalid value"
                };

                return (JValue)JToken.FromObject(value);
            }
            
            // todo separate method
            var registry = field.Value.AsManagedReferencesRegistry;

            if (registry.version == 1 || registry.version == 2)
            {
                var jArrayRefs = new JArray();

                foreach (var refObj in registry.references)
                {
                    var typeRef = refObj.type;

                    var jObjManagedType = new JObject
                    {
                        {
                            "class", typeRef.ClassName
                        },
                        {
                            "ns", typeRef.Namespace
                        },
                        {
                            "asm", typeRef.AsmName
                        }
                    };

                    var jObjData = new JObject();

                    foreach (var child in refObj.data)
                    {
                        jObjData.Add(child.FieldName, RecurseJsonDump(child, allowByteArrays));
                    }

                    JObject jObjRefObject;

                    if (registry.version == 1)
                    {
                        jObjRefObject = new JObject
                        {
                            {
                                "type", jObjManagedType
                            },
                            {
                                "data", jObjData
                            }
                        };
                    }
                    else
                    {
                        jObjRefObject = new JObject
                        {
                            {
                                "rid", refObj.rid
                            },
                            {
                                "type", jObjManagedType
                            },
                            {
                                "data", jObjData
                            }
                        };
                    }

                    jArrayRefs.Add(jObjRefObject);
                }

                var jObjReferences = new JObject
                {
                    {
                        "version", registry.version
                    },
                    {
                        "RefIds", jArrayRefs
                    }
                };

                return jObjReferences;
            }
            else
            {
                throw new NotSupportedException($"Registry version {registry.version} not supported!");
            }
        }
        
        var jObject = new JObject();

        foreach (var child in field)
        {
            jObject.Add(child.FieldName, RecurseJsonDump(child, allowByteArrays));
        }

        return jObject;
    }
    
    public static AssetTypeValueField? GetByteArrayTexture(AssetWorkspace workspace, AssetContainer tex)
    {
        var textureTemp = workspace.GetTemplateField(tex);
        
        if (textureTemp == null)
            return null;
        
        var imageData = textureTemp.Children.FirstOrDefault(f => f.Name == "image data");
        if (imageData == null)
            return null;
        
        imageData.ValueType = AssetValueType.ByteArray;

        var platformBlob = textureTemp.Children.FirstOrDefault(f => f.Name == "m_PlatformBlob");
        if (platformBlob != null)
        {
            var platformBlobArray = platformBlob.Children[0];
            platformBlobArray.ValueType = AssetValueType.ByteArray;
        }

        var baseField = textureTemp.MakeValue(tex.FileReader, tex.FilePosition);
        return baseField;
    }

    public static byte[]? GetRawTextureBytes(TextureFile texFile, AssetsFileInstance inst)
    {
        var rootPath = Path.GetDirectoryName(inst.path);
        
        if (texFile.m_StreamData.size == 0 || texFile.m_StreamData.path == string.Empty)
            return texFile.pictureData;
        
        var fixedStreamPath = texFile.m_StreamData.path;
        if (inst.parentBundle == null && fixedStreamPath.StartsWith("archive:/"))
        {
            fixedStreamPath = Path.GetFileName(fixedStreamPath);
        }
        if (!Path.IsPathRooted(fixedStreamPath) && rootPath != null)
        {
            fixedStreamPath = Path.Combine(rootPath, fixedStreamPath);
        }
        if (File.Exists(fixedStreamPath))
        {
            Stream stream = File.OpenRead(fixedStreamPath);
            stream.Position = (long)texFile.m_StreamData.offset;
            texFile.pictureData = new byte[texFile.m_StreamData.size];
            stream.ReadExactly(texFile.pictureData, 0, (int)texFile.m_StreamData.size);
        }
        else
        {
            return null;
        }
        return texFile.pictureData;
    }

    public static byte[]? GetPlatformBlob(AssetTypeValueField texBaseField)
    {
        var blob = texBaseField["m_PlatformBlob"];
        byte[]? platformBlob = null;
        if (!blob.IsDummy)
        {
            platformBlob = blob["Array"].AsByteArray;
        }
        return platformBlob;
    }
}