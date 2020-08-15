// image_decompress_opencv.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include <string>
#include <opencv2/core.hpp>
#include <opencv2/highgui.hpp>


extern "C"
{
	__declspec(dllexport) void processImage(unsigned char data[], int flag, int width, int height, int len, unsigned char** image)
	{
		cv::Mat frame(height, width, CV_8UC4, data);

		//frame = cv::imdecode(frame, cv::IMREAD_COLOR);
		cv::Mat decompressed = cv::imdecode(frame, flag);

		// Set image data
		// len should be calculated and set in Unity
		*image = (unsigned char*) malloc(len);

		memcpy(image, decompressed.data, len);
	}
}
