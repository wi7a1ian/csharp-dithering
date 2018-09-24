using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Laboratory.Algorithms.Dithering
{
    /// <summary>
    /// Bayer Ordered Dithering Algorithm ( based on threshold )
    /// http://en.wikipedia.org/wiki/Ordered_dithering
    /// </summary>
    public class OrderedMonoDithering : MonoDithering
    {
        private int[][] _thresholdMap;
        private int _mapSize;

        public OrderedMonoDithering(float[][] thresholdMap)
        {
            _thresholdMap = NormalizeThresholdMatrix(thresholdMap);
            _mapSize = _thresholdMap.Length;
        }

        public OrderedMonoDithering(int[][] thresholdMap, int mapBase)
        {
            _thresholdMap = NormalizeThresholdMatrix(thresholdMap, mapBase);
            _mapSize = _thresholdMap.Length;
        }

        /// <summary>
        /// foreach y
        ///     foreach x
        ///         oldpixel := pixel[x][y] + threshold_map_4x4[x mod 4][y mod 4]
        ///         newpixel := find_closest_palette_color(oldpixel)
        ///         pixel[x][y] := newpixel
        /// </summary>
        protected override void CalculateDithering()
        {
            Parallel.For(0, Height, y => // (thread safe operation)
            //for (int y = 0; y < Height; y++)
            {
                // Note: no need for parallelization, 
                // let CPU optimize by loading vector of adjecent bytes
                for (int x = 0; x < Width; x++)
                {
                    int avgValue = GetPixel8bpp(x, y);

                    int threshold = _thresholdMap[y % _mapSize][x % _mapSize];

                    int grayPx = avgValue < threshold ? 0 : 255;

                    SetPixel8bpp(x, y, grayPx);
                }
            });
        }

        private int[][] NormalizeThresholdMatrix(float[][] thresholdMatrix)
        {
            ValidateThresholdMatrix(thresholdMatrix);

            int size = thresholdMatrix.Length;
            int[][] outMap = new int[size][];

            for (int y = 0; y < thresholdMatrix.Length; y++)
            {
                outMap[y] = new int[size];
                for (int x = 0; x < thresholdMatrix[y].Length; x++)
                {
                    outMap[y][x] = (int)(thresholdMatrix[y][x] * 255);
                }
            }

            return outMap;
        }

        private int[][] NormalizeThresholdMatrix(int[][] thresholdMatrix, int baseValue)
        {
            ValidateThresholdMatrix(thresholdMatrix);

            int size = thresholdMatrix.Length;
            int[][] outMap = new int[size][];

            for (int y = 0; y < thresholdMatrix.Length; y++)
            {
                outMap[y] = new int[size];
                for (int x = 0; x < thresholdMatrix[y].Length; x++)
                {
                    outMap[y][x] =  thresholdMatrix[y][x] * 255 / baseValue;
                }
            }

            return outMap;
        }


        private static void ValidateThresholdMatrix(Array thresholdMatrix) // or (dynamic thresholdMatrix)
        {
            if (thresholdMatrix == null)
                throw new ArgumentNullException("thresholdMatrix");

            if (thresholdMatrix.Length == 0)
                throw new ArgumentException("thresholdMatrix is empty");

            if (thresholdMatrix.Length != (thresholdMatrix.GetValue(0) as Array).Length)
                throw new ArgumentException("thresholdMatrix needs to be a square matrix");
        }
    }

    public class Bayer2x2MonoDithering : OrderedMonoDithering
    {
        private static readonly int[][] _thresholdMap = new int[][]{
            new int[]{1, 3},
            new int[]{4, 2}
        };
        private static readonly int _thresholdMapBase = 5;

        public Bayer2x2MonoDithering()
            : base(_thresholdMap, _thresholdMapBase)
        {

        }
    }

    public class Bayer3x3MonoDithering : OrderedMonoDithering
    {
        private static readonly int[][] _thresholdMap = new int[][]{
                new int[]{3, 7, 4},
                new int[]{6, 1, 9},
                new int[]{2, 8, 5}
        };
        private static readonly int _thresholdMapBase = 10;

        public Bayer3x3MonoDithering()
            : base(_thresholdMap, _thresholdMapBase)
        {

        }
    }

    public class Bayer4x4MonoDithering : OrderedMonoDithering
    {
        private static readonly int[][] _thresholdMap = new int[][]{
                new int[]{1, 9, 3, 11},
                new int[]{13, 5, 15, 7},
                new int[]{4, 12, 2, 10},
                new int[]{16, 8, 14, 6}
        };
        private static readonly int _thresholdMapBase = 17;

        public Bayer4x4MonoDithering()
            : base(_thresholdMap, _thresholdMapBase)
        {

        }
    }

    public class Bayer8x8MonoDithering : OrderedMonoDithering
    {
        private static readonly int[][] _thresholdMap = new int[][]{
                new int[]{1, 49, 13, 61, 4, 52, 16, 64},
                new int[]{33, 17, 45, 29, 36, 20, 48, 32},
                new int[]{9, 57, 5, 53, 12, 60, 8, 56},
                new int[]{41, 25, 37, 21, 44, 28, 40, 24},
                new int[]{3, 51, 15, 63, 2, 50, 14, 62},
                new int[]{35, 19, 47, 31, 34, 18, 46, 30},
                new int[]{11, 59, 7, 55, 10, 58, 6, 54},
                new int[]{43, 27, 39, 23, 42, 26, 38, 22}
        };
        private static readonly int _thresholdMapBase = 64;

        public Bayer8x8MonoDithering()
            : base(_thresholdMap, _thresholdMapBase)
        {

        }
    }
}
