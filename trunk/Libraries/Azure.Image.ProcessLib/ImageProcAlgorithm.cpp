#include "stdafx.h"
#include "ImageProcAlgorithm.h"
#include <math.h>
#include "ImageProcess.h"

ProcessParam gProcessParam;

extern HANDLE ghStartEventProc1 ;
extern HANDLE ghStartEventProc2 ;
extern HANDLE ghStartEventProc3 ;
extern HANDLE ghFinishEventProc1 ;
extern HANDLE ghFinishEventProc2 ;
extern HANDLE ghFinishEventProc3 ;
extern BYTE* gpBufProc1 ;
extern BYTE* gpBufProc2 ;
extern BYTE* gpBufProc3 ;
extern BYTE* gpSaturationData;
extern BYTE* gpBufDst ;
extern int giColorGradation;

int LUT_16BIT_8BIT_GAMMA_C3[65536] = { 0 };
BYTE LUT_16BIT_8BIT_GAMMA[65536] = { 0 };

inline double fastPow(double a, double b) {
  union {
    double d;
    int x[2];
  } u = { a };
  u.x[1] = (int)(b * (u.x[1] - 1072632447) + 1072632447);
  u.x[0] = 0;
  return u.d;
}

int MV_CreateLUT(BYTE* pLUT, double dMax, double dMin, int iLen, double dGamma, bool bIsInvert)
{
	if (pLUT == NULL || iLen == 0 || dGamma < 0.00001)
		return -1;
	for (int i = 0; i < iLen; ++i)
	{
		if (i <= dMin)
		{
			pLUT[i] = (bIsInvert) ? 255 : 0;
		}
		else if(i >= dMax)
		{
			pLUT[i] = (bIsInvert) ? 0 : 255;
		}
		else
		{
			if (bIsInvert)
			{
				pLUT[i] = 255 - (int)(255*pow((double)(i-dMin)/(dMax - dMin), 1.0/dGamma));
			}
			else
			{
				pLUT[i] = (int)(255*pow((double)(i-dMin)/(dMax - dMin), 1.0/dGamma));
				//pLUT[i] = (int)(255*fastPow((double)(i-dMin)/(dMax - dMin), 1.0/dGamma));
			}
			if (pLUT[i] > 255)
			{
				pLUT[i] = 255;
			}
			else if (pLUT[i] < 0)
			{
				pLUT[i] = 0;
			}
		}
	}
	return 0;
}
//int MV_CreateC3LUT(int* pLUT, double dMax, double dMin, int iLen, double dGamma)
//{
//	if (pLUT == NULL || iLen == 0 || dGamma < 0.00001)
//		return -1;
//	for (int i = 0; i < iLen; ++i)
//	{
//		if (i <= dMin){
//			pLUT[i] = 0;
//		}
//		else if(i >= dMax){
//			pLUT[i] = 255;
//		}
//		else{
//			pLUT[i] = (int)(255*pow((double)(i-dMin)/(dMax-dMin), 1.0/dGamma));
//		}
//	}
//	return 0;
//}
/*
输出超出最大值的点阵
*/
int MV_SaturationMask_16u8u_C1(PBYTE lSrcData, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData, int iDstStep, int iMax)
{
	ASSERT(lSrcData);
	ASSERT(lDstData);

	BYTE* dst = NULL;
	BYTE* src = NULL;
	for(int i = 0; i < iHeight;i++)
	{
		dst = lDstData+i*iDstStep;
		src = lSrcData+i*iSrcStep;
		for (int j = 0; j < iWidth; ++j)
		{
			if (((unsigned short*)src)[j] > iMax)
				dst[j] = 1;
			else 
				dst[j] = 0;
			
		}	
	}
	return 0;
}
/*
输出超出最大值的点阵
*/
int MV_SaturationMask_16u8u_C3(PBYTE lSrcData, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData, int iDstStep, int iMax)
{
	ASSERT(lSrcData);
	ASSERT(lDstData);

	BYTE* dst = NULL;
	BYTE* src = NULL;
	for(int i = 0; i < iHeight;i++)
	{
		dst = lDstData+i*iDstStep;
		src = lSrcData+i*iSrcStep;
		for (int j = 0; j < iWidth*3; j++)
		{
			if (((unsigned short*)src)[j] > iMax)
				dst[j] = 1;
			else 
				dst[j] = 0;
			
		}	
	}
	return 0;
}

