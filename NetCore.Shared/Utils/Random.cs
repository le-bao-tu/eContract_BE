using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace NetCore.Shared
{

    public class GenerateData{
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string Blogs { get; set; } 
    }
    /// <summary>
    /// A source of random-but-legal data.
    /// </summary>
    public class RandomData
    {

        // These values were chosen for Service Desk and Portal.
        private const int MIN_EMAIL_LENGTH = MIN_EMAIL_USERPART_LENGTH + 1 + MIN_HOST_LENGTH;   // "user@host".
        private const int MAX_EMAIL_LENGTH = 64;    // Email address is apparently limited to 64 (CSR) or 128 (Customer).
        private const int MIN_EMAIL_USERPART_LENGTH = 1;    // "x".
        private const int MAX_EMAIL_USERPART_LENGTH = MAX_EMAIL_LENGTH / 2;  // Kind of arbitrary.
        private const int MIN_HOST_LENGTH = 1 + 1 + MAX_TLD_LENGTH;    // "x.tld".
        private const int MAX_HOST_LENGTH = 253;            // As per RFC2181 ss. 11.
        private const int MAX_HOST_PART_LENGTH = 63;        // As per RFC1034 ss. 3.1.
        private const int MAX_HOST_PARTS = 127;             // Implied by MAX_HOST_LENGTH=253 (min host part len is 2).
        private const int MIN_URL_LENGTH = MAX_SCHEME_LENGTH + 3 + MIN_HOST_LENGTH; // "scheme://host".
        private const int MAX_URL_LENGTH = 4096;            // Kind of arbitrary.
        private const int MAX_URL_PORT_NUMBER = 65535;      // Port numbers are 16 bits.
        private const int MAX_URL_REST_PART_LENGTH = 1024;  // Kind of arbitrary.
        private const int MAX_SCHEME_LENGTH = 6;            // "gopher" and "telnet" are the longest in urlSchemes[].
        private readonly string[] topLevelDomains = new[] { "org" };   // I can't get anything else to work.  I wanted: { "biz", "com", "info", "mil", "museum", "org", "us", "ru" }
        private const int MAX_TLD_LENGTH = 6;               // "museum" is the longest in topLevelDomains[].
        private readonly string[] urlSchemes = new[] { "ftp", "gopher", "http", "https", "ssh", "telnet", "wais" }; // Among others

        private readonly Random randomNumbers = new Random();

        #region Character set arrays

        private static readonly char[] CHARS_ALPHA;
        private static readonly char[] CHARS_ALPHANUM;
        private static readonly char[] CHARS_ENGLISH_ALPHANUM;
        private static readonly char[] CHARS_ENGLISH_ALPHA;
        private static readonly char[] CHARS_ENGLISH_ALPHA_WHITESPACE;
        private static readonly char[] CHARS_ENGLISH_ALPHANUM_WHITESPACE;
        private static readonly char[] CHARS_ENGLISH_PRINTABLE;
        private static readonly char[] CHARS_ENGLISH_PRINTABLE_WHITESPACE;
        private static readonly char[] CHARS_PRINTABLE;
        private static readonly char[] CHARS_PRINTABLE_WHITESPACE;
        private static readonly char[] CHARS_ALPHA_UPPER = new[]
                                                         {
                                                             'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H',
                                                             'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P',
                                                             'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X',
                                                             'Y', 'Z'
                                                         };
        private static readonly char[] CHARS_ALPHA_LOWER = new[]
                                                       {
                                                             'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h',
                                                             'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p',
                                                             'q', 'r', 's', 't', 'u', 'v', 'w', 'x',
                                                             'y', 'z'
                                                         };
        private static readonly char[] CHARS_ALPHA_LOWER_VI = new[]
      {            'á','à','ạ','ả','ã','â','ấ','ầ','ậ','ẩ','ẫ','ă','ắ','ằ','ặ','ẳ','ẵ',
                   'Á','À','Ạ','Ả','Ã','Â','Ấ','Ầ','Ậ','Ẩ','Ẫ','Ă','Ắ','Ằ','Ặ','Ẳ','Ẵ',
                   'é','è','ẹ','ẻ','ẽ','ê','ế','ề','ệ','ể','ễ','É','È','Ẹ','Ẻ','Ẽ','Ê','Ế','Ề','Ệ','Ể','Ễ',
                   'ó','ò','ọ','ỏ','õ','ô','ố','ồ','ộ','ổ','ỗ','ơ','ớ','ờ','ợ','ở','ỡ',
                   'Ó','Ò','Ọ','Ỏ','Õ','Ô','Ố','Ồ','Ộ','Ổ','Ỗ','Ơ','Ớ','Ờ','Ợ','Ở','Ỡ',
                   'ú','ù','ụ','ủ','ũ','ư','ứ','ừ','ự','ử','ữ',
                   'Ú','Ù','Ụ','Ủ','Ũ','Ư','Ứ','Ừ','Ự','Ử','Ữ',
                   'í','ì','ị','ỉ','ĩ',  'Í','Ì','Ị','Ỉ','Ĩ', 'đ','Đ', 'ý','ỳ','ỵ','ỷ','ỹ', 'Ý','Ỳ','Ỵ','Ỷ','Ỹ'

                                                         };
        // private static readonly char[] CHARS_CHINESE = new[]        // Traditional(?) Chinese
        //                                                  {
        //                                                      '健', '康','繁','荣'
        //                                                  };
        // private static readonly char[] CHARS_CYRILLIC = new[]       // Cyrillic
        //                                                  {
        //                                                      'И', 'в', 'а', 'н', 'Ч', 'е', 'т', 'ё', 'р', 'ы', 'й', 'с', 'и', 'л', 'ь', 'ч'
        //                                                  };
        private static readonly char[] CHARS_NUM = new[]
                                                       {
                                                           '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'
                                                       };

        private static readonly char[] CHARS_SPECIAL1 = new[]
                                                            {
                                                                // Specifically excludes '@', '[', ']', '\', and '.'
                                                                '!', '#', '$', '%', '&', '\'', '*', '+',
                                                                '-', '/' , '=', '?', '^', '_', '`', '{',
                                                                '|', '}'
                                                            };
        private static readonly char[] CHARS_SPECIAL2 = new[]
                                                            {
                                                                '@', '[', ']', '\\', '.'
                                                            };
        private static readonly char[] CHARS_WHITESPACE = new[]
                                                            {
                                                                ' ', '\t'
                                                                // but not '\r', '\n',
                                                            };
        #endregion

        static RandomData()
        {
            // Build some character arrays that we can't just declare.
            List<char> tempCharset;

            tempCharset = new List<char>();
            tempCharset.AddRange(CHARS_ALPHA_UPPER);
            tempCharset.AddRange(CHARS_ALPHA_LOWER);
            tempCharset.AddRange(CHARS_ALPHA_LOWER_VI);
            // tempCharset.AddRange(CHARS_CHINESE);
            // tempCharset.AddRange(CHARS_CYRILLIC);
            CHARS_ALPHA = tempCharset.ToArray();

            tempCharset = new List<char>();
            tempCharset.AddRange(CHARS_ALPHA_UPPER);
            tempCharset.AddRange(CHARS_ALPHA_LOWER);
            tempCharset.AddRange(CHARS_ALPHA_LOWER_VI);
            // tempCharset.AddRange(CHARS_CHINESE);
            // tempCharset.AddRange(CHARS_CYRILLIC);
            tempCharset.AddRange(CHARS_NUM);
            CHARS_ALPHANUM = tempCharset.ToArray();

            tempCharset = new List<char>();
            tempCharset.AddRange(CHARS_ALPHA_UPPER);
            tempCharset.AddRange(CHARS_ALPHA_LOWER);
            CHARS_ENGLISH_ALPHA = tempCharset.ToArray();

            tempCharset = new List<char>();
            tempCharset.AddRange(CHARS_ALPHA_UPPER);
            tempCharset.AddRange(CHARS_ALPHA_LOWER);
            tempCharset.AddRange(CHARS_WHITESPACE);
            CHARS_ENGLISH_ALPHA_WHITESPACE = tempCharset.ToArray();

            tempCharset = new List<char>();
            tempCharset.AddRange(CHARS_ALPHA_UPPER);
            tempCharset.AddRange(CHARS_ALPHA_LOWER);
            tempCharset.AddRange(CHARS_NUM);
            CHARS_ENGLISH_ALPHANUM = tempCharset.ToArray();

            tempCharset = new List<char>();
            tempCharset.AddRange(CHARS_ALPHA_UPPER);
            tempCharset.AddRange(CHARS_ALPHA_LOWER);
            tempCharset.AddRange(CHARS_NUM);
            tempCharset.AddRange(CHARS_WHITESPACE);
            CHARS_ENGLISH_ALPHANUM_WHITESPACE = tempCharset.ToArray();

            tempCharset = new List<char>();
            tempCharset.AddRange(CHARS_ALPHA_UPPER);
            tempCharset.AddRange(CHARS_ALPHA_LOWER);
            tempCharset.AddRange(CHARS_NUM);
            tempCharset.AddRange(CHARS_SPECIAL1);
            CHARS_ENGLISH_PRINTABLE = tempCharset.ToArray();

            tempCharset = new List<char>();
            tempCharset.AddRange(CHARS_ALPHA_UPPER);
            tempCharset.AddRange(CHARS_ALPHA_LOWER);
            tempCharset.AddRange(CHARS_NUM);
            tempCharset.AddRange(CHARS_SPECIAL1);
            tempCharset.AddRange(CHARS_WHITESPACE);
            CHARS_ENGLISH_PRINTABLE_WHITESPACE = tempCharset.ToArray();

            tempCharset = new List<char>();
            tempCharset.AddRange(CHARS_ALPHA_UPPER);
            tempCharset.AddRange(CHARS_ALPHA_LOWER);
            tempCharset.AddRange(CHARS_ALPHA_LOWER_VI);
            // tempCharset.AddRange(CHARS_CHINESE);
            // tempCharset.AddRange(CHARS_CYRILLIC);
            tempCharset.AddRange(CHARS_NUM);
            tempCharset.AddRange(CHARS_SPECIAL1);
            CHARS_PRINTABLE = tempCharset.ToArray();

            tempCharset = new List<char>();
            tempCharset.AddRange(CHARS_ALPHA_UPPER);
            tempCharset.AddRange(CHARS_ALPHA_LOWER);
            tempCharset.AddRange(CHARS_ALPHA_LOWER_VI);
            // tempCharset.AddRange(CHARS_CHINESE);
            // tempCharset.AddRange(CHARS_CYRILLIC);
            tempCharset.AddRange(CHARS_NUM);
            tempCharset.AddRange(CHARS_SPECIAL1);
            tempCharset.AddRange(CHARS_WHITESPACE);
            CHARS_PRINTABLE_WHITESPACE = tempCharset.ToArray();
        }

        /// <summary>
        /// Generate and return a boolean, evenly distributed.
        /// </summary>
        /// <returns>The value.</returns>
        public bool RandomBool()
        {
            return randomNumbers.NextBool();
        }

        /// <summary>
        /// Returns a random item from the specified collection of items.
        /// </summary>
        /// <param name="collection">The items to select from.</param>
        /// <returns>The item.</returns>
        public DataType RandomChoice<DataType>(IList<DataType> collection)
        {
            if (collection == null || collection.Count == 0)
            {
                throw new Exception("There are no values.");
            }
            DataType result = collection[RandomInteger(0, collection.Count - 1)];
            return result;
        }

        /// <summary>
        /// Generate and return a random date and time within the specified range, inclusive.
        /// </summary>
        /// <param name="min">The minimum date and time.</param>
        /// <param name="max">The maximum date and time.</param>
        /// <returns></returns>
        public DateTime RandomDate(DateTime min, DateTime max)
        {
            long range = max.Ticks - min.Ticks;
            long randomTicks = min.Ticks + ((long)(randomNumbers.NextDouble() * range));
            DateTime result = new DateTime(randomTicks);
            return result;
        }

        /// <summary>
        /// Generate and return a random email address with a valid top level domain, 
        /// evenly distributed in length and value.
        /// </summary>
        /// <returns>The email address</returns>
        public string RandomEmailAddress()
        {
            return RandomEmailAddress(MAX_EMAIL_LENGTH);
        }

        /// <summary>
        /// Generate and return a random email address with a valid top level domain, 
        /// up to the specified length, evenly distributed in length and value.
        /// </summary>
        /// <param name="maxAddrLength">The maximum length to return.</param>
        /// <returns>The email address</returns>
        public string RandomEmailAddress(int maxAddrLength)
        {
            // ServiceDesk erroneously thinks email address user-parts have a very restricted character set.
            // This is the validation pattern from SSJS/inc/Validation.js:
            //      /^[A-Z0-9._%'-]+@[A-Z0-9._%-]+\.[A-Z]{2,4}$/i
            // We also omit "." because it really isn't allowed at the start or end of a local-part, and I don't feel
            // like duplicating the RandomHostName() label-loop code here right now.
            char[] userPartCharset = new[] {
                                               'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H',
                                               'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P',
                                               'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
                                               'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h',
                                               'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p',
                                               'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
                                               '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
                                               '_', '%', '\'', '-'
                                           };
            int maxLength = RandomInteger(MIN_EMAIL_LENGTH, Math.Min(maxAddrLength, MAX_EMAIL_LENGTH));
            int remainingLength = maxLength - (MIN_HOST_LENGTH + 1);
            string result = randomNumbers.NextString(userPartCharset, MIN_EMAIL_USERPART_LENGTH, Math.Min(remainingLength, MAX_EMAIL_USERPART_LENGTH)) + '@';
            remainingLength = maxLength - result.Length;
            result += RandomHostName(remainingLength);
            remainingLength = maxLength - result.Length;
            if (result.Length > maxLength)
            {
                throw new Exception(string.Format("Illegal value produced. maxAddrLength={0}; maxLength={1}; result.Length={2}; remainingLength={3}; value={4}", maxAddrLength, maxLength, result.Length, remainingLength, result));
            }
            return result;
        }

        /// <summary>
        /// Generate and return a random hostname with a valid top level domain,
        /// evenly distributed in length and value.
        /// </summary>
        /// <returns>The hostname.</returns>
        public string RandomHostName()
        {
            return RandomHostName(MAX_HOST_LENGTH);
        }

        /// <summary>
        /// Generate and return a random hostname with a valid top level domain,
        /// up to the specified length, evenly distributed in length and value.
        /// </summary>
        /// <param name="maxHostNameLength">The maximum length to return.</param>
        /// <returns>The hostname.</returns>
        public string RandomHostName(int maxHostNameLength)
        {
            int maxLength = RandomInteger(MIN_HOST_LENGTH, Math.Min(maxHostNameLength, MAX_HOST_LENGTH));
            int hostPartCount = RandomInteger(1, MAX_HOST_PARTS);
            string result = randomNumbers.NextChoice(topLevelDomains);
            int remainingLength = maxLength - result.Length;
            for (int i = 0; i < hostPartCount && remainingLength >= 2; i++)     // ... "hostname" ...
            {
                string hostPart = randomNumbers.NextChoice(CHARS_ENGLISH_ALPHA) +
                                  randomNumbers.NextString(CHARS_ENGLISH_ALPHANUM, 0,
                                                           Math.Min(remainingLength, MAX_HOST_PART_LENGTH) - 2) +
                                  '.';
                result = hostPart + result;
                remainingLength = maxLength - result.Length;
            }
            if (result.Length > maxLength)
            {
                throw new Exception(string.Format("Illegal value produced. maxHostNameLength={0}; maxLength={1}; length={2}; value={3}", maxHostNameLength, maxLength, result.Length, result));
            }
            return result;
        }

        /// <summary>
        /// Generate and return an integer between Int32.MinValue and Int32.MaxValue inclusive, evenly distributed.
        /// </summary>
        /// <returns>The value</returns>
        public int RandomInteger()
        {
            return RandomInteger(Int32.MinValue, Int32.MaxValue);
        }

        /// <summary>
        /// Generate and return an integer within the value range (if any) inclusive, evenly distributed.
        /// </summary>
        /// <param name="min">The minimum value, or null for Int32.MinValue</param>
        /// <param name="max">The maximum value, or null for Int32.MaxValue</param>
        /// <returns>The value</returns>
        public int RandomInteger(int min, int max)
        {
            if (max < Int32.MaxValue)
            {
                max++;  // Because Random.Next() returns min through max-1.
            }
            if (min > max)
            {
                throw new Exception(string.Format("Minimum value {0} greater than maximum value {1}.", min, max));
            }
            int result = randomNumbers.Next(min, max);
            return result;
        }

        /// <summary>
        /// Selects one random item from each non-empty set in the parameter list, and then returns one
        /// item chosen randomly from among those selections.
        /// </summary>
        /// <param name="ChoiceLists">The lists from which the choices are to be made.</param>
        /// <returns>The chosen item.</returns>
        public string RandomItem(params List<string>[] ChoiceLists)
        {
            List<string> choices = new List<string>();
            foreach (List<string> list in ChoiceLists)
            {
                if (list.Count > 0)
                {
                    choices.Add(randomNumbers.NextChoice(list));
                }
            }
            string result = randomNumbers.NextChoice(choices);
            return result;
        }

        /// <summary>
        /// Generate and return a random "name" string, evenly distributed in value and length.
        /// </summary>
        /// <returns>The name.</returns>
        public string RandomName()
        {
            return RandomName(20);
        }

        /// <summary>
        /// Generate and return a random "name" string, evenly distributed in value and length.
        /// </summary>
        /// <param name="maxLength">The maxmimum length.</param>
        /// <returns>The name.</returns>
        public string RandomName(int maxLength)
        {
            string result = randomNumbers.NextString(CHARS_ALPHA, 1, maxLength);
            return result;
        }

        /// <summary>
        /// Generate and return a random telephone number string.
        /// </summary>
        /// <param name="useInternational">Should the number be in international form?</param>
        /// <param name="areaCodeUsesDash">If in non-international form, should the area code be separated from the local number by a dash (true) or by parentheses (false)?</param>
        /// <returns>The value.</returns>
        public string RandomPhone(bool useInternational, bool areaCodeUsesDash)
        {
            string result;
            if (useInternational)
            {
                int country = RandomInteger(1, 99);
                int number1 = RandomInteger(100000000, 999999999);
                int number2 = RandomInteger(0, 999999999);
                result = string.Format("+{0} {1}{2}", country, number1, number2);
            }
            else
            {
                int areacode = RandomInteger(100, 999);
                int exchange = RandomInteger(100, 999);
                int instrument = RandomInteger(0000, 9999);
                if (areaCodeUsesDash)
                {
                    result = string.Format("{0:000}-{1:000}-{2:0000}", areacode, exchange, instrument);
                }
                else
                {
                    result = string.Format("({0:000}) {1:000}-{2:0000}", areacode, exchange, instrument);
                }
            }
            return result;
        }

        /// <summary>
        /// Generate and return a short random string without whitespace.
        /// </summary>
        /// <returns>The string.</returns>
        public string RandomString()
        {
            return RandomString(1, 20, false, false);
        }

        /// <summary>
        /// Generate and return a random string of the specified length without whitespace.
        /// </summary>
        /// <param name="maxLength">The length.</param>
        /// <returns>The string.</returns>
        public string RandomString(int maxLength)
        {
            return RandomString(1, maxLength, false, false);
        }

        /// <summary>
        /// Generate and return a random string of the specified length, possibly containing whitespace.
        /// </summary>
        /// <param name="maxLength">The length.</param>
        /// <param name="isWhitespaceAllowed">Is whitespace allowed in the string?</param>
        /// <param name="mustBeEnglish">Must the string be English letters?</param>
        /// <returns>The string.</returns>
        public string RandomString(int maxLength, bool isWhitespaceAllowed, bool mustBeEnglish)
        {
            return RandomString(1, maxLength, isWhitespaceAllowed, mustBeEnglish);
        }

        /// <summary>
        /// Generate and return a random string of the specified length, possibly containing whitespace.
        /// </summary>
        /// <param name="minLength">The minimum length of the string.</param>
        /// <param name="maxLength">The maximum length of the string.</param>
        /// <param name="isWhitespaceAllowed">Is whitespace allowed in the string?</param>
        /// <param name="mustBeEnglish">Must the string be English letters?</param>
        /// <returns>The string.</returns>
        public string RandomString(int minLength, int maxLength, bool isWhitespaceAllowed, bool mustBeEnglish)
        {
            char[] charset;
            if (mustBeEnglish)
            {
                if (isWhitespaceAllowed) charset = CHARS_ENGLISH_ALPHA_WHITESPACE;
                else charset = CHARS_ENGLISH_ALPHA;
            }
            else
            {
                if (isWhitespaceAllowed)
                {
                    charset = CHARS_PRINTABLE_WHITESPACE;
                }
                else charset = CHARS_PRINTABLE;
            }

            return RandomString(charset, minLength, maxLength);
        }

        /// <summary>
        /// Generate and return a random string of the specified length, possibly containing whitespace.
        /// </summary>
        /// <param name="charset">The characters to select from.</param>
        /// <param name="minLength">The minimum length of the string.</param>
        /// <param name="maxLength">The maximum length of the string.</param>
        /// <returns>The string.</returns>
        public string RandomString(char[] charset, int minLength, int maxLength)
        {

            string result = randomNumbers.NextString(charset, minLength, maxLength);
            return result;
        }

        /// <summary>
        /// Generate and return a random URL with a valid scheme, port, and top level domain,
        /// evenly distributed in length and value.
        /// </summary>
        /// <returns>The URL.</returns>
        public string RandomUrl(int maxUrlLength)
        {
            string result = randomNumbers.NextChoice(urlSchemes) + "://";
            bool includePort = randomNumbers.NextBool();
            int maxLength = RandomInteger(MIN_URL_LENGTH + (includePort ? 6 : 0), Math.Min(maxUrlLength, MAX_URL_LENGTH));
            int remainingLength = maxLength - result.Length - (includePort ? 6 : 0);
            result += RandomHostName(remainingLength);
            result += (includePort ? ":" + RandomInteger(1, MAX_URL_PORT_NUMBER) : "");  // ... ":port" or "" ...
            remainingLength = maxLength - result.Length;
            if (remainingLength > 0)
            {
                result += '/';
                remainingLength = maxLength - result.Length;
            }
            if (remainingLength > 0)
            {
                result += randomNumbers.NextString(CHARS_ALPHANUM, 1, Math.Min(remainingLength, MAX_URL_REST_PART_LENGTH));
            }
            if (result.Length > maxLength)
            {
                throw new Exception(string.Format("Illegal value produced. maxUrlLength={0}; maxLength={1}; length={2}; value={3}", maxUrlLength, maxLength, result.Length, result));
            }
            return result;
        }

    }

    #region System.Random extensions
    public static class RandomExtensions
    {
        /// <summary>
        /// Returns a random true or false value.
        /// </summary>
        /// <param name="random">The Random instance.</param>
        /// <returns>True or false.</returns>
        public static bool NextBool(this Random random)
        {
            return random.Next(0, 2) == 1;
        }

        /// <summary>
        /// Returns a random item from the specified collection of items.
        /// </summary>
        /// <param name="random">The Random instance.</param>
        /// <param name="collection">The items to select from.</param>
        /// <returns>The item.</returns>
        public static DataType NextChoice<DataType>(this Random random, IList<DataType> collection)
        {
            if (collection == null || collection.Count == 0)
            {
                throw new Exception("There are no values.");
            }
            var item = collection[random.Next(0, collection.Count)];
            return item;
        }

        /// <summary>
        /// Returns a random string of up to the specified number of characters from the specified set.
        /// </summary>
        /// <param name="random">The Random instance.</param>
        /// <param name="validChars">The characters to select from.</param>
        /// <param name="minLength">The minimum length of the string.</param>
        /// <param name="maxLength">The maximum length of the string.</param>
        /// <returns>The string.</returns>
        public static string NextString(this Random random, char[] validChars, int minLength, int maxLength)
        {
            if (minLength > maxLength)
            {
                return "";      // This is a convenience so callers don't have to test this condition themselves.
            }
            string result = "";
            int length = random.Next(minLength, maxLength);
            for (int i = 0; i < length; i++)
            {
                result += random.NextChoice(validChars);
            }
            if (result.Length > maxLength)
            {
                throw new Exception(string.Format("Illegal value produced. maxLength={0}; result.Length={1}; value={2}", maxLength, result.Length, result));
            }
            return result;
        }
    }
    #endregion
}