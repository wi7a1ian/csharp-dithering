using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Drawing;
using Laboratory.Algorithms.Dithering;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Dithering.Test
{
    [TestClass]
    [DeploymentItem(@"TestData\", @"TestData\")]    // Copy from build output to the test deployment path. 
                                                    // Will also cause the Env.CurrentPath to point to TestResults sequential location.
    public class MonoDitheringTest
    {
        Bitmap testColorBitmap = null;

        [TestInitialize]
        public void TestInitialize()
        {
            testColorBitmap = new Bitmap(@"TestData\ColorLogo_1.tif");
        }

        [TestCleanup]
        public void TestCleanup()
        {
            testColorBitmap.Dispose();
            testColorBitmap = null;
        }

        [TestMethod]
        public void MonoDithering_FloydSteinberg_DitheringIsWorking()
        {
            // given
            string tiffFile = string.Format("{0}.tif", GetCurrentMethod());
            MonoDithering converter = new FloydSteinbergMonoDithering();

            // when
            using (var bmpDithered = converter.Dither(testColorBitmap))
            {
                // then
                Assert.AreEqual(bmpDithered.PixelFormat, PixelFormat.Format8bppIndexed);
            }
        }

        [TestMethod]
        [TestCategory("I/O")]
        public void MonoDithering_FloydSteinberg_CanSaveAsMonohromaticTiff()
        {
            // given
            string tiffFile = string.Format("{0}.tif", GetCurrentMethod());
            MonoDithering converter = new FloydSteinbergMonoDithering();

            // when
            using (var bmpDithered = converter.Dither(testColorBitmap))
                converter.SaveAsMonoTiff(bmpDithered, tiffFile);

            // then
            using (var bmpDithered = new Bitmap(tiffFile))
                Assert.IsTrue(bmpDithered.PixelFormat == PixelFormat.Format1bppIndexed);
        }

        [TestMethod]
        public void MonoDithering_Bayer4x4_DitheringIsWorking()
        {
            // given
            string tiffFile = string.Format("{0}.tif", GetCurrentMethod());
            MonoDithering converter = new Bayer4x4MonoDithering();

            // when
            using (var bmpDithered = converter.Dither(testColorBitmap))
            {
                // then
                Assert.AreEqual(bmpDithered.PixelFormat, PixelFormat.Format8bppIndexed);
            }
        }

        [TestMethod]
        [TestCategory("I/O")]
        public void MonoDithering_Bayer4x4_CanSaveAsMonohromaticTiff()
        {
            // given
            string tiffFile = string.Format("{0}.tif", GetCurrentMethod());
            MonoDithering converter = new Bayer4x4MonoDithering();

            // when
            using (var bmpDithered = converter.Dither(testColorBitmap))
                converter.SaveAsMonoTiff(bmpDithered, tiffFile);

            // then
            using (var bmpDithered = new Bitmap(tiffFile))
                Assert.IsTrue(bmpDithered.PixelFormat == PixelFormat.Format1bppIndexed);
        }

        [TestMethod]
        [Description("I know it is machine/load dependent but needed that to meet certain acceptance criterias.")]
        [TestCategory("Performance")]
        [Timeout(800)]
        public void MonoDithering_FloydSteinberg_RunsForLessThan800ms()
        {
            // given
            MonoDithering converter = new FloydSteinbergMonoDithering();

            // when
            using (var bmpDithered = converter.Dither(testColorBitmap))
            {
                // Nop
            }

            // then
            // Timeout?
        }

        [TestMethod]
        [Description("I know it is machine/load dependent but needed that to meet certain acceptance criterias.")]
        [TestCategory("Performance")]
        [Timeout(500)]
        public void MonoDithering_Bayer8x8_RunsForLessThan500ms()
        {
            // given
            MonoDithering converter = new Bayer8x8MonoDithering();

            // when
            using (var bmpDithered = converter.Dither(testColorBitmap))
            {
                // Nop
            }

            // then
            // Timeout?
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void MonoDithering_NullRefBitmapIsNotAccepted()
        {
            // given
            MonoDithering converter = new Bayer8x8MonoDithering();

            // when
            var bmpDithered = converter.Dither(null);

            // then
            Assert.Fail("Exception was not thrown.");
        }

        [TestMethod]
        public void MonoDithering_Input24bppRgbBitmapIsWorking()
        {
            // given
            MonoDithering converter = new Bayer4x4MonoDithering();
            string tiffFile = string.Format("{0}.tif", GetCurrentMethod());

            using (var inputBitmap = new Bitmap(testColorBitmap.Width, testColorBitmap.Height, PixelFormat.Format24bppRgb))
            {
                using (var g = Graphics.FromImage(inputBitmap))
                {
                    g.DrawImage(testColorBitmap, new Rectangle(0, 0, testColorBitmap.Width, testColorBitmap.Height));
                }

                // when
                using (var bmpDithered = converter.Dither(inputBitmap))
                {
                    converter.SaveAsMonoTiff(bmpDithered, tiffFile);

                    // then
                    Assert.AreEqual(bmpDithered.PixelFormat, PixelFormat.Format8bppIndexed);
                }
            }
        }

        [TestMethod]
        public void MonoDithering_Input32bppArgbRgbBitmapIsWorking()
        {
            // given
            MonoDithering converter = new Bayer4x4MonoDithering();
            string tiffFile = string.Format("{0}.tif", GetCurrentMethod());

            using (var inputBitmap = new Bitmap(testColorBitmap.Width, testColorBitmap.Height, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(inputBitmap))
                {
                    g.DrawImage(testColorBitmap, new Rectangle(0, 0, testColorBitmap.Width, testColorBitmap.Height));
                }

                // when
                using (var bmpDithered = converter.Dither(inputBitmap))
                {
                    converter.SaveAsMonoTiff(bmpDithered, tiffFile);

                    // then
                    Assert.AreEqual(bmpDithered.PixelFormat, PixelFormat.Format8bppIndexed);
                }
            }
        }

        [TestMethod]
        [Description("I do not think this brings any value since noone is using 32bppRgb bitmaps...")]
        //[Ignore]
        public void MonoDithering_Input32bppRgbBitmapIsWorking()
        {
            // given
            MonoDithering converter = new Bayer4x4MonoDithering();
            string tiffFile = string.Format("{0}.tif", GetCurrentMethod());

            using (var inputBitmap = new Bitmap(testColorBitmap.Width, testColorBitmap.Height, PixelFormat.Format32bppRgb))
            {
                using (var g = Graphics.FromImage(inputBitmap))
                {
                    g.DrawImage(testColorBitmap, new Rectangle(0, 0, testColorBitmap.Width, testColorBitmap.Height));
                }

                // when
                using (var bmpDithered = converter.Dither(inputBitmap))
                {
                    converter.SaveAsMonoTiff(bmpDithered, tiffFile);

                    // then
                    Assert.AreEqual(bmpDithered.PixelFormat, PixelFormat.Format8bppIndexed);
                }
            }
        }

        #region helpers

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetCurrentMethod()
        {
            var st = new StackTrace();
            var sf = st.GetFrame(1);
            return sf.GetMethod().Name;
        }

        #endregion helpers
    }
}
