using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenRA.Mods.Cnc.FileSystem;

namespace Localization.Tools
{
    public class CSF(string csfFilename="")
    {

        public string csfPath = csfFilename;

        public Dictionary<string, string> GetCsfDictionary(byte[] content)
        {
            return ReadCSF(content);
        }

        // 获取 CSF 文件内容的方法
        public Dictionary<string, string> GetCsfDictionary()
        {
            // 检查 CSF 文件是否存在，如果存在则读取它，否则返回 null
            return File.Exists(csfPath) ? ReadCSF(csfPath) : null;
        }

        static List<string> keys = [];
        static List<string> wRTS = [];

        // 读取 CSF 文件头部信息的方法
        static Tuple<byte[], uint, uint, uint, uint, uint> ReadHeader(BinaryReader csfFile)
        {
            byte[] headerBytes = csfFile.ReadBytes(0x18); // 读取头部的 24 个字节
            byte[] headerSubset = new byte[4];
            Array.Copy(headerBytes, headerSubset, 4); // 提取头部的前 4 个字节

            // 读取 CSF 版本号
            uint csfVersion = BitConverter.ToUInt32(headerBytes, 4);
            // 读取标签数
            uint numLabels = BitConverter.ToUInt32(headerBytes, 8);
            // 读取字符串数
            uint numStrings = BitConverter.ToUInt32(headerBytes, 12);
            // 读取未使用的字段
            uint unused = BitConverter.ToUInt32(headerBytes, 16);
            // 读取语言 ID
            uint langId = BitConverter.ToUInt32(headerBytes, 20);

            return Tuple.Create(headerSubset, csfVersion, numLabels, numStrings, unused, langId);
        }

        // 检查文件头部是否有效的方法
        static int CheckHeader(Tuple<byte[], uint, uint, uint, uint, uint> header)
        {
            byte[] fsc = header.Item1; // CSF版本号 

            // 检查文件头部标识是否为 " FSC"
            if (!ByteArrayCompare(fsc, Encoding.ASCII.GetBytes(" FSC")))
            {
                throw new InvalidDataException("This is not a valid CSF file!"); // 如果不是有效的 CSF 文件，抛出异常
            }

            uint numLabels = header.Item3; //读取标签数
            uint numStrings = header.Item4; //读取字符串数

            // 检查标签数是否等于字符串数
            //if (numLabels != numStrings && numLabels + 1 != numStrings)
            //{
            //    throw new InvalidDataException("Label count and string count are unequal"); // 如果不相等，抛出异常
            //}

            return (int)numStrings; //5236 5237
        }

        // 读取 CSF 文件并返回一个字典
        static Dictionary<string, string> ReadCSF(string csfFilename)
        {
            return ReadCSF(File.OpenRead(csfFilename));
        }

