
using UnityEngine;
using RosSharp.RosBridgeClient;
using System.Runtime.InteropServices;


public class RGDBMerger : MonoBehaviour
{

    [DllImport("image_decompress_opencv.dll", EntryPoint = "processImage")]
    public extern static void processImage(byte[] raw, int flag, int width, int height, int len, System.IntPtr image);


    public ImageSubscriber rgbImageSub;
    public ImageSubscriber depthImageSub;

    public RosSharp.RosBridgeClient.Messages.Sensor.Image rgbImage;
    public RosSharp.RosBridgeClient.Messages.Sensor.Image depthImage;


    public string Topic;
    private string publicationId;



    // Start is called before the first frame update
    void Start()
    {
        publicationId = GetComponent<RosConnector>().RosSocket.Advertise<RosSharp.RosBridgeClient.Messages.Sensor.Image>(Topic);
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


    protected void Publish(RosSharp.RosBridgeClient.Messages.Sensor.Image message)
    {
        GetComponent<RosConnector>().RosSocket.Publish(publicationId, message);
    }


    void MergeImages()
    {
        if (rgbImageSub.ImageData != null && depthImageSub.ImageData != null)
        {
            if (rgbImageSub.ImageData.Length > 0 && depthImageSub.ImageData.Length > 0)
            {
                // Calculate number of elements in byte array
                int len = (rgbImageSub.width * 3) * rgbImageSub.height;

                // Allocate managed memory array
                rgbImage.data = new byte[len];

                // Allocate unmanaged memory
                System.IntPtr mem = Marshal.AllocHGlobal(len);

                // Call dllimport function to fill in unmanaged memory
                processImage(rgbImageSub.ImageData, 1, rgbImageSub.width, rgbImageSub.height, len, mem);

                //Debug.Log(string.Format("rgbImage data size: {0} mem size: {1} rgb.data size: {2}", rgbImageSub.ImageData.Length, mem.GetType(), rgbImage.data.Length));

                // Copy unmanaged memory into managed byte array
                Marshal.Copy(mem, rgbImage.data, 0, len);

                // Deallocate unmanaged memory
                Marshal.FreeHGlobal(mem);

                // Set image fields
                rgbImage.encoding = "rgb8";
                rgbImage.height = 480;
                rgbImage.width = 640;
                rgbImage.is_bigendian = 0;
                rgbImage.step = 1920;

                // Test by publishing image and checking rviz
                // Publish(rgbImage)
            }
        }
        else
        {
            Debug.Log("Image data is null!");
            Debug.Log("rgb: " + rgbImageSub.ImageData + " depth: " + depthImageSub.ImageData);
        }
    }

}