//
// accept single channel image and return single channel too
int MV_Scale_16u8u_C1_SingleChannel(
	PBYTE lSrcData/*IN*/, 
	int iSrcType, 
	int iSrcStep, 
	int iWidth, 
	int iHeight, 
	int iMax, 
	int iMin, 
	double dGamma, 
	PBYTE lDstData/*OUT*/, 
	int iDstStep
	)
{
	ASSERT(lSrcData);
	ASSERT(lDstData);

	BYTE* dst = NULL;
	unsigned short* src = NULL;
	//int iWLen = 0;
	//int forLen = iHeight*iWidth;
	//int iMultTempDst = 0;
	//int iMultTempSrc = 0;
	//for(int i = 0; i < forLen;i++){
	//	dst = lDstData+iMultTempDst + iWLen;
	//	src = (unsigned short*)(lSrcData+iMultTempSrc) + iWLen;
	//	memcpy(dst, LUT_16BIT_8BIT_GAMMA + *src, 1);
	//	iWLen++;
	//	if (iWLen == gProcessParam.iWidth){
	//		iWLen = 0;
	//		//iHLen++;
	//		iMultTempDst = iMultTempDst + iDstStep;
	//		iMultTempSrc = iMultTempSrc + iSrcStep;
	//	}
	//}

	for (int i = 0; i < iHeight; i++)
	{
		dst = (BYTE*)(lDstData + (i * iDstStep));
		src = (unsigned short*)(lSrcData + (i * iSrcStep));

		for (int j = 0; j < iWidth; j++)
		{
			memcpy(dst++, LUT_16BIT_8BIT_GAMMA + *src++, 1);
		}
	}

	return 0;
}

int MV_Scale_8u_C1_SingleChannel(PBYTE lSrcData/*IN*/, 
								 int iSrcType, 
								 int iSrcStep, 
								 int iWidth, 
								 int iHeight, 
								 int iMax, 
								 int iMin, 
								 double dGamma, 
								 PBYTE lDstData/*OUT*/, 
								 int iDstStep)
{
	ASSERT(lSrcData);
	ASSERT(lDstData);

	BYTE* dst = NULL;
	BYTE* src = NULL;
	//int iWLen = 0;
	//int forLen = iHeight * iWidth;
	//int iMultTempDst = 0;
	//int iMultTempSrc = 0;

	//for(int i = 0; i < forLen; i++)
	//{
	//	dst = lDstData + iMultTempDst + iWLen;
	//	src = (lSrcData + iMultTempSrc) + iWLen;
	//	memcpy(dst, LUT_16BIT_8BIT_GAMMA + *src, 1);
	//	iWLen++;
	//	if (iWLen == gProcessParam.iWidth)
	//{
	//		iWLen = 0;
	//		iMultTempDst = iMultTempDst + iDstStep;
	//		iMultTempSrc = iMultTempSrc + iSrcStep;
	//	}
	//}

	for (int i = 0; i < iHeight; i++)
	{
		dst = (BYTE*)(lDstData + (i * iDstStep));
		src = (BYTE*)(lSrcData + (i * iSrcStep));

		for (int j = 0; j < iWidth; j++)
		{
			memcpy(dst++, LUT_16BIT_8BIT_GAMMA + *src++, 1);
		}
	}

	return 0;
}


