using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Drawing;
using Spire.Pdf;
using Spire.Pdf.Conversion;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Serilog;
using System.Reflection;
//using Spire.Pdf.General.Find;
//using System.Diagnostics;

namespace NetCore.Shared
{
    public static class AsyncHelper
    {
        // AsyncHelper.RunSync(() => DoAsyncStuff());  
        private static readonly TaskFactory _taskFactory = new
            TaskFactory(CancellationToken.None,
                        TaskCreationOptions.None,
                        TaskContinuationOptions.None,
                        TaskScheduler.Default);

        public static TResult RunSync<TResult>(Func<Task<TResult>> func)
            => _taskFactory
                .StartNew(func)
                .Unwrap()
                .GetAwaiter()
                .GetResult();

        public static void RunSync(Func<Task> func)
            => _taskFactory
                .StartNew(func)
                .Unwrap()
                .GetAwaiter()
                .GetResult();
    }

    public class ObjectScore
    {
        public object Value { get; set; }
        public int Score { get; set; }
    }

    public partial class Utils
    {
        public static string GetValueByKeyOfDN(string value, string key)
        {
            var spl = value.Split(key);
            if (spl != null && spl.Count() > 1)
            {
                var valueCN = spl[1].Trim(new char[] { '=' });
                var cns = valueCN.Split(",");

                return cns != null && cns.Count() > 0 ? cns[0] : string.Empty;
            }

            return string.Empty;
        }

        public static byte[] FromBase64UrlToBytes(string base64Url)
        {
            string padded = base64Url.Length % 4 == 0
                ? base64Url : base64Url + "====".Substring(base64Url.Length % 4);
            string base64 = padded.Replace("_", "/")
                                  .Replace("-", "+");
            return Convert.FromBase64String(base64);
        }

        public static string GetConfig(string code)
        {

            IConfigurationRoot configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                                                         .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                                                                         .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json",
                                                                         optional: true, reloadOnChange: false)
                                                                         //.AddJsonFile($"appsettings.{AppConstants.EnvironmentName}.json",
                                                                         //optional: true, reloadOnChange: false)
                                                                         .Build();
            var value = configuration[code];
            return value;
        }

