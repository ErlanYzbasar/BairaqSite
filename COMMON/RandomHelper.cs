namespace COMMON;

public static class RandomHelper
{
    public static string GetNumberRandom(int length)
    {
        var randomCode = string.Empty;
        int number;
        var random = new Random();

        for (var i = 0; i < length; i++)
        {
            number = random.Next();
            number %= 10;
            randomCode += number.ToString();
        }

        return randomCode;
    }

    //Function to get random number
    private static readonly Random Getrandom = new Random();
    private static readonly object SyncLock = new object();

    public static int GetRandomNumber(int min, int max)
    {
        lock (SyncLock)
        {
            // synchronize
            return Getrandom.Next(min, max);
        }
    }
}