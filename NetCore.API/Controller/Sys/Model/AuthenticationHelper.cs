using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.IO;
using NetCore.Shared;
using System.Text.Json;

namespace NetCore.API
{
    public static class AuthenticationHelper
    {
        #region AES Encrypt
        public static byte[] AES_Encrypt(byte[] bytesToBeEncrypted, byte[] passwordBytes)
        {
            byte[] encryptedBytes = null;

            // Set your salt here, change it to meet your flavor:
            byte[] saltBytes = passwordBytes;
            // Example:
            //saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

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

                    using (CryptoStream cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                        cs.Close();
                    }
                    encryptedBytes = ms.ToArray();
                }
            }

            return encryptedBytes;
        }

        public static byte[] AES_Decrypt(byte[] bytesToBeDecrypted, byte[] passwordBytes)
        {
            byte[] decryptedBytes = null;
            // Set your salt here to meet your flavor:
            byte[] saltBytes = passwordBytes;
            // Example:
            //saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

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

                    using (CryptoStream cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
                        cs.Close();
                    }
                    decryptedBytes = ms.ToArray();
                }
            }

            return decryptedBytes;
        }

        public static string Encrypt(string text, string password)
        {
            byte[] passwordBytes = GetPasswordBytes(password);
            byte[] originalBytes = Encoding.UTF8.GetBytes(text);
            byte[] encryptedBytes = null;

            // Hash the password with SHA256
            passwordBytes = SHA256.Create().ComputeHash(passwordBytes);

            // Getting the salt size
            int saltSize = GetSaltSize(passwordBytes);

            // Generating salt bytes
            byte[] saltBytes = GetRandomBytes(saltSize);

            // Appending salt bytes to original bytes
            byte[] bytesToBeEncrypted = new byte[saltBytes.Length + originalBytes.Length];
            for (int i = 0; i < saltBytes.Length; i++)
            {
                bytesToBeEncrypted[i] = saltBytes[i];
            }
            for (int i = 0; i < originalBytes.Length; i++)
            {
                bytesToBeEncrypted[i + saltBytes.Length] = originalBytes[i];
            }

            encryptedBytes = AES_Encrypt(bytesToBeEncrypted, passwordBytes);

            return Convert.ToBase64String(encryptedBytes);
        }
        public static byte[] GetPasswordBytes(string password)
        {
            var ba = Encoding.UTF8.GetBytes(password);
            return System.Security.Cryptography.SHA256.Create().ComputeHash(ba);
        }

        public static string Decrypt(string decryptedText, string password)
        {
            byte[] passwordBytes = GetPasswordBytes(password);
            byte[] bytesToBeDecrypted = Convert.FromBase64String(decryptedText);

            // Hash the password with SHA256
            passwordBytes = SHA256.Create().ComputeHash(passwordBytes);

            byte[] decryptedBytes = AES_Decrypt(bytesToBeDecrypted, passwordBytes);

            // Getting the size of salt
            int saltSize = GetSaltSize(passwordBytes);

            // Removing salt bytes, retrieving original bytes
            byte[] originalBytes = new byte[decryptedBytes.Length - saltSize];
            for (int i = saltSize; i < decryptedBytes.Length; i++)
            {
                originalBytes[i - saltSize] = decryptedBytes[i];
            }

            return Encoding.UTF8.GetString(originalBytes);
        }

        public static int GetSaltSize(byte[] passwordBytes)
        {
            var key = new Rfc2898DeriveBytes(passwordBytes, passwordBytes, 1000);
            byte[] ba = key.GetBytes(2);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ba.Length; i++)
            {
                sb.Append(Convert.ToInt32(ba[i]).ToString());
            }
            int saltSize = 0;
            string s = sb.ToString();
            foreach (char c in s)
            {
                int intc = Convert.ToInt32(c.ToString());
                saltSize = saltSize + intc;
            }

            return saltSize;
        }

        public static byte[] GetRandomBytes(int length)
        {
            byte[] ba = new byte[length];
            RNGCryptoServiceProvider.Create().GetBytes(ba);
            return ba;
        }
        #endregion

        public static string BuildToken(BaseUserLoginModel user, bool isRememberMe, double timeToLive)
        {
            //var claims = new[]
            //{
            //    new Claim(ClaimTypes.Name, user.Id.ToString()),
            //    new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
            //    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            //};
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimConstants.USER_EMAIL, user.Email??""),
                new Claim(ClaimConstants.USER_NAME, user.UserName??""),
                new Claim(ClaimConstants.FULL_NAME, user.Name??""),
                new Claim(ClaimConstants.ORG_ID, user.OrganizationId.HasValue ? user.OrganizationId.Value.ToString() : ""),
                new Claim(ClaimConstants.USER_ID, user.Id.ToString()),
                //new Claim(ClaimConstants.ROLES, user.ApplicationId.ToString()),
                //new Claim(ClaimConstants.APPS, JsonSerializer.Serialize(listApplication.Select(x=>x.Code))),
                new Claim(ClaimConstants.ROLES, JsonSerializer.Serialize(user.ListRole)),
                new Claim(ClaimConstants.RIGHTS, JsonSerializer.Serialize(user.ListRight)),
                //new Claim(ClaimConstants.EXPIRES_AT, iat.ToUnixTime().ToString()),
                //new Claim(ClaimConstants.ISSUED_AT,  DateTime.UtcNow.tou().ToString()),
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Utils.GetConfig("Authentication:Jwt:Key")));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(Utils.GetConfig("Authentication:Jwt:Issuer"),
                           Utils.GetConfig("Authentication:Jwt:Issuer"),
                           claims,
                           notBefore: DateTime.UtcNow,
                           expires: DateTime.UtcNow.AddSeconds(timeToLive),
                           signingCredentials: creds);
            if (isRememberMe)
            {
                token = new JwtSecurityToken(Utils.GetConfig("Authentication:Jwt:Issuer"),
                             Utils.GetConfig("Authentication:Jwt:Issuer"),
                             claims,
                             notBefore: DateTime.UtcNow,
                             expires: DateTime.UtcNow.AddDays(1),
                             signingCredentials: creds);
            }

            return new JwtSecurityTokenHandler().WriteToken(token);

        }

        /// <summary>
        ///     Tạo Id quyền riêng
        /// </summary>
        /// <returns></returns>
        public static Guid MakeIndependentPermission()
        {
            return new Guid("00000000-0000-0000-0000-000000000000");
        }

        /// <summary>
        ///     Lấy về danh sách RoleId đã kế thừa từ string
        /// </summary>
        /// <param name="currInheritedFromRoles"></param>
        /// <returns></returns>
        public static List<Guid> LoadRolesInherited(string currInheritedFromRoles)
        {
            try
            {
                if (string.IsNullOrEmpty(currInheritedFromRoles))
                {
                    var result = new List<Guid>();
                    return result;
                }
                var jsonObject = JsonSerializer.Deserialize<List<Guid>>(currInheritedFromRoles);

                var newlist = new List<Guid>();

                foreach (var item in jsonObject)
                    if (!newlist.Contains(item))
                        newlist.Add(item);
                jsonObject = jsonObject.GroupBy(role => role)
                    .Select(g => g.First())
                    .ToList();

                return jsonObject;
            }
            catch (Exception)
            {
                var result = new List<Guid>();
                return result;
            }
        }

        /// <summary>
        ///     Lấy về danh sách OrgId đã kế thừa từ string
        /// </summary>
        /// <param name="currInheritedFromDVs"></param>
        /// <returns></returns>
        public static List<Guid> LoadDVsInherited(string currInheritedFromDVs)
        {
            try
            {
                if (string.IsNullOrEmpty(currInheritedFromDVs))
                {
                    var result = new List<Guid>();
                    return result;
                }
                var jsonObject = JsonSerializer.Deserialize<List<Guid>>(currInheritedFromDVs);

                var newlist = new List<Guid>();

                foreach (var item in jsonObject)
                    if (!newlist.Contains(item))
                        newlist.Add(item);
                jsonObject = jsonObject.GroupBy(role => role)
                    .Select(g => g.First())
                    .ToList();

                return jsonObject;
            }
            catch (Exception)
            {
                var result = new List<Guid>();
                return result;
            }
        }

        /// <summary>
        ///     Sinh ra chuỗi Json kế thừa từ 1 danh sách RoleId lưu vào DB
        /// </summary>
        /// <param name="listRoleId"></param>
        /// <returns></returns>
        public static string GenRolesInherited(List<Guid> listRoleId)
        {
            try
            {
                var jsonStr = JsonSerializer.Serialize(listRoleId);
                return jsonStr;
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>
        ///     Sinh ra chuỗi Json kế thừa từ 1 RoleId lưu vào DB
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public static string GenRolesInherited(Guid roleId)
        {
            var roles = new List<Guid>
            {
                roleId
            };
            return GenRolesInherited(roles);
        }

        /// <summary>
        ///     Bỏ 1 RoleId kế thừa
        /// </summary>
        /// <param name="currInheritedFromRoles"></param>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public static string RemoveRolesInherited(string currInheritedFromRoles, Guid roleId)
        {
            var roles = LoadRolesInherited(currInheritedFromRoles) ?? new List<Guid>();
            if (roles.Contains(roleId)) roles.Remove(roleId);
            var result = GenRolesInherited(roles);
            return result;
        }

        /// <summary>
        ///     Bỏ 1 danh sách RoleId kế thừa
        /// </summary>
        /// <param name="currInheritedFromRoles"></param>
        /// <param name="listRoleId"></param>
        /// <returns></returns>
        public static string RemoveRolesInherited(string currInheritedFromRoles, List<Guid> listRoleId)
        {
            var result = currInheritedFromRoles;
            if (listRoleId == null)
                listRoleId = new List<Guid>();
            foreach (var role in listRoleId) result = RemoveRolesInherited(result, role);
            return result;
        }

        /// <summary>
        ///     Thêm 1 RoleId kế thừa
        /// </summary>
        /// <param name="currInheritedFromRoles"></param>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public static string AddRolesInherited(string currInheritedFromRoles, Guid roleId)
        {
            //check RolesInherited truoc
            var roles = LoadRolesInherited(currInheritedFromRoles) ?? new List<Guid>();
            if (!roles.Contains(roleId))
                roles.Add(roleId);
            var result = GenRolesInherited(roles);
            return result;
        }

        /// <summary>
        ///     Thêm 1 danh sách RoleId kế thừa
        /// </summary>
        /// <param name="currInheritedFromRoles"></param>
        /// <param name="listRoleId"></param>
        /// <returns></returns>
        public static string AddRolesInherited(string currInheritedFromRoles, List<Guid> listRoleId)
        {
            var result = currInheritedFromRoles;
            if (listRoleId == null)
                listRoleId = new List<Guid>();
            foreach (var role in listRoleId) result = AddRolesInherited(result, role);
            return result;
        }
    }
}