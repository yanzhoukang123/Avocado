#ifndef  __ABIIMAGEPROC__
#define  __ABIIMAGEPROC__

#ifdef __cplusplus
#define ABIAPI extern "C" __declspec (dllexport)
#else
#define ABIAPI __declspec (dllexport)
#endif

#define P8uC1	0x01
#define P16uC1	0x02
#define P8uC3	0x03
#define P16uC3	0x04
#define P8uC3_1	0x05

#define FlipHorizontal	0x05
#define FlipVertical	0x06

BOOL SaveImgRGB(CString lFileName, PBYTE lBufPtr, int lWidth, int lHeight, BOOL lReverse);
//ABIAPI DWORD WINAPI MVC_BUF_Resize(PBYTE lSrcData, PBYTE lDstData ,int lDepth, int lWidth, int lHeight, int lReverse , double lRatioX, double lRatioY);
ABIAPI DWORD WINAPI MVC_IMG_ToByte(PBYTE lDstData, PCHAR lSrcPath);
ABIAPI DWORD WINAPI MVC_IMG_ToGray(PBYTE lDstData, PCHAR lSrcPath);
ABIAPI DWORD WINAPI MVC_IMG_Init(PCHAR lSrcPath, PBYTE lDstData, int *lDepth, int *lWidth, int *lHeight, int *lReverse);
ABIAPI DWORD WINAPI MVC_BUF_ToGray(PBYTE lSrcData, PBYTE lDstData, int lDepth, int lWidth, int lHeight, int lReverse);

/**
RGB2Gray ：灰度转换
flip : 翻转
********************
lSrcData : 输入图像数组
iSrcType ：输入图像类型
iSrcStep ：源图像一行字节数
iWidth   ：图像宽
iHeight  ：图像高
lDstData ：目的图像数组
iDstStep ：目的 图像一行字节数
********************
error return ippStsNoErr; no error return !ippStsNoErr
**/
ABIAPI DWORD WINAPI MVC_BUF_RGB2Gray(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData/*OUT*/, int iDstStep);
ABIAPI DWORD WINAPI MVC_BUF_Flip(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData/*OUT*/, int iDstStep, int iFlipMode);
/**
Rotate : 图像旋转
********************
lSrcData : 输入图像数组
iSrcType ：输入图像类型
iSrcStep ：源图像一行字节数
iWidth   ：图像宽
iHeight  ：图像高
lDstData ：目的图像数组
iDstStep ：目的 图像一行字节数
dAngle   : 角度大小
********************
error return ippStsNoErr; no error return !ippStsNoErr
**/
ABIAPI DWORD WINAPI MVC_BUF_Rotate(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData/*OUT*/, int iDstWidth, int iDstHeight, int iDstStep, double dAngle);
/**
RGBCompose : 颜色合并
********************
lSrcDataR : 输入图像R通道
lSrcDataG : 输入图像G通道
lSrcDataB : 输入图像B通道
iSrcType ：输入图像类型
iSrcStep ：源图像一行字节数
iWidth   ：图像宽
iHeight  ：图像高
lDstData ：目的图像数组
iDstStep ：目的 图像一行字节数
********************
error return ippStsNoErr; no error return !ippStsNoErr
**/
ABIAPI DWORD WINAPI MVC_BUF_RGBCompose(PBYTE lSrcDataR/*IN*/, PBYTE lSrcDataG/*IN*/, PBYTE lSrcDataB/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData/*out*/, int iDstStep);

/**
RGBExtract : 颜色提取
********************
lSrcData : 输入图像数组
iSrcType ：输入图像类型
iSrcStep ：源图像一行字节数
iWidth   ：图像宽
iHeight  ：图像高
lDstData ：目的图像数组
iDstStep ：目的 图像一行字节数
********************
error return ippStsNoErr; no error return !ippStsNoErr
**/
ABIAPI DWORD WINAPI MVC_BUF_RGBExtract(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstDataR/*OUT*/, 
	PBYTE lDstDataG/*OUT*/, PBYTE lDstDataB/*OUT*/, int iDstStep);
ABIAPI DWORD WINAPI MVC_BUF_RGBExtractSingleChannel(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstDataR/*OUT*/, 
	PBYTE lDstDataG/*OUT*/, PBYTE lDstDataB/*OUT*/, int iDstStep);

/**
Resize : 图像缩放
********************
lSrcData : 输入图像数组
iSrcType ：输入图像类型
iSrcStep ：源图像一行字节数
iWidth   ：图像宽
iHeight  ：图像高
lDstData ：目的图像数组
iDstStep ：目的 图像一行字节数
dAngle   : 角度大小
********************
error return ippStsNoErr; no error return !ippStsNoErr
**/
ABIAPI DWORD WINAPI MVC_BUF_Resize(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData/*OUT*/, int iDstStep, double dXFactor, double dYFactor);

