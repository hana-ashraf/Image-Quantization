using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Linq;

///Algorithms Project
///Intelligent Scissors
///

namespace ImageQuantization
{
    /// <summary>
    /// Holds the pixel color in 3 byte values: red, green and blue
    /// </summary>
    public struct RGBPixel
    {
        public byte red, green, blue;
        public RGBPixel(double R, double G, double B)
        {
            red = (byte)R;
            green = (byte)G;
            blue = (byte)B;
        }
    }
    public struct RGBPixelD
    {
        public double red, green, blue;
    }


    /// <summary>
        /// Library of static functions that deal with images
        /// </summary>
    public class ImageOperations
    {
        /// <summary>
        /// Open an image and load it into 2D array of colors (size: Height x Width)
        /// </summary>
        /// <param name="ImagePath">Image file path</param>
        /// <returns>2D array of colors</returns>
      //  public static int V = 0;
        public static RGBPixel[,] OpenImage(string ImagePath)
        {
            Bitmap original_bm = new Bitmap(ImagePath);
            int Height = original_bm.Height;
            int Width = original_bm.Width;

            RGBPixel[,] Buffer = new RGBPixel[Height, Width];

            unsafe
            {
                BitmapData bmd = original_bm.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, original_bm.PixelFormat);
                int x, y;
                int nWidth = 0;
                bool Format32 = false;
                bool Format24 = false;
                bool Format8 = false;

                if (original_bm.PixelFormat == PixelFormat.Format24bppRgb)
                {
                    Format24 = true;
                    nWidth = Width * 3;
                }
                else if (original_bm.PixelFormat == PixelFormat.Format32bppArgb || original_bm.PixelFormat == PixelFormat.Format32bppRgb || original_bm.PixelFormat == PixelFormat.Format32bppPArgb)
                {
                    Format32 = true;
                    nWidth = Width * 4;
                }
                else if (original_bm.PixelFormat == PixelFormat.Format8bppIndexed)
                {
                    Format8 = true;
                    nWidth = Width;
                }
                int nOffset = bmd.Stride - nWidth;
                byte* p = (byte*)bmd.Scan0;
                for (y = 0; y < Height; y++)
                {
                    for (x = 0; x < Width; x++)
                    {
                        if (Format8)
                        {
                            Buffer[y, x].red = Buffer[y, x].green = Buffer[y, x].blue = p[0];
                            p++;
                        }
                        else
                        {
                            Buffer[y, x].red = p[2];
                            Buffer[y, x].green = p[1];
                            Buffer[y, x].blue = p[0];
                            if (Format24) p += 3;
                            else if (Format32) p += 4;
                        }
                    }
                    p += nOffset;
                }
                original_bm.UnlockBits(bmd);
            }

            return Buffer;
        }

        /// <summary>
                /// Get the height of the image 
                /// </summary>
                /// <param name="ImageMatrix">2D array that contains the image</param>
                /// <returns>Image Height</returns>
        public static int GetHeight(RGBPixel[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(0);
        }

        /// <summary>
                /// Get the width of the image 
                /// </summary>
                /// <param name="ImageMatrix">2D array that contains the image</param>
                /// <returns>Image Width</returns>
        public static int GetWidth(RGBPixel[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(1);
        }

        /// <summary>
                /// Display the given image on the given PictureBox object
                /// </summary>
                /// <param name="ImageMatrix">2D array that contains the image</param>
                /// <param name="PicBox">PictureBox object to display the image on it</param>
        public static void DisplayImage(RGBPixel[,] ImageMatrix, PictureBox PicBox)
        {
            // Create Image:
            //==============
            int Height = ImageMatrix.GetLength(0);
            int Width = ImageMatrix.GetLength(1);

            Bitmap ImageBMP = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);

            unsafe
            {
                BitmapData bmd = ImageBMP.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, ImageBMP.PixelFormat);
                int nWidth = 0;
                nWidth = Width * 3;
                int nOffset = bmd.Stride - nWidth;
                byte* p = (byte*)bmd.Scan0;
                for (int i = 0; i < Height; i++)
                {
                    for (int j = 0; j < Width; j++)
                    {

                        p[2] = ImageMatrix[i, j].red;
                        p[1] = ImageMatrix[i, j].green;
                        p[0] = ImageMatrix[i, j].blue;
                        p += 3;
                    }

                    p += nOffset;
                }
                ImageBMP.UnlockBits(bmd);
            }
            PicBox.Image = ImageBMP;


        }

