using System;
using UnityEngine;
using RosSharp.RosBridgeClient;
using System.Runtime.InteropServices;


public class RGDBMerger : MonoBehaviour
{

    [DllImport("image_decompress_opencv.dll", EntryPoint = "processImage")]
    public extern static void processImage(byte[] raw, int lenCompressed, int flag, int width, int height, int len, out System.IntPtr image);


    public ImageSubscriber rgbImageSub;
    public DepthImageSubscriber depthImageSub;

    public RosSharp.RosBridgeClient.Messages.Sensor.Image rgbImage;
    public RosSharp.RosBridgeClient.Messages.Sensor.Image depthImage;


    public string TopicRGB;
    private string publicationIdRGB;
    public string TopicDepth;
    private string publicationIdDepth;




    // Start is called before the first frame update
    void Start()
    {
        publicationIdRGB = GetComponent<RosConnector>().RosSocket.Advertise<RosSharp.RosBridgeClient.Messages.Sensor.Image>(TopicRGB);
        publicationIdDepth = GetComponent<RosConnector>().RosSocket.Advertise<RosSharp.RosBridgeClient.Messages.Sensor.Image>(TopicDepth);
        rgbImage = new RosSharp.RosBridgeClient.Messages.Sensor.Image();
        depthImage = new RosSharp.RosBridgeClient.Messages.Sensor.Image();
    }

    private void OnDestroy()
    {

    }

    // Update is called once per frame
    void Update()
    {
        MergeImages();
    }


    protected void PublishRGB(RosSharp.RosBridgeClient.Messages.Sensor.Image message)
    {
        GetComponent<RosConnector>().RosSocket.Publish(publicationIdRGB, message);
    }

    protected void PublishDepth(RosSharp.RosBridgeClient.Messages.Sensor.Image message)
    {
        GetComponent<RosConnector>().RosSocket.Publish(publicationIdDepth, message);
    }


    void MergeImages()
    {
        if (rgbImageSub.IsMessageReceived && depthImageSub.IsMessageReceived)
        {                
            // Calculate number of elements in byte array
            int len = (rgbImageSub.width * 3) * rgbImageSub.height;

            // Allocate managed memory array
            rgbImage.data = new byte[len];
 
            // Allocate unmanaged memory
            System.IntPtr mem = Marshal.AllocHGlobal(len);
 
            // Call dllimport function to fill in unmanaged memory
            processImage(rgbImageSub.ImageData, rgbImageSub.ImageData.Length, 0, rgbImageSub.width, rgbImageSub.height, len, out mem);
 
            //Debug.Log(string.Format("rgbImage data size: {0} mem size: {1} rgb.data size: {2}", rgbImageSub.ImageData.Length, mem.GetType(), rgbImage.data.Length));

            // Copy unmanaged memory into managed byte array
            Marshal.Copy(mem, rgbImage.data, 0, len);

            // Deallocate unmanaged memory
            // Unity crashes here?
            Marshal.FreeHGlobal(mem);

            // Set image fields
            rgbImage.encoding = "rgb8";
            rgbImage.height = 480;
            rgbImage.width = 640;
            rgbImage.is_bigendian = 0;
            rgbImage.step = 1920;

            rgbImage.header.frame_id = "camera_rgb_optical_frame";

            // Test by publishing image and checking rviz
            PublishRGB(rgbImage);
                
            

            /* Depth image */
                
            // Calculate number of elements in byte array
            // Dynamically determine number of bits to represent pixels? 8bit vs 16bit vs 32bit
            // pngs are 32 bit so *4 is used
            int lenDepth = (depthImageSub.width * depthImageSub.height) * 4;

            // Allocate managed memory array
            depthImage.data = new byte[lenDepth];

            // Allocate unmanaged memory
            System.IntPtr memDepth = Marshal.AllocHGlobal(lenDepth);
                
            // Call dllimport function to fill in unmanaged memory
            processImage(depthImageSub.ImageData, depthImageSub.ImageData.Length, 1, depthImageSub.width, depthImageSub.height, lenDepth, out memDepth);
                
            // Copy unmanaged memory into managed byte array
            Marshal.Copy(memDepth, depthImage.data, 0, lenDepth);

            // Deallocate unmanaged memory
            // Unity crashes here?
            Marshal.FreeHGlobal(memDepth);
                
            // Set image fields for png
            depthImage.encoding = "32FC1";
            depthImage.height = 480;
            depthImage.width = 640;
            depthImage.is_bigendian = 0;
            depthImage.step = 2560;
            depthImage.header.frame_id = "camera_rgb_optical_frame";
                
            PublishDepth(depthImage);         
        }
        /*else
        {
            Debug.Log("Image data is null!");
            Debug.Log("rgb: " + rgbImageSub.ImageData + " depth: " + depthImageSub.ImageData);
        }*/
    }

}