int MV_Scale_16u8u_C1(PBYTE lSrcData/*IN*/,
	                  int iSrcType,
					  int iSrcStep,
					  int iWidth, int iHeight,
					  int iMax, int iMin,
					  double dGamma,
					  PBYTE lDstData/*OUT*/,
					  int iDstStep,
					  PBYTE lSaturationData,
					  int iSaturationStep,
					  bool bSaturation,
					  int iSaturatonValue)
{
	ASSERT(lSrcData);
	ASSERT(lDstData);

	double k = 255.0;
	BYTE* dst = NULL;
	BYTE* src = NULL;
	BYTE* saturation = NULL;
	int tmp = 0;
	unsigned short tmpValue;

	for(int i = 0; i < iHeight;i++)
	{
		dst = lDstData+i*iDstStep;
		src = lSrcData+i*iSrcStep;

		for (int j = 0; j < iWidth; ++j)
		{
			tmp = j * 3;

			if (iSrcType == P8uC1)
			{
				tmpValue = src[j];
			}
			else
			{
				tmpValue = ((unsigned short*)src)[j];
			}

			if (LUT_16BIT_8BIT_GAMMA[tmpValue] > 255)
			{
				dst[tmp] = 255;
				dst[tmp+1] = 255;
				dst[tmp+2] = 255;
			}
			else if(LUT_16BIT_8BIT_GAMMA[tmpValue] < 0)
			{
				dst[tmp] = 0;
				dst[tmp+1] = 0;
				dst[tmp+2] = 0;
			}
			else
			{
				dst[tmp] = LUT_16BIT_8BIT_GAMMA[tmpValue];
				dst[tmp+1] = LUT_16BIT_8BIT_GAMMA[tmpValue];
				dst[tmp+2] = LUT_16BIT_8BIT_GAMMA[tmpValue];
			}

			if (bSaturation)
			{
				saturation = lSaturationData + i * iSrcStep;

				if (iSrcType == P8uC1)
				{
					if (saturation[j] > iSaturatonValue)
					{
						dst[tmp] = 255;
						dst[tmp+1] = 0;
						dst[tmp+2] = 0;
					}
				}
				else
				{
					if (((unsigned short*)saturation)[j] > iSaturatonValue)
					{
						//填充红色 (255, 0, 0)
						dst[tmp] = 255;
						dst[tmp+1] = 0;
						dst[tmp+2] = 0;
					}
				}
			}
		}
	}
	return 0;
}

int MV_Scale_16u8u_C3(PBYTE lSrcData/*IN*/,
	                  int iSrcType,
					  int iSrcStep,
					  int iWidth, int iHeight,
					  int iMax, int iMin,
					  double dGamma,
					  PBYTE lDstData/*OUT*/,
					  int iDstStep,
					  PBYTE lSaturationData,
					  int iSaturationStep,
					  bool bSaturation,
					  int iSaturatonValue,
					  int iColorGradation)
{
	ASSERT(lSrcData);
	ASSERT(lDstData);
	
	gProcessParam.dGamma = dGamma;
	gProcessParam.iDstStep = iDstStep;
	gProcessParam.iSrcStep = iSrcStep;
	gProcessParam.iHeight = iHeight;
	gProcessParam.iWidth = iWidth;
	gProcessParam.iSaturatonValue = iSaturatonValue;
	gProcessParam.bSaturation = bSaturation;
	gProcessParam.iSaturatonStep = iSaturationStep;
	gProcessParam.iPixelType = iSrcType;

	gpBufProc1 = lSrcData;
	gpBufProc2 = lSrcData;
	gpBufProc3 = lSrcData;
	gpBufDst = lDstData;
	gpSaturationData = lSaturationData;

	giColorGradation = iColorGradation;

	SetEvent(ghStartEventProc1);
	SetEvent(ghStartEventProc2);
	SetEvent(ghStartEventProc3);

	WaitForSingleObject(ghFinishEventProc1, INFINITE);
	WaitForSingleObject(ghFinishEventProc2, INFINITE);
	WaitForSingleObject(ghFinishEventProc3, INFINITE);

	gpBufProc1 = NULL;
	gpBufProc2 = NULL;
	gpBufProc3 = NULL;
	gpBufDst = NULL;
	gpSaturationData = NULL;
	return 0;
}

