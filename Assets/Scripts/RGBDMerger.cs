using UnityEngine;
using RosSharp.RosBridgeClient;
using System.Runtime.InteropServices;
using System.Diagnostics;

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

    private PointCloud pointCloud;
    private Mesh combinedMesh;

    public OdometrySubscriber odomSub;


    Stopwatch stopwatch = new Stopwatch();

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
    }
    

    private void OnDestroy()
    {
    }
    #endregion

    #region MonoBehaviour Update
    // Update is called once per frame
    void Update()
    {
        RosSharp.RosBridgeClient.Messages.Standard.Time rgbStamp = null;
        RosSharp.RosBridgeClient.Messages.Standard.Time depthStamp = null;
        
        // Make sure both images have been received
        //if (usingCompressedDepth && (rgbImageSub.ImageData != null && depthImageSub.ImageData != null) ||
        //    usingCompressedDepth == false && (rgbImageSub.ImageData != null && depthImageRawSub.ImageData != null))
        if (usingCompressedDepth && (rgbImageSub.HasNew && depthImageSub.HasNew) ||
            usingCompressedDepth == false && (rgbImageSub.HasNew && depthImageRawSub.HasNew))
        {
            rgbStamp = rgbImageSub.Stamp;
            depthStamp = usingCompressedDepth ? depthImageSub.Stamp : depthImageRawSub.Stamp;
            UnityEngine.Debug.Log("Image data received");
        }
        else
        {
            UnityEngine.Debug.Log("Image data not received...");
        }
        
        

        // If images are received, then decompress them
        if (rgbStamp != null)
        {
            UnityEngine.Debug.Log("Loading images");
            DecompressImages();
            if (usingCompressedDepth == false)
            {
                depthImage.data = depthImageRawSub.GetNew();
            }
            LoadImages();

            //UnityEngine.Debug.Log("Publishing...");
            // Test by publishing image and checking rviz
            //PublishRGB();
            //PublishDepth();
            //MergeImages();
            //BuildMeshFromPointCloud();

        }
    }
    #endregion

    void LoadImages()
    {
        if (rgbImageSub.ImageData != null)
        //if (rgbImageSub.ImageData != null)
        //if (rgbImageSub.HasNew)
            {
            //Debug.Log("color texture updated");
            //UnityEngine.Profiling.Profiler.BeginSample("Apply Color");
            colorTexture.LoadRawTextureData(rgbImage.data);
            colorTexture.Apply();
            //UnityEngine.Profiling.Profiler.EndSample();
        }

        if (usingCompressedDepth && depthImageSub.ImageData != null)
        //if (usingCompressedDepth && depthImageSub.HasNew)
            {
            //Debug.Log("depth texture updated");
            //UnityEngine.Profiling.Profiler.BeginSample("Apply Color");
            depthTexture.LoadRawTextureData(depthImage.data);
            depthTexture.Apply();
            //UnityEngine.Profiling.Profiler.EndSample();
        }

        else if (usingCompressedDepth == false && depthImageRawSub.ImageData != null)
        //else if (usingCompressedDepth == false && depthImageRawSub.HasNew)
        {
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
        //stopwatch.Stop();
        //UnityEngine.Debug.Log(string.Format("Elapsed time for OnRenderObject: {0}", stopwatch.ElapsedMilliseconds/1000.0));
        //stopwatch.Restart();
 
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
    }
}