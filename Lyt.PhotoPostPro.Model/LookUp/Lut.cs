namespace Lyt.PhotoPostPro.Model;

/// <summary> A class that represents a 3D LUT with a 3D multidimensional array.  </summary>
public sealed class Lut
{
    private static readonly string[] Separators = { " " };

    /// <summary> Creates a new Lut from the array of string lines a 3DL file </summary>
    /// <param name="lines"> The file Content</param>
    /// <returns>A LUT object</returns>
    public static Lut From3dlLines(string[] lines)
    {
        var lut = new Lut();

        // Skip all:
        // - empty lines
        // - comment lines starting with #
        // - lines starting with "Mesh" or "3DMesh"
        int lineIndex = 0;
        string line = lines[lineIndex];
        while (
            (string.IsNullOrEmpty(line) ||
            (line.StartsWith("3DMesh", StringComparison.InvariantCultureIgnoreCase)) ||
            (line.StartsWith("Mesh", StringComparison.InvariantCultureIgnoreCase)) ||
            (line.StartsWith("#", StringComparison.InvariantCultureIgnoreCase))))
        {
            ++lineIndex;
            if (lineIndex >= lines.GetLength(0))
            {
                throw new ArgumentException("Invalid 3DL file");
            }

            line = lines[lineIndex];
        }

        // Now we are on the line containing the slices
        var tokens = line.Split(Lut.Separators, 256, StringSplitOptions.RemoveEmptyEntries);

        // All tokens should parse as ints and be in increasing order
        int cubeSize = tokens.GetLength(0);
        lut.Slices = new List<int>(cubeSize);
        for (int k = 0; k < cubeSize; ++k)
        {
            int value = 0;
            if (int.TryParse(tokens[k], out value))
            {
                lut.Slices.Add(value);
                if (k > 0)
                {
                    if (lut.Slices[k - 1] >= value)
                    {
                        throw new ArgumentException("Invalid 3DL file");
                    }
                }
            }
            else
            {
                throw new ArgumentException("Invalid 3DL file");
            }
        }

        lut.OutputDepth = GetLikelyLutBitDepth(lut.Slices[cubeSize - 1]);

        // Now all remaining lines not starting with # should parse as three ints
        // First pass to parse and to make sure that the output depth is correct,
        // important because usually it is not.
        int[] rawValues = new int[3 * lines.GetLength(0)];
        int rawIndex = 0;
        int maxValue = int.MinValue;

        ++lineIndex;
        for (int k = lineIndex; k < lines.GetLength(0); ++k)
        {
            string dataLine = lines[k];
            if (dataLine.StartsWith("#", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var dataTokens = dataLine.Split(Lut.Separators, 256, StringSplitOptions.RemoveEmptyEntries);
            if (dataTokens.GetLength(0) != 3)
            {
                continue;
            }

            if (!int.TryParse(dataTokens[0], out int redValue))
            {
                continue;
            }

            int greValue = 0;
            if (!int.TryParse(dataTokens[1], out greValue))
            {
                continue;
            }

            int bluValue = 0;
            if (!int.TryParse(dataTokens[2], out bluValue))
            {
                continue;
            }

            if (redValue > maxValue)
            {
                maxValue = redValue;
            }

            if (bluValue > maxValue)
            {
                maxValue = bluValue;
            }

            if (greValue > maxValue)
            {
                maxValue = greValue;
            }

            rawValues[rawIndex++] = redValue;
            rawValues[rawIndex++] = greValue;
            rawValues[rawIndex++] = bluValue;
        }

        int bitDepth = GetLikelyLutBitDepth(maxValue);
        if (bitDepth > lut.OutputDepth)
        {
            Debug.WriteLine("Shaper bit depth overriden, was {0}, found {1}", lut.OutputDepth, bitDepth);
            lut.OutputDepth = bitDepth;
        }

        lut.Dimension = DimensionFromPixelCount(rawIndex / 3);
        lut.Table = new LutColor[lut.Dimension, lut.Dimension, lut.Dimension];

        // Second pass to create the color table.
        cubeSize = lut.Dimension;
        float maxFloat = (float)Math.Pow(2.0, lut.OutputDepth) - 1.0f;
        int cubeIndex = 0;
        int i = 0;
        while (i < rawIndex)
        {
            int redValue = rawValues[i++];
            int greValue = rawValues[i++];
            int bluValue = rawValues[i++];

            int squaredSize = cubeSize * cubeSize;
            int redIndex = cubeIndex / squaredSize;
            int greIndex = (cubeIndex % squaredSize) / cubeSize;
            int bluIndex = cubeIndex % cubeSize;

            ++cubeIndex;
            if (lut.Table[redIndex, greIndex, bluIndex] != null)
            {
                throw new Exception("Invalid table indexing");
            }

            lut.Table[redIndex, greIndex, bluIndex] =
                LutColor.FromRgbInt(redValue, greValue, bluValue, maxFloat);
        }

        lut.Validate();

        Debug.WriteLine(
            "Lut loaded, {0} colors, size {1}, depth {2}",
            rawIndex / 3, cubeSize, lut.OutputDepth);
        return lut;
    }

    /// <summary> Creates a new Lut from the array of string lines a .Cube file </summary>
    /// <param name="lines"> The file Content</param>
    /// <returns>A LUT object</returns>
    public static Lut FromCubeLines(string[] lines)
    {
        var lut = new Lut();

        // Skip all:
        // - empty lines
        // - comment lines starting with #
        // - lines starting with "TITLE" or "LUT"
        int lineIndex = 0;
        string line = lines[lineIndex];
        while (
            (string.IsNullOrEmpty(line) ||
            (line.StartsWith("TITLE", StringComparison.InvariantCultureIgnoreCase)) ||
            (line.StartsWith("LUT", StringComparison.InvariantCultureIgnoreCase)) ||
            (line.StartsWith("#", StringComparison.InvariantCultureIgnoreCase))))
        {
            ++lineIndex;
            if (lineIndex >= lines.GetLength(0))
            {
                throw new ArgumentException("Invalid Cube file");
            }

            line = lines[lineIndex];
        }

        // Now all remaining lines not starting with # should parse as three floats
        // First pass to parse and to figure out the output depth.
        float[] rawValues = new float[3 * lines.GetLength(0)];
        int rawIndex = 0;
        for (int k = lineIndex; k < lines.GetLength(0); ++k)
        {
            string dataLine = lines[k];
            if (dataLine.StartsWith("#", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string[] dataTokens = dataLine.Split(Lut.Separators, 256, StringSplitOptions.RemoveEmptyEntries);
            if (dataTokens.GetLength(0) != 3)
            {
                continue;
            }

            if (!float.TryParse(dataTokens[0], out float redValue))
            {
                continue;
            }

            if (!float.TryParse(dataTokens[1], out float greValue))
            {
                continue;
            }

            if (!float.TryParse(dataTokens[2], out float bluValue))
            {
                continue;
            }

            rawValues[rawIndex++] = redValue;
            rawValues[rawIndex++] = greValue;
            rawValues[rawIndex++] = bluValue;
        }

        lut.Dimension = DimensionFromPixelCount(rawIndex / 3);
        lut.Table = new LutColor[lut.Dimension, lut.Dimension, lut.Dimension];

        // Second pass to create the color table.
        int cubeSize = lut.Dimension;
        int cubeIndex = 0;
        int i = 0;
        while (i < rawIndex)
        {
            float redValue = rawValues[i++];
            float greValue = rawValues[i++];
            float bluValue = rawValues[i++];

            int squaredSize = cubeSize * cubeSize;
            int redIndex = cubeIndex / squaredSize;
            int greIndex = (cubeIndex % squaredSize) / cubeSize;
            int bluIndex = cubeIndex % cubeSize;

            ++cubeIndex;
            if (lut.Table[redIndex, greIndex, bluIndex] != null)
            {
                throw new Exception("LUT - Invalid table indexing");
            }

            lut.Table[redIndex, greIndex, bluIndex] = LutColor.FromRgbFloat(redValue, greValue, bluValue);
        }

        lut.Validate();
        Debug.WriteLine("CUBE Lut loaded, {0} colors, size {1}", rawIndex / 3, lut.Dimension);
        return lut;
    }

    public LutAlgorithm Algorithm { get; private set; } = LutAlgorithm.Tetrahedral;

    public int Dimension { get; private set; }

    public int OutputDepth { get; private set; }

    public List<int> Slices { get; private set; } = [];

    public LutColor[,,] Table { get; private set; } = new LutColor[0, 0, 0];

    // Simplified API that should improve pref' just a bit 
    public LutColor LookupTetrahedral(float r, float g, float b)
    {
        int cubeSizeMinusOne = this.Dimension - 1;
        return
            this.TetrahedralInterpolate(r * cubeSizeMinusOne, g * cubeSizeMinusOne, b * cubeSizeMinusOne);
    }

#if DEBUG
    public LutColor Lookup(LutColor color)
    {
        color.Validate();

        LutColor lutColor;
        int cubeSizeMinusOne = this.Dimension - 1;
        switch (this.Algorithm)
        {
            default:
            case LutAlgorithm.Swizzle:
                // Simple color swizzle for testing
                return new LutColor()
                {
                    R = color.B,
                    G = color.G,
                    B = color.R,
                };

            case LutAlgorithm.TriLinear:
                lutColor = this.TriLinearInterpolate(color.R * cubeSizeMinusOne, color.G * cubeSizeMinusOne, color.B * cubeSizeMinusOne);
                break;

            case LutAlgorithm.Tetrahedral:
                lutColor = this.TetrahedralInterpolate(color.R * cubeSizeMinusOne, color.G * cubeSizeMinusOne, color.B * cubeSizeMinusOne);
                break;
        }

        lutColor.Validate();
        return lutColor;
    }

    private LutColor TriLinearInterpolate(float redPoint, float greenPoint, float bluePoint)
    {
        // float input from 0 to cubeSize-1
        // Gets the interpolated color at an interpolated lattice point.

        int cubeSize = this.Dimension;
        int cubeSizeMinusOne = this.Dimension - 1;

        int lowerRedPoint = Clamp((int)Math.Floor(redPoint), 0, cubeSizeMinusOne);
        int upperRedPoint = Clamp(lowerRedPoint + 1, 0, cubeSizeMinusOne);
        int lowerGreenPoint = Clamp((int)Math.Floor(greenPoint), 0, cubeSizeMinusOne);
        int upperGreenPoint = Clamp(lowerGreenPoint + 1, 0, cubeSizeMinusOne);
        int lowerBluePoint = Clamp((int)Math.Floor(bluePoint), 0, cubeSizeMinusOne);
        int upperBluePoint = Clamp(lowerBluePoint + 1, 0, cubeSizeMinusOne);

        // Tri linear interpolation requires 8 vertices
        var C000 = this.Table[lowerRedPoint, lowerGreenPoint, lowerBluePoint];
        var C010 = this.Table[lowerRedPoint, lowerGreenPoint, upperBluePoint];
        var C100 = this.Table[upperRedPoint, lowerGreenPoint, lowerBluePoint];
        var C001 = this.Table[lowerRedPoint, upperGreenPoint, lowerBluePoint];
        var C110 = this.Table[upperRedPoint, lowerGreenPoint, upperBluePoint];
        var C111 = this.Table[upperRedPoint, upperGreenPoint, upperBluePoint];
        var C101 = this.Table[upperRedPoint, upperGreenPoint, lowerBluePoint];
        var C011 = this.Table[lowerRedPoint, upperGreenPoint, upperBluePoint];

        float alphaRed = 1.0f - (upperRedPoint - redPoint);
        var C00 = LutColor.Lerp(C000, C100, alphaRed);
        var C10 = LutColor.Lerp(C010, C110, alphaRed);
        var C01 = LutColor.Lerp(C001, C101, alphaRed);
        var C11 = LutColor.Lerp(C011, C111, alphaRed);
        float alphaBlue = 1.0f - (upperBluePoint - bluePoint);
        var C1 = LutColor.Lerp(C01, C11, alphaBlue);
        var C0 = LutColor.Lerp(C00, C10, alphaBlue);
        return LutColor.Lerp(C0, C1, 1.0f - (upperGreenPoint - greenPoint));
    }

#endif 

    // Tetrahedral interpolation. Based on code found in Truelight Software Library paper.
    // http://www.filmlight.ltd.uk/pdf/whitepapers/FL-TL-TN-0057-SoftwareLib.pdf
    private LutColor TetrahedralInterpolate(float redPoint, float greenPoint, float bluePoint)
    {
        // When interpolating on the tetrahedrons, only 4 lookups and four lerps are needed.
        int cubeSize = this.Dimension;
        int cubeSizeMinusOne = this.Dimension - 1;

        int lowerRedPoint = Clamp((int)Math.Floor(redPoint), 0, cubeSizeMinusOne);
        int upperRedPoint = Clamp(lowerRedPoint + 1, 0, cubeSizeMinusOne);
        int lowerGreenPoint = Clamp((int)Math.Floor(greenPoint), 0, cubeSizeMinusOne);
        int upperGreenPoint = Clamp(lowerGreenPoint + 1, 0, cubeSizeMinusOne);
        int lowerBluePoint = Clamp((int)Math.Floor(bluePoint), 0, cubeSizeMinusOne);
        int upperBluePoint = Clamp(lowerBluePoint + 1, 0, cubeSizeMinusOne);

        float deltaRed = redPoint - lowerRedPoint;
        float deltaBlue = bluePoint - lowerBluePoint;
        float deltaGreen = greenPoint - lowerGreenPoint;
        var c000 = this.Table[lowerRedPoint, lowerGreenPoint, lowerBluePoint];
        var c111 = this.Table[upperRedPoint, upperGreenPoint, upperBluePoint];

        var c = new LutColor();
        if (deltaRed > deltaGreen)
        {
            if (deltaGreen > deltaBlue)
            {
                var c100 = this.Table[upperRedPoint, lowerGreenPoint, lowerBluePoint];
                var c110 = this.Table[upperRedPoint, upperGreenPoint, lowerBluePoint];
                float alphaR = 1.0f - deltaRed;
                float deltaRG = deltaRed - deltaGreen;
                float deltaGB = deltaGreen - deltaBlue;
                c.R = alphaR * c000.R + deltaRG * c100.R + deltaGB * c110.R + deltaBlue * c111.R;
                c.G = alphaR * c000.G + deltaRG * c100.G + deltaGB * c110.G + deltaBlue * c111.G;
                c.B = alphaR * c000.B + deltaRG * c100.B + deltaGB * c110.B + deltaBlue * c111.B;
            }
            else if (deltaRed > deltaBlue)
            {
                var c100 = this.Table[upperRedPoint, lowerGreenPoint, lowerBluePoint];
                var c101 = this.Table[upperRedPoint, lowerGreenPoint, upperBluePoint];
                float alphaR = 1.0f - deltaRed;
                float deltaRB = deltaRed - deltaBlue;
                float deltaBG = deltaBlue - deltaGreen;
                c.R = alphaR * c000.R + deltaRB * c100.R + deltaBG * c101.R + deltaGreen * c111.R;
                c.G = alphaR * c000.G + deltaRB * c100.G + deltaBG * c101.G + deltaGreen * c111.G;
                c.B = alphaR * c000.B + deltaRB * c100.B + deltaBG * c101.B + deltaGreen * c111.B;
            }
            else
            {
                var c001 = this.Table[lowerRedPoint, lowerGreenPoint, upperBluePoint];
                var c101 = this.Table[upperRedPoint, lowerGreenPoint, upperBluePoint];
                float alphaB = 1.0f - deltaBlue;
                float deltaBR = deltaBlue - deltaRed;
                float deltaRG = deltaRed - deltaGreen;
                c.R = alphaB * c000.R + deltaBR * c001.R + deltaRG * c101.R + deltaGreen * c111.R;
                c.G = alphaB * c000.G + deltaBR * c001.G + deltaRG * c101.G + deltaGreen * c111.G;
                c.B = alphaB * c000.B + deltaBR * c001.B + deltaRG * c101.B + deltaGreen * c111.B;
            }
        }
        else
        {
            if (deltaBlue > deltaGreen)
            {
                var c001 = this.Table[lowerRedPoint, lowerGreenPoint, upperBluePoint];
                var c011 = this.Table[lowerRedPoint, upperGreenPoint, upperBluePoint];
                float alphaB = 1.0f - deltaBlue;
                float deltaBG = deltaBlue - deltaGreen;
                float deltaGR = deltaGreen - deltaRed;
                c.R = alphaB * c000.R + deltaBG * c001.R + deltaGR * c011.R + deltaRed * c111.R;
                c.G = alphaB * c000.G + deltaBG * c001.G + deltaGR * c011.G + deltaRed * c111.G;
                c.B = alphaB * c000.B + deltaBG * c001.B + deltaGR * c011.B + deltaRed * c111.B;
            }
            else if (deltaBlue > deltaRed)
            {
                float alphaG = 1.0f - deltaGreen;
                float deltaGB = deltaGreen - deltaBlue;
                float deltaBR = deltaBlue - deltaRed;
                var c010 = this.Table[lowerRedPoint, upperGreenPoint, lowerBluePoint];
                var c011 = this.Table[lowerRedPoint, upperGreenPoint, upperBluePoint];
                c.R = alphaG * c000.R + deltaGB * c010.R + deltaBR * c011.R + deltaRed * c111.R;
                c.G = alphaG * c000.G + deltaGB * c010.G + deltaBR * c011.G + deltaRed * c111.G;
                c.B = alphaG * c000.B + deltaGB * c010.B + deltaBR * c011.B + deltaRed * c111.B;
            }
            else
            {
                var c010 = this.Table[lowerRedPoint, upperGreenPoint, lowerBluePoint];
                var c110 = this.Table[upperRedPoint, upperGreenPoint, lowerBluePoint];
                float alphaG = 1.0f - deltaGreen;
                float deltaGR = deltaGreen - deltaRed;
                float deltaRB = deltaRed - deltaBlue;
                c.R = alphaG * c000.R + deltaGR * c010.R + deltaRB * c110.R + deltaBlue * c111.R;
                c.G = alphaG * c000.G + deltaGR * c010.G + deltaRB * c110.G + deltaBlue * c111.G;
                c.B = alphaG * c000.B + deltaGR * c010.B + deltaRB * c110.B + deltaBlue * c111.B;
            }
        }

        return c;
    }

    private static int Clamp(int value, int min, int max)
    {
        if (min > max)
        {
            throw new ArgumentException("Invalid Clamp Values");
        }

        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }

    private static int GetLikelyLutBitDepth(int testValue)
    {
        const int MinBitDepth = 8;
        const int MaxBitDepth = 16;

        if (testValue < 0)
        {
            return -1;
        }

        // Only test even bit depths
        for (int bitDepth = MinBitDepth; bitDepth <= MaxBitDepth; bitDepth += 2)
        {
            int maxcode = (int)Math.Pow(2.0, bitDepth);
            int adjustedMax = maxcode * 2 - 1;
            if (testValue <= adjustedMax)
            {
                return bitDepth;
            }
        }

        return MaxBitDepth;
    }

    private static int DimensionFromPixelCount(int pixelCount)
    {
        int dimension = (int)(Math.Round(Math.Pow((double)pixelCount, 1.0 / 3.0)));
        if (dimension * dimension * dimension != pixelCount)
        {
            throw new Exception("Cannot infer 3D Lut size. ");
        }

        return dimension;
    }

    [Conditional("DEBUG")]
    public void Validate()
    {
        int size = this.Table.GetLength(0);
        for (int r = 0; r < size; ++r)
        {
            for (int g = 0; g < size; ++g)
            {
                for (int b = 0; b < size; ++b)
                {
                    var color = this.Table[r, g, b];
                    if (color == null)
                    {
                        throw new ArgumentException("No color loaded");
                    }
                    else
                    {
                        color.Validate();
                    }
                }
            }
        }
    }
}