int MV_Scale_16u8u_C1(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iMax, int iMin, double dGamma, PBYTE lDstData/*OUT*/, int iDstStep, int iColor)
{
	ASSERT(lSrcData);
	ASSERT(lDstData);

	double k = 255.0;
	BYTE* dst = NULL;
	BYTE* src = NULL;
	for(int i = 0; i < iHeight;i++){
		dst = lDstData+i*iDstStep;
		src = lSrcData+i*iSrcStep;
		for (int j = 0; j < iWidth; ++j){
			if (((unsigned short*)src)[j] > iMax){
				dst[j] = 0;
			}
			else if (((unsigned short*)src)[j] < iMin){
				dst[j] = 0;
			}
			else if (dGamma == 1.0){
				dst[j] = k*((((unsigned short*)src)[j])/65535.0);
			}
			else{
				dst[j] = k*pow(double((((unsigned short*)src)[j])/65535.0), 1.0/dGamma);
			}
		}	
	}
	return 0;
}
int MV_Scale_16u8u_C3(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iMax, int iMin, double dGamma, PBYTE lDstData/*OUT*/, int iDstStep, int iIsSaturation)
{
	ASSERT(lSrcData);
	ASSERT(lDstData);

	double k = 255.0;
	BYTE* dst = NULL;
	BYTE* src = NULL;
	for(int i = 0; i < iHeight;i++)
	{
		dst = lDstData+i*iDstStep;
		src = lSrcData+i*iSrcStep;
		for (int j = 0; j < iWidth; j++)
		{
			if (((unsigned short*)src)[j*3] > iMax || ((unsigned short*)src)[j*3+1] > iMax || ((unsigned short*)src)[j*3+2] > iMax)
			{
				if (iIsSaturation == 1.0)
				{
					dst[j*3] = 255;
					dst[j*3+1] = 255;
					dst[j*3+2] = 0;
				}
				else
				{
					dst[j*3] = 255;
					dst[j*3+1] = 255;
					dst[j*3+2] = 255;
				}
				
			}
			else if (((unsigned short*)src)[j*3] <= iMin && ((unsigned short*)src)[j*3+1] <= iMin && ((unsigned short*)src)[j*3+2] <= iMin)
			{
				dst[j*3] = dst[j*3+1] = dst[j*3+2] = 0;
			}
			else  if (dGamma == 1.0)
			{
				dst[j*3] = k*(((unsigned short*)src)[j*3]/65535.0);
				dst[j*3+1] = k*(((unsigned short*)src)[j*3+1]/65535.0);
				dst[j*3+2] = k*(((unsigned short*)src)[j*3+2]/65535.0);
			}
			else
			{
				dst[j*3] = k*pow((((unsigned short*)src)[j*3]/65535.0), 1.0/dGamma);
				dst[j*3+1] = k*pow((((unsigned short*)src)[j*3+1]/65535.0), 1.0/dGamma);
				dst[j*3+2] = k*pow((((unsigned short*)src)[j*3+2]/65535.0), 1.0/dGamma);
			}
		}	
	}
	return 0;
}

int MV_GetPixel_8u_C1(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iXPos, int iYPos, int* iValue/*OUT*/)
{
	ASSERT(lSrcData);

	int iPos = iSrcStep*iYPos + iXPos;
	iValue[0] = lSrcData[iPos];
	return 0;
}
int MV_SetPixel_8u_C1(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iXPos, int iYPos, int* iValue/*IN*/)
{
	ASSERT(lSrcData);

	int iPos = iSrcStep*iYPos + iXPos;
	lSrcData[iPos] = iValue[0] ;
	return 0;
}

int MV_GetPixel_8u_C3(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iXPos, int iYPos, int* iValue/*OUT*/)
{
	ASSERT(lSrcData);

	int iPos = iSrcStep*iYPos + iXPos*3;
	iValue[2] = lSrcData[iPos];
	iValue[1] = lSrcData[iPos+1];
	iValue[0] = lSrcData[iPos+2];
	return 0;
}
int MV_SetPixel_8u_C3(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iXPos, int iYPos, int* iValue/*IN*/)
{
	ASSERT(lSrcData);

	int iPos = iSrcStep*iYPos + iXPos*3;
	lSrcData[iPos] = iValue[0] ;
	lSrcData[iPos+1] = iValue[1] ;
	lSrcData[iPos+2] = iValue[2] ;
	return 0;
}

