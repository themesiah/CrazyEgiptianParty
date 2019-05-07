using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class FormatterSerializer
{
    public static byte[] Serialize(object toSerialize)
    {
        BinaryFormatter bf = new BinaryFormatter();
        MemoryStream ms = new MemoryStream();
        bf.Serialize(ms, toSerialize);
        return ms.ToArray();
    }

    public static T Deserialize<T>(byte[] toDeserialize)
    {
        BinaryFormatter bf = new BinaryFormatter();
        MemoryStream ms = new MemoryStream(toDeserialize);
        return (T)bf.Deserialize(ms);
    }
}
