#ifndef  __ABIALGORITHM__
#define  __ABIALGORITHM__

struct ProcessParam
{
	int iWidth;
	int iHeight;
	int iSrcStep;
	int iDstStep;
	int iSaturatonValue;
	int iSaturatonStep;
	double dGamma;
	BOOL bSaturation;
	int iPixelType;
};

int MV_CreateC3LUT(int* pLUT, double dMax, double dMin, int iLen, double dGamma);
int MV_CreateLUT(BYTE* pLUT, double dMax, double dMin,int iLen, double dGamma, bool bIsInvert);
int MV_SaturationMask_16u8u_C1(PBYTE lSrcData, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData, int iDstStep, int iMax);
int MV_SaturationMask_16u8u_C3(PBYTE lSrcData, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData, int iDstStep, int iMax);

int MV_Scale_8u_C1_SingleChannel(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iMax, int iMin, double iGamma, PBYTE lDstData/*OUT*/, int iDstStep);

int MV_Scale_16u8u_C1_SingleChannel(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iMax, int iMin, double iGamma, PBYTE lDstData/*OUT*/, int iDstStep);
int MV_Scale_16u8u_C1(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iMax, int iMin, double iGamma, PBYTE lDstData/*OUT*/, int iDstStep, PBYTE lSaturationData, int iSaturationStep, bool bSaturation, int iSaturatonValue);
int MV_Scale_16u8u_C3(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iMax, int iMin, double iGamma, PBYTE lDstData/*OUT*/, int iDstStep, PBYTE lSaturationData, int iSaturationStep,bool bSaturation, int iSaturatonValue, int iColorGradation);
int MV_Scale_16u8u_C1(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iMax, int iMin, double iGamma, PBYTE lDstData/*OUT*/, int iDstStep, int iThreshold);
int MV_Scale_16u8u_C3(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iMax, int iMin, double iGamma, PBYTE lDstData/*OUT*/, int iDstStep, int iThreshold);

int MV_GetPixel_8u_C1(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iXPos, int iYPos, int* iValue/*OUT*/);
int MV_SetPixel_8u_C1(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iXPos, int iYPos, int* iValue/*OUT*/);
int MV_GetPixel_8u_C3(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iXPos, int iYPos, int* iValue/*OUT*/);
int MV_SetPixel_8u_C3(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iXPos, int iYPos, int* iValue/*OUT*/);
int MV_GetPixel_16u_C1(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iXPos, int iYPos, int* iValue/*OUT*/);
int MV_SetPixel_16u_C1(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iXPos, int iYPos, int* iValue/*OUT*/);
int MV_GetPixel_16u_C3(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iXPos, int iYPos, int* iValue/*OUT*/);
int MV_SetPixel_16u_C3(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iXPos, int iYPos, int* iValue/*OUT*/);

int MV_Despeckle_8u_C3(PBYTE lSrcData/*IN*/, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData/*OUT*/, int iDstStep);
int MV_Despeckle_8u_C1(PBYTE lSrcData/*IN*/, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData/*OUT*/, int iDstStep);
int MV_Despeckle_16u_C3(PBYTE lSrcData/*IN*/, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData/*OUT*/, int iDstStep);
int MV_Despeckle_16u_C1(PBYTE lSrcData/*IN*/, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData/*OUT*/, int iDstStep);

int MV_Histogram_16u_C1(PBYTE lSrcData/*IN*/, int iWidth, int iHeight, int* pHist);
int MV_Histogram_16u_C1(PBYTE lSrcData/*IN*/, int iWidth, int iHeight, int iSrcStep, int* pHist);
int MV_Histogram_16u_C3(PBYTE lSrcData/*IN*/, int iWidth, int iHeight, int* pHistR, int* pHistG, int* pHistB);
int MV_Histogram_16u_C3(PBYTE lSrcData/*IN*/, int iWidth, int iHeight, int* pHist);

int MV_Histogram_8u_C3(PBYTE lSrcData/*IN*/, int iWidth, int iHeight, int* pHist);
int MV_Histogram_8u_C1(PBYTE lSrcData/*IN*/, int iWidth, int iHeight, int* pHist);

int MV_TextMask_8u_C3(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lSrcDstData/*OUT*/, int iMaskStep);
int MV_TextMask_8u_C1(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lSrcDstData/*OUT*/, int iMaskStep);
int MV_TextMask_16u_C3(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lSrcDstData/*OUT*/, int iMaskStep);
int MV_TextMask_16u_C1(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lSrcDstData/*OUT*/, int iMaskStep);

int MV_Animation_8u_C3(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lMaskData/*OUT*/, int iMaskStep, int* pRect, PBYTE lDstData );
int MV_Animation_8u_C1(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lMaskData/*OUT*/, int iMaskStep, int* pRect, PBYTE lDstData );
int MV_Animation_16u_C3(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lMaskData/*OUT*/, int iMaskStep, int* pRect, PBYTE lDstData );
int MV_Animation_16u_C1(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lMaskData/*OUT*/, int iMaskStep, int* pRect, PBYTE lDstData );
#endif   //__ABIALGORITHM__