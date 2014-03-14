using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

namespace PostForm
{
    public static class LogRecord
    {
        private static string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");

        #region 旧有的通用日志记录

        /// <summary>
        /// 写日志文件
        /// </summary>
        /// <param name="LogPath">文件路径</param>
        /// <param name="content">日志内容</param>
        /// <param name="Rank">日志类别(0 - Info,1 - Err)</param>
        public static void WriteLog(string content)
        {
            string LogName = DateTime.Now.ToString("yyyy-MM-dd") + ".txt";
            string filesource = Path.Combine("C:\\", LogName);
            content = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]\t" + content;
            Console.WriteLine(content);
            WriteFile(LogPath, LogName, content, 0);
        }

        private static void WriteSelfLog(string content)
        {
            string LogName = "_filelog.txt";
            string filesource = Path.Combine(LogPath, LogName);
            content = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "]\t" + content;
            Console.WriteLine(content);
            WriteFile(LogPath, LogName, content, 0);
        }

        /// <summary>
        /// 写文件
        /// </summary>
        /// <param name="FilePath">文件路径</param>
        /// <param name="FileName">文件名</param>
        /// <param name="content">文件内容</param>
        /// <param name="fileMode">文件写入模式(0 - 增量修改 1 - 覆盖)</param>
        private static void WriteFile(string FilePath, string FileName, string content, int fileMode)
        {
            WriteFile(FilePath, FileName, content, fileMode, "utf-8");
        }

        private static void WriteFile(string FilePath, string FileName, string content, int fileMode, string encode)
        {

            try
            {
                DirectoryInfo dir = new DirectoryInfo(FilePath);
                if (!dir.Exists) dir.Create();
            }
            catch (Exception ex)
            {
                WriteSelfLog("Directory err：" + ex.Message);
            }

            FileMode fm = FileMode.OpenOrCreate;
            if (fileMode == 1) fm = FileMode.Create;

            FileStream fs = null;
            string _filePath = Path.Combine(FilePath, FileName);
            try
            {
                fs = new FileStream(_filePath, fm, FileAccess.Write);
                StreamWriter m_streamWriter = new StreamWriter(fs, Encoding.GetEncoding(encode));
                m_streamWriter.BaseStream.Seek(0, SeekOrigin.End);
                m_streamWriter.WriteLine(content);
                m_streamWriter.Flush();
                m_streamWriter.Close();
            }
            catch (Exception ex)
            {
                WriteSelfLog("save err：" + ex.Message);
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }
        }

        public static string ReadFile(string filePath)
        {
            StreamReader vStreamReader = new StreamReader(filePath, UnicodeEncoding.GetEncoding("GB2312"));
            string tempStr = vStreamReader.ReadToEnd();
            vStreamReader.Close();
            return tempStr;
        }

        #endregion


        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <param name="type"></param>
        public static void WriteLog(string content, string type)
        {
            string LogName = type + ".txt";
            content = DateTime.Now.ToString("HH:mm:ss") + "\t" + content;
            WriteFile(LogPath, LogName, content, 0);
            if (DateTime.Now.Hour == 23 && DateTime.Now.Minute >= 55)
            {
                if (!Directory.Exists(LogPath + "\\History\\" + type))
                    Directory.CreateDirectory(LogPath + "\\History\\" + type);
                File.Move(LogPath + "\\" + LogName, LogPath + "\\History\\" + type + "\\" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt");
            }
        }

        /// <summary>
        /// 记录不包含子节点简单格式XML日志
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Item"></param>
        /// <param name="Subfolder"></param>
        public static void WriteSampleXmlLog<T>(T Item, string Subfolder = "")
        {
            XmlDocument doc = new XmlDocument();
            XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(dec);
            XmlElement root = doc.CreateElement(typeof(T).ToString());
            doc.AppendChild(root);

            Type type = typeof(T);
            object obj = Activator.CreateInstance(type);
            PropertyInfo[] Props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo p in Props)
            {
                string ElementName = p.Name.Substring(0);
                object ElementValue = p.GetValue(Item, null);
                XmlElement element = doc.CreateElement(ElementName);
                element.InnerText = ElementValue == null ? "" : ElementValue.ToString();
                root.AppendChild(element);
            }
            string Path = LogPath + "\\" + Subfolder;
            if (!Directory.Exists(Path))
                Directory.CreateDirectory(Path);
            doc.Save(Path + (Subfolder != "" ? "\\" : "") + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".xml");
        }


        /// <summary>
        /// 读取指定文件夹中指定数目的XML日志
        /// </summary>
        /// <param name="Amount"></param>
        /// <param name="Subfolder"></param>
        /// <returns></returns>
        public static List<XmlDocument> ReadXmlLogs(int Amount, string Subfolder = "")
        {
            List<XmlDocument> Result = new List<XmlDocument>();
            try
            {
                string[] Files = System.IO.Directory.GetFiles(LogPath + (string.IsNullOrWhiteSpace(Subfolder) ? "" : "\\") + Subfolder, "*.xml");
                if (Amount > Files.Length)
                    Amount = Files.Length;
                for (int i = Files.Length - 1; i >= Files.Length - Amount; i--)
                {
                    XmlDocument Doc = new XmlDocument();
                    Doc.Load(Files[i]);
                    Result.Add(Doc);
                }
            }
            catch { }
            return Result;
        }

        /// <summary>
        /// 读取指定文件夹中指定数目的日志实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Amount"></param>
        /// <param name="Subfolder"></param>
        /// <returns></returns>
        public static List<T> ReadXmlLogs<T>(int Amount, string Subfolder = "")
        {
            List<T> Result = new List<T>();
            try
            {
                string[] Files = System.IO.Directory.GetFiles(LogPath + (string.IsNullOrWhiteSpace(Subfolder) ? "" : "\\") + Subfolder, "*.xml");
                if (Amount > Files.Length)
                    Amount = Files.Length;
                for (int i = Files.Length - 1; i >= Files.Length - Amount; i--)
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(T));
                    XmlReader xr = XmlReader.Create(Files[i], null);
                    T x = (T)serializer.Deserialize(xr);
                    Result.Add(x);
                }
            }
            catch { }
            return Result;
        }



        /// <summary>
        /// 写可序列化实体的XML日志
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <param name="Subfolder"></param>
        public static void WriteSerXmlLog<T>(T item, string Subfolder = "")
        {
            XmlDocument xml = new XmlDocument();
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            string Path = LogPath + "\\" + Subfolder;
            if (!Directory.Exists(Path))
                Directory.CreateDirectory(Path);
            XmlWriter xw = XmlWriter.Create(Path + (Subfolder != "" ? "\\" : "") + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".xml");
            serializer.Serialize(xw, item);
            xw.Close();
        }


        /// <summary>
        /// 读可序列化实体的XML日志
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Path"></param>
        /// <returns></returns>
        public static T ReadSerXmlLog<T>(string Path)
        {
            if (File.Exists(Path))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                XmlReader xr = XmlReader.Create(Path, null);
                T x = (T)serializer.Deserialize(xr);
                return x;
            }
            else
                return default(T);
        }
    }
}
