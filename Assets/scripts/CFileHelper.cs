 
using System.Collections;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using UnityEngine;
public class CFileHelper
{ 
    
    /// <summary>
    /// It saves the specified object¡¯s data into a XML file.
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
       
        try
        {
            string path = fileName;
            Stream stream = File.OpenRead(path);
            XmlSerializer serializer = new XmlSerializer(type);
  
            object obj = serializer.Deserialize(stream);
  
            stream.Close();
            return obj;
        }
        catch (Exception ex)
        {
            Debug.Log("2:" + ex.Message);
        }
        return null;

       
       
        
    }
}
