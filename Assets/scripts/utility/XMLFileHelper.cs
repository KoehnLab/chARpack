 
using System.Collections;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using UnityEngine;
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
    public static void SaveData(string fileName, object data)
    {
        string path = fileName;
        Stream stream = File.Create(path);
        var xmlWriterSettings = new XmlWriterSettings() { Indent = true };
        // Debug.Log("stream is open");
        // Convert the object to XML data and put it in the stream
        XmlSerializer serializer = new XmlSerializer(data.GetType());
        using (XmlWriter writer = XmlWriter.Create(stream, xmlWriterSettings))
        {
            // Serialize using the XmlTextWriter. 
            serializer.Serialize(writer, data);
            writer.Close();
        }
        //using (XmlWriter writer = new XmlTextWriter(stream, Encoding.Unicode))
        //{
        //    // Serialize using the XmlTextWriter. 
        //    serializer.Serialize(writer, data);
        //    writer.Close();
        //}



        // Close the file
        stream.Close();
    }

    /// <summary>
    /// It reads from a XML file into the specified type class.
    /// </summary>
    public static object LoadData(string fileName, Type type)
    {

        //Stream stream = File.OpenRead(fileName);

        //XmlRootAttribute xRoot = new XmlRootAttribute();
        ////xRoot.ElementName = "message";
        //xRoot.IsNullable = true;
        //XmlSerializer serializer = new XmlSerializer(type, xRoot);

        //object obj = serializer.Deserialize(stream);

        //stream.Close();
        //return obj;


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