int MV_GetPixel_16u_C1(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iXPos, int iYPos, int* iValue/*OUT*/)
{
	ASSERT(lSrcData);

	int iPos = (iSrcStep/2)*iYPos + iXPos;
	iValue[0] = ((unsigned short*)lSrcData)[iPos];
	return 0;
}
int MV_SetPixel_16u_C1(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iXPos, int iYPos, int* iValue/*IN*/)
{
	ASSERT(lSrcData);

	int iPos = (iSrcStep/2)*iYPos + iXPos;
	((unsigned short*)lSrcData)[iPos] = iValue[0];
	return 0;
}
int MV_GetPixel_16u_C3(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iXPos, int iYPos, int* iValue/*OUT*/)
{
	ASSERT(lSrcData);

	int iPos = (iSrcStep/2)*iYPos + iXPos*3;
	iValue[0] = ((unsigned short*)lSrcData)[iPos];
	iValue[1] = ((unsigned short*)lSrcData)[iPos+1];
	iValue[2] = ((unsigned short*)lSrcData)[iPos+2];
	return 0;
}
int MV_SetPixel_16u_C3(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iXPos, int iYPos, int* iValue/*IN*/)
{
	ASSERT(lSrcData);

	int iPos = (iSrcStep/2)*iYPos + iXPos*3;
	((unsigned short*)lSrcData)[iPos] = iValue[0];
	((unsigned short*)lSrcData)[iPos+1] = iValue[1];
	((unsigned short*)lSrcData)[iPos+2] = iValue[2];
	return 0;
}

int MV_Despeckle_8u_C3(PBYTE lSrcData/*IN*/, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData/*OUT*/, int iDstStep)
{	
	ASSERT(lSrcData);
	ASSERT(lDstData);

	BYTE* dst = NULL;
	BYTE* src = NULL;
	for(int i = 0; i < iHeight;i++)
	{
		dst = lDstData+i*iDstStep;
		src = lSrcData+i*iSrcStep;
		for (int j = 0; j < iWidth*3; j++)
		{
			if (dst[j] > src[j]*2 || dst[j]*2 < src[j])
			{
				//OutputDebugString(_T("噪点"));
			}
			else
			{
				dst[j] = src[j];
			}
		}
	}

	return 0;
}
int MV_Despeckle_8u_C1(PBYTE lSrcData/*IN*/, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData/*OUT*/, int iDstStep)
{
	ASSERT(lSrcData);
	ASSERT(lDstData);

	BYTE* dst = NULL;
	BYTE* src = NULL;
	for(int i = 0; i < iHeight;i++)
	{
		dst = lDstData+i*iDstStep;
		src = lSrcData+i*iSrcStep;
		for (int j = 0; j < iWidth; j++)
		{
			if (dst[j] > src[j]*2 || dst[j]*2 < src[j])
			{
				//OutputDebugString(_T("噪点"));
			}
			else
			{
				dst[j] = src[j];
			}
		}
	}

	return 0;
}

