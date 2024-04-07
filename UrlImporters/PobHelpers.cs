using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace PassiveSkillTreePlanter.UrlImporters;

public class PobHelpers
{
    public static string CodeToXml(string code)
    {
        //Inflate(common.base64.decode(buf:gsub("-","+"):gsub("_","/")))
        using var inputStream = new MemoryStream(Convert.FromBase64String(code.Replace('-', '+').Replace('_', '/')));
        inputStream.Seek(2, SeekOrigin.Begin);
        using var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress);
        using var targetStream = new MemoryStream();
        deflateStream.CopyTo(targetStream);
        return Encoding.Default.GetString(targetStream.ToArray());
    }
}