        public static bool IsValidBase64String(string base64String)
        {
            base64String = base64String.Replace("-----BEGIN CERTIFICATE REQUEST-----", "").Replace("-----END CERTIFICATE REQUEST-----", "");
            if (string.IsNullOrEmpty(base64String) || base64String.Length % 4 != 0 || base64String.Contains(" ") || base64String.Contains("\t") || base64String.Contains("\r") || base64String.Contains("\n"))
                return false;
            try
            {
                Convert.FromBase64String(base64String);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Mã hóa MD5
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string MD5Hash(string text)
        {
            StringBuilder hash = new StringBuilder();
            MD5CryptoServiceProvider md5provider = new MD5CryptoServiceProvider();
            byte[] bytes = md5provider.ComputeHash(new UTF8Encoding().GetBytes(text));

            for (int i = 0; i < bytes.Length; i++)
            {
                hash.Append(bytes[i].ToString("x2"));
            }
            return hash.ToString();
        }

        // This presumes that weeks start with Monday.
        // Week 1 is the 1st week of the year with a Thursday in it.
        public static int GetIso8601WeekOfYear(DateTime time)
        {
            // Seriously cheat.  If its Monday, Tuesday or Wednesday, then it'll 
            // be the same week# as whatever Thursday, Friday or Saturday are,
            // and we always get those right
            DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                time = time.AddDays(3);
            }

            // Return the week of our adjusted day
            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        public static string ReOrderPemission(string permissionString)
        {

            var listPermission = permissionString.ToList();
            var nuberPermission = new List<ObjectScore>();
            foreach (var item in listPermission)
            {
                switch (item)
                {
                    case 'r':
                        nuberPermission.Add(new ObjectScore
                        {
                            Score = 1,
                            Value = item
                        });
                        break;
                    case 'w':
                        nuberPermission.Add(new ObjectScore
                        {
                            Score = 1,
                            Value = item
                        });
                        break;
                    case 'e':
                        nuberPermission.Add(new ObjectScore
                        {
                            Score = 1,
                            Value = item
                        });
                        break;
                    case 'd':
                        nuberPermission.Add(new ObjectScore
                        {
                            Score = 1,
                            Value = item
                        });
                        break;
                    case 'f':
                        nuberPermission.Add(new ObjectScore
                        {
                            Score = 1,
                            Value = item
                        });
                        break;
                    default:
                        break;
                }
            }
            nuberPermission.OrderBy(s => s.Score);
            return string.Join("", nuberPermission.Select(s => s.Value.ToString()));
        }

        private static readonly string[] VietnameseSigns = new string[]
        {
        "aAeEoOuUiIdDyY",
        "áàạảãâấầậẩẫăắằặẳẵ",
        "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",
        "éèẹẻẽêếềệểễ",
        "ÉÈẸẺẼÊẾỀỆỂỄ",
        "óòọỏõôốồộổỗơớờợởỡ",
        "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",
        "úùụủũưứừựửữ",
        "ÚÙỤỦŨƯỨỪỰỬỮ",
        "íìịỉĩ",
        "ÍÌỊỈĨ",
        "đ",
        "Đ",
        "ýỳỵỷỹ",
        "ÝỲỴỶỸ"
        };

        public static string RemoveVietnameseSign(string str)
        {
            for (int i = 1; i < VietnameseSigns.Length; i++)
            {
                for (int j = 0; j < VietnameseSigns[i].Length; j++)

                    str = str.Replace(VietnameseSigns[i][j], VietnameseSigns[0][i - 1]);

            }

            return str;
        }

        public static string BuildVietnameseSign(string str)
        {
            for (int i = 1; i < VietnameseSigns.Length; i++)
            {
                for (int j = 0; j < VietnameseSigns[i].Length; j++)

                    str = str.Replace(VietnameseSigns[0][i - 1], VietnameseSigns[i][j]);

            }

            return str;
        }

        public static string GetValidFileName(string fileName)
        {
            // remove any invalid character from the filename.
            String ret = Regex.Replace(fileName.Trim(), "[^A-Za-z0-9_. ]+", "");
            return ret.Replace(" ", String.Empty);
        }

        /// <summary>
        /// Convert url title
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string ConvertToUrlTitle(string name)
        {
            string strNewName = name;

            #region Replace unicode chars
            Regex regex = new Regex("\\p{IsCombiningDiacriticalMarks}+");
            string temp = name.Normalize(NormalizationForm.FormD);
            strNewName = regex.Replace(temp, String.Empty).Replace('\u0111', 'd').Replace('\u0110', 'D');
            #endregion

            #region Replace special chars
            string strSpecialString = "~\"“”#%&*:;<>?/\\{|}.+_@$^()[]`,!-'";

            foreach (char c in strSpecialString.ToCharArray())
            {
                strNewName = strNewName.Replace(c, ' ');
            }
            #endregion

            #region Replace space

            // Create the Regex.
            var r = new Regex(@"\s+");
            // Strip multiple spaces.
            strNewName = r.Replace(strNewName, @" ").Replace(" ", "-").Trim('-');

            #endregion)

            return strNewName;
        }
        /// <summary>
        /// Check if a string is a guid or not
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        public static bool IsGuid(string inputString)
        {
            try
            {
                var guid = new Guid(inputString);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool IsNumber(string inputString)
        {
            try
            {
                var guid = int.Parse(inputString);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string GeneratePageUrl(string pageTitle)
        {
            var result = RemoveVietnameseSign(pageTitle);

            // Replace spaces
            result = result.Replace(" ", "-");

            // Replace double spaces
            result = result.Replace("--", "-");

            // Remove triple spaces
            result = result.Replace("---", "-");

            return result;

        }

        /// <summary>
        /// Tạo chuỗi 6 chữ số
        /// </summary>
        /// <returns></returns>
        public static string GenerateNewRandom()
        {
            Random generator = new Random();
            String r = generator.Next(0, 1000000).ToString("D6");
            if (r.Distinct().Count() == 1)
            {
                r = GenerateNewRandom();
            }
            return r;
        }

        /// <summary>
        /// Tạo chuỗi 4 chữ số
        /// </summary>
        /// <returns></returns>
        public static string GenerateNewRandom4Number()
        {
            Random generator = new Random();
            String r = generator.Next(0, 10000).ToString("D");
            if (r.Distinct().Count() == 1)
            {
                r = GenerateNewRandom();
            }
            return r;
        }

        public static string PassowrdRandomString(int size, bool lowerCase)
        {
            var builder = new StringBuilder();
            var random = new Random();
            for (int i = 0; i < size; i++)
            {
                char ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }
            return lowerCase ? builder.ToString().ToLower() : builder.ToString();
        }

        public static string PassowrdCreateSalt512()
        {
            var message = PassowrdRandomString(512, false);
            return BitConverter.ToString((new SHA512Managed()).ComputeHash(Encoding.ASCII.GetBytes(message))).Replace("-", "");
        }

        public static string RandomPassword(int numericLength, int lCaseLength, int uCaseLength, int specialLength)
        {
            Random random = new Random();

            //char set random
            string PASSWORD_CHARS_LCASE = "abcdefgijkmnopqrstwxyz";
            string PASSWORD_CHARS_UCASE = "ABCDEFGHJKLMNPQRSTWXYZ";
            string PASSWORD_CHARS_NUMERIC = "1234567890";
            string PASSWORD_CHARS_SPECIAL = "!@#$%^&*()-+<>?";
            if ((numericLength + lCaseLength + uCaseLength + specialLength) < 8)
                return string.Empty;
            else
            {
                //get char
                var strNumeric = new string(Enumerable.Repeat(PASSWORD_CHARS_NUMERIC, numericLength)
                    .Select(s => s[random.Next(s.Length)]).ToArray());

                var strUper = new string(Enumerable.Repeat(PASSWORD_CHARS_UCASE, uCaseLength)
                    .Select(s => s[random.Next(s.Length)]).ToArray());

                var strSpecial = new string(Enumerable.Repeat(PASSWORD_CHARS_SPECIAL, specialLength)
                    .Select(s => s[random.Next(s.Length)]).ToArray());

                var strLower = new string(Enumerable.Repeat(PASSWORD_CHARS_LCASE, lCaseLength)
                    .Select(s => s[random.Next(s.Length)]).ToArray());

                //result : ký tự số + chữ hoa + chữ thường + các ký tự đặc biệt > 8
                var strResult = strNumeric + strUper + strSpecial + strLower;
                return strResult;
            }
        }

        public static string PasswordGenerateHmac(string clearMessage, string secretKeyString)
        {
            var encoder = new ASCIIEncoding();
            var messageBytes = encoder.GetBytes(clearMessage);
            var secretKeyBytes = new byte[secretKeyString.Length / 2];
            for (int index = 0; index < secretKeyBytes.Length; index++)
            {
                string byteValue = secretKeyString.Substring(index * 2, 2);
                secretKeyBytes[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }
            var hmacsha512 = new HMACSHA512(secretKeyBytes);
            byte[] hashValue = hmacsha512.ComputeHash(messageBytes);
            string hmac = "";
            foreach (byte x in hashValue)
            {
                hmac += String.Format("{0:x2}", x);
            }
            return hmac.ToUpper();
        }

        public static Expression<Func<T, bool>> PredicateByName<T>(string propName, object propValue)
        {
            var parameterExpression = Expression.Parameter(typeof(T));
            var propertyOrField = Expression.PropertyOrField(parameterExpression, propName);
            var binaryExpression = Expression.GreaterThan(propertyOrField, Expression.Constant(propValue));
            return Expression.Lambda<Func<T, bool>>(binaryExpression, parameterExpression);
        }

        public static string GenerateAutoCode(string prefix, long number)
        {
            return prefix + (number > 9999 ? number.ToString() : (10000 + number).ToString().Remove(0, 1)); ;
        }

        /// <summary>
        /// Chuyển đổi mã HEX sang RGB
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        public static Color HexToColor(String hexString)
        // Translates a html hexadecimal definition of a color into a .NET Framework Color.
        // The input string must start with a '#' character and be followed by 6 hexadecimal
        // digits. The digits A-F are not case sensitive. If the conversion was not successfull
        // the color white will be returned.
        {
            Color actColor;
            int r, g, b;
            r = 0;
            g = 0;
            b = 0;
            if ((hexString.StartsWith("#")) && (hexString.Length == 7))
            {
                r = int.Parse(hexString.Substring(1, 2), NumberStyles.AllowHexSpecifier);
                g = int.Parse(hexString.Substring(3, 2), NumberStyles.AllowHexSpecifier);
                b = int.Parse(hexString.Substring(5, 2), NumberStyles.AllowHexSpecifier);
                actColor = Color.FromArgb(r, g, b);
            }
            else
            {
                actColor = Color.Black;
            }
            return actColor;
        }

        //Encode
        public static string Base64Encode(string plainText)
        {
            try
            {
                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
                return System.Convert.ToBase64String(plainTextBytes);
            }
            catch (Exception)
            {
                return string.Empty;
            }

        }
        //Decode
        public static string Base64Decode(string base64EncodedData)
        {
            try
            {
                var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
                return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
        public static byte[] Base64DecodeToByteArray(string base64EncodedData)
        {
            try
            {
                var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
                return base64EncodedBytes;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string BuildStringFromTemplate(string source, params object[] parameters)
        {
            return String.Format(source, parameters);
        }

        /// <summary>
        /// Replace các trường thông tin trên nội dung notify
        /// </summary>
        /// <param name="data"></param>
        /// <param name="contents"></param>
        public static string[] ReplaceContentNotify(object data, params string[] contents)
        {
            string placeHolder, placeHolderValue = string.Empty;
            for (int i = 0; i < contents.Length; i++)
            {
                foreach (PropertyInfo prop in data.GetType().GetProperties())
                {
                    if (TemplatePlaceHolder.PlaceHolder.TryGetValue(prop.Name, out placeHolder))
                    {
                        placeHolderValue = prop.GetValue(data, null) != null ? prop.GetValue(data, null).ToString() : string.Empty;
                        if (contents[i] != null)
                        {
                            contents[i] = contents[i].Replace(placeHolder, prop.GetValue(data, null).ToString());
                        }
                    }
                }
            }

            return contents;
        }

        public static List<string> DecodeCertificate(string certificateBase64)
        {
            var certs = new List<string>();
            var base64EncodedBytes = System.Convert.FromBase64String(certificateBase64);
            var certStr = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);

            var listCert = certStr.Split("-----END CERTIFICATE-----").ToList();

            // xóa bản ghi cuối cùng chỉ có \n
            listCert.Remove(listCert.LastOrDefault());

            listCert.ForEach(cert =>
            {
                var certSpl = cert.Split("-----BEGIN CERTIFICATE-----").ToList();
                certs.Add(certSpl.LastOrDefault().Trim('\n').Replace("\n", "").Replace("\r", ""));
            });

            return certs;
        }
    }

    public class TokenRequest
    {
        public string Token { get; set; }
        public string Password { get; set; }
    }

    public class TokenInfo
    {
        public Guid ObjectId { get; set; }
        public int Level { get; set; }
        public long Tick { get; set; }
        public DateTime DateTimeExpired { get; set; }
    }

    public static class Encrypt
    {
        #region Encrypt Function

        public static byte[] AES_Decrypt(byte[] bytesToBeDecrypted, byte[] passwordBytes)
        {
            byte[] decryptedBytes = null;

            // Set your salt here, change it to meet your flavor:
            // The salt bytes must be at least 8 bytes.
            byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC;
                    AES.Padding = PaddingMode.Zeros;

                    using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
                        cs.Close();
                    }
                    decryptedBytes = ms.ToArray();
                }
            }

            return decryptedBytes;
        }
        public static byte[] AES_Encrypt(byte[] bytesToBeEncrypted, byte[] passwordBytes)
        {
            byte[] encryptedBytes = null;

            // Set your salt here, change it to meet your flavor:
            // The salt bytes must be at least 8 bytes.
            byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC;
                    AES.Padding = PaddingMode.Zeros;

                    using (var cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                        cs.Close();
                    }
                    encryptedBytes = ms.ToArray();
                }
            }

            return encryptedBytes;
        }
        public static string DecryptText(string input, string password)
        {
            // Get the bytes of the string
            byte[] bytesToBeDecrypted = Convert.FromBase64String(input);
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            passwordBytes = SHA256.Create().ComputeHash(passwordBytes);

            byte[] bytesDecrypted = AES_Decrypt(bytesToBeDecrypted, passwordBytes);

            string result = Encoding.UTF8.GetString(bytesDecrypted);

            return result;
        }
        public static string EncryptText(string input, string password)
        {
            // Get the bytes of the string
            byte[] bytesToBeEncrypted = Encoding.UTF8.GetBytes(input);
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            // Hash the password with SHA256
            passwordBytes = SHA256.Create().ComputeHash(passwordBytes);

            byte[] bytesEncrypted = AES_Encrypt(bytesToBeEncrypted, passwordBytes);

            string result = Convert.ToBase64String(bytesEncrypted);

            return result;
        }

        public static string EncryptSha256(string input)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        #endregion   
    }

    public static class Security
    {
        #region Check sum
        public static class Algorithms
        {
            public static readonly HashAlgorithm MD5 = new MD5CryptoServiceProvider();
            public static readonly HashAlgorithm SHA1 = new SHA1Managed();
            public static readonly HashAlgorithm SHA256 = new SHA256Managed();
            public static readonly HashAlgorithm SHA384 = new SHA384Managed();
            public static readonly HashAlgorithm SHA512 = new SHA512Managed();
            //public static readonly HashAlgorithm RIPEMD160 = new RIPEMD160Managed();
        }
        public static string GetHashFromFile(string fileName, HashAlgorithm algorithm)
        {
            using (var stream = new BufferedStream(File.OpenRead(fileName), 100000))
            {
                return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty);
            }
        }
        public static bool VerifyHashFromFile(string fileName, HashAlgorithm algorithm, string hashInput)
        {
            bool verify = false;
            string hashResult = "";

            using (var stream = new BufferedStream(File.OpenRead(fileName), 100000))
            {
                hashResult = BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty);
                if (hashResult.SequenceEqual(hashInput)) verify = true;
            }

            return verify;
        }
        #endregion
    }

    public static class TokenHelpers
    {
        #region basic token

        /// <summary>
        /// Tạo token theo key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string CreateBasicToken(string key)
        {
            try
            {
                string token = string.Empty;

                byte[] keyData = Encoding.UTF8.GetBytes(key);

                // Token chứa mã đối tượng tải về
                if (keyData != null) token = Convert.ToBase64String(keyData.ToArray());
                //Safe URl
                token = Base64UrlEncoder.Encode(token);
                return token;
            }
            catch (Exception)
            {
                throw;
            }

        }

        /// <summary>
        /// Lấy key theo token
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static string GetKeyFromBasicToken(string token)
        {
            try
            {
                //Safe URl
                token = Base64UrlEncoder.Decode(token);
                string key = string.Empty;

                if (IsBase64(token))
                {
                    byte[] dataToken = Convert.FromBase64String(token);

                    if (dataToken != null) key = Encoding.UTF8.GetString(dataToken);
                }
                return key;
            }
            catch (Exception)
            {

                throw;
            }
        }
        #endregion 

        #region token download

        /// <summary>
        /// Tạo token chứa mã đối tượng, thời gian hiệu lực
        /// </summary>
        /// <param name="objectId">mã đối tượng</param>
        /// <param name="ticks">thời gian hiệu lực</param>
        /// <param name="keyEncrypt">khóa mã hóa</param>
        /// <returns></returns>
        public static string CreateUniqueToken(string objectId, long ticks, string keyEncrypt)
        {
            try
            {
                string token = string.Empty;

                byte[] key = Encoding.UTF8.GetBytes(objectId);
                byte[] time = Encoding.UTF8.GetBytes(ticks.ToString());

                // Token chứa thông tin thời gian hết hạn và mã đối tượng tải về
                if (time.Concat(key) != null) token = Convert.ToBase64String(key.Concat(time).ToArray());

                // Mã hóa token
                if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(keyEncrypt)) token = Encrypt.EncryptText(token, keyEncrypt);
                //Safe URl
                token = Base64UrlEncoder.Encode(token);
                return token;
            }
            catch (Exception)
            {
                throw;
            }

        }

        /// <summary>
        /// Lấy thời gian hết hạn theo token
        /// </summary>
        /// <param name="token">mã đối tượng</param>
        /// <param name="keyEncrypt">khóa mã hóa</param>
        /// <returns></returns>
        public static DateTime? GetDateTimeExpired(string token, string keyEncrypt)
        {
            try
            {
                //Safe URl
                token = Base64UrlEncoder.Decode(token);
                // Giải mã chuỗi token nếu dùng mã hóa
                if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(keyEncrypt)) token = Encrypt.DecryptText(token, keyEncrypt);
                token = token.Replace("\0", string.Empty);
                DateTime unixYear0 = new DateTime(1970, 1, 1, 0, 0, 1);
                DateTime dateTimeExpired = DateTime.Now;

                string timeTicksExpiredString = string.Empty;

                if (IsBase64(token))
                {
                    byte[] dataToken = Convert.FromBase64String(token);
                    if (dataToken != null)
                    {
                        byte[] dataTick = new byte[dataToken.Length - 36];

                        Array.Copy(dataToken, 36, dataTick, 0, dataToken.Length - 36);
                        if (dataTick != null) timeTicksExpiredString = Encoding.UTF8.GetString(dataTick);
                        if (!string.IsNullOrEmpty(timeTicksExpiredString))
                        {
                            long ticks = long.Parse(timeTicksExpiredString);
                            dateTimeExpired = new DateTime(unixYear0.Ticks + ticks);
                        }
                    }
                    return dateTimeExpired;
                }
                return null;

            }
            catch (Exception)
            {

                throw;
            }

        }
        /// <summary>
        /// Lấy mã đối tượng theo token
        /// </summary>
        /// <param name="token">mã đối tượng</param>
        /// <param name="keyEncrypt">khóa mã hóa</param>
        /// <returns></returns>
        public static Guid GetObjectId(string token, string keyEncrypt)
        {
            try
            {
                //Safe URl
                token = Base64UrlEncoder.Decode(token);
                // Giải mã chuỗi token nếu sử dụng mã hóa
                if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(keyEncrypt)) token = Encrypt.DecryptText(token, keyEncrypt);
                token = token.Replace("\0", string.Empty);
                Guid objectId = Guid.Empty;

                if (IsBase64(token))
                {
                    string objectStringId = string.Empty;
                    byte[] dataToken = Convert.FromBase64String(token);
                    byte[] dataGuid = new byte[36];
                    Array.Copy(dataToken, 0, dataGuid, 0, 36);
                    if (dataGuid != null) objectStringId = Encoding.UTF8.GetString(dataGuid);

                    if (!string.IsNullOrEmpty(objectStringId) && Utils.IsGuid(objectStringId))
                    {
                        objectId = new Guid(objectStringId);
                    }
                }


                return objectId;
            }
            catch (Exception)
            {

                throw;
            }

        }
        #endregion
        #region token tokenInfo
        /// <summary>
        /// Tạo token
        /// </summary>
        /// <param name="tokenInfo"></param>
        /// <param name="keyEncrypt"></param>
        /// <returns></returns>
        public static string CreateToken(TokenInfo tokenInfo, string keyEncrypt)
        {
            try
            {
                string token = string.Empty;

                byte[] objectId = Encoding.UTF8.GetBytes(tokenInfo.ObjectId.ToString());
                byte[] level = Encoding.UTF8.GetBytes(tokenInfo.Level.ToString());
                byte[] tick = Encoding.UTF8.GetBytes(tokenInfo.Tick.ToString());

                // Token chứa thông tin thời gian hết hạn và mã đối tượng tải về
                if (level.Concat(objectId).Concat(tick) != null) token = Convert.ToBase64String(level.Concat(objectId).Concat(tick).ToArray());
                // Mã hóa token
                if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(keyEncrypt)) token = Encrypt.EncryptText(token, keyEncrypt);
                //Safe URl
                token = Base64UrlEncoder.Encode(token);
                return token;
            }
            catch (Exception)
            {
                throw;
            }

        }
        /// <summary>
        /// Lấy token
        /// </summary>
        /// <param name="token"></param>
        /// <param name="keyEncrypt"></param>
        /// <returns></returns>
        public static TokenInfo GetToken(string token, string keyEncrypt)
        {
            try
            {
                //Safe URl
                token = Base64UrlEncoder.Decode(token);
                // Giải mã chuỗi token nếu dùng mã hóa
                if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(keyEncrypt)) token = Encrypt.DecryptText(token, keyEncrypt);
                token = token.Replace("\0", string.Empty);
                if (IsBase64(token))
                {
                    byte[] dataToken = Convert.FromBase64String(token);
                    if (dataToken != null)
                    {
                        var result = new TokenInfo();
                        byte[] dataLevel = new byte[1];
                        Array.Copy(dataToken, 0, dataLevel, 0, 1);
                        byte[] dataGuid = new byte[36];
                        Array.Copy(dataToken, 1, dataGuid, 0, 36);
                        byte[] dataTick = new byte[dataToken.Length - 37];
                        Array.Copy(dataToken, 37, dataTick, 0, dataToken.Length - 37);
                        if (dataLevel != null && dataGuid != null && dataTick != null)
                        {
                            result.ObjectId = new Guid(Encoding.UTF8.GetString(dataGuid));
                            result.Level = Convert.ToInt16(Encoding.UTF8.GetString(dataLevel));
                            result.Tick = long.Parse(Encoding.UTF8.GetString(dataTick));
                            DateTime unixYear0 = new DateTime(1970, 1, 1, 0, 0, 1);
                            DateTime dateTimeExpired = DateTime.Now;
                            string timeTicksExpiredString = string.Empty;
                            dateTimeExpired = new DateTime(unixYear0.Ticks + result.Tick);
                            result.DateTimeExpired = dateTimeExpired;
                        }
                        return result;
                    }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }

        }
        #endregion
        public static bool IsBase64(this string base64String)
        {
            if (base64String == null || base64String.Length == 0 || base64String.Length % 4 != 0
               || base64String.Contains(" ") || base64String.Contains("\t") || base64String.Contains("\r") || base64String.Contains("\n"))
                return false;

            try
            {
                Convert.FromBase64String(base64String);
                return true;
            }
            catch (Exception)
            {
                // Handle the exception
            }
            return false;
        }

    }

    public static class ConvertPDF
    {
        public static void ConvertToPDFA(ref MemoryStream content)
        {
            try
            {
                PdfDocument pdf = new PdfDocument();
                //Load the PDF file
                pdf.LoadFromStream(content);

                //Get the conformance level of the PDF file           
                PdfConformanceLevel conformance = pdf.Conformance;
                if (conformance != PdfConformanceLevel.Pdf_A3A)
                {
                    //Convert PDF/A
                    MemoryStream convertData = new MemoryStream(0);
                    PdfStandardsConverter converter = new PdfStandardsConverter(content);
                    converter.ToPdfA3A(convertData);
                    content = convertData;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static async Task<MemoryStream> ConvertDocxToPDFAsync(MemoryStream content)
        {
            try
            {
                FileBase64Model file = new FileBase64Model()
                {
                    FileName = "ConvertData",
                    FileBase64 = Base64Convert.ConvertMemoryStreamToBase64(content)
                };
                FileConverterResponseModel dataResult = new FileConverterResponseModel();

                var uri = Utils.GetConfig("PDFCovert:uri");
                var url = new Uri(uri + "api/v1/pdf-converter/from-docx");
                HttpClientHandler clientHandler = new HttpClientHandler();
                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

                HttpClient httpClient = new HttpClient(clientHandler);

                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var dt = new StringContent(JsonSerializer.Serialize(file), Encoding.UTF8, "application/json");
                var rp = await httpClient.PostAsync(url, dt);

                var rs = rp.Content.ReadAsStringAsync().Result;
                dataResult = JsonSerializer.Deserialize<FileConverterResponseModel>(rs);

                if (dataResult.Code == (int)Code.Success)
                {
                    var fileBase64 = dataResult.Data.FileBase64;
                    var base64EncodedBytes = System.Convert.FromBase64String(fileBase64);
                    MemoryStream convertData = new MemoryStream(base64EncodedBytes);

                    return convertData;
                }
                else
                {
                    throw new Exception(dataResult.Message);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                throw ex;
            }
        }

        public static async Task<FileBase64Model> ConvertWordToPDFAsync(FileBase64Model file)
        {
            FileConverterResponseModel dataResult = new FileConverterResponseModel();
            try
            {
                var uri = Utils.GetConfig("PDFCovert:uri");
                var url = new Uri(uri + "api/v1/pdf-converter/from-docx");
                HttpClientHandler clientHandler = new HttpClientHandler();
                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

                HttpClient httpClient = new HttpClient(clientHandler);

                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var content = new StringContent(JsonSerializer.Serialize(file), Encoding.UTF8, "application/json");
                var rp = await httpClient.PostAsync(url, content);

                var rs = rp.Content.ReadAsStringAsync().Result;
                dataResult = JsonSerializer.Deserialize<FileConverterResponseModel>(rs);

                if (dataResult.Code == (int)Code.Success)
                {
                    return new FileBase64Model()
                    {
                        Code = Code.Success,
                        FileBase64 = dataResult.Data.FileBase64,
                        FileName = dataResult.Data.FileName
                    };
                }
                else
                {
                    return new FileBase64Model()
                    {
                        Code = Code.ServerError,
                        Message = dataResult.Message
                    };
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new FileBase64Model()
                {
                    Code = Code.ServerError,
                    Message = ex.Message
                };
            }
        }

        public static async Task<FileBase64Model> ConvertDocxMetaDataToPDFAsync(FileBase64Model file)
        {
            FileConverterResponseModel dataResult = new FileConverterResponseModel();
            try
            {
                var uri = Utils.GetConfig("PDFCovert:uri");
                var url = new Uri(uri + "api/v1/pdf-converter/from-docx-meta-data");
                HttpClientHandler clientHandler = new HttpClientHandler();
                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

                HttpClient httpClient = new HttpClient(clientHandler);

                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                foreach (var item in file.ListData)
                {
                    if (item.Value == null)
                    {
                        item.Value = "";
                    }
                }

                var content = new StringContent(JsonSerializer.Serialize(file), Encoding.UTF8, "application/json");
                var rp = await httpClient.PostAsync(url, content);

                var rs = rp.Content.ReadAsStringAsync().Result;
                dataResult = JsonSerializer.Deserialize<FileConverterResponseModel>(rs);

                if (dataResult.Code == (int)Code.Success)
                {
                    return new FileBase64Model()
                    {
                        Code = Code.Success,
                        FileBase64 = dataResult.Data.FileBase64,
                        FileName = dataResult.Data.FileName
                    };
                }
                else
                {
                    return new FileBase64Model()
                    {
                        Code = Code.ServerError,
                        Message = dataResult.Message
                    };
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex, MessageConstants.ErrorLogMessage);
                return new FileBase64Model()
                {
                    Code = Code.ServerError,
                    Message = ex.Message
                };
            }

        }
    }

    public static class Base64Convert
    {
        public static string ConvertStreamToBase64(Stream stream)
        {
            byte[] bytes;
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                bytes = memoryStream.ToArray();
            }

            string base64 = Convert.ToBase64String(bytes);
            return base64;
        }

        public static string ConvertMemoryStreamToBase64(MemoryStream memoryStream)
        {
            byte[] bytes = memoryStream.ToArray();

            string base64 = Convert.ToBase64String(bytes);
            return base64;
        }

        public static MemoryStream ConvertBase64ToMemoryStream(string base64Content)
        {
            byte[] bytes = Convert.FromBase64String(base64Content);

            MemoryStream ms = new MemoryStream(bytes);
            return ms;
        }
    }

    public static class SHA256Convert
    {
        public static string ConvertMemoryStreamToSHA256(MemoryStream memoryStream)
        {
            StringBuilder hash = new StringBuilder();
            using (SHA256 sHA256 = SHA256.Create())
            {
                memoryStream.Position = 0;
                try
                {
                    byte[] hashValue = sHA256.ComputeHash(memoryStream);
                    foreach (byte elemt in hashValue)
                    {
                        hash.Append(elemt.ToString("x2"));
                    }
                }
                catch { }
            }

            return hash.ToString();
        }
    }
}