        /// <summary>
               /// Apply Gaussian smoothing filter to enhance the edge detection 
               /// </summary>
               /// <param name="ImageMatrix">Colored image matrix</param>
               /// <param name="filterSize">Gaussian mask size</param>
               /// <param name="sigma">Gaussian sigma</param>
               /// <returns>smoothed color image</returns>
        public static RGBPixel[,] GaussianFilter1D(RGBPixel[,] ImageMatrix, int filterSize, double sigma)
        {
            int Height = GetHeight(ImageMatrix);
            int Width = GetWidth(ImageMatrix);

            RGBPixelD[,] VerFiltered = new RGBPixelD[Height, Width];
            RGBPixel[,] Filtered = new RGBPixel[Height, Width];


            // Create Filter in Spatial Domain:
            //=================================
            //make the filter ODD size
            if (filterSize % 2 == 0) filterSize++;

            double[] Filter = new double[filterSize];

            //Compute Filter in Spatial Domain :
            //==================================
            double Sum1 = 0;
            int HalfSize = filterSize / 2;
            for (int y = -HalfSize; y <= HalfSize; y++)
            {
                //Filter[y+HalfSize] = (1.0 / (Math.Sqrt(2 * 22.0/7.0) * Segma)) * Math.Exp(-(double)(y*y) / (double)(2 * Segma * Segma)) ;
                Filter[y + HalfSize] = Math.Exp(-(double)(y * y) / (double)(2 * sigma * sigma));
                Sum1 += Filter[y + HalfSize];
            }
            for (int y = -HalfSize; y <= HalfSize; y++)
            {
                Filter[y + HalfSize] /= Sum1;
            }

            //Filter Original Image Vertically:
            //=================================
            int ii, jj;
            RGBPixelD Sum;
            RGBPixel Item1;
            RGBPixelD Item2;

            for (int j = 0; j < Width; j++)
                for (int i = 0; i < Height; i++)
                {
                    Sum.red = 0;
                    Sum.green = 0;
                    Sum.blue = 0;
                    for (int y = -HalfSize; y <= HalfSize; y++)
                    {
                        ii = i + y;
                        if (ii >= 0 && ii < Height)
                        {
                            Item1 = ImageMatrix[ii, j];
                            Sum.red += Filter[y + HalfSize] * Item1.red;
                            Sum.green += Filter[y + HalfSize] * Item1.green;
                            Sum.blue += Filter[y + HalfSize] * Item1.blue;
                        }
                    }
                    VerFiltered[i, j] = Sum;
                }

            //Filter Resulting Image Horizontally:
            //===================================
            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                {
                    Sum.red = 0;
                    Sum.green = 0;
                    Sum.blue = 0;
                    for (int x = -HalfSize; x <= HalfSize; x++)
                    {
                        jj = j + x;
                        if (jj >= 0 && jj < Width)
                        {
                            Item2 = VerFiltered[i, jj];
                            Sum.red += Filter[x + HalfSize] * Item2.red;
                            Sum.green += Filter[x + HalfSize] * Item2.green;
                            Sum.blue += Filter[x + HalfSize] * Item2.blue;
                        }
                    }
                    Filtered[i, j].red = (byte)Sum.red;
                    Filtered[i, j].green = (byte)Sum.green;
                    Filtered[i, j].blue = (byte)Sum.blue;
                }

            return Filtered;
        }
        public static List<RGBPixel> Distinct(RGBPixel[,] ImageMatrix)
        {
            //Total complexity of Distinct function = O(N)*O(N)=O(N^2)
            long result = 0; //O(1)
            bool[] validate = new bool[16777216];//O(1)
            List<RGBPixel> Distinctcolors = new List<RGBPixel>();//O(1)

            for (int i = 0; i < ImageOperations.GetHeight(ImageMatrix); i++) //O(N)
            {
                for (int j = 0; j < ImageOperations.GetWidth(ImageMatrix); j++)//O(N)
                {
                    result = (ImageMatrix[i, j].red << 16) + (ImageMatrix[i, j].green << 8) + (ImageMatrix[i, j].blue << 0);//O(1)
                    if (validate[result] == true)//O(1)
                    {
                        continue;//O(1)
                    }
                    else
                    {
                        validate[result] = true;//O(1)
                        Distinctcolors.Add(ImageMatrix[i, j]);//O(1)
                    }
                }
            }
            MessageBox.Show("Distinct Colors: " + Distinctcolors.Count.ToString());//O(1)
            return Distinctcolors;
        }


