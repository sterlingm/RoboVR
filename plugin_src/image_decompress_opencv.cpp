// image_decompress_opencv.cpp : Defines the exported functions for the DLL application.
//
#include <opencv2/imgproc.hpp>
#include <opencv2/imgcodecs.hpp>

enum compressionFormat {
	UNDEFINED = -1, INV_DEPTH
};

struct ConfigHeader {
	compressionFormat format;
	float quantParams[2];
};

extern "C"
{
	__declspec(dllexport) void processImage(unsigned char data[], int lenCompressed, int flag, int width, int height, int len, unsigned char** image)
	{
		// RGB
		if (flag == 0)
		{
			// Create image data vector
			const std::vector<unsigned char> imageData(data, data + lenCompressed);

			// Decompress the image
			cv::Mat decompressed = cv::imdecode(imageData, cv::IMREAD_COLOR);

			// image_transport sets encoding to bgr when the image has 3 channels, then converts from bgr to rgb with cvtColor
			// We need to apply brg2rgb	
			cv::cvtColor(decompressed, decompressed, CV_BGR2RGB);

			// Set image data
			// len should be calculated and set in Unity
			//*image = (unsigned char*)malloc(len);

			// Copy over the data
			memcpy(*image, decompressed.data, len);
		}

		// Depth
		// THIS ASSUMES PNG COMPRESSED IMAGE
		else
		{
			// Hold compression details
			ConfigHeader compressionConfig;
			memcpy(&compressionConfig, data, sizeof(compressionConfig));

			// Set quantization parameters
			float depthQuantA = compressionConfig.quantParams[0];
			float depthQuantB = compressionConfig.quantParams[1];

			// Create image data vector - all pixels after compression info data
			const std::vector<unsigned char> imageData(data + sizeof(compressionConfig), data + lenCompressed);

			// Decompress the image (still need to deal with quantization)
			cv::Mat decompressed = cv::imdecode(imageData, cv::IMREAD_UNCHANGED);

			// Create matrix to hold the result
			// CV_32FC1 for PNG compressed image
			cv::Mat decodedMat(height, width, CV_32FC1);

			/* Reconstruct the image from quantization */
			// Create iterators
			cv::MatIterator_<float> itDepthImg = decodedMat.begin<float>();
			cv::MatIterator_<float> itDepthImg_end = decodedMat.end<float>();
			cv::MatConstIterator_<unsigned short> itInvDepthImg = decompressed.begin<unsigned short>();
			cv::MatConstIterator_<unsigned short> itInvDepthImg_end = decompressed.end<unsigned short>();

			// Apply quantization parameters to fill the final matrix with the correct values
			for (; (itDepthImg != itDepthImg_end) && (itInvDepthImg != itInvDepthImg_end); ++itDepthImg, ++itInvDepthImg)
			{
				// Check for NaN and max depth
				if (*itInvDepthImg)
				{
					*itDepthImg = depthQuantA / ((float)*itInvDepthImg - depthQuantB);
				}
				else
				{
					*itDepthImg = std::numeric_limits<float>::quiet_NaN();
				}
			}

			// Copy the data into image output array
			memcpy(*image, decodedMat.data, len);
		}
	}
}
