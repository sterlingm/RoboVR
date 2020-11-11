using UnityEngine;
using RosSharp.RosBridgeClient;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System;

public class RGBDMerger : MonoBehaviour
{

    [DllImport("image_decompress_opencv.dll", EntryPoint = "processImage")]
    public extern static void processImage(byte[] raw, int lenCompressed, int flag, int width, int height, int len, out System.IntPtr image);


    public ImageSubscriber rgbImageSub;
    public DepthImageSubscriber depthImageSub;
    public ImageRawSubscriber depthImageRawSub;

    public bool usingCompressedDepth;

    public RosSharp.RosBridgeClient.Messages.Sensor.Image rgbImage;
    public RosSharp.RosBridgeClient.Messages.Sensor.Image depthImage;

    private Texture2D depthTexture;
    private Texture2D colorTexture;
    public Material material;

    public string TopicRGB;
    private string publicationIdRGB;
    public string TopicDepth;
    private string publicationIdDepth;
    
    public OdometrySubscriber odomSub;


    Stopwatch stopwatch;

    #region MonoBehaviour Start, Destroy 
    // Start is called before the first frame update
    void Start()
    {
        publicationIdRGB = GetComponent<RosConnector>().RosSocket.Advertise<RosSharp.RosBridgeClient.Messages.Sensor.Image>(TopicRGB);
        publicationIdDepth = GetComponent<RosConnector>().RosSocket.Advertise<RosSharp.RosBridgeClient.Messages.Sensor.Image>(TopicDepth);
        rgbImage = new RosSharp.RosBridgeClient.Messages.Sensor.Image();
        depthImage = new RosSharp.RosBridgeClient.Messages.Sensor.Image();

        // Create a texture for the depth image and color image
        depthTexture = new Texture2D(depthImageSub.width, depthImageSub.height, TextureFormat.R16, false);
        colorTexture = new Texture2D(rgbImageSub.width, rgbImageSub.height, TextureFormat.RGB24, false);

        stopwatch = new Stopwatch();
    }
    

    private void OnDestroy()
    {
    }
    #endregion

    #region MonoBehaviour Update
    // Update is called once per frame
    void Update()
    {
        //UnityEngine.Debug.Log(string.Format("Frame rate: {0}", 1 / Time.deltaTime));

        RosSharp.RosBridgeClient.Messages.Standard.Time rgbStamp = null;
        RosSharp.RosBridgeClient.Messages.Standard.Time depthStamp = null;

        // Check if new data has been received
        bool rgbImageUpdated = rgbImageSub.HasNew;
        bool depthImageUpdated = usingCompressedDepth ? depthImageSub.HasNew : depthImageRawSub.HasNew;
        
        // Set time stamps
        if(rgbImageUpdated)
        {
            rgbStamp = rgbImageSub.Stamp;
        }
        if(depthImageUpdated)
        {
            depthStamp = usingCompressedDepth ? depthImageSub.Stamp : depthImageRawSub.Stamp;
        }
        // Print some debug info
        if (depthImageUpdated || rgbImageUpdated)
        {
            UnityEngine.Debug.Log("Image data received");
        }
        else
        {
            UnityEngine.Debug.Log("Image data not received...");
        }

        
        DateTime start = DateTime.Now;
        /*
         * Do decompression if new data has been received
         */
        if (rgbImageUpdated)
        {
            DecompressRGB();
        }
        if(depthImageUpdated)
        {
            if(usingCompressedDepth)
            {
                DecompressDepth();
            }
            else
            {
                depthImage.data = depthImageRawSub.GetNew();
            }
        }
        DateTime stop = DateTime.Now;
        Double elapsedMsDec = (stop - start).TotalMilliseconds;        
        print(string.Format("elapsedMsDec: {0}", elapsedMsDec));

        start = DateTime.Now;
        // Load the image data into the textures
        LoadImages(rgbImageUpdated, depthImageUpdated);
        stop = DateTime.Now;
        Double elapsedMsLoad = (stop - start).TotalMilliseconds;
        print(string.Format("elapsedMsLoad: {0}", elapsedMsLoad));

    }
    #endregion

    void LoadImages(bool rgb, bool depth)
    {
        if (rgb)
        {
            //Debug.Log("color texture updated");
            //UnityEngine.Profiling.Profiler.BeginSample("Apply Color");
            colorTexture.LoadRawTextureData(rgbImage.data);
            colorTexture.Apply();
            //UnityEngine.Profiling.Profiler.EndSample();
        }

        if (depth)
        {
            //Debug.Log("depth texture updated");
            //UnityEngine.Profiling.Profiler.BeginSample("Apply Color");
            depthTexture.LoadRawTextureData(depthImage.data);
            depthTexture.Apply();
            //UnityEngine.Profiling.Profiler.EndSample();
        }
    }

