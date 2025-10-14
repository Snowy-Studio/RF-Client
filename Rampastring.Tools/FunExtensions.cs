using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.International.Converters.TraditionalChineseToSimplifiedConverter;
using Rampastring.Tools;

namespace Localization.Tools
{
    public static class FunExtensions
    {
        public static bool 是否为多人图(string mapFilePath)
        {
            try
            {
                var ini = new IniFile(mapFilePath);

                return !ini.SectionExists("Basic") || ini.GetIntValue("Basic", "MultiplayerOnly", 0) == 1;

            }
            catch
            {
                return false;
            }
        }

        public static string FindDeepestMainDir(string startDir)
        {
            var dirQueue = new Queue<string>();
            dirQueue.Enqueue(startDir);

            while (dirQueue.Count > 0)
            {
                var currentDir = dirQueue.Dequeue();

                // 文件数量
                var fileCount = Directory.GetFiles(currentDir).Length;
                if (fileCount >= 2)
                {
                    return currentDir; // 找到主目录
                }

                // 如果只有一个子目录，继续深入
                var subDirs = Directory.GetDirectories(currentDir);
                if (subDirs.Length == 1)
                {
                    dirQueue.Enqueue(subDirs[0]);
                }
                else if (subDirs.Length > 1)
                {
                    // 多个子目录时，如果文件少，也可以认为当前就是主目录
                    return currentDir;
                }
            }

            // 如果没找到，返回起始目录
            return startDir;
        }

        /// <summary>
        /// 判断字符串是否包含中文
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool CheckChineseStr(this string str)
        {
            string pat = @"[\u4e00-\u9fff]";
            Regex rg = new Regex(pat);
            Match mh = rg.Match(str);
            return mh.Success;
        }

        public static bool IsUtf8Encoded(this byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return false;

            try
            {
                Encoding utf8 = Encoding.UTF8;
                utf8.GetString(bytes); // 尝试解码
                return true;
            }
            catch (DecoderFallbackException)
            {
                return false; // 发生解码异常，说明不是有效的 UTF-8
            }
        }