        public static double distance(List<RGBPixel> Distinctcolors, int first, int second)
        {
            //Total complexity of distance function = O(1)
            double dis = 0;//O(1)
            double r, g, b;//O(1)
            r = (Distinctcolors[first].red - Distinctcolors[second].red) * (Distinctcolors[first].red - Distinctcolors[second].red);//O(1)
            g = (Distinctcolors[first].green - Distinctcolors[second].green) * (Distinctcolors[first].green - Distinctcolors[second].green);//O(1)
            b = (Distinctcolors[first].blue - Distinctcolors[second].blue) * (Distinctcolors[first].blue - Distinctcolors[second].blue);//O(1)
            dis = Math.Sqrt(r + g + b);//O(1)
            return dis;
        }
        public static Dictionary<int, List<int>> clustring_fun(List<int>[] nighboorlist, bool[] explored, int cluster_num)
        {
            for (int i = 0; i < nighboorlist.Length; i++)//O(D)
            {

                if (explored[i] == false && nighboorlist[i].Count == 0)//O(1)
                {
                    cluster_dic.Add(cluster_num, new List<int> { i });//O(1)
                    cluster_num++;//O(1)
                    explored[i] = true;//O(1)

                }
                else if (explored[i] == false && nighboorlist[i].Count != 0)//O(1)
                {
                    cluster_dic.Add(cluster_num, new List<int>());//O(1)
                    cluster_dfs(i, ref explored, nighboorlist, cluster_dic, cluster_num);//O(D)
                    cluster_num++;//O(1)

                }
            }
            return cluster_dic;

        }
        public static RGBPixel[,] mapping_fun(List<RGBPixel> Distinctcolors, List<RGBPixel> pallete, RGBPixel[,] Image)
        {
            //Total complexity of mapping_fun function = O(K*D)+O(N^2)=O(N^2)
            RGBPixel[] replace = new RGBPixel[16777217];//O(1)
            int result;//O(1)
            foreach (var i in cluster_dic)//O(K)
            {
                RGBPixel x = pallete[i.Key];//O(1)
                result = 0;//O(1)
                for (int j = 0; j < i.Value.Count; j++)//O(D)
                {
                    result = (Distinctcolors[i.Value[j]].red << 16) + (Distinctcolors[i.Value[j]].green << 8) + (Distinctcolors[i.Value[j]].blue << 0);//O(1)
                    replace[result] = x;//O(1)
                }
            }

            for (int i = 0; i < GetHeight(Image); i++)//O(N)
            {
                for (int j = 0; j < GetWidth(Image); j++)//O(N)
                {
                    result = (Image[i, j].red << 16) + (Image[i, j].green << 8) + (Image[i, j].blue << 0);//O(1)
                    Image[i, j] = replace[result];//O(1)
                }
            }
            return Image;
        }
        public static int node(bool[] explored, double[] cost_of_edge, List<RGBPixel> Distinctcolors)
        {
            //Total complexity of node function = O(D)
            double min = double.MaxValue; //O(1)
            int index = -1;//O(1)
            int count = 0;//O(1)
            while (count < Distinctcolors.Count)//O(D)
            {
                if (explored[count] == false && cost_of_edge[count] < min) //O(1)
                {
                    min = cost_of_edge[count]; //O(1)
                    index = count; //O(1)
                }
                count++; //O(1)
            }
            return index;
        }
        public static Dictionary<int, List<int>> cluster_dic;//O(1)