    #region Publishing methods
    private void PublishRGB()
    {
        setRGBMsgFields();
        GetComponent<RosConnector>().RosSocket.Publish(publicationIdRGB, rgbImage);
    }

    private void PublishDepth()
    {
        setDepthMsgFields();
        GetComponent<RosConnector>().RosSocket.Publish(publicationIdDepth, depthImage);
    }

    // For publishing for testing
    private void setRGBMsgFields()
    {
        // Set image fields
        rgbImage.encoding = rgbImageSub.encoding;
        rgbImage.height = 480;
        rgbImage.width = 640;
        rgbImage.is_bigendian = 0;
        rgbImage.step = 1920;

        rgbImage.header.frame_id = "camera_rgb_optical_frame";
    }
    

    // For publishing for testing
    private void setDepthMsgFields()
    {
        // Set image fields for png
        depthImage.encoding = usingCompressedDepth ? depthImageSub.encoding : depthImageRawSub.encoding;
        depthImage.height = 480;
        depthImage.width = 640;
        depthImage.is_bigendian = 0;
        depthImage.step = 1280;
        depthImage.header.frame_id = "camera_rgb_optical_frame";
    }
    #endregion

    #region Decompression methods
    protected void DecompressRGB()
    {
        // Calculate number of elements in byte array
        int len= rgbImageSub.width * rgbImageSub.height;
        if (rgbImageSub.encoding.Equals("rgb8"))
        {
            len *= 3;
        }
        // Put in other encoding representations...

        // Allocate managed memory array
        rgbImage.data = new byte[len];

        // Allocate unmanaged memory
        System.IntPtr mem = Marshal.AllocHGlobal(len);

        // Call dllimport function to fill in unmanaged memory
        processImage(rgbImageSub.GetNew(), rgbImageSub.Length(), 0, rgbImageSub.width, rgbImageSub.height, len, out mem);

        //Debug.Log(string.Format("rgbImage data size: {0} mem size: {1} rgb.data size: {2}", rgbImageSub.ImageData.Length, mem.GetType(), rgbImage.data.Length));

        // Copy unmanaged memory into managed byte array
        Marshal.Copy(mem, rgbImage.data, 0, len);

        // Deallocate unmanaged memory
        Marshal.FreeHGlobal(mem);
    }

    protected void DecompressDepth()
    {
        // Calculate number of elements in byte array
        // Dynamically determine number of bits to represent pixels? 8bit vs 16bit vs 32bit
        // pngs are 32 bit so *4 is used
        int lenDepth = (depthImageSub.width * depthImageSub.height);
        if (depthImageSub.encoding.Equals("32FC1"))
        {
            lenDepth *= 4;
        }
        // Put in other encoding representations...

        // Allocate managed memory array
        depthImage.data = new byte[lenDepth];

        // Allocate unmanaged memory
        System.IntPtr memDepth = Marshal.AllocHGlobal(lenDepth);

        // Call dllimport function to fill in unmanaged memory
        processImage(depthImageSub.GetNew(), depthImageSub.Length(), 1, depthImageSub.width, depthImageSub.height, lenDepth, out memDepth);

        // Copy unmanaged memory into managed byte array
        Marshal.Copy(memDepth, depthImage.data, 0, lenDepth);

        // Deallocate unmanaged memory
        Marshal.FreeHGlobal(memDepth);
    }

    protected void DecompressImages()
    {
        // Each decompression takes ~10-30ms

        // Always using RGB compressed
        DecompressRGB();

        // Check if using depth compressed
        if (usingCompressedDepth)
        {
            DecompressDepth();
        }
    }
    #endregion
    
    
    private void OnRenderObject()
    {
        /*stopwatch.Stop();
        UnityEngine.Debug.Log(string.Format("Elapsed time for OnRenderObject: {0}", stopwatch.ElapsedMilliseconds));
        stopwatch.Restart();*/
        DateTime start = DateTime.Now;
        
        // Set textures and pass
        material.SetTexture("_MainTex", depthTexture);
        material.SetTexture("_ColorTex", colorTexture);
        material.SetPass(0);

        // Set TF from base_link to odom
        Matrix4x4 m = Matrix4x4.TRS(odomSub.PublishedTransform.position,
                          odomSub.PublishedTransform.rotation, new UnityEngine.Vector3(1, 1, 1));
        material.SetMatrix("transformationMatrix", m);


        // Draw mesh
        Graphics.DrawProceduralNow(MeshTopology.Points, depthImageSub.width * depthImageSub.height, 1);

        DateTime stop = DateTime.Now;
        Double elapsedMsRen = (stop - start).TotalMilliseconds;
        print(string.Format("elapsedMsRen: {0}", elapsedMsRen));

        /*stopwatch.Stop();
        long elapsedTimeRen = stopwatch.ElapsedMilliseconds;
        print(string.Format("elapsedTimeRen: {0}", elapsedTimeRen));*/
    }
}