        public static void OpenUrl(string url)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true  // 必须设置为 true 才能用默认浏览器打开
                };
                Process.Start(psi);
            }
            catch (System.Exception ex)
            {
                // 处理异常
                Console.WriteLine("打开网页失败: " + ex.Message);
            }
        }

        public static bool IsValidGb18030(this byte[] bytes)
        {
            int i = 0;
            while (i < bytes.Length)
            {
                byte b = bytes[i];

                if (b <= 0x7F) // 单字节 ASCII
                {
                    i++;
                }
                else if (b >= 0x81 && b <= 0xFE) // 可能是 2 字节或 4 字节字符
                {
                    if (i + 1 < bytes.Length && bytes[i + 1] >= 0x40 && bytes[i + 1] <= 0xFE) // 2 字节 GBK
                    {
                        i += 2;
                    }
                    else if (i + 3 < bytes.Length &&
                             bytes[i + 1] >= 0x30 && bytes[i + 1] <= 0x39 &&
                             bytes[i + 2] >= 0x81 && bytes[i + 2] <= 0xFE &&
                             bytes[i + 3] >= 0x30 && bytes[i + 3] <= 0x39) // 4 字节 GB18030 扩展字符
                    {
                        i += 4;
                    }
                    else
                    {
                        return false; // 不符合 GB18030 规则
                    }
                }
                else
                {
                    return false; // 非法字节
                }
            }

            return true;
        }

        /// <summary>
        /// 将字典中的繁体中文值转换为简体中文
        /// </summary>
        /// <param name="dict">需要转换的字典</param>
        public static void ConvertValuesToSimplified<Tkey>(this Dictionary<Tkey, string> dict)
        {
            var keys = new List<Tkey>(dict.Keys);
            foreach (Tkey key in keys)
            {
                dict[key] = ChineseConverter.Convert(dict[key], ChineseConversionDirection.TraditionalToSimplified);
            }
        }

        public static string ConvertValuesToSimplified(this string s)
        {
            return ChineseConverter.Convert(s, ChineseConversionDirection.TraditionalToSimplified);
        }

        /// <summary>
        /// 将文件大小转换为MB
        /// </summary>
        /// <param name="fileSizeInBytes">需要转换的文件大小</param>
        /// <param name="decimalPlaces">保留多少位小数</param>
        /// <returns></returns>
        public static string ToFileSizeString(this long fileSizeInBytes, int decimalPlaces)
        {
            double fileSizeInMB = (double)fileSizeInBytes / (1024 * 1024); // Convert bytes to megabytes
            string formatSpecifier = "0." + new string('0', decimalPlaces);
            string fileSizeFormatted = fileSizeInMB.ToString(formatSpecifier); // Format to the specified number of decimal places
            return fileSizeFormatted;
        }

        public static bool IsValidEmail(this string email)
        {
            // 定义一个正则表达式模式来验证电子邮件地址
            string pattern = @"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$";

            // 使用正则表达式进行匹配验证
            Regex regex = new Regex(pattern);
            return regex.IsMatch(email);
        }

        public static Dictionary<TKey, TSource> ToDictionaryWithConflictLog<TSource, TKey>(
            this IEnumerable<TSource> source, Func<TSource, TKey> keySelector,
            string debugName, Func<TKey, string> logKey, Func<TSource, string> logValue)
        {
            return ToDictionaryWithConflictLog(source, keySelector, x => x, debugName, logKey, logValue);
        }
        public static Dictionary<TKey, TElement> ToDictionaryWithConflictLog<TSource, TKey, TElement>(
            this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector,
            string debugName, Func<TKey, string> logKey = null, Func<TElement, string> logValue = null)
        {
            var output = new Dictionary<TKey, TElement>();
            IntoDictionaryWithConflictLog(source, keySelector, elementSelector, debugName, output, logKey, logValue);
            return output;
        }

        public static void IntoDictionaryWithConflictLog<TSource, TKey, TElement>(
            this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector,
            string debugName, Dictionary<TKey, TElement> output,
            Func<TKey, string> logKey = null, Func<TElement, string> logValue = null)
        {
            // Fall back on ToString() if null functions are provided:
            logKey ??= s => s.ToString();
            logValue ??= s => s.ToString();

            // Try to build a dictionary and log all duplicates found (if any):
            Dictionary<TKey, List<string>> dupKeys = null;
            var capacity = source is ICollection<TSource> collection ? collection.Count : 0;
            output.Clear();
            output.EnsureCapacity(capacity);
            foreach (var item in source)
            {
                var key = keySelector(item);
                var element = elementSelector(item);

                // Discard elements with null keys
                if (!typeof(TKey).IsValueType && key == null)
                    continue;

                // Check for a key conflict:
                if (!output.TryAdd(key, element))
                {
                    dupKeys ??= new Dictionary<TKey, List<string>>();
                    if (!dupKeys.TryGetValue(key, out var dupKeyMessages))
                    {
                        // Log the initial conflicting value already inserted:
                        dupKeyMessages = new List<string>
                        {
                            logValue(output[key])
                        };
                        dupKeys.Add(key, dupKeyMessages);
                    }

                    // Log this conflicting value:
                    dupKeyMessages.Add(logValue(element));
                }
            }

            // If any duplicates were found, throw a descriptive error
            if (dupKeys != null)
            {
                var badKeysFormatted = new StringBuilder(
                    $"{debugName}, duplicate values found for the following keys: ");
                foreach (var p in dupKeys)
                    badKeysFormatted.Append(CultureInfo.InvariantCulture, $"{logKey(p.Key)}: [{string.Join(",", p.Value)}]");
                throw new ArgumentException(badKeysFormatted.ToString());
            }
        }

        public static string GetWMIValue(string className, string propertyName)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher($"SELECT {propertyName} FROM {className}");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var value = obj[propertyName];
                    if (value != null)
                        return value.ToString().Trim();
                }
            }
            catch
            {
            }

            return "";
        }

        public static V GetOrAdd<K, V>(this Dictionary<K, V> d, K k)
            where V : new()
        {
            return d.GetOrAdd(k, new V());
        }

        public static V GetOrAdd<K, V>(this Dictionary<K, V> d, K k, V v)
        {
            // SAFETY: Dictionary cannot be modified whilst the ref is alive.
            ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(d, k, out var exists);
            if (!exists)
                value = v;
            return value;
        }

        public static V GetOrAdd<K, V>(this Dictionary<K, V> d, K k, Func<K, V> createFn)
        {
            // Cannot use CollectionsMarshal.GetValueRefOrAddDefault here,
            // the creation function could mutate the dictionary which would invalidate the ref.
            if (!d.TryGetValue(k, out var ret))
                d.Add(k, ret = createFn(k));
            return ret;
        }

        public static T GetOrAdd<T>(this HashSet<T> set, T value)
        {
            if (!set.TryGetValue(value, out var ret))
                set.Add(ret = value);
            return ret;
        }

        public static (int R, int G, int B) ConvertHSVToRGB(int H, int S, int V)
        {
            if (H == 360)
                H = 359; // 360为全黑，原因不明
            float R = 0f, G = 0f, B = 0f;

            if (S == 0)
            {
                // 如果饱和度为 0，颜色是灰色（V 值决定亮度）
                return (V, V, V);
            }

            // 将 S 和 V 归一化到 [0, 1] 之间
            float normalizedS = S / 255f;
            float normalizedV = V / 255f;

            // 获取 H 区间
            int H1 = (int)(H / 60f);
            float F = (H / 60f) - H1;

            // 计算 P, Q, T 的值
            float P = normalizedV * (1f - normalizedS);
            float Q = normalizedV * (1f - F * normalizedS);
            float T = normalizedV * (1f - (1f - F) * normalizedS);

            switch (H1)
            {
                case 0: R = normalizedV; G = T; B = P; break;
                case 1: R = Q; G = normalizedV; B = P; break;
                case 2: R = P; G = normalizedV; B = T; break;
                case 3: R = P; G = Q; B = normalizedV; break;
                case 4: R = T; G = P; B = normalizedV; break;
                case 5: R = normalizedV; G = P; B = Q; break;
            }

            // 将 R, G, B 放大到 [0, 255] 范围
            R = R * 255;
            G = G * 255;
            B = B * 255;

            // 返回 RGB 整数值
            return ((int)R, (int)G, (int)B);
        }

        public static string ComputeHash(this string filePath)
        {
            using var hashAlgorithm = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = hashAlgorithm.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public static string GetTimeStamp()
        {
            TimeSpan ts = DateTime.Now - DateTime.UnixEpoch;
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }
    }
}