int MV_Despeckle_16u_C3(PBYTE lSrcData/*IN*/, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData/*OUT*/, int iDstStep)
{
	ASSERT(lSrcData);
	ASSERT(lDstData);

	BYTE* dst = NULL;
	BYTE* src = NULL;
	for(int i = 0; i < iHeight;i++)
	{
		dst = lDstData+i*iDstStep;
		src = lSrcData+i*iSrcStep;
		for (int j = 0; j < iWidth*3; j++)
		{
			if (((unsigned short*)dst)[j] > ((unsigned short*)src)[j]*2 || ((unsigned short*)dst)[j]*2 < ((unsigned short*)src)[j])
			{
				//OutputDebugString(_T("噪点"));
			}
			else
			{
				((unsigned short*)dst)[j] = ((unsigned short*)src)[j];
			}
		}
	}

	return 0;

}
int MV_Despeckle_16u_C1(PBYTE lSrcData/*IN*/, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData/*OUT*/, int iDstStep)
{
	ASSERT(lSrcData);
	ASSERT(lDstData);

	BYTE* dst = NULL;
	BYTE* src = NULL;
	for(int i = 0; i < iHeight;i++)
	{
		dst = lDstData+i*iDstStep;
		src = lSrcData+i*iSrcStep;
		for (int j = 0; j < iWidth; j++)
		{
			if (((unsigned short*)dst)[j] > ((unsigned short*)src)[j]*2 || ((unsigned short*)dst)[j]*2 < ((unsigned short*)src)[j])
			{
				//OutputDebugString(_T("噪点"));
			}
			else
			{
				((unsigned short*)dst)[j] = ((unsigned short*)src)[j];
			}
		}
	}

	return 0;
}

int MV_Histogram_16u_C1(PBYTE lSrcData/*IN*/, int iWidth, int iHeight, int iSrcStep, int* pHist)
{
	//int iPos = 0;
	BYTE* pSrcBuffer = (BYTE*)lSrcData;

	for(int i =0 ; i < iHeight; ++i)
	{
		unsigned short* pSrc = (unsigned short*)pSrcBuffer + (i * iSrcStep / 2);

		for(int j = 0; j < iWidth; ++j)
		{
			pHist[pSrc[j]]++;
		}
	}
	return 0;
}

int MV_Histogram_16u_C1(PBYTE lSrcData/*IN*/, int iWidth, int iHeight, int* pHist)
{
	int iPos = 0;
	for(int i =0 ; i <iHeight; ++i)
	{
		for(int j = 0; j < iWidth; ++j)
		{
			iPos = i*iWidth+j;
			pHist[((const unsigned short*)lSrcData)[iPos]]++;
		}
	}
	return 0;
}
int MV_Histogram_16u_C3(PBYTE lSrcData/*IN*/, int iWidth, int iHeight, int* pHistR, int* pHistG, int* pHistB)
{
	int iPos = 0;
	for(int i =0 ; i <iHeight; ++i)
	{
		for(int j = 0; j < iWidth; ++j)
		{
			iPos = 3*i*iWidth+3*j;
			pHistR[((const unsigned short*)lSrcData)[iPos]]++;
			pHistG[((const unsigned short*)lSrcData)[iPos+1]]++;
			pHistB[((const unsigned short*)lSrcData)[iPos+2]]++;
		}
	}
	return 0;
}
int MV_Histogram_16u_C3(PBYTE lSrcData/*IN*/, int iWidth, int iHeight, int* pHist)
{
	int iPos = 0;
	unsigned short iValue = 0;
	for(int i =0 ; i <iHeight; ++i)
	{
		for(int j = 0; j < iWidth; ++j)
		{
			iPos = 3*i*iWidth+3*j;
			iValue = max(((const unsigned short*)lSrcData)[iPos], ((const unsigned short*)lSrcData)[iPos+1]);
			iValue = max(iValue, ((const unsigned short*)lSrcData)[iPos+2]);
			//0值太多，所以不统计0
			//if (iValue == 0)
			//	continue;
			pHist[iValue]++;
		}
	}
	return 0;
}

int MV_Histogram_8u_C3(PBYTE lSrcData/*IN*/, int iWidth, int iHeight, int* pHist)
{
	int iPos = 0;
	unsigned short iValue = 0;

	for(int i = 0; i < iHeight; ++i)
	{
		for(int j = 0; j < iWidth; ++j)
		{
			iPos = 3 * i * iWidth + 3 * j;
			iValue = max(lSrcData[iPos], lSrcData[iPos + 1]);
			iValue = max(iValue, lSrcData[iPos + 2]);
			pHist[iValue]++;
		}
	}

	return 0;
}