//ABIAPI DWORD WINAPI MVC_BUF_CreateGammaC3LUT(double dMax, double dMin, double dGamma);
ABIAPI DWORD WINAPI MVC_BUF_CreateGammaLUT(double dMax, double dMin, double dGamma, bool bIsInvert);
/**
Scale : 位深变换 只支持16u转8u
********************
lSrcData : 输入图像数组
iSrcType ：输入图像类型
iSrcStep ：源图像一行字节数
iWidth   ：图像宽
iHeight  ：图像高
iMax     : 原图像最大值
iMin     ：原图像最小值
lDstData ：目的图像数组
iDstStep ：目的 图像一行字节数
iColorGradation : 0 : R层 ， 1 ： G 层 ， 2 ： B 层， 3 ： 所有
********************
error return ippStsNoErr; no error return !ippStsNoErr
**/
ABIAPI DWORD WINAPI MVC_BUF_Scale(
	PBYTE lSrcData/*IN*/,
	int iSrcType,
	int iSrcStep,
	int iWidth,
	int iHeight,
	int iMax,
	int iMin,
	double iGamma,
	PBYTE lDstData/*OUT*/,
	int iDstStep,
	PBYTE lSaturationData,
	int iSaturationStep,
	bool bSaturation,
	int iSaturatonValue,
	int iColorGradation,
	bool bIsInvert);
ABIAPI DWORD WINAPI MVC_BUF_ScaleSingleChannel(
	PBYTE lSrcData/*IN*/, 
	int iSrcType, 
	int iSrcStep, 
	int iWidth, 
	int iHeight, 
	int iMax, 
	int iMin, 
	double iGamma,  
	PBYTE lDstData/*OUT*/, 
	int iDstStep,
	bool bIsInvert
	);
/**
AutoScale : 位深变换 只支持16u转8u
********************
lSrcData : 输入图像数组
iSrcType ：输入图像类型
iSrcStep ：源图像一行字节数
iWidth   ：图像宽
iHeight  ：图像高
lDstData ：目的图像数组
iDstStep ：目的 图像一行字节数
********************
error return ippStsNoErr; no error return !ippStsNoErr
**/
ABIAPI DWORD WINAPI MVC_BUF_AutoScale(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData/*OUT*/, int iDstStep, int* iMaxValue, int* iMinValue, PBYTE lSaturationData, int iSaturationStep, bool bSaturation, int iSaturatonValue, int iColorGradation);

ABIAPI DWORD WINAPI WINAPI MVC_BUF_GetAutoScaleValues(
	PBYTE lSrcData/*IN*/, 
	int iSrcType, 
	int iSrcStep, 
	int iWidth, 
	int iHeight, 
	int* iMaxValue, 
	int* iMinValue
	);

/**
SaturationScale : 位深变换 只支持16u转8u
********************
lSrcData : 输入图像数组
iSrcType ：输入图像类型
iSrcStep ：源图像一行字节数
iWidth   ：图像宽
iHeight  ：图像高
lDstData ：目的图像数组
iDstStep ：目的 图像一行字节数
********************
error return ippStsNoErr; no error return !ippStsNoErr
**/
ABIAPI DWORD WINAPI MVC_BUF_SaturationScale(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iMax, int iMin, double iGamma, PBYTE lDstData/*OUT*/, int iDstStep, int iColor);
ABIAPI DWORD WINAPI MVC_BUF_SaturationMask(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData/*OUT*/, int iDstStep, int iMax);
/**
Max : 取当前图像最大值
********************
lSrcData : 输入图像数组
iSrcType ：输入图像类型
iSrcStep ：源图像一行字节数
iWidth   ：图像宽
iHeight  ：图像高
iMaxValue: 像素最大值
********************
error return ippStsNoErr; no error return !ippStsNoErr
**/
ABIAPI DWORD WINAPI MVC_BUF_Max(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, unsigned short* iMaxValue/*OUT*/);

/**
Scale : 位深变换 只支持16u转8u
********************
lSrcData : 输入图像数组
iSrcType ：输入图像类型
iSrcStep ：源图像一行字节数
iWidth   ：图像宽
iHeight  ：图像高
iMax     : 原图像最大值
iMin     ：原图像最小值
lDstData ：目的图像数组
iDstStep ：目的 图像一行字节数
********************
error return ippStsNoErr; no error return !ippStsNoErr
**/
ABIAPI DWORD WINAPI MVC_BUF_ScaleSaturation(
	PBYTE lSrcData/*IN*/,
	int iSrcType,
	int iSrcStep,
	int iWidth,
	int iHeight,
	int iMax,
	int iMin,
	double iGamma,
	PBYTE lDstData/*OUT*/,
	int iDstStep,
	int *pColors);


