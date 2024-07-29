 
using System.Collections;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using UnityEngine;
using chARpackStructs;
public class XMLFileHelper
{ 
    
    /// <summary>
    /// It saves the specified objects data into a XML file.
    /// </summary>
    public static string GetXMLString(object data)
    {
        StringBuilder strXML=new StringBuilder();
        XmlWriter xml =   XmlWriter.Create(strXML); 
        // Convert the object to XML data and put it in the stream
        XmlSerializer serializer = new XmlSerializer(data.GetType());
         
        try
        {
            serializer.Serialize(xml, data);
        }
        catch (Exception ex)
        {
           Debug.Log("3:" + ex.Message);
        } 
        // Close the file
        xml.Close();


        return strXML.ToString();
    }


    /// <summary>
    /// It saves the specified object¡¯s data into a XML file.
    /// </summary>
    public static void SaveData(string fileName, List<cmlData> data)
    {
        string path = fileName;
        XmlSerializer serializer = new XmlSerializer(data.GetType());

        using (FileStream stream = new FileStream(path, FileMode.Create))
        {
            serializer.Serialize(stream, data);

            // Write a newline character to the stream after closing the XmlWriter
            byte[] newLineBytes = Encoding.UTF8.GetBytes(Environment.NewLine);
            stream.Write(newLineBytes, 0, newLineBytes.Length);
        }
    }

    /// <summary>
    /// It reads from a XML file into the specified type class.
    /// </summary>
    public static object LoadData(string fileName, Type type)
    {
        try 
        {
            Stream stream = File.OpenRead(fileName);
            XmlSerializer serializer = new XmlSerializer(type);

            object obj = serializer.Deserialize(stream);

            stream.Close();
            return obj;
        }
        catch (Exception ex)
        {
            Debug.Log("2:" + ex.GetBaseException());
        }
        return null;

    }

    public static object LoadDataFromResources(string filePath, Type type)
    {
        var path = filePath;
        if (Path.HasExtension(path))
        {
            path = Path.Join(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
        }
        try
        {
            TextAsset xmlAsText = Resources.Load<TextAsset>(path);

            MemoryStream stream = new MemoryStream(xmlAsText.bytes);

            XmlSerializer serializer = new XmlSerializer(typeof(List<cmlData>));
            var obj = (List<cmlData>)serializer.Deserialize(stream);

            stream.Close();
            return obj;
        }
        catch (Exception ex)
        {
            Debug.Log("2:" + ex.GetBaseException());
        }
        return null;

    }

    /// <summary>
    /// It reads from a XML file into the specified type class.
    /// </summary>
    public static object LoadFromString(string content, Type type)
    {
        try
        {
            // convert string to stream
            byte[] byteArray = Encoding.UTF8.GetBytes(content);
            //byte[] byteArray = Encoding.ASCII.GetBytes(contents);
            MemoryStream stream = new MemoryStream(byteArray);

            // convert stream to string
            // StreamReader reader = new StreamReader(stream);

            XmlSerializer serializer = new XmlSerializer(type);

            object obj = serializer.Deserialize(stream);

            stream.Close();
            return obj;
        }
        catch (Exception ex)
        {
            Debug.Log("2:" + ex.GetBaseException());
        }
        return null;
    }

}
