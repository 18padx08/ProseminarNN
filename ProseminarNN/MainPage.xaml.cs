using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Input;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ProseminarNN
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public int CurrentNoise { get; set; }
        private NeuralNetwork.NeuralNetworkClass nn;
        public MainPage()
        {
            this.InitializeComponent();
            nn = new NeuralNetwork.NeuralNetworkClass();
           
        }

       

        private bool startTracking = false;
        private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            lock (pointers)
            {
                if (!startTracking)
                {
                    startTracking = true;
                    pointers.Clear();
                    theLine.Points = new PointCollection();
                }
            }
        }

        List<PointerPoint> pointers = new List<PointerPoint>();
        private void Grid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (startTracking)
            {
                lock (pointers)
                {
                    pointers.AddRange(e.GetIntermediatePoints(this.Root));
                    theLine.Points.Clear();
                    foreach (var p in pointers)
                    {
                        theLine.Points.Add(p.Position);
                    }
                }
            }
        }
        List<bool> Result = new List<bool>();
        async private void Grid_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            byte[] buffer = new byte[28 * 28*4];
            int[] binImage = new int[28 * 28];
            var ram = new InMemoryRandomAccessStream();
            var ram2 = new InMemoryRandomAccessStream();

            var source = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, ram);
            var inputImage = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, ram2);
            lock (pointers)
            {
                Result.Clear();
                startTracking = false;
                theLine.Points.Clear();

                //transform to a 20x20 pixel screen

               
                foreach (var point in pointers)
                {
                    var x = point.Position.X / this.Root.Width *27.0f;
                    var y = point.Position.Y / this.Root.Height*27.0;
                    //buffer[(int)y * 28*4 + (int)x*4] = 0xff;
                    binImage[(int)y * 28 + (int)x] = 255;
                    
                }
                var rand = new Random();
                for (int i=0; i< binImage.Length; i++)
                {
                    
                    binImage[i] += rand.Next() % CurrentNoise;
                    binImage[i] = binImage[i] > 255? 255 : binImage[i];
                }
                //buffer[10 * 20*4 + 10*4] = 0xff;
               
            }
            var output = nn.Reconstruct(binImage.ToList());
            byte[] newBuf = new byte[28 * 28 * 4];
            int realIndex = 0;
            for(int i = 0; i < output.Count; i++)
            {
                newBuf[realIndex] = (byte)(255 * output[i] > 255? 255 : 255 * output[i]);
                newBuf[realIndex + 1] = (byte)(255 * output[i] > 255 ? 255 : 255 * output[i]);
                newBuf[realIndex + 2] = (byte)(255 * output[i] > 255 ? 255 : 255 * output[i]);
                newBuf[realIndex + 3] = (byte)(255 * output[i] > 255 ? 255 : 255 * output[i]);
                buffer[realIndex] = (byte)binImage[i];
                buffer[realIndex+1] = (byte)binImage[i];
                buffer[realIndex+2] = (byte)binImage[i];
                buffer[realIndex+3] = (byte)binImage[i];
                realIndex += 4;
            }
            source.SetPixelData(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Ignore, 28, 28, 96, 96, newBuf.ToArray());
            inputImage.SetPixelData(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Ignore, 28, 28, 96, 96, buffer.ToArray());
            await source.FlushAsync();
            await inputImage.FlushAsync();
            BitmapImage im = new BitmapImage();
            im.SetSource(ram);
            BitmapImage im2 = new BitmapImage();
            im2.SetSource(ram2);
            this.LastCaptured.Source = im;
            this.LastInput.Source = im2;
        }
        IList<IList<int>> trainingData = new List<IList<int>>();
        async private void Button_Click(object sender, RoutedEventArgs e)
        {
            //Open file from FileSystem
            var file = await Package.Current.InstalledLocation.GetFileAsync("train-images.idx3-ubyte");
            var labels = await Package.Current.InstalledLocation.GetFileAsync("train-labels.idx1-ubyte");
            //make sure we close the file properly 
            using(var labelS = await labels.OpenAsync(FileAccessMode.Read))
            using(var stream = await file.OpenAsync(FileAccessMode.Read))
            {
                byte[] magic = new byte[4];
                byte[] number_images = new byte[4];
                byte[] width = new byte[4];
                byte[] length = new byte[4];
                //ACcording to http://yann.lecun.com/exdb/mnist/ we have the first 32 bytes as the header
                //read the magic number (4 bytes since int32)
                await stream.ReadAsync(magic.AsBuffer(), 4, InputStreamOptions.None);
                //read the picture number (4 bytes since int32)
                await stream.ReadAsync(number_images.AsBuffer(), 4, InputStreamOptions.None);
                //read the width number (4 bytes since int32)
                await stream.ReadAsync(width.AsBuffer(), 4, InputStreamOptions.None);
                //read the length number (4 bytes since int32)
                await stream.ReadAsync(length.AsBuffer(), 4, InputStreamOptions.None);
                //inverse the byte order since we are BigEndian https://en.wikipedia.org/wiki/Endianness
                Array.Reverse(number_images);
                Array.Reverse(width);
                Array.Reverse(length);
                //Convert our byte arrays to actual integer (since we don't have pointer access we need this HelperFunction)
                var num = BitConverter.ToInt32(number_images, 0);
                var wi = BitConverter.ToInt32(width, 0);
                var len = BitConverter.ToInt32(length, 0);
                List<Image> imageList = new List<Image>();
                //start itterating over pictures
                //read label magicheader
                //magicnumber -> number of items
                byte[] labelheader = new byte[8];
                await labelS.ReadAsync(labelheader.AsBuffer(), 8, InputStreamOptions.None);
                int hasthree = 0;
                int hassix = 0;
                for(int i =0; i < num; i++)
                {
                    //iterate over pictures
                    //allocate temp buffer to hold a tmp repr. of the picture loaded from the file
                    byte[] tmpBuffer = new byte[wi * len];
                    int[] td = new int[wi * len];
                    var ram = new InMemoryRandomAccessStream();
                    int binCount = 0;
                    byte[] label = new byte[1];
                    await labelS.ReadAsync(label.AsBuffer(), 1, InputStreamOptions.None);
                    //only learn the number 6
                    await stream.ReadAsync(tmpBuffer.AsBuffer(), (uint)(wi*len), InputStreamOptions.None);
                    if (!(label[0] == 6 || label[0] == 3) || (hasthree > 10 && hassix >10) ) continue;
                    if (hasthree > 10) continue;
                    if (hassix > 10) continue;
                    if(label[0] == 6 && hassix<=10)
                    {
                        hassix++;
                    }
                    else if(hasthree <= 10)
                    {
                        hasthree++;
                    }
                    for (int xy = 0; xy < tmpBuffer.Length; xy++)
                    {
                        td[xy] = tmpBuffer[xy];
                    }
                    //iterate over all pixels in the picture. since we are dealing with a RGBA8 format on windows multiply with 4 (1 byte for each channel)
                    /* for(int xy = 0; xy < (wi*len *4)-1;/* always add 4 since we must skip GBA channels xy+=4)
                     {
                         //buffer to hold the currentpixel
                         byte[] pixelBuffer = new byte[1];
                         //picture is wi *len
                         //assign at pixel location xy the value 0x(input)(input)(input)(input) actually inverts the picture sinc 0xffffffff is white rather than black 
                         tmpBuffer[xy] = (await stream.ReadAsync(pixelBuffer.AsBuffer(), 1, InputStreamOptions.None)).ToArray()[0];
                         tmpBuffer[xy + 1] = tmpBuffer[xy];
                         tmpBuffer[xy + 2] = tmpBuffer[xy];
                         tmpBuffer[xy + 3] = tmpBuffer[xy];
                         if(tmpBuffer[xy] > 200)
                         {
                             td[binCount] = 1;
                         }
                         else
                         {
                             td[binCount] = 0;
                         }
                         binCount++;
                     }*/
                  
                        trainingData.Add(td.ToList());
                    //NOT IMPORTANT FOR PYTHON CODE
                    //Encode picture from array to be able to assign it to a image source
                    /*BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.BmpEncoderId, ram);
                    encoder.SetPixelData(BitmapPixelFormat.Rgba8, BitmapAlphaMode.Ignore, (uint)wi, (uint)len, 96, 96, tmpBuffer);
                    //ensure everything has been written to the InMemoryRandomAccessStream
                    await encoder.FlushAsync();
                    //Create ImageSOurce
                    BitmapImage img = new BitmapImage();
                    //Set Source of image to InMemoryRandomAccessStream
                    img.SetSource(ram);
                    //Add a new Image to the list (in order to show it on the gui)
                    imageList.Add(new Image() { Source = img });*/
                }
                //set FlipView ItemsSOurce to present pictures in FlipView on screen
                //this.Pictures.ItemsSource = imageList;
                trainingData = trainingData.Reverse().ToList();
                nn.SetTrainingData(trainingData);
                var t1 = Task.Run(() => { nn.TrainRBM(200,1,0.0001); });
                //var t2 = Task.Run(() => { nn.TrainRBM(200,2,0.1); });
                //var t3 = Task.Run(() => { nn.TrainRBM(200, 2, 0.1); });
               // var t4 = Task.Run(() => { nn.TrainRBM(200, 2, 0.1); });
                

                await t1.AsAsyncAction();
               // await t2.AsAsyncAction();
                //await t3.AsAsyncAction();
                //await t4.AsAsyncAction();
               
                MessageDialog md = new MessageDialog("Finished loading and training");
                await md.ShowAsync();
            }
        }

        private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            CurrentNoise = (int)e.NewValue;
        }
    }
}
