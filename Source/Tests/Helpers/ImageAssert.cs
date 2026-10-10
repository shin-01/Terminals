using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Helpers
{
    public class ImageAssert
    {
        internal const string IMAGE_FILE = @"ControlPanel.png";

        internal static void EqualsToExpectedIcon(string deploymentDirectory, Image favoriteIcon)
        {
            bool areEqual = AreEqual(deploymentDirectory, favoriteIcon);
            Assert.IsTrue(areEqual, "The icon wasnt assigned properly.");
        }

        internal static void DoesntEqualsExpectedIcon(string deploymentDirectory, Image favoriteIcon)
        {
            bool areEqual = AreEqual(deploymentDirectory, favoriteIcon);
            Assert.IsFalse(areEqual, "UpdateIcon cant save favorite.");
        }

        private static bool AreEqual(string deploymentDirectory, Image favoriteIcon)
        {
            string fullIconPath = Path.Combine(deploymentDirectory, IMAGE_FILE);
            Image expectedImage = Image.FromFile(fullIconPath);
            return AreEqual(expectedImage, favoriteIcon);
        }

        private static bool AreEqual(Image firstImage, Image secondImage)
        {
            if (firstImage == null && secondImage == null)
                return true;
            if (firstImage == null || secondImage == null)
                return false;

            if (firstImage.Width != secondImage.Width || firstImage.Height != secondImage.Height)
                return false;

            // comparing re-encoded png bytes is not deterministic across GDI+ sessions
            // (the database round-trip re-encodes the image through RawFormat); compare
            // the actual pixel data instead
            using (Bitmap firstBitmap = new Bitmap(firstImage))
            using (Bitmap secondBitmap = new Bitmap(secondImage))
            {
                BitmapData firstData = firstBitmap.LockBits(new Rectangle(0, 0, firstBitmap.Width, firstBitmap.Height),
                    ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                BitmapData secondData = secondBitmap.LockBits(new Rectangle(0, 0, secondBitmap.Width, secondBitmap.Height),
                    ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                try
                {
                    int byteCount = firstData.Stride * firstData.Height;
                    if (byteCount != secondData.Stride * secondData.Height)
                        return false;

                    byte[] firstBytes = new byte[byteCount];
                    byte[] secondBytes = new byte[byteCount];
                    System.Runtime.InteropServices.Marshal.Copy(firstData.Scan0, firstBytes, 0, byteCount);
                    System.Runtime.InteropServices.Marshal.Copy(secondData.Scan0, secondBytes, 0, byteCount);

                    return firstBytes.SequenceEqual(secondBytes);
                }
                finally
                {
                    firstBitmap.UnlockBits(firstData);
                    secondBitmap.UnlockBits(secondData);
                }
            }
        }
    }
}
