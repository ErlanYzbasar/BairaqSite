using System.Security.Cryptography;
using System.Text;

namespace COMMON;

public static class Md5Helper
{
    #region MD5 арқылы құпияластыру  +  CreateHashMD5(string s)

    public static string CreateHashMd5(string s)
    {
        var algorithm = MD5.Create();
        var data = algorithm.ComputeHash(Encoding.UTF8.GetBytes(s));
        var md5 = "";
        for (var i = 0; i < data.Length; i++)
        {
            md5 += data[i].ToString("x2").ToUpperInvariant();
        }

        return md5;
    }

    #endregion

    #region Құпия сөзді құпияластыру +PasswordMd5Encrypt(string password)

    public static string PasswordMd5Encrypt(string password)
    {
        const string salt = "QarSolutions2024";
        password = CreateHashMd5(password);
        password = CreateHashMd5(password + salt);
        return password;
    }

    #endregion
}