/**
Min : 取当前图像最小值
********************
lSrcData : 输入图像数组
iSrcType ：输入图像类型
iSrcStep ：源图像一行字节数
iWidth   ：图像宽
iHeight  ：图像高
iMinValue: 像素最大值
********************
error return ippStsNoErr; no error return !ippStsNoErr
**/
ABIAPI DWORD WINAPI MVC_BUF_Min(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, unsigned short* iMinValue/*OUT*/);

/**
GetPixel : 取当前图像值
********************
lSrcData : 输入图像数组
iSrcType ：输入图像类型
iSrcStep ：源图像一行字节数
iWidth   ：图像宽
iHeight  ：图像高
iXPos, iYPos ：像素位置
iValue   : 像素值
**/
ABIAPI DWORD WINAPI MVC_BUF_GetPixel(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iXPos, int iYPos, int* iValue/*OUT*/);

/**
SetPixel : 设置当前图像值
********************
lSrcData : 输入图像数组
iSrcType ：输入图像类型
iSrcStep ：源图像一行字节数
iWidth   ：图像宽
iHeight  ：图像高
iXPos, iYPos ：像素位置
iValue   : 像素值
**/
ABIAPI DWORD WINAPI MVC_BUF_SetPixel(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iXPos, int iYPos, int* iValue/*IN*/);

/**
Invert	 : 黑白转换
********************
lSrcData : 输入图像数组
iSrcType ：输入图像类型
iSrcStep ：源图像一行字节数
iWidth   ：图像宽
iHeight  ：图像高
lDstData ：目的图像数组
********************
error return ippStsNoErr; no error return !ippStsNoErr
**/
ABIAPI DWORD WINAPI MVC_BUF_Invert(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData/*OUT*/);

/**
Despeckle: 去噪滤波
********************
lSrcData : 输入图像数组
iSrcType ：输入图像类型
iSrcStep ：源图像一行字节数
iWidth   ：图像宽
iHeight  ：图像高
lDstData ：目的图像数组
iDstStep : 源图像一行字节数
********************
error return ippStsNoErr; no error return !ippStsNoErr
**/
ABIAPI DWORD WINAPI MVC_BUF_Despeckle(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData/*OUT*/, int iDstStep);

ABIAPI DWORD WINAPI MVC_BUF_MedianFilter(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData/*OUT*/, int iDstStep);

/**
Crop     : 裁减
********************
lSrcData : 输入图像数组
iSrcType ：输入图像类型
iSrcStep ：源图像一行字节数
iWidth   ：图像宽
iHeight  ：图像高
lDstData ：目的图像数组
iDstStep : 源图像一行字节数
pRect    : 四个值的数组，分别为x, y, width, height
********************
error return ippStsNoErr; no error return !ippStsNoErr
**/
ABIAPI DWORD WINAPI MVC_BUF_Crop(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, PBYTE lDstData/*OUT*/, int iDstStep, int* pRect);

/**
Despeckle: 去噪滤波
********************
lSrcData : 输入图像数组
iSrcType ：输入图像类型
iSrcStep ：源图像一行字节数
iWidth   ：图像宽
iHeight  ：图像高
lDstData ：目的图像数组
iDstStep : 源图像一行字节数
********************
error return ippStsNoErr; no error return !ippStsNoErr
**/
ABIAPI DWORD WINAPI MVC_BUF_TextOverlay(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, TCHAR* pText, int iTextLen, int* pRect, PBYTE lDstData/*OUT*/);


ABIAPI DWORD WINAPI MVC_BUF_TextMask(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lSrcDstData/*OUT*/, int iMaskStep);

ABIAPI DWORD WINAPI MVC_BUF_Animation(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lMaskData/*OUT*/, int iMaskStep, int* pRect, PBYTE lDstData );

/**
ImageAdd : 两张图像加法操作 （两张图像格式相同）
********************
lSrcDataDst : 输入图像,也作为输出图像
lSrcData ：输入图像
iSrcType ：输入图像类型
iSrcStep ：源图像一行字节数
iWidth   ：图像宽
iHeight  ：图像高
********************
error return ippStsNoErr; no error return !ippStsNoErr
**/
ABIAPI DWORD WINAPI MVC_BUF_ImageAdd(PBYTE lSrcDataDst/*IN*/, PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight);
/**
MVC_BUF_ImageDivided : 两张图像加法操作 （两张图像格式相同）
********************
lSrcDstData : 输入图像,也作为输出图像
iSrcType ：输入图像类型
iSrcStep ：源图像一行字节数
iWidth   ：图像宽
iHeight  ：图像高
********************
error return ippStsNoErr; no error return !ippStsNoErr
**/

