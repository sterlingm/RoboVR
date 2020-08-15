
using UnityEngine;
using RosSharp.RosBridgeClient;
using System.Runtime.InteropServices;


public class RGDBMerger : MonoBehaviour
{


    [DllImport("image_decompress_opencv.dll", EntryPoint = "processImage")]
    public extern static void processImage(byte[] raw, int flag, int width, int height, int len, System.IntPtr image);
    

    public ImageSubscriber rgbImage;
    public ImageSubscriber depthImage;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnDestroy()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        MergeImages();
    }

    void MergeImages()
    {
        if(rgbImage.ImageData != null && depthImage.ImageData != null)
        {
            if (rgbImage.ImageData.Length > 0 && depthImage.ImageData.Length > 0)
            {
                RosSharp.RosBridgeClient.Messages.Sensor.Image rgb = new RosSharp.RosBridgeClient.Messages.Sensor.Image();
                int len = (rgbImage.width * 3) * rgbImage.height;
                rgb.data = new byte[len];

                System.IntPtr mem = Marshal.AllocHGlobal(len);

                processImage(rgbImage.ImageData, 1, rgbImage.width, rgbImage.height, len, mem);

                Debug.Log(string.Format("rgbImage data size: {0} mem size: {1} rgb.data size: {2}", rgbImage.ImageData.Length, mem.GetType(), rgb.data.Length));

                Debug.Log("Before copy rgb.data[0]: " + rgb.data[0] + " rgb.data[654321]: " + rgb.data[654321]);

                Marshal.Copy(mem, rgb.data, 0, len);        

                Debug.Log("After copy rgb.data[0]: " + rgb.data[0] + " rgb.data[654321]: " + rgb.data[654321]);

            }
        }
        else
        {
            Debug.Log("Image data is null!");
            Debug.Log("rgb: " + rgbImage.ImageData + " depth: " + depthImage.ImageData);
        }
    }



}