int MV_Histogram_8u_C1(PBYTE lSrcData/*IN*/, int iWidth, int iHeight, int* pHist)
{
	int iPos = 0;
	unsigned short iValue = 0;

	for(int i = 0; i < iHeight; ++i)
	{
		for(int j = 0; j < iWidth; ++j)
		{
			iPos = i * iWidth + j;
			iValue = max(lSrcData[iPos], lSrcData[iPos + 1]);
			iValue = max(iValue, lSrcData[iPos + 2]);
			pHist[iValue]++;
		}
	}

	return 0;
}


int MV_TextMask_8u_C3(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lSrcDstData/*OUT*/, int iMaskStep)
{
	ASSERT(lSrcData);
	ASSERT(lSrcDstData);

	BYTE* dst = NULL;
	BYTE* src = NULL;
	int posSrc, posMask;
	for(int i = 0; i < iHeight;i++)
	{
		for (int j = 0; j < iWidth; j++)
		{
			posSrc = i*iSrcStep + j*3;
			posMask = i*iMaskStep + j*4;
			if (lSrcDstData[posMask+3] != 0)
			{
				lSrcData[posSrc] = lSrcDstData[posMask];
				lSrcData[posSrc+1] = lSrcDstData[posMask+1];
				lSrcData[posSrc+2] = lSrcDstData[posMask+2];
			}
		}
	}

	return 0;
}
int MV_TextMask_8u_C1(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lSrcDstData/*OUT*/, int iMaskStep)
{
	ASSERT(lSrcData);
	ASSERT(lSrcDstData);

	BYTE* dst = NULL;
	BYTE* src = NULL;
	int posSrc, posMask;
	for(int i = 0; i < iHeight;i++)
	{
		for (int j = 0; j < iWidth; j++)
		{
			posSrc = i*iSrcStep + j;
			posMask = i*iMaskStep + j*4;
			if (lSrcDstData[posMask+3] != 0)
			{
				lSrcData[posSrc] = lSrcDstData[posMask];
			}
		}
	}

	return 0;
}

int MV_TextMask_16u_C3(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lSrcDstData/*OUT*/, int iMaskStep)
{
	ASSERT(lSrcData);
	ASSERT(lSrcDstData);

	BYTE* dst = NULL;
	BYTE* src = NULL;
	int posSrc, posMask;
	for(int i = 0; i < iHeight;i++)
	{
		for (int j = 0; j < iWidth; j++)
		{
			posSrc = i*iSrcStep + j*3*2; //16bit
			posMask = i*iMaskStep + j*4; //8bit
			if (lSrcDstData[posMask+3] != 0)
			{
				((unsigned short*)lSrcData)[posSrc+2] = lSrcDstData[posMask]*257;
				((unsigned short*)lSrcData)[posSrc+1] = lSrcDstData[posMask+1]*257;
				((unsigned short*)lSrcData)[posSrc] = lSrcDstData[posMask+2]*257;
			}
		}
	}

	return 0;
}
int MV_TextMask_16u_C1(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lSrcDstData/*OUT*/, int iMaskStep)
{
	ASSERT(lSrcData);
	ASSERT(lSrcDstData);

	BYTE* dst = NULL;
	BYTE* src = NULL;
	int posSrc, posMask;
	for(int i = 0; i < iHeight;i++)
	{
		for (int j = 0; j < iWidth; j++)
		{
			posSrc = i*iSrcStep + j*2; //16bit
			posMask = i*iMaskStep + j*4; //8bit
			if (lSrcDstData[posMask+3] != 0)
			{
				((unsigned short*)lSrcData)[posSrc] = lSrcDstData[posMask]*257;
			}
		}
	}

	return 0;
}