        static Dictionary<string, string> ReadCSF(Stream csfStream)
        {
            try
           {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var nameStrMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                using (var csfFile = new BinaryReader(csfStream))
                {
                    Tuple<byte[], uint, uint, uint, uint, uint> header = ReadHeader(csfFile);
                    int numLabels = CheckHeader(header);

                    keys.Clear();
                    wRTS.Clear();

                    for (int i = 0; i < numLabels; i++)
                    {
                        try
                        {
                            byte[] lblBytes = csfFile.ReadBytes(4);
                            byte[] oneBytes = csfFile.ReadBytes(4);
                            byte[] uinameLengthBytes = csfFile.ReadBytes(4);

                            string lbl = Encoding.ASCII.GetString(lblBytes);
                            int one = BitConverter.ToInt32(oneBytes);
                            int uinameLength = BitConverter.ToInt32(uinameLengthBytes);

                            //if (lbl != " LBL" || one != 1)
                            //{ //VOX:iseaatc
                            //    if (nameStrMap.Count > 0)
                            //        continue;
                            //    else
                            //        throw new InvalidDataException("Invalid label format");
                            //}
                            //if (lbl != " LBL" || one != 1)
                            //{
                            //    Console.Write("");
                                
                            //}

                                byte[] uiNameBytes = csfFile.ReadBytes(uinameLength);
                            byte[] rtsIdBytes = csfFile.ReadBytes(4);

                            string uiName = Encoding.UTF8.GetString(uiNameBytes).TrimEnd('\0');
                            if (uiNameBytes.IsValidGb18030())
                            {
                                uiName = Encoding.GetEncoding("GB18030").GetString(uiNameBytes).TrimEnd('\0');
                                keys.Add(uiName);
                            }
                                //if (uiName.Contains("ALL:all12"))
                                //    Console.Write("");
                                //if (uiName.Contains("ALL:"))
                                //{
                                //    var uiName2 = Encoding.GetEncoding("GB18030").GetString(uiNameBytes).TrimEnd('\0');
                                //    Console.Write(uiName2);
                                //}


                                string rtsId = Encoding.ASCII.GetString(rtsIdBytes);

                            uint rtsLen = csfFile.ReadUInt32() * 2;
                            byte[] contentRaw = csfFile.ReadBytes((int)rtsLen);

                            if (rtsId != " RTS")  
                            {
                                uint extraLen = csfFile.ReadUInt32();
                                byte[] extraRaw = csfFile.ReadBytes((int)extraLen);
                            }

                            if (rtsId == "WRTS")
                                wRTS.Add(uiName);

                            //if (uiName == "VOX:iyursee") //732
                            //    Console.Write("");
                             
                            string content = BytesToString(contentRaw);
                            
                            nameStrMap[uiName] = content;
                            if (lbl != " LBL" || one != 1) 
                                csfFile.ReadBytes(16);

                        }
                        catch(Exception ex)
                        {
                            continue;
                        }
                    }
                }

                return nameStrMap;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        static Dictionary<string, string> ReadCSF(byte[] csfData)
        {
            using (var csfStream = new MemoryStream(csfData))
            {
                return ReadCSF(csfStream);
            }
        }

        static bool IsUtf8(byte[] bytes)
        {
            int i = 0;
            while (i < bytes.Length)
            {
                if ((bytes[i] & 0x80) == 0) // 0xxxxxxx, 1字节
                {
                    i++;
                    continue;
                }

                if ((bytes[i] & 0xE0) == 0xC0) // 110xxxxx 10xxxxxx, 2字节
                {
                    if (i + 1 >= bytes.Length || (bytes[i + 1] & 0xC0) != 0x80)
                        return false;
                    i += 2;
                    continue;
                }

                if ((bytes[i] & 0xF0) == 0xE0) // 1110xxxx 10xxxxxx 10xxxxxx, 3字节
                {
                    if (i + 2 >= bytes.Length ||
                        (bytes[i + 1] & 0xC0) != 0x80 ||
                        (bytes[i + 2] & 0xC0) != 0x80)
                        return false;
                    i += 3;
                    continue;
                }

                if ((bytes[i] & 0xF8) == 0xF0) // 11110xxx 10xxxxxx 10xxxxxx 10xxxxxx, 4字节
                {
                    if (i + 3 >= bytes.Length ||
                        (bytes[i + 1] & 0xC0) != 0x80 ||
                        (bytes[i + 2] & 0xC0) != 0x80 ||
                        (bytes[i + 3] & 0xC0) != 0x80)
                        return false;
                    i += 4;
                    continue;
                }

                return false; // 其他情况不是 UTF-8
            }
            return true;
        }

        // 将字节数组转换为字符串
        static string BytesToString(byte[] content)
        {
            byte[] strInvert = new byte[content.Length];
            for (int idx = content.Length - 1; idx >= 0; idx--)
            {
                strInvert[idx] = (byte)(content[idx] ^ 0xFF); // 对字节进行 XOR 操作
            }

            string result = Encoding.Unicode.GetString(strInvert); // 将处理后的字节数组转换为字符串
            return result;
        }

        // 比较两个字节数组是否相等
        static bool ByteArrayCompare(byte[] a1, byte[] a2)
        {
            if (a1.Length != a2.Length)
                return false;

            for (int i = 0; i < a1.Length; i++)
            {
                if (a1[i] != a2[i])
                    return false;
            }

            return true;
        }

        // 将字典中的数据写入 CSF 文件
        public static void WriteCSF(Dictionary<string, string> csfDictionary, string csfFilename)
        {
            if(csfDictionary == null) { return; }

            using (BinaryWriter csfFile = new BinaryWriter(File.Open(csfFilename, FileMode.Create)))
            {
                // 写入文件头部
                csfFile.Write(Encoding.ASCII.GetBytes(" FSC")); // 文件标识
                csfFile.Write((uint)3); // 版本号
                csfFile.Write((uint)csfDictionary.Count); // 标签数
                csfFile.Write((uint)csfDictionary.Count); // 字符串数
                csfFile.Write((uint)0); // 未使用的字段
                csfFile.Write((uint)0); // 语言 ID

                // 遍历字典并写入每个条目
                foreach (var item in csfDictionary)
                {
                    string uiName = item.Key;

                    string content = item.Value;

                    csfFile.Write(Encoding.ASCII.GetBytes(" LBL")); // 标签前缀
                    csfFile.Write((uint)1);
                    //csfFile.Write((uint)uiName.Length); // UI 名称长度
                    if(keys.Contains(uiName))
                    {
                        var s = Encoding.GetEncoding("GB18030").GetBytes(uiName);
                        csfFile.Write((uint)s.Length);
                        csfFile.Write(s); // UI 名称
                    }
                    else
                    {
                        csfFile.Write((uint)uiName.Length);
                        csfFile.Write(Encoding.UTF8.GetBytes(uiName)); // UI 名称
                    }
                    // csfFile.Write(Encoding.UTF8.GetBytes(uiName)); // UI 名称
                    if (wRTS.Contains(uiName))
                    {
                        csfFile.Write(Encoding.ASCII.GetBytes("WRTS")); // 内容前缀
                        byte[] contentBytes = StringToBytes(content); // 将字符串转换为字节数组
                        csfFile.Write((uint)contentBytes.Length / 2); // 写入内容长度
                        csfFile.Write(contentBytes); // 写入内容
                        
                        var s = uiName.Split(':')[1] + 'c';
                        byte[] sBytes = Encoding.ASCII.GetBytes(s);  // 将字符串转换为字节数组
                        uint length = (uint)sBytes.Length;  // 获取字符串的长度
                        csfFile.Write(BitConverter.GetBytes(length));  // 写入 4 字节的长度
                        csfFile.Write(sBytes);  // 写入字符串的字节
                   
                    }
                    else
                    {
                        csfFile.Write(Encoding.ASCII.GetBytes(" RTS")); // 内容前缀
                        byte[] contentBytes = StringToBytes(content); // 将字符串转换为字节数组
                        csfFile.Write((uint)contentBytes.Length / 2); // 写入内容长度
                        csfFile.Write(contentBytes); // 写入内容
                    }
                    
                }
            }
        }

        // 将字符串转换为 CSF 文件格式的字节数组
        static byte[] StringToBytes(string str)
        {
            var contentBytes = Encoding.Unicode.GetBytes(str); // 将字符串转换为字节数组
            var strInvert = new byte[contentBytes.Length];
            for (int idx = 0; idx < contentBytes.Length; idx++)
            {
                strInvert[idx] = (byte)(contentBytes[idx] ^ 0xFF); // 对每个字节进行 XOR 操作
            }
            return strInvert;
        }

        public static Dictionary<string, string> 获取目录下的CSF字典(string path)
        {
            var combinedDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var miscsfs = MixLoader.MixFile.GetDirFiles(path,".csf"); //读目录下的MIX里的CSF
            foreach (var csf in miscsfs)
            {
                var csfDictionary = new CSF().GetCsfDictionary(csf);
                if (csfDictionary == null) continue;
                foreach (var kvp in csfDictionary)
                {
                    // 如果键已经存在，替换它
                    combinedDictionary[kvp.Key] = kvp.Value;
                }
            }

            var csfs = Directory.GetFiles(path, "*.csf").OrderBy(f => f); // 读目录下的CSF,按文件名升序处理

            foreach (var csf in csfs)
            {
                var csfDictionary = new CSF(csf).GetCsfDictionary();
                if (csfDictionary == null) continue;
                foreach (var kvp in csfDictionary)
                {
                    // 如果键已经存在，替换它
                    combinedDictionary[kvp.Key] = kvp.Value;
                }
            }

            return combinedDictionary;
        }

        public static void 将繁体的CSF转化为简体CSF(string oldCsf ,string newCsf)
        {
            var oldCsfDictionary = new CSF(oldCsf).GetCsfDictionary();
            if (oldCsfDictionary != null)
            {
                oldCsfDictionary.ConvertValuesToSimplified();
                CSF.WriteCSF(oldCsfDictionary, newCsf);
            }
            else
            {
                File.Copy(oldCsf, newCsf,true);
            }
        }
    }
}