ABIAPI DWORD WINAPI MVC_BUF_ImageSubC(PBYTE lSrcDstData/*IN*//*OUT*/, int iValue, int iSrcType, int iSrcStep, int iWidth, int iHeight);

ABIAPI DWORD WINAPI MVC_BUF_ImageDivC(PBYTE lSrcDstData/*IN*//*OUT*/, int iDivisor, int iSrcType, int iSrcStep, int iWidth, int iHeight);

/**
MVC_BUF_ImageMulti : 两张图像加法操作 （两张图像格式相同）
********************
lSrcDstData : 输入图像,也作为输出图像
iSrcType ：输入图像类型
iSrcStep ：源图像一行字节数
iWidth   ：图像宽
iHeight  ：图像高
********************
error return ippStsNoErr; no error return !ippStsNoErr
**/
ABIAPI DWORD WINAPI MVC_BUF_ImageMulC(PBYTE lSrcDstData/*IN*//*OUT*/,int iMultiplier, int iSrcType, int iSrcStep, int iWidth, int iHeight);


/********************************************
 * Divided and Add algorithm
 * Src1/10 + Src2/10 = lDstData
 * lSrcData1, lSrcData2 is BYTE type
 * lDstData is Float Type
********************************************/
ABIAPI DWORD WINAPI MVC_BUF_AddDivided16uTo32f(PBYTE lSrcData1, int iSrcStep, float* lSrcData2,  float* lDstData, int iDstStep, int iWidth, int iHeight, int iDivisorConstant);

ABIAPI DWORD WINAPI MVC_BUF_Add16uTo32f(PBYTE srcData, int iSrcStep, float* dstData, int iDstStep, int iWidth, int iHeight);

ABIAPI DWORD WINAPI MVC_BUF_DivC_32f_C1R(float* srcImageBuf, int iDivisor, float* dstImageBuf, int iWidth, int iHeight);

/********************************************
 * convert 32f image to 8u image
********************************************/
ABIAPI DWORD WINAPI MVC_BUF_Scale32fTo16u(float* lSrcData, int iSrcStep, PBYTE lDstData, int iDstStep, int iWidth, int iHeight);


/********************************************
 * sub lSrcData2 from lSrcData1 to lDstData
 * lSrcData1 - lSrcData2 = lDstData
********************************************/
ABIAPI DWORD WINAPI MVC_BUF_Sub16u(PBYTE lSrc, PBYTE lSrcDst, int iSrcStep, int iDstStep, int iWidth, int iHeight);

ABIAPI DWORD WINAPI MVC_BUF_Histogram(PBYTE lSrc, int iSrcStep, int iSrcType, int iWidth, int iHeight, int** pHist, int** pLevels, int nLevels[3]);

ABIAPI DWORD WINAPI MVC_BUF_HistogramEven_16u_C1R(PBYTE lSrc, int iSrcStep, int iWidth, int iHeight, int* pHist, int* pLevels, int nLevels, int nLowerLevel, int nUpperLevel);

ABIAPI DWORD WINAPI MVC_BUF_HistogramEven(PBYTE pSrcData, int iSrcStep, int iSrcType, int iWidth, int iHeight, int** pHist, int** pLevels, int nLevels[3], int nLowerLevel[3], int nUpperLevel[3]);

ABIAPI DWORD WINAPI MVC_BUF_DivAndScale(PBYTE srcImageData, int iSrcStep, PBYTE oprImageData, int iOprStep, float* resImageData, int iDstStep, int iWidth, int iHeight, int iUpperLimit);

ABIAPI DWORD WINAPI MVC_BUF_DivAndScaleWithROI(PBYTE srcImageData, int iSrcStep, PBYTE oprImageData, int iOprStep, float* resImageData, int iDstStep, int iWidth, int iHeight, int iUpperLimit, int iOffset);

ABIAPI DWORD WINAPI MVC_BUF_CopyReplicateBorder(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData/*OUT*/, int iDstStep, int iBorderSize);

ABIAPI DWORD WINAPI MVC_BUF_CopyConstBorder(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData/*OUT*/, int iDstStep, int iBorderSize, int constVal);

ABIAPI DWORD WINAPI MVC_BUF_Convert8bppTo16bpp(PBYTE srcData, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE dstData, int iDstStep);

ABIAPI DWORD WINAPI MVC_BUF_MeanStdDev(PBYTE srcData, int iSrcType, int iSrcStep, int iWidth, int iHeight, double* pMean, double* pStdDev);
#endif