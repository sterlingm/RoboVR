using UnityEngine;
using RosSharp.RosBridgeClient;
using System.Runtime.InteropServices;

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


        // Don't get the mesh from the game object because that assumes you have a mesh to drag onto this object
        //combinedMesh = GetComponent<MeshFilter>().mesh;
        //combinedMesh.vertices = vertices;
        //mesh.triangles = triangles;
        //mesh.Optimize();
        //mesh.RecalculateNormals();

        // New as of 10/20
        // Build a new mesh since we don't already have one ready to go
        combinedMesh = new Mesh();
        GetComponent<MeshFilter>().mesh = combinedMesh;
        //CreateExMesh();
        //combinedMesh.Clear();
    }

    void CreateExMesh()
    {
        Vector3[] points = new Vector3[4];
        int[] indices = new int[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            Point p = new Point();
            p.x = i;
            p.y = i;
            p.z = i < 2 ? i : i*2;
            Vector3 v = new Vector3(p.x, p.y, p.z);
            points[i] = v;
            indices[i] = i;
        }
        int[] triangles = { 0, 1, 2, 2, 1, 3 };
        Vector2[] vertices_uv = new Vector2[4];
        vertices_uv[0] = new Vector2(0.0f, 1.0f);
        vertices_uv[1] = new Vector2(1.0f, 1.0f);
        vertices_uv[2] = new Vector2(0.0f, 0.0f);
        vertices_uv[3] = new Vector2(1.0f, 0.0f);

        // Build Mesh here?
        combinedMesh.vertices = points;
        combinedMesh.uv = vertices_uv;
        combinedMesh.triangles = triangles;
        combinedMesh.RecalculateNormals();
    }

    private void OnDestroy()
    {
    }
    #endregion

    #region MonoBehaviour Update
    // Update is called once per frame
    void Update()
    {
        Debug.Log("Joysticks: "+UnityEngine.Input.GetJoystickNames());
        //Debug.Log(UnityEngine.Input.GetAxis("Vertical"));

        RosSharp.RosBridgeClient.Messages.Standard.Time rgbStamp = null;
        RosSharp.RosBridgeClient.Messages.Standard.Time depthStamp = null;
        
        // Make sure both images have been received
        if (usingCompressedDepth && (rgbImageSub.ImageData != null && depthImageSub.ImageData != null) ||
            usingCompressedDepth == false && (rgbImageSub.ImageData != null && depthImageRawSub.ImageData != null))
        {
            rgbStamp = rgbImageSub.Stamp;
            depthStamp = usingCompressedDepth ? depthImageSub.Stamp : depthImageRawSub.Stamp;
        }
        else
        {
            Debug.Log("Have not received image data");
        }
        
        

        // Check if the images are stamped close together
        if (rgbStamp != null && (rgbStamp.secs - depthStamp.secs < 3))
        {
            DecompressImages();
            if (usingCompressedDepth == false)
            {
                depthImage.data = depthImageRawSub.ImageData;
            }
            LoadImages();

            Debug.Log("Publishing...");
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
        {
            //Debug.Log("color texture updated");
            //UnityEngine.Profiling.Profiler.BeginSample("Apply Color");
            colorTexture.LoadRawTextureData(rgbImage.data);
            colorTexture.Apply();
            //UnityEngine.Profiling.Profiler.EndSample();
        }

        if (usingCompressedDepth && depthImageSub.ImageData != null)
        {
            //Debug.Log("depth texture updated");
            //UnityEngine.Profiling.Profiler.BeginSample("Apply Color");
            depthTexture.LoadRawTextureData(depthImage.data);
            depthTexture.Apply();
            //UnityEngine.Profiling.Profiler.EndSample();
        }

        else if (usingCompressedDepth == false && depthImageRawSub.ImageData != null)
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
        processImage(rgbImageSub.ImageData, rgbImageSub.ImageData.Length, 0, rgbImageSub.width, rgbImageSub.height, len, out mem);

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
        processImage(depthImageSub.ImageData, depthImageSub.ImageData.Length, 1, depthImageSub.width, depthImageSub.height, lenDepth, out memDepth);

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

    protected void MergeImages()
    {
        // Fx = 570.3422
        // Fy = 319.5
        float focalX = 570.3422f;
        float focalY = 319.5f;
        pointCloud = new PointCloud(depthImage, rgbImage, focalX, focalY);
    }

    private void BuildMeshFromPointCloud()
    {
        Vector3[] points = new Vector3[pointCloud.Points.Length];
        int[] indices = new int[points.Length];
        for (int i=0;i<points.Length;i++)
        {
            Point p = pointCloud.Points[i];
            Vector3 v = new Vector3(p.x, p.y, p.z);
            points[i] = v;
            indices[i] = i;
        }
        // Build Mesh here?
        combinedMesh.vertices = points;

        // Build a sub mesh that is simply all the points of the root mesh
        /*UnityEngine.Rendering.SubMeshDescriptor subMeshDesc = new UnityEngine.Rendering.SubMeshDescriptor();
        subMeshDesc.topology = MeshTopology.Points;
        subMeshDesc.firstVertex = 0;
        subMeshDesc.indexCount = pointCloud.Points.Length;
        subMeshDesc.vertexCount = pointCloud.Points.Length;
        combinedMesh.SetSubMesh(0, subMeshDesc);
        combinedMesh.subMeshCount = 1;

        combinedMesh.SetIndices(indices, MeshTopology.Points, 0);*/

        //for (int i = 0; i < 20; i++)
            //pointCloud.printRandPoint(i);
    }

    private void OnRenderObject()
    {
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