int MV_Animation_8u_C3(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lMaskData/*OUT*/, int iMaskStep, int* pRect, PBYTE lDstData )
{
	ASSERT(lSrcData);
	ASSERT(lMaskData);
	ASSERT(lDstData);

	memcpy(lDstData, lSrcData, iHeight*iSrcStep);

	BYTE* dst = NULL;
	BYTE* msk = NULL;
	int iMskIndexI = 0;
	int iMskIndexJ = 0;
	for(int i = pRect[1]; i < pRect[1]+pRect[3];i++)
	{
		iMskIndexI = i - pRect[1];
		dst = lDstData+i*iSrcStep;
		msk = lMaskData+iMskIndexI*iMaskStep;
		for (int j = pRect[0]; j < pRect[0]+pRect[2]; j++)
		{
			iMskIndexJ = (j - pRect[0])*3;
			if (msk[iMskIndexJ] != 0 || msk[iMskIndexJ+1] != 0 || msk[iMskIndexJ+2] != 0)
			{
				dst[j*3] = msk[iMskIndexJ];
				dst[j*3+1] = msk[iMskIndexJ+1];
				dst[j*3+2] = msk[iMskIndexJ+2];
			}
		}
	}
	return 0;
}
int MV_Animation_8u_C1(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lMaskData/*OUT*/, int iMaskStep, int* pRect, PBYTE lDstData )
{
	ASSERT(lSrcData);
	ASSERT(lMaskData);
	ASSERT(lDstData);

	memcpy(lDstData, lSrcData, iHeight*iSrcStep);

	BYTE* dst = NULL;
	BYTE* msk = NULL;
	for(int i = pRect[1]; i < pRect[1]+pRect[3];i++)
	{
		dst = lDstData+i*iSrcStep;
		msk = lMaskData+(i-pRect[1])*iMaskStep;
		for (int j = pRect[0]; j < pRect[0]+pRect[2]; j++)
		{
			if (msk[j-pRect[0]] != 0)
			{
				dst[j] = msk[j-pRect[0]];
			}
		}
	}
	return 0;
}

int MV_Animation_16u_C3(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lMaskData/*OUT*/, int iMaskStep, int* pRect, PBYTE lDstData )
{
	ASSERT(lSrcData);
	ASSERT(lMaskData);
	ASSERT(lDstData);

	memcpy(lDstData, lSrcData, iHeight*iSrcStep);

	BYTE* dst = NULL;
	BYTE* msk = NULL;
	int iMskIndexI = 0;
	int iMskIndexJ = 0;
	for(int i = pRect[1]; i < pRect[1]+pRect[3];i++)
	{
		iMskIndexI = i-pRect[1];
		dst = lDstData+i*iSrcStep;
		msk = lMaskData+iMskIndexI*iMaskStep;
		for (int j = pRect[0]; j < pRect[0]+pRect[2]; j++)
		{
			iMskIndexJ = (j-pRect[0])*3;
			if (msk[iMskIndexJ+2] != 0 || msk[iMskIndexJ+1] != 0 || msk[iMskIndexJ] != 0)
			{
				((unsigned short*)dst)[j*3+2] = msk[iMskIndexJ]*257;
				((unsigned short*)dst)[j*3+1] = msk[iMskIndexJ+1]*257;
				((unsigned short*)dst)[j*3] = msk[iMskIndexJ+2]*257;
			}
		}
	}
	return 0;
}
int MV_Animation_16u_C1(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lMaskData/*OUT*/, int iMaskStep, int* pRect, PBYTE lDstData )
{
	ASSERT(lSrcData);
	ASSERT(lMaskData);
	ASSERT(lDstData);

	memcpy(lDstData, lSrcData, iHeight*iSrcStep);

	BYTE* dst = NULL;
	BYTE* msk = NULL;
	for(int i = pRect[1]; i < pRect[1]+pRect[3];i++)
	{
		dst = lDstData+i*iSrcStep;
		msk = lMaskData+(i-pRect[1])*iMaskStep;
		for (int j = pRect[0]; j < pRect[0]+pRect[2]; j++)
		{
			if (msk[j-pRect[0]] != 0)
			{
				((unsigned short*)dst)[j] = msk[j-pRect[0]]*257;
			}
		}
	}

	return 0;
}