        public static void MST(List<RGBPixel> Distinctcolors, int num_of_clusters, RGBPixel[,] Image, PictureBox PicBox)// mst and distance
        {
            int[] parent = new int[Distinctcolors.Count];//O(1)
            double[] cost_of_edge = new double[Distinctcolors.Count];//O(1)
            bool[] explored = new bool[Distinctcolors.Count];//O(1)
            double mst = 0, dist;//O(1)
            int[] child1 = new int[Distinctcolors.Count];//O(1)
            int idx = 0;//O(1)
            for (int i = 0; i < Distinctcolors.Count; i++)//O(D)
            {
                explored[i] = false;//O(1)
                cost_of_edge[i] = double.MaxValue;//O(1)
            }
            parent[0] = 0;//O(1)
            cost_of_edge[0] = 0;//O(1)
            //Construct Graph complexity =O(D)*O(D)=O(D^2)
            for (int i = 0; i < Distinctcolors.Count - 1; i++)//O(D)
            {

                idx = node(explored, cost_of_edge, Distinctcolors);//O(D)

                explored[idx] = true;//O(1)
                for (int node = 0; node < Distinctcolors.Count; node++)//O(D)
                {
                    if (explored[node] == false)//O(1)
                    {
                        dist = distance(Distinctcolors, idx, node);//O(1)
                        if (dist < cost_of_edge[node])//O(1)
                        {
                            parent[node] = idx;//O(1)
                            cost_of_edge[node] = dist;//O(1)
                            child1[node] = node;//O(1)
                        }
                    }

                }
            }

            Dictionary<int, KeyValuePair<int, int>> edges = new Dictionary<int, KeyValuePair<int, int>>(); //O(1)
            Dictionary<int, double> edw = new Dictionary<int, double>();//O(1)
            List<int>[] nighboorlist = new List<int>[Distinctcolors.Count];//O(1)
            //Mst complexity =O(V) 
            for (int i = 0; i < Distinctcolors.Count; i++)//O(V)
            {
                mst += cost_of_edge[i];//O(1)
                edges.Add(i, new KeyValuePair<int, int>(parent[i], child1[i]));//O(1)
                edw.Add(i, cost_of_edge[i]);//O(1)
            }
            MessageBox.Show("MST is: " + mst.ToString());//O(1)
            double m = 0; int child = 0;//O(1)



            for (int i = 0; i < num_of_clusters - 1; i++)//O(K)
            {
                foreach (var item in edw)//O(D)
                {
                    if (item.Value > m)//O(1)
                    {
                        m = item.Value;//O(1)
                        child = item.Key;//O(1)
                    }
                }
                edw.Remove(child);//O(1)
                edw.Add(child, -1);//O(1)
                edges.Remove(child);//O(1)
                edges.Add(child, new KeyValuePair<int, int>(child, child));//O(1)
                m = 0;//O(1)
            }

            for (int j = 0; j < Distinctcolors.Count; j++)//O(D)
            {
                nighboorlist[j] = new List<int>();//O(1)
            }
            int c = 0;//O(1)
            double output = 0;//O(1)

            foreach (KeyValuePair<int, KeyValuePair<int, int>> item in edges)//O(D)
            {
                edw.TryGetValue(item.Key, out output);//O(1)

                c = item.Key;//O(1)
                KeyValuePair<int, int> cur = edges.ElementAt(c).Value;//O(1)
                if (cur.Key != c)//O(1)
                {
                    nighboorlist[c].Add(cur.Key);//O(1)
                    nighboorlist[cur.Key].Add(c);//O(1)
                }
            }
            bool[] visited = new bool[nighboorlist.Length];//O(1)
            int cluster_num = 0;//O(1)
            List<RGBPixel> pallete = new List<RGBPixel>(num_of_clusters);//O(1)
            cluster_dic = new Dictionary<int, List<int>>();//O(1)
            cluster_dic = clustring_fun(nighboorlist, visited, cluster_num);//O(D^2)
            foreach (var i in cluster_dic)//Exact (D)
            {
                double r = 0, g = 0, b = 0;//O(1)
                for (int j = 0; j < i.Value.Count; j++) // Upper (D)
                {
                    r += Distinctcolors[i.Value[j]].red;//O(1)
                    g += Distinctcolors[i.Value[j]].green;//O(1)
                    b += Distinctcolors[i.Value[j]].blue;//O(1)
                }
                r /= i.Value.Count;//O(1)
                g /= i.Value.Count;//O(1)
                b /= i.Value.Count;//O(1)
                RGBPixel tp = new RGBPixel(r, g, b);//O(1)
                pallete.Add(tp);//O(1)
            }
            Image = mapping_fun(Distinctcolors, pallete, Image); //O(N^2)
            DisplayImage(Image, PicBox);//O(N^2)
        }

        public static void cluster_dfs(int x, ref bool[] visited, List<int>[] nighboorlist, Dictionary<int, List<int>> cluster_dict, int cluster_num)
        {
            //Total complexity of cluster_dfs function = O(D)
            List<int> li = new List<int>();//O(1)
            cluster_dict.TryGetValue(cluster_num, out li);//O(1)
            li.Add(x);//O(1)
            cluster_dict[cluster_num] = li;//O(1)
            visited[x] = true;//O(1)
            for (int i = 0; i < nighboorlist[x].Count; i++)//O(D)
            {
                if (visited[nighboorlist[x][i]] == false)//O(1)
                {
                    cluster_dfs(nighboorlist[x][i], ref visited, nighboorlist, cluster_dict, cluster_num);
                }
            }
        }
    }
}