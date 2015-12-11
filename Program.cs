using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace TimMarsh.Screenshot
{
    public class Screenshot
    {   
        public static void TakeScreenshot()
        {
            // Take Screenshot code goes here
        }

        public static void CompareScreenshot()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Screenshot comparison tool");
            sb.AppendLine("Tim Marsh - UEL Web Team");
            sb.AppendLine("------------------------------");

            var sourceDirectory = new DirectoryInfo(@"\\DL-SCT-TST\Screenshots");
            var outputDirectory = new DirectoryInfo(@"\\DL-SCT-TST\Screenshots\Output\" + DateTime.Now.ToString(@"ddMMyyHHmmss")).FullName;

            //BrokenLinkCheck(sb);
            // Compare top level
            CompareFiles(sourceDirectory, outputDirectory, sb);
            // Compare childred
            CompareDirectories(sourceDirectory, outputDirectory, sb);

            File.WriteAllText(outputDirectory + "\\Comparison Results.txt", sb.ToString());
        }

        private static void CompareDirectories(DirectoryInfo sourceDirectory, string outputDirectory, StringBuilder sb)
        {
            var directories = sourceDirectory.GetDirectories();
            foreach (var directory in directories)
            {
                CompareFiles(directory, outputDirectory, sb);
                CompareDirectories(directory, outputDirectory, sb);
            }
        }

        private static void CompareFiles(DirectoryInfo directory, string outputDirectory, StringBuilder sb)
        {
            var files = GetFiles(directory);

            if (files.Count != 2)
                return;

            CompareBitmaps(files, sb, outputDirectory);
        }

        private static List<FileInfo> GetFiles(DirectoryInfo directory)
        {
            return directory.GetFiles()
                .OrderByDescending(f => f.LastWriteTime)
                .Where(x => !x.Name.EndsWith(".db"))
                .Take(2).ToList();
        }

        private static float CompareBitmaps(List<FileInfo> files, StringBuilder sb, string outputDirectory)
        {
            if (!files.FirstOrDefault().DirectoryName.StartsWith(@"\\DL-SCT-TST\Screenshots\Output"))
            {
                sb.AppendLine("Comparing files at: " + files.First().DirectoryName);

                string ssPath = @"\\DL-SCT-TST\Screenshots";
                outputDirectory = outputDirectory + files.First().DirectoryName.Replace(ssPath, "");

                Bitmap newFile = new Bitmap(files.First().FullName);
                Bitmap oldFile = new Bitmap(files.Last().FullName);

                LockBitmap lockBitmap1 = new LockBitmap(oldFile);
                LockBitmap lockBitmap2 = new LockBitmap(newFile);

                lockBitmap1.LockBits();
                lockBitmap2.LockBits();

                int DiferentPixels = 0;
                Bitmap container = new Bitmap(lockBitmap1.Width, lockBitmap1.Height);
                for (int i = 0; i < lockBitmap1.Width; ++i)
                {
                    for (int j = 0; j < lockBitmap1.Height; ++j)
                    {
                        Color secondColor = lockBitmap2.GetPixel(i, j);
                        Color firstColor = lockBitmap1.GetPixel(i, j);

                        if (firstColor != secondColor)
                        {
                            DiferentPixels++;
                            container.SetPixel(i, j, SetTransparency(127, Color.Red));
                        }
                    }
                }

                lockBitmap1.UnlockBits();
                lockBitmap2.UnlockBits();

                int TotalPixels = oldFile.Width * oldFile.Height;
                float difference = (float)((float)DiferentPixels / (float)TotalPixels);

                Bitmap containerCombine = new Bitmap(oldFile.Width, oldFile.Height);

                RectangleF rectf = new RectangleF(20, 40, 550, 250);

                using (Graphics graphics = Graphics.FromImage(containerCombine))
                {
                    graphics.DrawImage(oldFile, new Point(0, 0));
                    graphics.DrawImage(container, new Point(0, 0));

                    graphics.DrawString((difference * 100).ToString("0.00") + "% difference", new Font("Tahoma", 40),
                        Brushes.Black, rectf);
                }

                string currTime = DateTime.Now.ToString(@"MMM-ddd-d-HH.mm.ss");

                bool exists = Directory.Exists(outputDirectory);

                if (!exists)
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                string fileName = Path.Combine(outputDirectory, currTime + ".png");

                containerCombine.Save(fileName, ImageFormat.Jpeg);
                containerCombine.Dispose();

                sb.AppendLine((difference * 100).ToString("0.00") + "% difference between files");

                return difference * 100;
            }
            return 0;
        }

        private static Image ResizeImage(int newSize, Image originalImage)
        {
            if (originalImage.Width <= newSize)
                newSize = originalImage.Width;

            var newHeight = originalImage.Height * newSize / originalImage.Width;

            if (newHeight > newSize)
            {
                // Resize with height instead
                newSize = originalImage.Width * newSize / originalImage.Height;
                newHeight = newSize;
            }

            return originalImage.GetThumbnailImage(newSize, newHeight, null, IntPtr.Zero);
        }

        private static Color SetTransparency(int A, Color color)
        {
            return Color.FromArgb(A, color.R, color.G, color.B);
        }

        public class LockBitmap
        {
            Bitmap source = null;
            IntPtr Iptr = IntPtr.Zero;
            BitmapData bitmapData = null;

            public byte[] Pixels { get; set; }
            public int Depth { get; private set; }
            public int Width { get; private set; }
            public int Height { get; private set; }

            public LockBitmap(Bitmap source)
            {
                this.source = source;
            }

            /// <summary>
            /// Lock bitmap data
            /// </summary>
            public void LockBits()
            {
                try
                {
                    // Get width and height of bitmap
                    Width = source.Width;
                    Height = source.Height;

                    // get total locked pixels count
                    int PixelCount = Width * Height;

                    // Create rectangle to lock
                    Rectangle rect = new Rectangle(0, 0, Width, Height);

                    // get source bitmap pixel format size
                    Depth = System.Drawing.Bitmap.GetPixelFormatSize(source.PixelFormat);

                    // Check if bpp (Bits Per Pixel) is 8, 24, or 32
                    if (Depth != 8 && Depth != 24 && Depth != 32)
                    {
                        throw new ArgumentException("Only 8, 24 and 32 bpp images are supported.");
                    }

                    // Lock bitmap and return bitmap data
                    bitmapData = source.LockBits(rect, ImageLockMode.ReadWrite,
                        source.PixelFormat);

                    // create byte array to copy pixel values
                    int step = Depth / 8;
                    Pixels = new byte[PixelCount * step];
                    Iptr = bitmapData.Scan0;

                    // Copy data from pointer to array
                    Marshal.Copy(Iptr, Pixels, 0, Pixels.Length);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            /// <summary>
            /// Unlock bitmap data
            /// </summary>
            public void UnlockBits()
            {
                try
                {
                    // Copy data from byte array to pointer
                    Marshal.Copy(Pixels, 0, Iptr, Pixels.Length);

                    // Unlock bitmap data
                    source.UnlockBits(bitmapData);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            /// <summary>
            /// Get the color of the specified pixel
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            public Color GetPixel(int x, int y)
            {
                Color clr = Color.Empty;

                // Get color components count
                int cCount = Depth / 8;

                // Get start index of the specified pixel
                int i = ((y * Width) + x) * cCount;

                if (i >= Pixels.Length)
                    return clr;

                if (i > Pixels.Length - cCount)
                    throw new IndexOutOfRangeException();

                if (Depth == 32) // For 32 bpp get Red, Green, Blue and Alpha
                {
                    byte b = Pixels[i];
                    byte g = Pixels[i + 1];
                    byte r = Pixels[i + 2];
                    byte a = Pixels[i + 3]; // a
                    clr = Color.FromArgb(a, r, g, b);
                }
                if (Depth == 24) // For 24 bpp get Red, Green and Blue
                {
                    byte b = Pixels[i];
                    byte g = Pixels[i + 1];
                    byte r = Pixels[i + 2];
                    clr = Color.FromArgb(r, g, b);
                }
                if (Depth == 8)
                    // For 8 bpp get color value (Red, Green and Blue values are the same)
                {
                    byte c = Pixels[i];
                    clr = Color.FromArgb(c, c, c);
                }
                return clr;
            }

            /// <summary>
            /// Set the color of the specified pixel
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <param name="color"></param>
            public void SetPixel(int x, int y, Color color)
            {
                // Get color components count
                int cCount = Depth / 8;

                // Get start index of the specified pixel
                int i = ((y * Width) + x) * cCount;

                if (Depth == 32) // For 32 bpp set Red, Green, Blue and Alpha
                {
                    Pixels[i] = color.B;
                    Pixels[i + 1] = color.G;
                    Pixels[i + 2] = color.R;
                    Pixels[i + 3] = color.A;
                }
                if (Depth == 24) // For 24 bpp set Red, Green and Blue
                {
                    Pixels[i] = color.B;
                    Pixels[i + 1] = color.G;
                    Pixels[i + 2] = color.R;
                }
                if (Depth == 8)
                    // For 8 bpp set color value (Red, Green and Blue values are the same)
                {
                    Pixels[i] = color.B;
                }
            }
        }
    }

    public class Program
    {
        

        static void Main(string[] args)
        {
            Screenshot.CompareScreenshot();
        }

        private static void BrokenLinkCheck(StringBuilder sb)
        {
            var result = new WebClient().DownloadString("http://main-vnext.uel.ac.uk/pagelist").Split(new string[] { "\r\n" }, StringSplitOptions.None).ToList();
            result.RemoveAll(x => x.Equals("\r\n"));

            foreach (var link in result)
            {
                HttpWebResponse response = null;
                try
                {
                    //Creating the HttpWebRequest
                    HttpWebRequest request =
                        WebRequest.Create("http://main-vnext.uel.ac.uk/pagelist" + link.Replace("\r\n", "")) as HttpWebRequest;
                    //Setting the Request method HEAD, you can also use GET too.
                    request.Method = "HEAD";
                    request.Timeout = 5000;
                    //Getting the Web Response.
                    response = request.GetResponse() as HttpWebResponse;
                    //Returns TRUE if the Status code == 200
                    sb.AppendLine(link.Replace("\r\n", "") + " OK " + response.StatusCode);
                    Console.WriteLine(link.Replace("\r\n", "") + " OK " + response.StatusCode);
                }
                catch (Exception ex)
                {
                    //Any exception will returns false.
                    sb.AppendLine("----------------------------");
                    sb.AppendLine(link.Replace("\r\n", "") + " FAIL ");
                    Console.WriteLine(link.Replace("\r\n", "") + " FAIL " + ex.Message);
                    sb.AppendLine("----------------------------");

                }
                finally
                {
                    if (response != null)
                        response.Close();
                }
            }
        }
    }
}
