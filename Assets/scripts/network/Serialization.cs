using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class Serializer
{
    public static byte[] Serialize<T>(T data)
        where T : struct
    {
        var formatter = new BinaryFormatter();
        var stream = new MemoryStream();
        formatter.Serialize(stream, data);
        return stream.ToArray();
    }
    public static T Deserialize<T>(byte[] array)
        where T : struct
    {
        var stream = new MemoryStream(array);
        var formatter = new BinaryFormatter();
        return (T)formatter.Deserialize(stream);
    }
}
