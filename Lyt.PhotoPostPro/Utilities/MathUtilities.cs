namespace Lyt.PhotoPostPro.Utilities;

public static class MathUtilities
{
    public static (int Numerator, int Denominator) FloatToFraction(float value)
    {
        // Handle negative numbers safely
        int sign = value < 0 ? -1 : 1;
        value = Math.Abs(value);

        // Convert to string to see how many decimal places exist
        // "R" format ensures maximum precision format retention
        string text = value.ToString("R");
        int decimalPlaces = 0;

        if (text.Contains('.'))
        {
            decimalPlaces = text.Length - text.IndexOf('.') - 1;
        }

        // Calculate numerator and denominator based on 10^decimalPlaces
        int denominator = (int)Math.Pow(10, decimalPlaces);
        int numerator = (int)Math.Round(value * denominator);

        // Simplify using GCD
        int gcd = Gcd(numerator, denominator);

        return (sign * (numerator / gcd), denominator / gcd);
    }

    private static int Gcd(int a, int b)
    {
        while (b != 0)
        {
            int temp = b;
            b = a % b;
            a = temp;
        }
        return a;
    }
}
