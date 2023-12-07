// ImageProcess.cpp : Defines the initialization routines for the DLL.
//

#include "stdafx.h"
#include "ImageProcess.h"
#include "ImageProcAlgorithm.h"
#include "atlimage.h"
#include <math.h>

#include "include\\ippi.h"
#include "include\\ippcc.h"
#include "include\\ippcv.h"

#pragma comment(lib, "lib\\ippiemerged.lib")
#pragma comment(lib, "lib\\ippimerged.lib")
#pragma comment(lib, "lib\\ippccemerged.lib")
#pragma comment(lib, "lib\\ippccmerged.lib")
#pragma comment(lib, "lib\\ippsemerged.lib")
#pragma comment(lib, "lib\\ippsmerged.lib")
#pragma comment(lib, "lib\\ippcorel.lib")
#pragma comment(lib, "lib\\ippcvmerged.lib")
#pragma comment(lib, "lib\\ippcvemerged.lib")

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

//extern "C" { int _afxForceUSRDLL; } 

//#include "TTime.h"

 //CPerfTimer GPerfTimer;
 //double dScaleProcessTime;
 //double dScaleWholeTime;
//extern int LUT_16BIT_8BIT_GAMMA_C3[65536];
extern BYTE LUT_16BIT_8BIT_GAMMA[65536];

HANDLE ghProcessThread_1 = NULL;
HANDLE ghProcessThread_2 = NULL;
HANDLE ghProcessThread_3 = NULL;
HANDLE ghStartEventProc1 = NULL;
HANDLE ghStartEventProc2 = NULL;
HANDLE ghStartEventProc3 = NULL;
HANDLE ghFinishEventProc1 = NULL;
HANDLE ghFinishEventProc2 = NULL;
HANDLE ghFinishEventProc3 = NULL;
BYTE* gpBufProc1 = NULL;
BYTE* gpBufProc2 = NULL;
BYTE* gpBufProc3 = NULL;
BYTE* gpBufDst = NULL;
BYTE* gpSaturationData = NULL;
BOOL gbProceExit = FALSE;
int giColorGradation = 3; //all color level

extern ProcessParam gProcessParam;

//=========================================
// 临界区
class CritSect
{
public:
	CritSect	()	{ InitializeCriticalSection	(&m_critsect);	}
	~CritSect	()	{ DeleteCriticalSection		(&m_critsect);	}
	void Enter	()	{ EnterCriticalSection		(&m_critsect);	}
	void Leave	()	{ LeaveCriticalSection		(&m_critsect);	}
private:
	CRITICAL_SECTION m_critsect;
};

//=========================================
// 临界区自动锁
class Lock
{
public:
	Lock(CritSect *pLocker)
	{
		m_pLocker = pLocker;
		m_pLocker->Enter();
	}
	~Lock()
	{
		m_pLocker->Leave();
	}
private:
	CritSect *m_pLocker;
};

CritSect gCritSect;

DWORD WINAPI ProcessPoc_1(__in LPVOID lpParameter)
{
	while (!gbProceExit)
	{
		WaitForSingleObject(ghStartEventProc1, INFINITE);
		if (gpBufProc1 == NULL)
		{
			continue;
		}
		BYTE* dst = NULL;
		BYTE* saturation = NULL;
		int iWLen = 0;
		//int iHLen = 0;
		int tmp = 0;
		int forLen = gProcessParam.iHeight*gProcessParam.iWidth;
		double dTime1 = 0;
		double dTime2 = 0;

		//GPerfTimer.TimeBegin();
		int iMultTempDst = 0;
		int iMultTempSrc = 0;
		int iMultTempSatr = 0;

		// P8uC3 = PixelFormats.Bgr24
		// P8uC3_1 = PixelFormats.Rgb24
		if (gProcessParam.iPixelType == P8uC3 || gProcessParam.iPixelType == P8uC3_1)
		{
			BYTE* src = NULL;

			for(int i = 0; i < forLen; i++)
			{
				tmp = iWLen + iWLen + iWLen;
				dst = gpBufDst + iMultTempDst + tmp;
				src = (BYTE*)(gpBufProc1 + iMultTempSrc) + tmp;
				memcpy(dst, LUT_16BIT_8BIT_GAMMA + *src, 1);

				if (gProcessParam.bSaturation)
				{
					saturation = gpSaturationData + iMultTempSatr;
					switch(giColorGradation)
					{
					case 0: // R level 
						if (saturation[tmp] > gProcessParam.iSaturatonValue)
						{
							dst[0] = 255;
						}
						break;
					case 1: // G level
						if (saturation[tmp+1] > gProcessParam.iSaturatonValue)
						{
							dst[0] = 255;
						}
						break;
					case 2: // B level
						if (saturation[tmp+2] > gProcessParam.iSaturatonValue)
						{
							dst[0] = 255;
						}
						break;
					case 3: // All level
						if (saturation[tmp] > gProcessParam.iSaturatonValue ||
							saturation[tmp+1] > gProcessParam.iSaturatonValue ||
							saturation[tmp+2] > gProcessParam.iSaturatonValue)
						{
							dst[0] = 255;
						}
						break;
					}
				}
			
				iWLen++;
				if (iWLen == gProcessParam.iWidth)
				{
					iWLen = 0;
					iMultTempDst = iMultTempDst + gProcessParam.iDstStep;
					iMultTempSrc = iMultTempSrc + gProcessParam.iSrcStep;
					iMultTempSatr = iMultTempSatr + gProcessParam.iSaturatonStep;
				}
			}
		}
		else
		{
			unsigned short* src = NULL;
		
			for(int i = 0; i < forLen;i++)
			{			
				tmp = iWLen+iWLen+iWLen;
				//dst = gpBufDst+iHLen*gProcessParam.iDstStep + tmp;
				dst = gpBufDst+iMultTempDst + tmp;
				//src = (unsigned short*)(gpBufProc1+iHLen*gProcessParam.iSrcStep) + tmp;
				src = (unsigned short*)(gpBufProc1+iMultTempSrc) + tmp;
				memcpy(dst, LUT_16BIT_8BIT_GAMMA + *src, 1);
				if (gProcessParam.bSaturation){
					//saturation = gpSaturationData+i*gProcessParam.iSrcStep;
					saturation = gpSaturationData+iMultTempSatr;
					switch(giColorGradation)
					{
					case 0: // R level 
						if (((unsigned short*)saturation)[tmp] > gProcessParam.iSaturatonValue){					
							dst[0] = 255;
						}
						break;
					case 1: // G level
						if (((unsigned short*)saturation)[tmp+1] > gProcessParam.iSaturatonValue){					
							dst[0] = 255;
						}
						break;
					case 2: // B level
						if (((unsigned short*)saturation)[tmp+2] > gProcessParam.iSaturatonValue){					
							dst[0] = 255;
						}
						break;
					case 3: // All level
						if (((unsigned short*)saturation)[tmp] > gProcessParam.iSaturatonValue || ((unsigned short*)saturation)[tmp+1] > gProcessParam.iSaturatonValue || ((unsigned short*)saturation)[tmp+2] > gProcessParam.iSaturatonValue){					
							dst[0] = 255;
						}
						break;
					}
				}
			
				iWLen++;
				if (iWLen == gProcessParam.iWidth){
					iWLen = 0;
					//iHLen++;
					iMultTempDst = iMultTempDst + gProcessParam.iDstStep;
					iMultTempSrc = iMultTempSrc + gProcessParam.iSrcStep;
					iMultTempSatr = iMultTempSatr + gProcessParam.iSaturatonStep;
				}
			}	
		}

		//dTime2 = GPerfTimer.TimeLastSecound()*1000;
		//char sDebug[256];
		//sprintf(sDebug, "Scale Processs Time1 = %.2f, Time2 = %.2f\n", dTime1, dTime2);
		//OutputDebugString(sDebug);

		SetEvent(ghFinishEventProc1);
	}
	return 0;
}

DWORD WINAPI ProcessPoc_2(__in LPVOID lpParameter)
{
	while (!gbProceExit)
	{
		WaitForSingleObject(ghStartEventProc2, INFINITE);

		if (gpBufProc2 == NULL)
		{
			continue;
		}

		BYTE* dst = NULL;
		BYTE* saturation = NULL;
		int iWLen = 0;
		int iHLen = 0;
		int tmp = 0;
		int iMultTempDst = 0;
		int iMultTempSrc = 0;
		int iMultTempSatr = 0;
		int forLen = gProcessParam.iHeight*gProcessParam.iWidth;

		// P8uC3 = PixelFormats.Bgr24
		// P8uC3_1 = PixelFormats.Rgb24
		if (gProcessParam.iPixelType == P8uC3 || gProcessParam.iPixelType == P8uC3_1)
		{
			BYTE* src = NULL;

			for(int i = 0; i < forLen; i++)
			{
				tmp = iWLen + iWLen + iWLen + 1;
				dst = gpBufDst + iMultTempDst + tmp;
				src = (BYTE*)(gpBufProc1 + iMultTempSrc) + tmp;
				memcpy(dst, LUT_16BIT_8BIT_GAMMA + *src, 1);

				if (gProcessParam.bSaturation)
				{
					saturation = gpSaturationData + iMultTempSatr;
					switch(giColorGradation)
					{
					case 0:
						if (saturation[tmp-1] > gProcessParam.iSaturatonValue)
						{
							dst[0] = 255;
						}
						break;
					case 1:
						if (saturation[tmp] > gProcessParam.iSaturatonValue)
						{
							dst[0] = 255;
						}
						break;
					case 2:
						if (saturation[tmp+1] > gProcessParam.iSaturatonValue)
						{
							dst[0] = 255;
						}
						break;
					case 3:
						if (saturation[tmp-1] > gProcessParam.iSaturatonValue ||
							saturation[tmp] > gProcessParam.iSaturatonValue ||
							saturation[tmp+1] > gProcessParam.iSaturatonValue)
						{
							dst[0] = 255;
						}
						break;
					}
				}
			
				iWLen++;
				if (iWLen == gProcessParam.iWidth)
				{
					iWLen = 0;
					iMultTempDst = iMultTempDst + gProcessParam.iDstStep;
					iMultTempSrc = iMultTempSrc + gProcessParam.iSrcStep;
					iMultTempSatr = iMultTempSatr + gProcessParam.iSaturatonStep;
				}
			}
		}
		else
		{
			unsigned short* src = NULL;

			for(int i = 0; i < forLen;i++)
			{
				tmp = iWLen+iWLen+iWLen+1;
				//dst = gpBufDst+iHLen*gProcessParam.iDstStep + tmp;
				dst = gpBufDst+iMultTempDst + tmp;
				//src = (unsigned short*)(gpBufProc1+iHLen*gProcessParam.iSrcStep) + tmp;
				src = (unsigned short*)(gpBufProc1+iMultTempSrc) + tmp;
				memcpy(dst, LUT_16BIT_8BIT_GAMMA + *src, 1);

				if (gProcessParam.bSaturation)
				{
					//saturation = gpSaturationData+i*gProcessParam.iSrcStep;
					saturation = gpSaturationData+iMultTempSatr;
					switch(giColorGradation)
					{
					case 0:
						if (((unsigned short*)saturation)[tmp-1] > gProcessParam.iSaturatonValue)
						{
							dst[0] = 255;
						}
						break;
					case 1:
						if (((unsigned short*)saturation)[tmp] > gProcessParam.iSaturatonValue)
						{
							dst[0] = 255;
						}
						break;
					case 2:
						if (((unsigned short*)saturation)[tmp+1] > gProcessParam.iSaturatonValue)
						{
							dst[0] = 255;
						}
						break;
					case 3:
						if (((unsigned short*)saturation)[tmp-1] > gProcessParam.iSaturatonValue ||
							((unsigned short*)saturation)[tmp] > gProcessParam.iSaturatonValue ||
							((unsigned short*)saturation)[tmp+1] > gProcessParam.iSaturatonValue)
						{
							dst[0] = 255;
						}
						break;
					}
				}
			
				iWLen++;
				if (iWLen == gProcessParam.iWidth)
				{
					iWLen = 0;
					//iHLen++;
					iMultTempDst = iMultTempDst + gProcessParam.iDstStep;
					iMultTempSrc = iMultTempSrc + gProcessParam.iSrcStep;
					iMultTempSatr = iMultTempSatr + gProcessParam.iSaturatonStep;
				}
			}
		}

		SetEvent(ghFinishEventProc2);
	}
	return 0;
}

DWORD WINAPI ProcessPoc_3(__in LPVOID lpParameter)
{
	while (!gbProceExit)
	{
		WaitForSingleObject(ghStartEventProc3, INFINITE);

		if (gpBufProc3 == NULL)
		{
			continue;
		}

		BYTE* dst = NULL;
		BYTE* saturation = NULL;
		int iWLen = 0;
		int tmp = 0;
		int forLen = gProcessParam.iHeight * gProcessParam.iWidth;
		int iMultTempDst = 0;
		int iMultTempSrc = 0;
		int iMultTempSatr = 0;

		// P8uC3 = PixelFormats.Bgr24
		// P8uC3_1 = PixelFormats.Rgb24
		if (gProcessParam.iPixelType == P8uC3 || gProcessParam.iPixelType == P8uC3_1)
		{
			BYTE* src = NULL;

			for(int i = 0; i < forLen; i++)
			{
				tmp = iWLen + iWLen + iWLen + 2;
				dst = gpBufDst + iMultTempDst + tmp;
				src = (BYTE*)(gpBufProc1 + iMultTempSrc) + tmp;
				memcpy(dst, LUT_16BIT_8BIT_GAMMA + *src, 1);

				if (gProcessParam.bSaturation)
				{
					saturation = gpSaturationData+iMultTempSatr;
					switch(giColorGradation)
					{
					case 0:
						if (saturation[tmp-2] > gProcessParam.iSaturatonValue)
						{
							dst[0] = 255;
						}
						break;
					case 1:
						if (saturation[tmp-1] > gProcessParam.iSaturatonValue)
						{
							dst[0] = 255;
						}
						break;
					case 2:
						if (saturation[tmp] > gProcessParam.iSaturatonValue)
						{
							dst[0] = 255;
						}
						break;
					case 3:
						if (saturation[tmp-2] > gProcessParam.iSaturatonValue ||
							saturation[tmp-1] > gProcessParam.iSaturatonValue ||
							saturation[tmp] > gProcessParam.iSaturatonValue)
						{
							dst[0] = 255;
						}
						break;
					}
				}
			
				iWLen++;
				if (iWLen == gProcessParam.iWidth)
				{
					iWLen = 0;
					iMultTempDst = iMultTempDst + gProcessParam.iDstStep;
					iMultTempSrc = iMultTempSrc + gProcessParam.iSrcStep;
					iMultTempSatr = iMultTempSatr + gProcessParam.iSaturatonStep;
				}
			}
		}
		else
		{
			unsigned short* src = NULL;

			for(int i = 0; i < forLen;i++)
			{
				tmp = iWLen+iWLen+iWLen+2;
				//dst = gpBufDst+iHLen*gProcessParam.iDstStep + tmp;
				dst = gpBufDst+iMultTempDst + tmp;
				//src = (unsigned short*)(gpBufProc1+iHLen*gProcessParam.iSrcStep) + tmp;
				src = (unsigned short*)(gpBufProc1+iMultTempSrc) + tmp;
				memcpy(dst, LUT_16BIT_8BIT_GAMMA + *src, 1);
				if (gProcessParam.bSaturation){
					//saturation = gpSaturationData+iHLen*gProcessParam.iSrcStep;
					saturation = gpSaturationData+iMultTempSatr;
					switch(giColorGradation)
					{
					case 0:
						if (((unsigned short*)saturation)[tmp-2] > gProcessParam.iSaturatonValue)
						{
							dst[0] = 255;
						}
						break;
					case 1:
						if (((unsigned short*)saturation)[tmp-1] > gProcessParam.iSaturatonValue)
						{
							dst[0] = 255;
						}
						break;
					case 2:
						if (((unsigned short*)saturation)[tmp] > gProcessParam.iSaturatonValue)
						{
							dst[0] = 255;
						}
						break;
					case 3:
						if (((unsigned short*)saturation)[tmp-2] > gProcessParam.iSaturatonValue ||
							((unsigned short*)saturation)[tmp-1] > gProcessParam.iSaturatonValue ||
							((unsigned short*)saturation)[tmp] > gProcessParam.iSaturatonValue)
						{
							dst[0] = 255;
						}
						break;
					}
				}
			
				iWLen++;
				if (iWLen == gProcessParam.iWidth)
				{
					iWLen = 0;
					//iHLen++;
					iMultTempDst = iMultTempDst + gProcessParam.iDstStep;
					iMultTempSrc = iMultTempSrc + gProcessParam.iSrcStep;
					iMultTempSatr = iMultTempSatr + gProcessParam.iSaturatonStep;
				}
			}
		}

		SetEvent(ghFinishEventProc3);
	}
	return 0;
}

BOOL APIENTRY DllMain(HANDLE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
	switch(ul_reason_for_call) 
	{
	case DLL_PROCESS_ATTACH:
		ghStartEventProc1 = CreateEvent(NULL, FALSE, FALSE, NULL);
		ghStartEventProc2 = CreateEvent(NULL, FALSE, FALSE, NULL);
		ghStartEventProc3 = CreateEvent(NULL, FALSE, FALSE, NULL);
		ghFinishEventProc1 = CreateEvent(NULL, FALSE, FALSE, NULL);
		ghFinishEventProc2 = CreateEvent(NULL, FALSE, FALSE, NULL);
		ghFinishEventProc3 = CreateEvent(NULL, FALSE, FALSE, NULL);
		ghProcessThread_1 = CreateThread(NULL, 0, ProcessPoc_1, NULL, 0, NULL);
		ghProcessThread_2 = CreateThread(NULL, 0, ProcessPoc_2, NULL, 0, NULL);
		ghProcessThread_3 = CreateThread(NULL, 0, ProcessPoc_3, NULL, 0, NULL);

		SetThreadPriority(ghProcessThread_1, static_cast<int>(THREAD_PRIORITY_HIGHEST));
		SetThreadPriority(ghProcessThread_2, static_cast<int>(THREAD_PRIORITY_HIGHEST));
		SetThreadPriority(ghProcessThread_3, static_cast<int>(THREAD_PRIORITY_HIGHEST));
		break;
	case DLL_THREAD_ATTACH:
		break;
	case DLL_THREAD_DETACH:
		break;
	case DLL_PROCESS_DETACH:
		gbProceExit = TRUE;
		SetEvent(ghStartEventProc1);
		SetEvent(ghStartEventProc2);
		SetEvent(ghStartEventProc3);
		if (WaitForSingleObject(ghProcessThread_1, 1000) == WAIT_TIMEOUT)
			TerminateThread(ghProcessThread_1, 0);
		if (WaitForSingleObject(ghProcessThread_2, 1000) == WAIT_TIMEOUT)
			TerminateThread(ghProcessThread_2, 0);
		if (WaitForSingleObject(ghProcessThread_3, 1000) == WAIT_TIMEOUT)
			TerminateThread(ghProcessThread_3, 0);
		CloseHandle(ghProcessThread_1);
		CloseHandle(ghProcessThread_2);
		CloseHandle(ghProcessThread_3);
		CloseHandle(ghStartEventProc1);
		CloseHandle(ghStartEventProc2);
		CloseHandle(ghStartEventProc3);
		CloseHandle(ghFinishEventProc1);
		CloseHandle(ghFinishEventProc2);
		CloseHandle(ghFinishEventProc3);
		break;	
	}
	return TRUE;
}

BOOL SaveImgGRY(CString lFileName, PBYTE lBufPtr, int lWidth, int lHeight, BOOL lReverse)
{
	BITMAPFILEHEADER bhh;
	BITMAPINFOHEADER bih;
	memset(&bhh,0,sizeof(BITMAPFILEHEADER));
	memset(&bih,0,sizeof(BITMAPINFOHEADER)); 

	int widthStep = (lWidth + 3)/4*4;
	//int widthStep		=	(lWidth + 3)*4 / 4; //每行实际占用的大小（每行都被填充到一个4字节边界）

	//构造灰度图的文件头
	bhh.bfOffBits		=	(DWORD)sizeof(BITMAPFILEHEADER)+(DWORD)sizeof(BITMAPINFOHEADER)+sizeof(RGBQUAD)*256; 
	bhh.bfSize			=	(DWORD)sizeof(BITMAPFILEHEADER)+(DWORD)sizeof(BITMAPINFOHEADER)+sizeof(RGBQUAD)*256+widthStep*lHeight;   
	bhh.bfReserved1		=	0;
	bhh.bfReserved2		=	0;
	bhh.bfType			=	0x4d42;

	//构造灰度图的信息头
	bih.biBitCount		=	8;
	bih.biSize			=	sizeof(BITMAPINFOHEADER);
	bih.biHeight		=	lReverse*lHeight;
	bih.biWidth			=	lWidth;
	bih.biPlanes		=	1;
	bih.biCompression	=	BI_RGB;
	bih.biSizeImage		=	widthStep*lHeight;

	bih.biXPelsPerMeter	=	0;
	bih.biYPelsPerMeter	=	0;
	bih.biClrImportant	=	0;
	bih.biClrUsed		=	0;

	//构造灰度图的调色版
	RGBQUAD rgbquad[256];
	for(int i=0;i<256;i++)
	{
		rgbquad[i].rgbBlue=i;
		rgbquad[i].rgbGreen=i;
		rgbquad[i].rgbRed=i;
		rgbquad[i].rgbReserved=0;
	}

	int DIBSize = widthStep * lHeight;

	//写入数据
	CFile cf;
	if(cf.Open(lFileName,CFile::modeCreate|CFile::modeWrite))
	{
		cf.Write(&bhh,sizeof(BITMAPFILEHEADER));
		cf.Write(&bih,sizeof(BITMAPINFOHEADER));
		cf.Write(&rgbquad,sizeof(RGBQUAD)*256);
		cf.Write(lBufPtr,DIBSize);
		cf.Close();
		return TRUE;
	}
	return FALSE; 
}

BOOL SaveImgRGB(CString lFileName, PBYTE lBufPtr, int lWidth, int lHeight, BOOL lReverse)
{  
	BITMAPFILEHEADER bhh;
	BITMAPINFOHEADER bih;
	memset(&bhh,0,sizeof(BITMAPFILEHEADER));
	memset(&bih,0,sizeof(BITMAPINFOHEADER));

	int widthStep				=	(((lWidth * 24) + 31) & (~31)) / 8; //每行实际占用的大小（每行都被填充到一个4字节边界）

	//构造彩色图的文件头
	bhh.bfOffBits				=	(DWORD)sizeof(BITMAPFILEHEADER) + (DWORD)sizeof(BITMAPINFOHEADER); 
	bhh.bfSize					=	(DWORD)sizeof(BITMAPFILEHEADER) + (DWORD)sizeof(BITMAPINFOHEADER) + widthStep * lHeight;  
	bhh.bfReserved1				=	0; 
	bhh.bfReserved2				=	0;
	bhh.bfType					=	0x4d42;  

	//构造彩色图的信息头
	bih.biBitCount				=	24;
	bih.biSize					=	sizeof(BITMAPINFOHEADER);
	bih.biHeight				=	lReverse*lHeight;
	bih.biWidth					=	lWidth;  
	bih.biPlanes				=	1;
	bih.biCompression			=	BI_RGB;
	bih.biSizeImage				=	widthStep*lHeight*3;  
	bih.biXPelsPerMeter			=	0;  
	bih.biYPelsPerMeter			=	0;  
	bih.biClrUsed				=	0;  
	bih.biClrImportant			=	0;   

	int DIBSize = widthStep * lHeight;

	//写入数据
	CFile file;  
	if(file.Open(lFileName,CFile::modeWrite | CFile::modeCreate))  
	{
		file.Write((LPSTR)&bhh,sizeof(BITMAPFILEHEADER));  
		file.Write((LPSTR)&bih,sizeof(BITMAPINFOHEADER));  
		file.Write(lBufPtr,DIBSize);  
		file.Close();  
		return TRUE;  
	}  
	return FALSE;  
}

BOOL GetRGBData(PBYTE lDstPtr, PBYTE lBufPtr, int lWidth, int lHeight, BOOL lReverse)
{  
	BITMAPFILEHEADER bhh;
	BITMAPINFOHEADER bih;
	memset(&bhh,0,sizeof(BITMAPFILEHEADER));
	memset(&bih,0,sizeof(BITMAPINFOHEADER));

	int widthStep				=	(((lWidth * 24) + 31) & (~31)) / 8; //每行实际占用的大小（每行都被填充到一个4字节边界）

	//构造彩色图的文件头
	bhh.bfOffBits				=	(DWORD)sizeof(BITMAPFILEHEADER) + (DWORD)sizeof(BITMAPINFOHEADER); 
	bhh.bfSize					=	(DWORD)sizeof(BITMAPFILEHEADER) + (DWORD)sizeof(BITMAPINFOHEADER) + widthStep * lHeight;  
	bhh.bfReserved1				=	0; 
	bhh.bfReserved2				=	0;
	bhh.bfType					=	0x4d42;  

	//构造彩色图的信息头
	bih.biBitCount				=	24;
	bih.biSize					=	sizeof(BITMAPINFOHEADER);
	bih.biHeight				=	lReverse*lHeight;
	bih.biWidth					=	lWidth;  
	bih.biPlanes				=	1;
	bih.biCompression			=	BI_RGB;
	bih.biSizeImage				=	widthStep*lHeight*3;  
	bih.biXPelsPerMeter			=	0;  
	bih.biYPelsPerMeter			=	0;  
	bih.biClrUsed				=	0;  
	bih.biClrImportant			=	0;   

	int DIBSize = widthStep * lHeight;

	//写入数据
	//CFile file;  
	//if(file.Open(lFileName,CFile::modeWrite | CFile::modeCreate))  
	{
		memcpy(lDstPtr,(LPSTR)&bhh,sizeof(BITMAPFILEHEADER));
		memcpy(lDstPtr+sizeof(BITMAPFILEHEADER),(LPSTR)&bih,sizeof(BITMAPINFOHEADER));
		memcpy(lDstPtr+sizeof(BITMAPFILEHEADER)+sizeof(BITMAPINFOHEADER),lBufPtr,DIBSize); 
		//return TRUE;  
	}  
	return TRUE;  
}

BOOL GetGrayData(PBYTE lDstPtr, PBYTE lBufPtr, int lWidth, int lHeight, BOOL lReverse)
{  
	BITMAPFILEHEADER bhh;
	BITMAPINFOHEADER bih;
	memset(&bhh,0,sizeof(BITMAPFILEHEADER));
	memset(&bih,0,sizeof(BITMAPINFOHEADER)); 

	//构造灰度图的文件头
	bhh.bfOffBits		=	(DWORD)sizeof(BITMAPFILEHEADER)+(DWORD)sizeof(BITMAPINFOHEADER)+sizeof(RGBQUAD)*256; 
	bhh.bfSize			=	(DWORD)sizeof(BITMAPFILEHEADER)+(DWORD)sizeof(BITMAPINFOHEADER)+sizeof(RGBQUAD)*256+lWidth*lHeight;   
	bhh.bfReserved1		=	0;
	bhh.bfReserved2		=	0;
	bhh.bfType			=	0x4d42;

	//构造灰度图的信息头
	bih.biBitCount		=	8;
	bih.biSize			=	sizeof(BITMAPINFOHEADER);
	bih.biHeight		=	lReverse*lHeight;
	bih.biWidth			=	lWidth;
	bih.biPlanes		=	1;
	bih.biCompression	=	BI_RGB;
	bih.biSizeImage		=	0;
	bih.biXPelsPerMeter	=	0;
	bih.biYPelsPerMeter	=	0;
	bih.biClrImportant	=	0;
	bih.biClrUsed		=	0;

	//构造灰度图的调色版
	RGBQUAD rgbquad[256];
	for(int i=0;i<256;i++)
	{
		rgbquad[i].rgbBlue=i;
		rgbquad[i].rgbGreen=i;
		rgbquad[i].rgbRed=i;
		rgbquad[i].rgbReserved=0;
	}

	//写入数据
	//CFile file;  
	//if(file.Open(lFileName,CFile::modeWrite | CFile::modeCreate))  
	{
		memcpy(lDstPtr,(LPSTR)&bhh,sizeof(BITMAPFILEHEADER));
		memcpy(lDstPtr+sizeof(BITMAPFILEHEADER),(LPSTR)&bih,sizeof(BITMAPINFOHEADER));
		memcpy(lDstPtr+sizeof(BITMAPFILEHEADER)+sizeof(BITMAPINFOHEADER),&rgbquad,sizeof(RGBQUAD)*256);
		memcpy(lDstPtr+sizeof(BITMAPFILEHEADER)+sizeof(BITMAPINFOHEADER)+sizeof(RGBQUAD)*256,lBufPtr,lWidth*lHeight); 
		//return TRUE;  
	}  
	return TRUE;  
}

/*
ABIAPI DWORD WINAPI MVC_BUF_Resize(PBYTE lSrcData, PBYTE lDstData ,int lDepth, int lWidth, int lHeight, int lReverse , double lRatioX, double lRatioY)
{
	//重设图像幅面大小
	int lStride = ((lWidth*lDepth) + 31)/32*4;
	PBYTE lNewByte = new BYTE[(int)(lStride*lHeight*(lRatioX+1)*(lRatioY+1))];
	IppiRect m_srcROI;
	m_srcROI.x = m_srcROI.y=0;

	IppiSize m_srcSize;
	IppiSize m_dstSize;

	m_srcSize.width		= m_srcROI.width	= lWidth;
	m_srcSize.height	= m_srcROI.height	= lHeight;

	m_dstSize.width		= (int)(lWidth*lRatioX);
	m_dstSize.height	= (int)(lHeight*lRatioY);

	int lSrcStride = ((m_srcSize.width *lDepth) + 31)/32*4;
	int lDstStride = ((m_dstSize.width *lDepth) + 31)/32*4;

	if(lDepth==8)
	{
		ippiResize_8u_C1R(lSrcData, m_srcSize, lSrcStride, m_srcROI, lNewByte, lDstStride, m_dstSize, lRatioX, lRatioY, IPPI_INTER_NN);
		SaveImgGRY(_T("D:\\GRAY-ORI.bmp"),lSrcData,m_srcSize.width,m_srcSize.height,lReverse*-1);
		SaveImgGRY(_T("D:\\GRAY-CVT.bmp"),lNewByte,m_dstSize.width,m_dstSize.height,lReverse*-1);
		GetGrayData(lDstData,lNewByte,lWidth*lRatioX, lHeight*lRatioY, lReverse*-1);
	}
	if(lDepth==24)
	{
		ippiResize_8u_C3R(lSrcData, m_srcSize, lSrcStride, m_srcROI, lNewByte, lDstStride, m_dstSize, lRatioX, lRatioY, IPPI_INTER_NN);
		SaveImgRGB(_T("D:\\RGB-ORI.bmp"),lSrcData,m_srcSize.width,m_srcSize.height,lReverse*-1);
		SaveImgRGB(_T("D:\\RGB-CVT.bmp"),lNewByte,m_dstSize.width,m_dstSize.height,lReverse*-1);
		GetRGBData(lDstData,lNewByte,lWidth*lRatioX, lHeight*lRatioY, lReverse*-1);
	}
	if(lDepth==32)
	{
		ippiResize_8u_C4R(lSrcData, m_srcSize, lSrcStride, m_srcROI, lNewByte, ((m_dstSize.width *8) + 31)/32*4, m_dstSize, lRatioX, lRatioY, IPPI_INTER_NN);
		SaveImgRGB(_T("D:\\TIF-ORI.bmp"),lSrcData,m_srcSize.width,m_srcSize.height,lReverse*-1);
		SaveImgRGB(_T("D:\\TIF-CVT.bmp"),lNewByte,m_dstSize.width,m_dstSize.height,lReverse*-1);
		GetGrayData(lDstData,lNewByte,lWidth*lRatioX, lHeight*lRatioY, lReverse*-1);
	}

	delete lNewByte;
	lNewByte = NULL;

	return 0;
}
*/

ABIAPI DWORD WINAPI MVC_IMG_ToByte(PBYTE lDstData, PCHAR lSrcPath)
{
	CString lPath = (CString)lSrcPath;
	if(lPath.Trim()=="") return -1;
	CFile Fs(lPath,CFile::modeRead);
	Fs.Read(lDstData,(UINT)Fs.GetLength());
	return 0;
}

ABIAPI DWORD WINAPI MVC_IMG_Init(PCHAR lSrcPath, PBYTE lDstData, int *lDep, int *lWid, int *lHei, int *lReverse)
{
	CString lPath = (CString)lSrcPath;
	if(lPath.Trim()=="") return -1;
	CImage lXImage;
	lXImage.Load(lPath);
	lXImage.Save(_T("D:\\RAW.bmp"));

	int lDepth = lXImage.GetBPP();
	int lColor = lDepth==24?1:0;
	int lWidth = lXImage.GetWidth();
	int lHeight = lXImage.GetHeight();
	int lWidthStride = ((lWidth * lDepth) + 31)/32*4;
	int lPitch = lXImage.GetPitch();
	DWORD dwSize = lWidthStride*lHeight;

	*lDep = lDepth;
	*lWid = lWidth;
	*lHei = lHeight;
	*lReverse = lXImage.GetPitch()<0?-1:1;

	if(lXImage.GetPitch()>0)
	{
		memcpy(lDstData, lXImage.GetBits(),dwSize);
	}
	else
	{
		PBYTE lBytr = (PBYTE)lXImage.GetBits() + (lXImage.GetPitch()*(lXImage.GetHeight()-1));
		memcpy(lDstData, lBytr, dwSize);
	}

	lXImage.Destroy();
	return 0;
}

ABIAPI DWORD WINAPI MVC_IMG_ToGray(PBYTE lDstData, PCHAR lSrcPath)
{
	CString lPath = (CString)lSrcPath;
	if(lPath.Trim()=="") return -1;
	CImage lXImage;
	lXImage.Load(lPath);

	int lDepth = lXImage.GetBPP();
	int lColor = (lDepth==24||lDepth==32)?1:0;
	int lWidth = lXImage.GetWidth();
	int lWidthStride = ((lWidth *lXImage.GetBPP()) + 31)/32*4;
	int lHeight = lXImage.GetHeight();
	DWORD dwSize = lWidthStride*lHeight;
	PBYTE lRawData = new BYTE[dwSize];
	if(lXImage.GetPitch()>0)
	{
		memcpy(lRawData, lXImage.GetBits(),dwSize);
	}
	else
	{
		PBYTE lBytr = (PBYTE)lXImage.GetBits() + (lXImage.GetPitch()*(lXImage.GetHeight()-1));
		memcpy(lRawData, lBytr, dwSize);
	}

	PBYTE lDstNewData = new BYTE[dwSize];

	//bmp24 | jpg 24 | tif 48
	if(lColor==1)
	{
		if(lDepth==24)
		{
			IppiSize srcRoi = {lWidth, lHeight };
			if(lXImage.GetPitch()<0)
			{
				Ipp32f coeffs[3] = {(Ipp32f)0.114,(Ipp32f)0.587,(Ipp32f)0.299};
				ippiColorToGray_8u_C3C1R ( lRawData, lWidthStride, lDstNewData, ((lWidth *8) + 31)/32*4, srcRoi, coeffs );
			}
			else
			{
				ippiRGBToGray_8u_C3C1R ( lRawData, lWidthStride, lDstNewData,  ((lWidth *8) + 31)/32*4, srcRoi);
			}
		}
		else if(lDepth==32)
		{
			IppiSize srcRoi = {lWidth, lHeight };
			ippiReduceBits_16u8u_C3R( (Ipp16u*)lRawData, lWidthStride, lDstNewData,  ((lWidth *8) + 31)/32*4, srcRoi, 0, ippDitherNone, 2);
			GetGrayData(lDstData,lDstNewData,lWidth, lHeight,lXImage.GetPitch()>0?-1:1);
		}
		SaveImgGRY(_T("D:\\YY.bmp"),lDstNewData,lWidth,lHeight,lXImage.GetPitch()>0?-1:1);
		GetGrayData(lDstData,lDstNewData,lWidth, lHeight,lXImage.GetPitch()>0?-1:1);
	}
	else //bmp8 | jpg 8 | tif 16
	{
		if(lDepth==8)
		{
			//SaveImgGRY(_T("D:\\YY.bmp"),lRawData,lWidth,lHeight,lXImage.GetPitch()>0?-1:1);
			GetGrayData(lDstData,lRawData,lWidth, lHeight,lXImage.GetPitch()>0?-1:1);
		}
		else if(lDepth==32)
		{
			IppiSize srcRoi = {lWidth, lHeight };
			//ippiReduceBits_32f8u_C3R( (Ipp32u*)lRawData, lWidthStride, lDstNewData,  ((lWidth *8) + 31)/32*4, srcRoi, 0, ippDitherNone, 2);
			//SaveImgGRY(_T("D:\\YY.bmp"),lDstNewData,lWidth,lHeight,lXImage.GetPitch()>0?-1:1);
			GetGrayData(lDstData,lDstNewData,lWidth, lHeight,lXImage.GetPitch()>0?-1:1);
		}
	}

	delete lDstNewData;
	lDstNewData = NULL;

	delete lRawData;
	lRawData = NULL;

	return 0;
}

ABIAPI DWORD WINAPI MVC_BUF_ToGray(PBYTE lSrcData, PBYTE lDstData, int lDepth, int lWidth, int lHeight, int lReverse)
{
	int lWidthStride = ((lWidth*lDepth) + 31)/32*4;
	PBYTE lNewByte = new BYTE[lWidthStride*lHeight];
	if(lDepth==24)
	{
		IppiSize srcRoi = {lWidth, lHeight };
		if(lReverse<0)
		{
			Ipp32f coeffs[3] = {(Ipp32f)0.114,(Ipp32f)0.587,(Ipp32f)0.299};
			ippiColorToGray_8u_C3C1R ( lSrcData, lWidthStride, lNewByte, ((lWidth *8) + 31)/32*4, srcRoi, coeffs );
		}
		else
		{
			ippiRGBToGray_8u_C3C1R ( lSrcData, lWidthStride, lNewByte,  ((lWidth *8) + 31)/32*4, srcRoi);
		}
		GetGrayData(lDstData,lNewByte,lWidth, lHeight, lReverse*-1);
	}
	else if(lDepth==32)
	{
		IppiSize srcRoi = {lWidth, lHeight };
		if(lReverse<0)
		{
			Ipp32f coeffs[3] = {(Ipp32f)0.114,(Ipp32f)0.587,(Ipp32f)0.299};
			ippiColorToGray_8u_AC4C1R ( lSrcData, lWidthStride, lNewByte, ((lWidth *8) + 31)/32*4, srcRoi, coeffs );
		}
		else
		{
			ippiRGBToGray_8u_AC4C1R ( lSrcData, lWidthStride, lNewByte,  ((lWidth *8) + 31)/32*4, srcRoi);
		}
		GetGrayData(lDstData,lNewByte,lWidth, lHeight, lReverse*-1);
	}
	else //bmp8 | jpg 8 | tif 16
	{
		if(lDepth==8)
		{
			GetGrayData(lDstData,lSrcData,lWidth, lHeight, lReverse*-1);
		}
	}
	delete lNewByte;
	lNewByte = NULL;
	return 0;
}


ABIAPI DWORD WINAPI MVC_BUF_RGB2Gray(PBYTE lSrcData, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData, int iDstStep)
{
	ASSERT(lSrcData);
	ASSERT(lDstData);

	IppStatus ret = ippStsNoErr;
	IppiSize size = {iWidth, iHeight};
	switch (iSrcType)
	{
	case P8uC1:
		
		break;
	case P16uC1:
		break;
	case P8uC3:
	case P8uC3_1:
		ret = ippiRGBToGray_8u_C3C1R((const Ipp8u *)lSrcData, iSrcStep, (Ipp8u *)lDstData, iDstStep, size);
		break;
	case P16uC3:
		ret = ippiRGBToGray_16u_C3C1R((const Ipp16u *)lSrcData, iSrcStep, (Ipp16u *)lDstData, iDstStep, size);
		break;
	}
	return ret;
}
ABIAPI DWORD WINAPI MVC_BUF_Flip(PBYTE lSrcData, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData, int iDstStep, int iFlipMode)
{
	ASSERT(lSrcData);
	ASSERT(lDstData);

	IppStatus ret = ippStsNoErr;
	IppiAxis mode;
	if (iFlipMode == FlipHorizontal){
		mode = ippAxsVertical;
	}
	else{
		mode = ippAxsHorizontal;
	}

	IppiSize size = {iWidth, iHeight};
	switch (iSrcType)
	{
	case P8uC1:
		ret = ippiMirror_8u_C1R((const Ipp8u*)lSrcData, iSrcStep, (Ipp8u*)lDstData, iDstStep, size, mode);
		break;
	case P16uC1:
		ret = ippiMirror_16u_C1R((const Ipp16u*)lSrcData, iSrcStep, (Ipp16u*)lDstData, iDstStep, size, mode);
		break;
	case P8uC3:
	case P8uC3_1:
		ret = ippiMirror_8u_C3R((const Ipp8u*)lSrcData, iSrcStep, (Ipp8u*)lDstData, iDstStep, size, mode);
		break;
	case P16uC3:
		ret = ippiMirror_16u_C3R((const Ipp16u*)lSrcData, iSrcStep, (Ipp16u*)lDstData, iDstStep, size, mode);
		break;
	}
	return ret;
}
ABIAPI DWORD WINAPI MVC_BUF_Rotate(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData/*OUT*/, int iDstWidth, int iDstHeight, int iDstStep, double dAngle)
{
	ASSERT(lSrcData);
	ASSERT(lDstData);

	IppStatus ret = ippStsNoErr;
	IppiSize size = {iWidth, iHeight};
	IppiRect  rect = {0, 0, iWidth, iHeight};
	IppiRect DstRect = {0, 0, iDstWidth, iDstHeight};
	IppiSize DstSize = {iDstWidth, iDstHeight};

	double xShift;
	double yShift; 
	double xCenterSrc =(double)iWidth/2.0;
	double yCenterSrc = (double)iHeight/2.0; 
	double xCenterDst = (double)iDstWidth/2.0; 
	double yCenterDst = (double)iDstHeight/2.0;
	double quad[4][2]; 
	double bound[2][2];
	ippiGetRotateShift ( xCenterSrc, yCenterSrc, dAngle, &xShift, &yShift );       
	ippiAddRotateShift ( 1.0, 1.0, dAngle, &xShift, &yShift);     
	xShift += xCenterDst - xCenterSrc;    
	yShift += yCenterDst - yCenterSrc;    
	ippiGetRotateQuad ( rect, quad, dAngle, xShift, yShift);      
	ippiGetRotateBound ( rect, bound, dAngle, xShift, yShift);    
	/*IppiRect dstRect = { (int)quad[1][0], (int)quad[1][1], (int)quad[3][0], (int)quad[3][1] }; */
	IppiRect dstRect = { 0, 0, iDstWidth, iDstHeight };
	ippiSet_8u_C1R ( 0, lDstData, 6, DstSize );    
	
	
	switch (iSrcType)
	{
	case P8uC1:
		//ret = ippiRotateCenter_8u_C1R((const Ipp8u*)lSrcData, size, iSrcStep, rect, (Ipp8u*)lDstData, iDstStep,DstRect, dAngle, xCenter, yCenter, IPPI_INTER_NN);
		ret = ippiRotate_8u_C1R ( (const Ipp8u*)lSrcData, size, iSrcStep, rect, (Ipp8u*)lDstData, iDstStep, DstRect, dAngle, xShift, yShift, IPPI_INTER_LINEAR);
		break;
	case P16uC1:
		//ret = ippiRotateCenter_16u_C1R((const Ipp16u*)lSrcData, size, iSrcStep, rect, (Ipp16u*)lDstData, iDstStep,DstRect, dAngle, xCenter, yCenter, IPPI_INTER_NN);
		ret = ippiRotate_16u_C1R ( (const Ipp16u*)lSrcData, size, iSrcStep, rect, (Ipp16u*)lDstData, iDstStep, DstRect, dAngle, xShift, yShift, IPPI_INTER_LINEAR);
		break;
	case P8uC3:
	case P8uC3_1:
		//ret = ippiRotateCenter_8u_C3R((const Ipp8u*)lSrcData, size, iSrcStep, rect, (Ipp8u*)lDstData, iDstStep,DstRect, dAngle, xCenter, yCenter, IPPI_INTER_LINEAR);
		ret = ippiRotate_8u_C3R ( (const Ipp8u*)lSrcData, size, iSrcStep, rect, (Ipp8u*)lDstData, iDstStep, DstRect, dAngle, xShift, yShift, IPPI_INTER_LINEAR);
		break;
	case P16uC3:
		//ret = ippiRotateCenter_16u_C3R((const Ipp16u*)lSrcData, size, iSrcStep, rect, (Ipp16u*)lDstData, iDstStep,DstRect, dAngle, xCenter, yCenter, IPPI_INTER_NN);
		ret = ippiRotate_16u_C3R ( (const Ipp16u*)lSrcData, size, iSrcStep, rect, (Ipp16u*)lDstData, iDstStep, DstRect, dAngle, xShift, yShift, IPPI_INTER_LINEAR);
		break;
	}
	return ret;
}
ABIAPI DWORD WINAPI MVC_BUF_RGBExtract(PBYTE lSrcData, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstDataR, PBYTE lDstDataG, PBYTE lDstDataB, int iDstStep)
{
	ASSERT(lSrcData);
	ASSERT(lDstDataR);
	ASSERT(lDstDataG);
	ASSERT(lDstDataB);

	IppStatus ret = ippStsNoErr;
	IppiSize size = {iWidth, iHeight};
	
	switch (iSrcType)
	{
	case P8uC1:
		ret = ippStsNoOperation;
		break;
	case P16uC1:
		ret = ippStsNoOperation;
		break;
	case P8uC3:
	case P8uC3_1:
		{
		BYTE* pR = NULL, *pG = NULL, *pB = NULL, *pPlane = NULL;
		int iChannelStep = ((iWidth * 8) + 31) / 32 * 4;
		pR = new BYTE[iChannelStep * iHeight];
		pG = new BYTE[iChannelStep * iHeight];
		pB = new BYTE[iChannelStep * iHeight];
		pPlane = new BYTE[iChannelStep * iHeight];

		if (pR == NULL || pG == NULL || pB == NULL || pPlane == NULL)
		{
			delete[] pR;
			delete[] pG;
			delete[] pB;
			delete[] pPlane;
			return ippStsMemAllocErr;
		}

		memset(pPlane, 0, iChannelStep * iHeight);
		memset(lDstDataR, 0, iDstStep * iHeight);
		memset(lDstDataG, 0, iDstStep * iHeight);
		memset(lDstDataB, 0, iDstStep * iHeight);

		Ipp8u* ImageData[3] = {(Ipp8u*)pR, (Ipp8u*)pG, (Ipp8u*)pB};
		ret =  ippiCopy_8u_C3P3R((const Ipp8u *)lSrcData, iSrcStep, ImageData, iChannelStep, size);

		ImageData[0] = (Ipp8u*)pPlane;
		ImageData[1] = (Ipp8u*)pPlane;
		ImageData[2] = (Ipp8u*)pB;
		ret = ippiCopy_8u_P3C3R(ImageData, iChannelStep, (Ipp8u*)lDstDataB, iDstStep, size);

		ImageData[0] = (Ipp8u*)pPlane;
		ImageData[1] = (Ipp8u*)pG;
		ImageData[2] = (Ipp8u*)pPlane;
		ret = ippiCopy_8u_P3C3R(ImageData, iChannelStep, (Ipp8u*)lDstDataG, iDstStep, size);

		ImageData[0] = (Ipp8u*)pR;
		ImageData[1] = (Ipp8u*)pPlane;
		ImageData[2] = (Ipp8u*)pPlane;
		ret = ippiCopy_8u_P3C3R(ImageData, iChannelStep, (Ipp8u*)lDstDataR, iDstStep, size);

		delete[] pR;
		delete[] pG;
		delete[] pB;
		delete[] pPlane;
		break;
		}
	case P16uC3:
		{
		BYTE* pR = NULL, *pG = NULL, *pB = NULL, *pPlane = NULL;
		int iChannelStep = ((iWidth * 16) + 31) / 32 * 4;
		pR = new BYTE[iChannelStep*iHeight];
		pG = new BYTE[iChannelStep*iHeight];
		pB = new BYTE[iChannelStep*iHeight];
		pPlane = new BYTE[iChannelStep*iHeight];

		if (pR == NULL || pG == NULL || pB == NULL || pPlane == NULL)
		{
			delete[] pR;
			delete[] pG;
			delete[] pB;
			delete[] pPlane;
			return ippStsMemAllocErr;
		}

		memset(pPlane, 0,iChannelStep*iHeight);
		memset(lDstDataR, 0, iDstStep*iHeight);
		memset(lDstDataG, 0, iDstStep*iHeight);
		memset(lDstDataB, 0, iDstStep*iHeight);

		Ipp16u* ImageData[3] = {(Ipp16u*)pR,(Ipp16u*)pG,(Ipp16u*)pB};
		ret =  ippiCopy_16u_C3P3R((const Ipp16u *)lSrcData, iSrcStep, ImageData, iChannelStep,  size);

		ImageData[0] = (Ipp16u*)pPlane;
		ImageData[1] = (Ipp16u*)pPlane;
		ImageData[2] = (Ipp16u*)pB;
		ret = ippiCopy_16u_P3C3R(ImageData, iChannelStep, (Ipp16u*)lDstDataB, iDstStep, size);

		ImageData[0] = (Ipp16u*)pPlane;
		ImageData[1] = (Ipp16u*)pG;
		ImageData[2] = (Ipp16u*)pPlane;
		ret = ippiCopy_16u_P3C3R(ImageData, iChannelStep, (Ipp16u*)lDstDataG, iDstStep, size);

		ImageData[0] = (Ipp16u*)pR;
		ImageData[1] = (Ipp16u*)pPlane;
		ImageData[2] = (Ipp16u*)pPlane;
		ret = ippiCopy_16u_P3C3R(ImageData, iChannelStep, (Ipp16u*)lDstDataR, iDstStep, size);

		delete[] pR;
		delete[] pG;
		delete[] pB;
		delete[] pPlane;
		break;
		}
	}
	return ret;
}

ABIAPI DWORD WINAPI MVC_BUF_RGBExtractSingleChannel(PBYTE lSrcData, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstDataR, PBYTE lDstDataG, PBYTE lDstDataB, int iDstStep)
{
	ASSERT(lSrcData);
	ASSERT(lDstDataR);
	ASSERT(lDstDataG);
	ASSERT(lDstDataB);

	IppStatus ret = ippStsNoErr;
	IppiSize size = {iWidth, iHeight};
	
	switch (iSrcType)
	{
	case P8uC1:
		ret = ippStsNoOperation;
		break;
	case P16uC1:
		ret = ippStsNoOperation;
		break;
	case P8uC3:
	case P8uC3_1:
		{
		memset(lDstDataR, 0, iDstStep*iHeight);
		memset(lDstDataG, 0, iDstStep*iHeight);
		memset(lDstDataB, 0, iDstStep*iHeight);

		Ipp8u* ImageData[3] = {(Ipp8u*)lDstDataR,(Ipp8u*)lDstDataG,(Ipp8u*)lDstDataB};
		ret = ippiCopy_8u_C3P3R((const Ipp8u *)lSrcData, iSrcStep, ImageData, iDstStep,  size);
		
		break;
		}
	case P16uC3:
		{
		memset(lDstDataR, 0, iDstStep*iHeight);
		memset(lDstDataG, 0, iDstStep*iHeight);		
		memset(lDstDataB, 0, iDstStep*iHeight);

		Ipp16u* ImageData[3] = {(Ipp16u*)lDstDataB,(Ipp16u*)lDstDataG,(Ipp16u*)lDstDataR};
		ret =  ippiCopy_16u_C3P3R((const Ipp16u *)lSrcData, iSrcStep, ImageData, iDstStep,  size);

		break;
		}
	}
	return ret;
}

ABIAPI DWORD WINAPI MVC_BUF_RGBCompose(PBYTE lSrcDataR/*IN*/, PBYTE lSrcDataG/*IN*/, PBYTE lSrcDataB/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData/*out*/, int iDstStep)
{
	//ASSERT(lSrcDataR);
	//ASSERT(lSrcDataG);
	//ASSERT(lSrcDataB);
	ASSERT(lDstData);

	IppStatus ret = ippStsNoErr;
	IppiSize size = {iWidth, iHeight};
	
	BYTE* pR = lSrcDataR, *pG = lSrcDataG, *pB = lSrcDataB;
	if ( lSrcDataR == NULL){
		pR = new BYTE[iSrcStep*iHeight];
		memset(pR, 0, iSrcStep*iHeight);
	}
	if (lSrcDataG == NULL){
		pG = new BYTE[iSrcStep*iHeight];
		memset(pG, 0, iSrcStep*iHeight);
	}
	if (lSrcDataB == NULL){
		pB = new BYTE[iSrcStep*iHeight];
		memset(pB, 0, iSrcStep*iHeight);
	}
	switch (iSrcType)
	{
	case P8uC1:
		{
			const Ipp8u* const ImageData[3] = {(Ipp8u*)pB,(Ipp8u*)pG,(Ipp8u*)pR};
			ret = ippiCopy_8u_P3C3R(ImageData, iSrcStep, (Ipp8u*)lDstData, iDstStep, size);
		}
		break;
	case P16uC1:
		{
			const Ipp16u* const ImageData[3] = {(Ipp16u*)pR,(Ipp16u*)pG,(Ipp16u*)pB};
			ret = ippiCopy_16u_P3C3R(ImageData, iSrcStep, (Ipp16u*)lDstData, iDstStep, size);
		}
		break;
	case P8uC3:
	case P8uC3_1:
		ret = ippStsNoOperation;
		break;
	case P16uC3:
		ret = ippStsNoOperation;
		break;
	}
	if ( lSrcDataR == NULL){
		delete[] pR;
	}
	if (lSrcDataG == NULL){
		delete[] pG;
	}
	if (lSrcDataB == NULL){
		delete[] pB;
	}
	return ret;
}

ABIAPI DWORD WINAPI MVC_BUF_Resize(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData/*OUT*/, int iDstStep, double dXFactor, double dYFactor)
{
	ASSERT(lSrcData);
	ASSERT(lDstData);

	IppStatus ret = ippStsNoErr;
	IppiSize SrcSize = {iWidth, iHeight};
	IppiSize DstSize = {iWidth*dXFactor, iHeight*dYFactor};
	IppiRect  rect = {0, 0, iWidth, iHeight};

	switch (iSrcType)
	{
	case P8uC1:
		ret = ippiResize_8u_C1R((const Ipp8u*)lSrcData, SrcSize, iSrcStep, rect, (Ipp8u*)lDstData, iDstStep, DstSize, dXFactor, dYFactor, IPPI_INTER_LINEAR);
		break;
	case P16uC1:
		ret = ippiResize_16u_C1R((const Ipp16u*)lSrcData, SrcSize, iSrcStep, rect, (Ipp16u*)lDstData, iDstStep, DstSize, dXFactor, dYFactor, IPPI_INTER_LINEAR);
		break;
	case P8uC3:
	case P8uC3_1:
		ret = ippiResize_8u_C3R((const Ipp8u*)lSrcData, SrcSize, iSrcStep, rect, (Ipp8u*)lDstData, iDstStep, DstSize, dXFactor, dYFactor, IPPI_INTER_LINEAR);
		break;
	case P16uC3:
		ret = ippiResize_16u_C3R((const Ipp16u*)lSrcData, SrcSize, iSrcStep, rect, (Ipp16u*)lDstData, iDstStep, DstSize, dXFactor, dYFactor, IPPI_INTER_LINEAR);
		break;
	}
	return ret;
}
ABIAPI DWORD WINAPI MVC_BUF_Max(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, unsigned short* iMaxValue/*OUT*/)
{
	ASSERT(lSrcData);
	ASSERT(iMaxValue);

	IppStatus ret = ippStsNoErr;
	IppiSize SrcSize = {iWidth, iHeight};

	switch (iSrcType)
	{
	case P8uC1:
		ret = ippiMax_8u_C1R((const Ipp8u*)lSrcData, iSrcStep, SrcSize, (Ipp8u*)iMaxValue);
		break;
	case P16uC1:
		ret = ippiMax_16u_C1R((const Ipp16u*)lSrcData, iSrcStep, SrcSize, (Ipp16u*)iMaxValue);
		break;
	case P8uC3:
	case P8uC3_1:
		{
		Ipp8u iMax[3] = {0};
		ret = ippiMax_8u_C3R((const Ipp8u*)lSrcData, iSrcStep, SrcSize, iMax);
		*iMaxValue = max((int)iMax[0], (int)iMax[1]);
		*iMaxValue = max(*iMaxValue, (int)iMax[2]);
		}
		break;
	case P16uC3:
		{
		Ipp16u arrMax[3] = {0};
		ret = ippiMax_16u_C3R((const Ipp16u*)lSrcData, iSrcStep, SrcSize, arrMax);
		*iMaxValue = max((int)arrMax[0], (int)arrMax[1]);
		*iMaxValue = max(*iMaxValue, (int)arrMax[2]);
		}
		break;
	}
	return ret;
}
ABIAPI DWORD WINAPI MVC_BUF_Min(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight,unsigned short* iMinValue/*OUT*/)
{
	ASSERT(lSrcData);
	ASSERT(iMinValue);

	IppStatus ret = ippStsNoErr;
	IppiSize SrcSize = {iWidth, iHeight};

	switch (iSrcType)
	{
	case P8uC1:
		ret = ippiMin_8u_C1R((const Ipp8u*)lSrcData, iSrcStep, SrcSize, (Ipp8u*)iMinValue);
		break;
	case P16uC1:
		ret = ippiMin_16u_C1R((const Ipp16u*)lSrcData, iSrcStep, SrcSize, (Ipp16u*)iMinValue);
		break;
	case P8uC3:
	case P8uC3_1:
		{
		Ipp8u arrMin[3] = {0};
		ret = ippiMin_8u_C3R((const Ipp8u*)lSrcData, iSrcStep, SrcSize, arrMin);
		*iMinValue = max((int)arrMin[0], (int)arrMin[1]);
		*iMinValue = max(*iMinValue, (int)arrMin[2]);
		}
		break;
	case P16uC3:
		{
		Ipp16u arrMin[3] = {0};
		ret = ippiMin_16u_C3R((const Ipp16u*)lSrcData, iSrcStep, SrcSize, arrMin);
		*iMinValue = max((int)arrMin[0], (int)arrMin[1]);
		*iMinValue = max(*iMinValue, (int)arrMin[2]);
		}
		break;
	}
	return ret;
}

//ABIAPI DWORD WINAPI MVC_BUF_CreateGammaC3LUT(double dMax, double dMin, double dGamma)
//{
//	int ret = 0;
//
//	ret = MV_CreateLUT(LUT_16BIT_8BIT_GAMMA_C3, dMax, dMin, 65536, dGamma);
//	return ret;
//}

ABIAPI DWORD WINAPI MVC_BUF_CreateGammaLUT(double dMax, double dMin, double dGamma, bool bIsInvert)
{
	int ret = 0;
	if (dGamma == 0)
		return -1;

	ret = MV_CreateLUT(LUT_16BIT_8BIT_GAMMA, dMax, dMin, 65536, dGamma, bIsInvert);
	return ret;
}


ABIAPI DWORD WINAPI MVC_BUF_ScaleSingleChannel(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iMax, int iMin, double dGamma,  PBYTE lDstData/*OUT*/, int iDstStep, bool bIsInvert)
{
	ASSERT(lSrcData);
	ASSERT(lDstData);	

	int ret = 0;

	switch (iSrcType)
	{
	case P8uC1:
		MV_CreateLUT(LUT_16BIT_8BIT_GAMMA, iMax, iMin, 256, dGamma, bIsInvert);
		ret = MV_Scale_8u_C1_SingleChannel(lSrcData, iSrcType, iSrcStep, iWidth, iHeight, iMax, iMin, dGamma, lDstData, iDstStep);
		break;
	case P16uC1:
		MV_CreateLUT(LUT_16BIT_8BIT_GAMMA, iMax, iMin, 65536, dGamma, bIsInvert);
		ret = MV_Scale_16u8u_C1_SingleChannel(lSrcData, iSrcType, iSrcStep, iWidth, iHeight, iMax, iMin, dGamma, lDstData, iDstStep);
		break;
	case P8uC3:
	case P8uC3_1:
		ret = -1;
		break;
	case P16uC3:
		ret = -1;
		break;
	}
	
	return ret;
}


ABIAPI DWORD WINAPI MVC_BUF_Scale(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iMax, int iMin, double iGamma,  PBYTE lDstData/*OUT*/, int iDstStep, PBYTE lSaturationData, int iSaturationStep, bool bSaturation, int iSaturatonValue, int iColorGradation, bool bIsInvert)
{
	ASSERT(lSrcData);
	ASSERT(lDstData);	

	int ret = 0;

	switch (iSrcType)
	{
	case P8uC1:
		{
		MV_CreateLUT(LUT_16BIT_8BIT_GAMMA, iMax, iMin, 256, iGamma, bIsInvert);
		ret = MV_Scale_16u8u_C1(lSrcData, iSrcType, iSrcStep, iWidth, iHeight, iMax, iMin, iGamma, lDstData, iDstStep, lSaturationData, iSaturationStep, bSaturation, iSaturatonValue);
		break;
		}
	case P16uC1:
		{
		MV_CreateLUT(LUT_16BIT_8BIT_GAMMA, iMax, iMin, 65536, iGamma, bIsInvert);
		ret = MV_Scale_16u8u_C1(lSrcData, iSrcType, iSrcStep, iWidth, iHeight, iMax, iMin, iGamma, lDstData, iDstStep, lSaturationData, iSaturationStep, bSaturation, iSaturatonValue);
		break;
		}
	case P8uC3:
	case P8uC3_1:
		{
		Lock l(&gCritSect);
		MV_CreateLUT(LUT_16BIT_8BIT_GAMMA, iMax, iMin, 256, iGamma, bIsInvert);
		ret = MV_Scale_16u8u_C3(lSrcData, iSrcType, iSrcStep, iWidth, iHeight, iMax, iMin, iGamma, lDstData, iDstStep, lSaturationData, iSaturationStep, bSaturation, iSaturatonValue, iColorGradation);
		break;
		}
	case P16uC3:
		{
			Lock l(&gCritSect);
			//GPerfTimer.TimeBegin();
			//MV_CreateC3LUT(LUT_16BIT_8BIT_GAMMA_C3, iMax, iMin, 65536, iGamma);
			MV_CreateLUT(LUT_16BIT_8BIT_GAMMA, iMax, iMin, 65536, iGamma, bIsInvert);
			//dScaleProcessTime = GPerfTimer.TimeLastSecound()*1000;
			ret = MV_Scale_16u8u_C3(lSrcData, iSrcType, iSrcStep, iWidth, iHeight, iMax, iMin, iGamma, lDstData, iDstStep,lSaturationData, iSaturationStep, bSaturation, iSaturatonValue, iColorGradation);
			//dScaleWholeTime = GPerfTimer.TimeLastSecound()*1000;
			//char sDebug[256];
			//sprintf(sDebug, "Scale Processs CreateLut time = %.2f, whole time = %.2f\n", dScaleProcessTime, dScaleWholeTime);
			//OutputDebugString(sDebug);
		}
		break;
	}

	//print Process time
	//double dScaleProcessTime = GPerfTimer.TimeLastSecound()*1000;
	//char sDebug[256];
	//sprintf(sDebug, "Scale Processs Time = %.2f\n", dScaleProcessTime);
	//OutputDebugString(sDebug);

	return ret;
}
ABIAPI DWORD WINAPI MVC_BUF_GetAutoScaleValues(PBYTE lSrcData/*IN*/, 
											   int iSrcType, 
											   int iSrcStep, 
											   int iWidth, 
											   int iHeight, 
											   int* iMaxValue, 
											   int* iMinValue)
{
	ASSERT(lSrcData);

	int ret = 0;
	IppiSize size = {iWidth, iHeight};
	switch (iSrcType)
	{
	case P8uC1:
		{
			Ipp32s *histoR = new Ipp32s[256];
			//Ipp32s *histoG  = new Ipp32s[256];
			//Ipp32s *histoB  = new Ipp32s[256];
			memset(histoR, 0, sizeof(int) * 256);
			//memset(histoG, 0, sizeof(int) * 256);
			//memset(histoB, 0, sizeof(int) * 256);

			MV_Histogram_8u_C1(lSrcData, iWidth, iHeight, histoR);

			double iMinPos = 0, iMaxPos = 256, iTotalMin = 0, iTotalMax = 0;
			double iTotalPixel = iWidth * iHeight;

			for (int i = 0; i < 256; ++i)
			{
				iTotalMin += histoR[i];
				if (iTotalMin / iTotalPixel < 0.2) 
					iMinPos = i;
				iTotalMax += histoR[255 - i];
				if ( iTotalMax / iTotalPixel < 0.008) 
					iMaxPos = 255 - i;
				if (iTotalMin / iTotalPixel > 0.2 && iTotalMax / iTotalPixel > 0.008)
					break;
			}

			*iMaxValue = (int)iMaxPos;
			*iMinValue = (int)iMinPos;

			delete[] histoR;
			//delete[] histoG;
			//delete[] histoB;
			//break;
		}
		break;
	case P16uC1:
		{	
		Ipp32s *histo = new Ipp32s[65536];
		memset(histo, 0, sizeof(int)*65536);
		MV_Histogram_16u_C1(lSrcData, iWidth, iHeight, histo);
		double iMinPos=0, iMaxPos=65536, iTotalMin=0, iTotalMax=0;
		double iTotalPixel = iWidth*iHeight;
		for (int i = 0; i < 65536; ++i)
		{
			iTotalMin+=histo[i];
			if ( iTotalMin / iTotalPixel < 0.02) 
				iMinPos = i;
			iTotalMax+= histo[65535-i];
			if ( iTotalMax / iTotalPixel < 0.008) 
				iMaxPos = 65535-i;
			if (iTotalMin / iTotalPixel > 0.02 && iTotalMax / iTotalPixel > 0.008)
				break;
		}
		*iMaxValue = (int)iMaxPos;
		*iMinValue = (int)iMinPos;
		delete[] histo;
		break;
		}
	case P8uC3:
	case P8uC3_1:
		{
			Ipp32s *histoR = new Ipp32s[256];
			//Ipp32s *histoG  = new Ipp32s[256];
			//Ipp32s *histoB  = new Ipp32s[256];
			memset(histoR, 0, sizeof(int) * 256);
			//memset(histoG, 0, sizeof(int) * 256);
			//memset(histoB, 0, sizeof(int) * 256);

			MV_Histogram_8u_C3(lSrcData, iWidth, iHeight, histoR);

			double iMinPos = 0, iMaxPos = 256, iTotalMin = 0, iTotalMax = 0;
			double iTotalPixel = iWidth * iHeight;

			for (int i = 0; i < 256; ++i)
			{
				iTotalMin += histoR[i];
				if (iTotalMin / iTotalPixel < 0.2) 
					iMinPos = i;
				iTotalMax += histoR[255 - i];
				if ( iTotalMax / iTotalPixel < 0.008) 
					iMaxPos = 255 - i;
				if (iTotalMin / iTotalPixel > 0.2 && iTotalMax / iTotalPixel > 0.008)
					break;
			}

			*iMaxValue = (int)iMaxPos;
			*iMinValue = (int)iMinPos;

			delete[] histoR;
			//delete[] histoG;
			//delete[] histoB;
			//break;
		}
		break;
	case P16uC3:
		{
		Ipp32s *histoR = new Ipp32s[65536];
		//Ipp32s *histoG  = new Ipp32s[65536];
		//Ipp32s *histoB  = new Ipp32s[65536];
		memset(histoR, 0, sizeof(int)*65536);
		//memset(histoG, 0, sizeof(int)*65536);
		//memset(histoB, 0, sizeof(int)*65536);
		MV_Histogram_16u_C3(lSrcData, iWidth, iHeight, histoR);
		double iMinPos=0, iMaxPos=65536, iTotalMin=0, iTotalMax=0;
		double iTotalPixel = iWidth*iHeight;
		for (int i = 0; i < 65536; ++i)
		{
			iTotalMin+=histoR[i];
			if ( iTotalMin / iTotalPixel < 0.2) 
				iMinPos = i;
			iTotalMax+= histoR[65535-i];
			if ( iTotalMax / iTotalPixel < 0.008) 
				iMaxPos = 65535-i;
			if (iTotalMin / iTotalPixel > 0.2 && iTotalMax / iTotalPixel > 0.008)
				break;
		}
		*iMaxValue = (int)iMaxPos;
		*iMinValue = (int)iMinPos;
		delete[] histoR;
		//delete[] histoG;
		//delete[] histoB;
		break;
		}
	}

	return ret;
}


ABIAPI DWORD WINAPI MVC_BUF_AutoScale(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData/*OUT*/, int iDstStep, int* iMaxValue, int* iMinValue, PBYTE lSaturationData, int iSaturationStep, bool bSaturation, int iSaturatonValue, int iColorGradation)
{
	ASSERT(lSrcData);
	ASSERT(lDstData);

	int ret = 0;
	IppiSize size = {iWidth, iHeight};
	switch (iSrcType)
	{
	case P8uC1:
		ret = -1;
		break;
	case P16uC1:
		{
		//ret = MV_Scale_16u8u_C1(lSrcData, iSrcType, iSrcStep, iWidth, iHeight, iMax, iMin, iGamma, lDstData, iDstStep);
		//ret = ippiScale_16u8u_C1R((const Ipp16u*)lSrcData, iSrcStep, (Ipp8u*)lDstData, iDstStep, size, ippAlgHintAccurate);
		
		Ipp32s *histo = new Ipp32s[65536];// = {0};
		memset(histo, 0, sizeof(int)*65536);
		MV_Histogram_16u_C1(lSrcData, iWidth, iHeight, histo);
		double iMinPos=0, iMaxPos=65536, iTotalMin=0, iTotalMax=0;
		double iTotalPixel = iWidth*iHeight;
		for (int i = 0; i < 65536; ++i)
		{
			iTotalMin+=histo[i];
			if ( iTotalMin / iTotalPixel < 0.02) 
				iMinPos = i;
			iTotalMax+= histo[65535-i];
			if ( iTotalMax / iTotalPixel < 0.008) 
				iMaxPos = 65535-i;
			if (iTotalMin / iTotalPixel > 0.02 && iTotalMax / iTotalPixel > 0.008)
				break;
		}
		ret = MV_Scale_16u8u_C1(lSrcData, iSrcType, iSrcStep, iWidth, iHeight, (int)iMaxPos, (int)iMinPos, 1, lDstData, iDstStep, lSaturationData, iSaturationStep,  bSaturation, iSaturatonValue);
		*iMaxValue = (int)iMaxPos;
		*iMinValue = (int)iMinPos;
		delete[] histo;
		break;
		}
	case P8uC3:
	case P8uC3_1:
		ret = -1;
		break;
	case P16uC3:
		//ret = MV_Scale_16u8u_C3(lSrcData, iSrcType, iSrcStep, iWidth, iHeight, iMax, iMin, iGamma, lDstData, iDstStep);
		//ret = ippiScale_16u8u_C3R((const Ipp16u*)lSrcData, iSrcStep, (Ipp8u*)lDstData, iDstStep, size, ippAlgHintAccurate);
		{		
		Ipp32s *histoR = new Ipp32s[65536];// = {0};
		Ipp32s *histoG  = new Ipp32s[65536];//[65536] = {0};
		Ipp32s *histoB  = new Ipp32s[65536];//[65536] = {0};
		memset(histoR, 0, sizeof(int)*65536);
		memset(histoG, 0, sizeof(int)*65536);
		memset(histoB, 0, sizeof(int)*65536);
		//MV_Histogram_16u_C3(lSrcData, iWidth, iHeight, histoR, histoG, histoB);
		MV_Histogram_16u_C3(lSrcData, iWidth, iHeight, histoR);
		double iMinPos=0, iMaxPos=65536, iTotalMin=0, iTotalMax=0;
		double iTotalPixel = iWidth*iHeight;
		for (int i = 0; i < 65536; ++i)
		{
			iTotalMin+=histoR[i];
			if ( iTotalMin / iTotalPixel < 0.2) 
				iMinPos = i;
			iTotalMax+= histoR[65535-i];
			if ( iTotalMax / iTotalPixel < 0.008) 
				iMaxPos = 65535-i;
			if (iTotalMin / iTotalPixel > 0.2 && iTotalMax / iTotalPixel > 0.008)
				break;
		}
		ret = MV_Scale_16u8u_C3(lSrcData, iSrcType, iSrcStep, iWidth, iHeight, (int)iMaxPos, (int)iMinPos, 1, lDstData, iDstStep, lSaturationData, iSaturationStep,  bSaturation, iSaturatonValue, iColorGradation);
		*iMaxValue = (int)iMaxPos;
		*iMinValue = (int)iMinPos;
		delete[] histoR;
		delete[] histoG;
		delete[] histoB;
		break;
		}
	}
	return ret;
}

/*
	只是输出符合要求的点坐标，输出数组中被标志为1的位置
*/
ABIAPI DWORD WINAPI MVC_BUF_SaturationMask(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData/*OUT*/, int iDstStep, int iMax)
{
	ASSERT(lSrcData);
	ASSERT(lDstData);

	int ret = 0;

	switch (iSrcType)
	{
	case P8uC1:
		ret = -1;
		break;
	case P16uC1:
		ret = MV_SaturationMask_16u8u_C1(lSrcData, iSrcType, iSrcStep, iWidth, iHeight, lDstData, iDstStep, iMax);
		break;
	case P8uC3:
	case P8uC3_1:
		ret = -1;
		break;
	case P16uC3:
		ret = MV_SaturationMask_16u8u_C3(lSrcData, iSrcType, iSrcStep, iWidth, iHeight, lDstData, iDstStep, iMax);
		break;
	}
	return ret;
}

ABIAPI DWORD WINAPI MVC_BUF_SaturationScale(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iMax, int iMin, double iGamma, PBYTE lDstData/*OUT*/, int iDstStep, int iColor)
{
	ASSERT(lSrcData);
	ASSERT(lDstData);

	int ret = 0;

	switch (iSrcType)
	{
	case P8uC1:
		ret = -1;
		break;
	case P16uC1:
		ret = MV_Scale_16u8u_C1(lSrcData, iSrcType, iSrcStep, iWidth, iHeight, iMax, iMin, iGamma, lDstData, iDstStep, iColor);
		break;
	case P8uC3:
	case P8uC3_1:
		ret = -1;
		break;
	case P16uC3:
		ret = MV_Scale_16u8u_C3(lSrcData, iSrcType, iSrcStep, iWidth, iHeight, iMax, iMin, iGamma, lDstData, iDstStep, iColor);
		break;
	}
	return ret;
}

ABIAPI DWORD WINAPI MVC_BUF_GetPixel(PBYTE lSrcData, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iXPos, int iYPos, int* pValue)
{
	ASSERT(lSrcData);
	int ret = 0;

	switch (iSrcType)
	{
	case P8uC1:
		ret = MV_GetPixel_8u_C1(lSrcData, iSrcType, iSrcStep, iWidth, iHeight, iXPos, iYPos, pValue);
		break;
	case P16uC1:
		ret = MV_GetPixel_16u_C1(lSrcData, iSrcType, iSrcStep, iWidth, iHeight, iXPos, iYPos, pValue);
		break;
	case P8uC3:
	case P8uC3_1:
		ret = MV_GetPixel_8u_C3(lSrcData, iSrcType, iSrcStep, iWidth, iHeight, iXPos, iYPos, pValue);
		break;
	case P16uC3:
		ret = MV_GetPixel_16u_C3(lSrcData, iSrcType, iSrcStep, iWidth, iHeight, iXPos, iYPos, pValue);
		break;
	}
	return ret;
}
ABIAPI DWORD WINAPI MVC_BUF_SetPixel(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, int iXPos, int iYPos, int* pValue/*IN*/)
{
	ASSERT(lSrcData);
	int ret = 0;

	switch (iSrcType)
	{
	case P8uC1:
		ret = MV_SetPixel_8u_C1(lSrcData, iSrcType, iSrcStep, iWidth, iHeight, iXPos, iYPos, pValue);
		break;
	case P16uC1:
		ret = MV_SetPixel_16u_C1(lSrcData, iSrcType, iSrcStep, iWidth, iHeight, iXPos, iYPos, pValue);
		break;
	case P8uC3:
	case P8uC3_1:
		ret = MV_SetPixel_8u_C3(lSrcData, iSrcType, iSrcStep, iWidth, iHeight, iXPos, iYPos, pValue);
		break;
	case P16uC3:
		ret = MV_SetPixel_16u_C3(lSrcData, iSrcType, iSrcStep, iWidth, iHeight, iXPos, iYPos, pValue);
		break;
	}
	return ret;
}

ABIAPI DWORD WINAPI MVC_BUF_Invert(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData/*OUT*/)
{
	ASSERT(lSrcData);
	ASSERT(lDstData);
	IppStatus ret = ippStsNoErr;

	IppiSize size = {iWidth, iHeight};
	switch (iSrcType)
	{
	case P8uC1:
		ret = ippiSet_8u_C1R(255, lDstData, iSrcStep, size);
		ret = ippiSub_8u_C1RSfs(lSrcData, iSrcStep, lDstData, iSrcStep, lDstData, iSrcStep, size, 0);
		break;
	case P16uC1:
		ret = ippiSet_16u_C1R(65535, (Ipp16u*)lDstData, iSrcStep, size);
		ret = ippiSub_16u_C1RSfs((Ipp16u*)lSrcData, iSrcStep, (Ipp16u*)lDstData, iSrcStep, (Ipp16u*)lDstData, iSrcStep, size, 0);
		break;
	case P8uC3:
	case P8uC3_1:
		{
		Ipp8u data[3] = {255,255,255};
		ret = ippiSet_8u_C3R(data, lDstData, iSrcStep, size);
		ret = ippiSub_8u_C3RSfs(lSrcData, iSrcStep, lDstData, iSrcStep, lDstData, iSrcStep, size, 0);
		break;
		}
	case P16uC3:
		{
		Ipp16u data[3] = {65535,65535,65535};
		ret = ippiSet_16u_C3R(data, (Ipp16u*)lDstData, iSrcStep, size);
		ret = ippiSub_16u_C3RSfs((Ipp16u*)lSrcData, iSrcStep, (Ipp16u*)lDstData, iSrcStep, (Ipp16u*)lDstData, iSrcStep, size, 0);
		break;
		}
	}

	return ret;
}

ABIAPI DWORD WINAPI MVC_BUF_Despeckle(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData/*OUT*/, int iDstStep)
{
	ASSERT(lSrcData);
	ASSERT(lDstData);
	IppStatus ret = ippStsNoErr;

	IppiSize size = {iWidth-2, iHeight-2};
	BYTE* pSrc, *pDst;

	Ipp32s Kernel[3][3] = {
		{1, 1, 1},
		{1, 0, 1},
		{1, 1, 1}
	};
	IppiPoint Anchor = {1, 1};
	IppiSize KernelSize = {3, 3};

	switch (iSrcType)
	{
	case P8uC1:
		{
		pSrc = lSrcData + iSrcStep + 1;
		pDst = lDstData + iDstStep  + 1;
		ret = ippiFilter_8u_C1R(pSrc, iSrcStep, pDst, iDstStep, size, (Ipp32s*)Kernel, KernelSize, Anchor, 8);
		MV_Despeckle_8u_C1(pSrc, iSrcStep-2, iWidth-2, iHeight-2, pDst, iDstStep-2);
		break;
		}
	case P16uC1:
		{
		pSrc = lSrcData + iSrcStep + 1*2;
		pDst = lDstData + iDstStep  + 1*2;
		ret = ippiFilter_16u_C1R((Ipp16u*)pSrc, iSrcStep, (Ipp16u*)pDst, iDstStep, size, (Ipp32s*)Kernel, KernelSize, Anchor, 8);
		MV_Despeckle_16u_C1(pSrc, iSrcStep-2*2, iWidth-2, iHeight-2, pDst, iDstStep-2*2);
		break;
		}
	case P8uC3:
	case P8uC3_1:
		{
		pSrc = lSrcData + iSrcStep + 1*3;
		pDst = lDstData + iDstStep  + 1*3;
		ret = ippiFilter_8u_C3R(pSrc, iSrcStep, pDst, iDstStep, size, (Ipp32s*)Kernel, KernelSize, Anchor, 8);
		MV_Despeckle_8u_C3(pSrc, iSrcStep-2*3, iWidth-2, iHeight-2, pDst, iDstStep-2*3);
		break;
		}
	case P16uC3:
		{
		pSrc = lSrcData + iSrcStep + 1*2*3;
		pDst = lDstData + iDstStep  + 1*2*3;
		ret = ippiFilter_16u_C3R((Ipp16u*)pSrc, iSrcStep, (Ipp16u*)pDst, iDstStep, size, (Ipp32s*)Kernel, KernelSize, Anchor, 8);
		MV_Despeckle_16u_C3(pSrc, iSrcStep-2*2*3, iWidth-2, iHeight-2, pDst, iDstStep-2*2*3);
		break;
		}
	}

	return ret;
}

ABIAPI DWORD WINAPI MVC_BUF_Crop(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, PBYTE lDstData/*OUT*/, int iDstStep, int* pRect)
{
	ASSERT(lSrcData);
	ASSERT(lDstData);
	ASSERT(pRect);
	IppStatus ret = ippStsNoErr;

	IppiSize size = {pRect[2], pRect[3]};
	BYTE* pSrc = NULL;
	switch (iSrcType)
	{
	case P8uC1:
		pSrc = lSrcData + iSrcStep*pRect[1] + pRect[0];
		ret = ippiCopy_8u_C1R(pSrc, iSrcStep, lDstData, iDstStep, size);
		break;
	case P16uC1:
		pSrc = lSrcData + iSrcStep*pRect[1] + 2*pRect[0];
		ret = ippiCopy_16u_C1R((const Ipp16u*)pSrc, iSrcStep, (Ipp16u*)lDstData, iDstStep, size);
		break;
	case P8uC3:
	case P8uC3_1:
		pSrc = lSrcData + iSrcStep*pRect[1] + 3*pRect[0];
		ret = ippiCopy_8u_C3R(pSrc, iSrcStep, lDstData, iDstStep, size);
		break;
	case P16uC3:
		pSrc = lSrcData + iSrcStep*pRect[1] + 3*2*pRect[0];
		ret = ippiCopy_16u_C3R((const Ipp16u*)pSrc, iSrcStep, (Ipp16u*)lDstData, iDstStep, size);
		break;
	}

	return ret;
}

ABIAPI DWORD WINAPI MVC_BUF_TextOverlay(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, TCHAR* pText, int iTextLen, int* pRect, PBYTE lDstData/*OUT*/)
{
	ASSERT(lSrcData);
	ASSERT(lDstData);
	IppStatus ret = ippStsNoErr;
	BYTE* pSrc = NULL;

	HDC pDC = ::GetDC(0);
	HDC TmpDC=CreateCompatibleDC(pDC);
	RECT pos = {pRect[0], pRect[1], pRect[2], pRect[3]};
	
	CFont font;
		VERIFY(font.CreateFont(
			12,                        // nHeight
			0,                         // nWidth
			0,                         // nEscapement
			0,                         // nOrientation
			FW_NORMAL,                 // nWeight
			FALSE,                     // bItalic
			FALSE,                     // bUnderline
			0,                         // cStrikeOut
			ANSI_CHARSET,              // nCharSet
			OUT_DEFAULT_PRECIS,        // nOutPrecision
			CLIP_DEFAULT_PRECIS,       // nClipPrecision
			DEFAULT_QUALITY,           // nQuality
			DEFAULT_PITCH | FF_SWISS,  // nPitchAndFamily
			_T("Arial")));                 // lpszFacename

	switch (iSrcType)
	{
	case P8uC1:
		{
		LPBITMAPINFO bmInfo =(LPBITMAPINFO) new BYTE[sizeof(BITMAPINFOHEADER)+sizeof(RGBQUAD)*256];
		memset(bmInfo,0,sizeof(BITMAPINFOHEADER)+sizeof(RGBQUAD)*256);
		bmInfo->bmiHeader.biSize=sizeof(BITMAPINFOHEADER);
		bmInfo->bmiHeader.biWidth=iWidth;
		bmInfo->bmiHeader.biHeight=iHeight;
		bmInfo->bmiHeader.biPlanes=1;
		bmInfo->bmiHeader.biBitCount=8;
		bmInfo->bmiHeader.biClrUsed = 256;
		
		for(int i = 0; i < 256; ++i)
		{
			bmInfo->bmiColors[i].rgbBlue = i;
			bmInfo->bmiColors[i].rgbGreen = i;
			bmInfo->bmiColors[i].rgbRed = i;
		}

		BYTE *pbase; 

		BYTE *tmp = new BYTE[iSrcStep*iHeight];
		//颠倒
		for (int i = 0; i < iHeight; i++)
		{
			memcpy(tmp+i*iSrcStep, lSrcData+(iHeight-i-1)*iSrcStep, iSrcStep);
		}
		
		HBITMAP TmpBmp=CreateDIBSection(TmpDC,bmInfo,DIB_PAL_COLORS,(void**)&pbase,0,0);
		if (0 == SetDIBits(pDC, TmpBmp, 0, iHeight, tmp,bmInfo, DIB_PAL_COLORS))
			ret = ippStsErr;		

		HFONT oldFont = (HFONT)SelectObject(TmpDC, &font);
		HGDIOBJ TmpObj=SelectObject(TmpDC,TmpBmp);
		::DrawText(TmpDC,pText,iTextLen, &pos, /*DT_EDITCONTROL|DT_EXTERNALLEADING|*/DT_NOPREFIX);

		//memset(lDstData, 255, iSrcStep*iHeight);
		if (0 == GetDIBits(pDC, TmpBmp, 0, iHeight, tmp, bmInfo, DIB_RGB_COLORS))
			ret = ippStsErr;

		//颠倒
		for (int i = 0; i < iHeight; i++)
		{
			memcpy(lDstData+i*iSrcStep, tmp+(iHeight-i-1)*iSrcStep, iSrcStep);
		}

		SelectObject(TmpDC,oldFont);
		DeleteObject(SelectObject(TmpDC,TmpObj));

		delete[] tmp;
		delete[] bmInfo;
		break;
		}
	case P16uC1:
		break;
	case P8uC3:
	case P8uC3_1:
		{
		BITMAPINFO bmInfo;
		memset(&bmInfo.bmiHeader,0,sizeof(BITMAPINFOHEADER));
		bmInfo.bmiHeader.biSize=sizeof(BITMAPINFOHEADER);
		bmInfo.bmiHeader.biWidth=iWidth;
		bmInfo.bmiHeader.biHeight=iHeight;
		bmInfo.bmiHeader.biPlanes=1;
		bmInfo.bmiHeader.biBitCount=24;
		BYTE *pbase; 

		BYTE *tmp = new BYTE[iSrcStep*iHeight];
		//颠倒
		for (int i = 0; i < iHeight; i++)
		{
			memcpy(tmp+i*iSrcStep, lSrcData+(iHeight-i-1)*iSrcStep, iSrcStep);
		}
		
		HBITMAP TmpBmp=CreateDIBSection(TmpDC,&bmInfo,DIB_RGB_COLORS,(void**)&pbase,0,0);
		if (0 == SetDIBits(pDC, TmpBmp, 0, iHeight, tmp,&bmInfo, DIB_RGB_COLORS))
			ret = ippStsErr;

		

		HFONT oldFont = (HFONT)SelectObject(TmpDC, &font);
		HGDIOBJ TmpObj=SelectObject(TmpDC,TmpBmp);
		::DrawText(TmpDC,pText,iTextLen, &pos, /*DT_EDITCONTROL|DT_EXTERNALLEADING|*/DT_NOPREFIX);

		//memset(lDstData, 255, iSrcStep*iHeight);
		if (0 == GetDIBits(pDC, TmpBmp, 0, iHeight, tmp, &bmInfo, DIB_RGB_COLORS))
			ret = ippStsErr;

		//颠倒
		for (int i = 0; i < iHeight; i++)
		{
			memcpy(lDstData+i*iSrcStep, tmp+(iHeight-i-1)*iSrcStep, iSrcStep);
		}

		SelectObject(TmpDC,oldFont);
		DeleteObject(SelectObject(TmpDC,TmpObj));

		delete[] tmp;
		break;
		}
	case P16uC3:
		break;
	}

	
	DeleteDC(TmpDC);
	::ReleaseDC(NULL, pDC);
	return ret;
}

ABIAPI DWORD WINAPI MVC_BUF_TextMask(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lSrcDstData/*OUT*/, int iMaskStep)
{
	ASSERT(lSrcData);
	ASSERT(lSrcDstData);
	int ret = ippStsNoErr;

	BYTE* pSrc = NULL;
	switch (iSrcType)
	{
	case P8uC1:
		ret = MV_TextMask_8u_C1(lSrcData, iSrcType, iSrcStep, iWidth, iHeight, lSrcDstData, iMaskStep);
		break;
	case P16uC1:
		ret = MV_TextMask_16u_C1(lSrcData, iSrcType, iSrcStep, iWidth, iHeight, lSrcDstData, iMaskStep);
		break;
	case P8uC3:
	case P8uC3_1:
		ret = MV_TextMask_8u_C3(lSrcData, iSrcType, iSrcStep, iWidth, iHeight, lSrcDstData, iMaskStep);
		break;
	case P16uC3:
		ret = MV_TextMask_16u_C3(lSrcData, iSrcType, iSrcStep, iWidth, iHeight, lSrcDstData, iMaskStep);
		break;
	}

	return ret;
}

ABIAPI DWORD WINAPI MVC_BUF_Animation(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lMaskData/*OUT*/, int iMaskStep, int* pRect, PBYTE lDstData )
{
	ASSERT(lSrcData);
	ASSERT(lMaskData);
	ASSERT(lDstData);
	int ret = ippStsNoErr;

	BYTE* pSrc = NULL;
	switch (iSrcType)
	{
	case P8uC1:
		
		ret = MV_Animation_8u_C1(lSrcData, iSrcType, iSrcStep, iWidth, iHeight, lMaskData, iMaskStep, pRect, lDstData);
		break;
	case P16uC1:
		ret = MV_Animation_16u_C1(lSrcData, iSrcType, iSrcStep, iWidth, iHeight, lMaskData, iMaskStep, pRect, lDstData);
		break;
	case P8uC3:
	case P8uC3_1:
		ret = MV_Animation_8u_C3(lSrcData, iSrcType, iSrcStep, iWidth, iHeight, lMaskData, iMaskStep, pRect, lDstData);
		break;
	case P16uC3:
		ret = MV_Animation_16u_C3(lSrcData, iSrcType, iSrcStep, iWidth, iHeight, lMaskData, iMaskStep, pRect, lDstData);
		break;
	}

	return ret;
}

ABIAPI DWORD WINAPI MVC_BUF_ImageAdd(PBYTE lSrcDataDst/*IN*/, PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight)
{
	ASSERT(lSrcDataDst);
	ASSERT(lSrcData);

	int ret = ippStsNoErr;
	IppiSize size = {iWidth, iHeight};
	switch (iSrcType)
	{
	case P8uC1:
		ippiAdd_8u_C1IRSfs((Ipp8u*)lSrcData, iSrcStep, (Ipp8u*)lSrcDataDst, iSrcStep, size, 0);
		break;
	case P16uC1:
		ippiAdd_16u_C1IRSfs((Ipp16u*)lSrcData, iSrcStep, (Ipp16u*)lSrcDataDst, iSrcStep, size, 0);
		break;
	case P8uC3:
	case P8uC3_1:
		ippiAdd_8u_C1IRSfs((Ipp8u*)lSrcData, iSrcStep, (Ipp8u*)lSrcDataDst, iSrcStep, size, 0);
		break;
	case P16uC3:
		ippiAdd_16u_C3IRSfs((Ipp16u*)lSrcData, iSrcStep, (Ipp16u*)lSrcDataDst, iSrcStep, size, 0);
		break;
	}

	return ret;
}
ABIAPI DWORD WINAPI MVC_BUF_ImageSubC(PBYTE lSrcDstData/*IN*//*OUT*/, int iValue, int iSrcType, int iSrcStep, int iWidth, int iHeight)
{
	ASSERT(lSrcDstData);

	int ret = ippStsNoErr;
	IppiSize size = {iWidth, iHeight};
	switch (iSrcType)
	{
	case P8uC1:
		ippiSubC_8u_C1IRSfs((Ipp8u)iValue, (Ipp8u*)lSrcDstData, iSrcStep, size, 0);
		break;
	case P16uC1:
		ippiSubC_16u_C1IRSfs((Ipp16u)iValue, (Ipp16u*)lSrcDstData, iSrcStep, size, 0);
		break;
	case P8uC3:
	case P8uC3_1:
		ippiSubC_8u_C3IRSfs((const Ipp8u*)&iValue, (Ipp8u*)lSrcDstData, iSrcStep, size, 0);
		break;
	case P16uC3:
		ippiSubC_16u_C3IRSfs((const Ipp16u*)&iValue, (Ipp16u*)lSrcDstData, iSrcStep, size, 0);
		break;
	}

	return ret;
}

ABIAPI DWORD WINAPI MVC_BUF_ImageDivC(PBYTE lSrcDstData/*IN*//*OUT*/, int iDivisor, int iSrcType, int iSrcStep, int iWidth, int iHeight)
{
	ASSERT(lSrcDstData);

	int ret = ippStsNoErr;
	IppiSize size = {iWidth, iHeight};
	switch (iSrcType)
	{
	case P8uC1:
		ippiDivC_8u_C1IRSfs((Ipp8u)iDivisor, (Ipp8u*)lSrcDstData, iSrcStep, size, 0);
		break;
	case P16uC1:
		ippiDivC_16u_C1IRSfs((Ipp16u)iDivisor, (Ipp16u*)lSrcDstData, iSrcStep, size, 0);
		break;
	case P8uC3:
	case P8uC3_1:
		ippiDivC_8u_C3IRSfs((const Ipp8u*)&iDivisor, (Ipp8u*)lSrcDstData, iSrcStep, size, 0);
		break;
	case P16uC3:
		ippiDivC_16u_C3IRSfs((const Ipp16u*)&iDivisor, (Ipp16u*)lSrcDstData, iSrcStep, size, 0);
		break;
	}

	return ret;
}
ABIAPI DWORD WINAPI MVC_BUF_ImageMulC(PBYTE lSrcDstData/*IN*//*OUT*/,int iMultiplier, int iSrcType, int iSrcStep, int iWidth, int iHeight)
{
	ASSERT(lSrcDstData);

	int ret = ippStsNoErr;
	IppiSize size = {iWidth, iHeight};
	switch (iSrcType)
	{
	case P8uC1:
		ippiMulC_8u_C1IRSfs((Ipp8u)iMultiplier, (Ipp8u*)lSrcDstData, iSrcStep, size, 0);
		break;
	case P16uC1:
		ippiMulC_16u_C1IRSfs((Ipp16u)iMultiplier, (Ipp16u*)lSrcDstData, iSrcStep, size, 0);
		break;
	case P8uC3:
	case P8uC3_1:
		ippiMulC_8u_C3IRSfs((const Ipp8u*)&iMultiplier, (Ipp8u*)lSrcDstData, iSrcStep, size, 0);
		break;
	case P16uC3:
		ippiMulC_16u_C3IRSfs((const Ipp16u*)&iMultiplier, (Ipp16u*)lSrcDstData, iSrcStep, size, 0);
		break;
	}

	return ret;
}

ABIAPI DWORD WINAPI MVC_BUF_AddDivided16uTo32f(PBYTE lSrcData1, int iSrcStep, float* lSrcData2,  float* lDstData, int iDstStep, int iWidth, int iHeight, int iDivisorConstant)
{
	ASSERT(lSrcData1);
	ASSERT(lDstData);

	int ret = ippStsNoErr;
	IppiSize roi = {iWidth, iHeight };
	Ipp32f* pTmpBuffer = new Ipp32f[iWidth*iHeight];
	Ipp32f* pTmpBufferSrc2 = new Ipp32f[iWidth*iHeight];

	if (lSrcData2 != NULL){
		//memcpy((BYTE*)pTmpBufferSrc2, (BYTE*)lSrcData2, sizeof(float)*iWidth*iHeight);
		memcpy(pTmpBufferSrc2, lSrcData2, sizeof(float)*iWidth*iHeight);
	}

	// src1 process, saved lDstData
	ret = ippiConvert_16u32f_C1R((Ipp16u*)lSrcData1, iSrcStep, pTmpBuffer, iDstStep, roi);
	if (ippStsNoErr != ret){
		return ret;
	}
	ret = ippiDivC_32f_C1R(pTmpBuffer, iDstStep, (float)iDivisorConstant, lDstData, iDstStep, roi);
	if (ippStsNoErr != ret){
		return ret;
	}

	//add lSrcData2 and lDstData to lDstData
	if (lSrcData2 != NULL){
		ret = ippiAdd_32f_C1R(pTmpBufferSrc2, iDstStep, lDstData, iDstStep, lDstData, iDstStep, roi);
	}

	delete[] pTmpBufferSrc2;
	delete[] pTmpBuffer;
	return ret;
}

ABIAPI DWORD WINAPI MVC_BUF_Add16uTo32f(PBYTE srcData, int iSrcStep, float* dstData, int iDstStep, int iWidth, int iHeight)
{
	ASSERT(srcData);
	ASSERT(dstData);

	int ippStatus = ippStsNoErr;
	IppiSize roiSize = { iWidth, iHeight };

	//IppStatus ippiAdd_16u32f_C1IR(const Ipp16u* pSrc, int srcStep, Ipp32f* pSrcDst, int srcDstStep, IppiSize roiSize);
	ippStatus = ippiAdd_16u32f_C1IR((Ipp16u*)srcData, iSrcStep, (Ipp32f*)dstData, iDstStep, roiSize);

	return ippStatus;
}

ABIAPI DWORD WINAPI MVC_BUF_Scale32fTo16u(float* lSrcData, int iSrcStep, PBYTE lDstData, int iDstStep, int iWidth, int iHeight)
{
	ASSERT(lDstData);
	ASSERT(lSrcData);

	int ret = ippStsNoErr;
	IppiSize roi = { iWidth, iHeight};

	//IppStatus ippiConvert_32f16u_C1R(const Ipp32f* pSrc, int srcStep, Ipp16u* pDst, int dstStep, IppiSize roiSize, IppRoundMode roundMode);
	ret = ippiConvert_32f16u_C1R((const Ipp32f*)lSrcData, iSrcStep, (Ipp16u*)lDstData, iDstStep, roi, ippRndNear);

	return ret;
}

ABIAPI DWORD WINAPI MVC_BUF_Sub16u(PBYTE lSrc, PBYTE lSrcDst, int iSrcStep, int iDstStep, int iWidth, int iHeight)
{
	ASSERT(lSrc);
	ASSERT(lSrcDst);

	int ret = ippStsNoErr;
	IppiSize roi = { iWidth, iHeight};
	ret = ippiSub_16u_C1IRSfs((const Ipp16u*)lSrc, iSrcStep, (Ipp16u*)lSrcDst, iDstStep, roi, 0);
	if (ret != ippStsNoErr)
	{
		OutputDebugString(_T("MVC_BUF_Sub16u Failed!"));
	}
	return ret;
}

ABIAPI DWORD WINAPI MVC_BUF_DivC_32f_C1R(float* srcImageBuf, int iDivisor, float* dstImageBuf, int iWidth, int iHeight)
{
	int ret = ippStsNoErr;
	IppiSize roi = {iWidth, iHeight};
	int iStep = ((iWidth * sizeof(float)*8) + 31) / 32 * 4;
	ret = ippiDivC_32f_C1R(srcImageBuf, iStep, (float)iDivisor, dstImageBuf, iStep, roi);
	if (ippStsNoErr != ret)
	{
		OutputDebugString(_T("MVC_BUF_DivC_32f_C1R Failed!"));
	}

	return ret;
}

ABIAPI DWORD WINAPI MVC_BUF_MedianFilter(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData/*OUT*/, int iDstStep)
{
	ASSERT(lSrcData);
	ASSERT(lDstData);
	IppStatus ret = ippStsNoErr;

	IppiSize dstRoiSize = {iWidth-2, iHeight-2};
	IppiSize maskSize = {3, 3}; // kernel size
	IppiPoint anchor = {1, 1};
	BYTE* pSrc, *pDst;

	switch (iSrcType)
	{
	case P8uC1:
		{
		pSrc = lSrcData + iSrcStep + 1;
		pDst = lDstData + iDstStep  + 1;
		ret = ippiFilterMedian_8u_C1R(pSrc, iSrcStep, pDst, iDstStep, dstRoiSize, maskSize, anchor);
		break;
		}
	case P16uC1:
		{
		pSrc = lSrcData + iSrcStep + 1*2;
		pDst = lDstData + iDstStep  + 1*2;
		ret = ippiFilterMedian_16u_C1R((Ipp16u*)pSrc, iSrcStep, (Ipp16u*)pDst, iDstStep, dstRoiSize, maskSize, anchor);
		break;
		}
	case P8uC3:
	case P8uC3_1:
		{
		pSrc = lSrcData + iSrcStep + 1*3;
		pDst = lDstData + iDstStep  + 1*3;
		ret = ippiFilterMedian_8u_C3R(pSrc, iSrcStep, pDst, iDstStep, dstRoiSize, maskSize, anchor);
		break;
		}
	case P16uC3:
		{
		pSrc = lSrcData + iSrcStep + 1*2*3;
		pDst = lDstData + iDstStep  + 1*2*3;
		ret = ippiFilterMedian_16u_C3R((Ipp16u*)pSrc, iSrcStep, (Ipp16u*)pDst, iDstStep, dstRoiSize, maskSize, anchor);
		break;
		}
	}

	return ret;
}

ABIAPI DWORD WINAPI MVC_BUF_Histogram(PBYTE lSrc, int iSrcStep, int iSrcType, int iWidth, int iHeight, int** pHist, int** pLevels, int nLevels[3])
{
	ASSERT(lSrc);
	IppStatus ret = ippStsNoErr;

	
	IppiSize size = {iWidth, iHeight}; 
	switch (iSrcType)
	{
	case P8uC1:
		{
			ippiHistogramRange_8u_C1R((Ipp8u*)lSrc, iSrcStep, size, (Ipp32s*)(pHist[0]), (const Ipp32s*)(pLevels[0]), nLevels[0]);
			break;
		}
	case P16uC1:
		{
			memset((Ipp32s*)(pHist[0]), 0, 65536*sizeof(Ipp32s));
			ippiHistogramRange_16u_C1R((Ipp16u*)lSrc, iSrcStep, size, (Ipp32s*)(pHist[0]), (const Ipp32s*)(pLevels[0]), nLevels[0]);
		break;
		}
	case P8uC3:
		{
			ippiHistogramRange_8u_C3R((Ipp8u*)lSrc, iSrcStep, size, (Ipp32s**)pHist, (const Ipp32s**)pLevels, nLevels);
		break;
		}
	case P8uC3_1:
		{
			ippiHistogramRange_8u_C3R((Ipp8u*)lSrc, iSrcStep, size, (Ipp32s**)pHist, (const Ipp32s**)pLevels, nLevels);
			break;
		}
	case P16uC3:
		{
			ippiHistogramRange_16u_C3R((Ipp16u*)lSrc, iSrcStep, size, (Ipp32s**)pHist, (const Ipp32s**)pLevels, nLevels);
		break;
		}
	}

	return ret;
}

// Computes the intensity histogram of an image using equal bins.
// This function computes the intensity histogram of an image in the ranges
// specified by the values lowerLevel (inclusive), upperLevel (exclusive), and nLevels.
// The function operates on the assumption that all histogram bins have the same width
// and equal boundary values of the bins (levels).
ABIAPI DWORD WINAPI MVC_BUF_HistogramEven_16u_C1R(PBYTE lSrcData, int iSrcStep, int iWidth, int iHeight, int* pHist, int* pLevels, int nLevels, int nLowerLevel, int nUpperLevel)
{
	ASSERT(lSrcData);
	IppStatus result = ippStsNoErr;
	IppiSize size = { iWidth, iHeight }; 
	
	result = ippiHistogramEven_16u_C1R((const Ipp16u*)lSrcData, iSrcStep, size, (Ipp32s*)pHist, (Ipp32s*)pLevels, nLevels, nLowerLevel, nUpperLevel);

	return result;
}

ABIAPI DWORD WINAPI MVC_BUF_HistogramEven(PBYTE pSrcData, int iSrcStep, int iSrcType, int iWidth, int iHeight, int** pHist, int** pLevels, int nLevels[3], int nLowerLevel[3], int nUpperLevel[3])
{
	ASSERT(pSrcData);
	IppStatus retVal = ippStsNoErr;

	IppiSize size = { iWidth, iHeight };

	switch (iSrcType)
	{
	case P8uC1:
		{
		retVal = ippiHistogramEven_8u_C1R((Ipp8u*)pSrcData, iSrcStep, size, (Ipp32s*)(pHist[0]), (Ipp32s*)(pLevels[0]), nLevels[0], nLowerLevel[0], nUpperLevel[0]);
		break;
		}
	case P16uC1:
		{
		retVal = ippiHistogramEven_16u_C1R((Ipp16u*)pSrcData, iSrcStep, size, (Ipp32s*)(pHist[0]), (Ipp32s*)(pLevels[0]), nLevels[0], nLowerLevel[0], nUpperLevel[0]);
		break;
		}
	case P8uC3:
		{
		retVal = ippiHistogramEven_8u_C3R((Ipp8u*)pSrcData, iSrcStep, size, (Ipp32s**)pHist, (Ipp32s**)pLevels, nLevels, nLowerLevel, nUpperLevel);
		break;
		}
	case P8uC3_1:
		{
		retVal = ippiHistogramEven_8u_C3R((Ipp8u*)pSrcData, iSrcStep, size, (Ipp32s**)pHist, (Ipp32s**)pLevels, nLevels, nLowerLevel, nUpperLevel);
		break;
		}
	case P16uC3:
		{
		retVal = ippiHistogramEven_16u_C3R((Ipp16u*)pSrcData, iSrcStep, size, (Ipp32s**)pHist, (Ipp32s**)pLevels, nLevels, nLowerLevel, nUpperLevel);
		break;
		}
	}

	return retVal;
}

ABIAPI DWORD WINAPI MVC_BUF_DivAndScale(PBYTE srcImageData, int iSrcStep, PBYTE oprImageData, int iOprStep, float* resImageData, int iDstStep, int iWidth, int iHeight, int iUpperLimit)
{
	ASSERT(srcImageData);
	ASSERT(oprImageData);

	if (srcImageData == NULL || oprImageData == NULL)
	{
		return ippStsNullPtrErr;
	}

	int result = ippStsNoErr;
	IppiSize roiSize = { iWidth, iHeight };
	Ipp32f* pSrcBuffer = new Ipp32f[iWidth * iHeight];
	Ipp32f* pOprBuffer = new Ipp32f[iWidth * iHeight];

	result = ippiConvert_16u32f_C1R((Ipp16u*)srcImageData, iSrcStep, pSrcBuffer, iDstStep, roiSize);
	if (result != ippStsNoErr)
	{
		return result;
	}

	result = ippiConvert_16u32f_C1R((Ipp16u*)oprImageData, iOprStep, pOprBuffer, iDstStep, roiSize);
	if (result != ippStsNoErr)
	{
		return result;
	}

	result = ippiDiv_32f_C1R(pOprBuffer, iDstStep, pSrcBuffer, iDstStep, resImageData, iDstStep, roiSize);
	if (result != ippStsNoErr)
	{
		return result;
	}

	Ipp32f pixelMax = 0;
	ippiMax_32f_C1R((const Ipp32f*)resImageData, iDstStep, roiSize, &pixelMax);
	float scaled = iUpperLimit / pixelMax;

	result = ippiMulC_32f_C1R((const Ipp32f*)resImageData, iDstStep, scaled, (Ipp32f*)resImageData, iDstStep, roiSize);
	if (ippStsNoErr != result)
	{
		return result;
	}

	delete[] pSrcBuffer;
	delete[] pOprBuffer;

	return result;
}

/***ABIAPI DWORD WINAPI MVC_BUF_DivAndScale(PBYTE srcImageData, int iSrcStep, PBYTE oprImageData, int iOprStep, float** resImageData, int iDstStep, int iWidth, int iHeight, int iUpperLimit)
{
	ASSERT(srcImageData);
	ASSERT(oprImageData);

	if (srcImageData == NULL || oprImageData == NULL)
	{
		return ippStsNullPtrErr;
	}

	int result = ippStsNoErr;
	IppiSize roiSize = { iWidth, iHeight };
	Ipp32f* pSrcBuffer = new Ipp32f[iWidth * iHeight];
	Ipp32f* pOprBuffer = new Ipp32f[iWidth * iHeight];

	result = ippiConvert_16u32f_C1R((Ipp16u*)srcImageData, iSrcStep, pSrcBuffer, iDstStep, roiSize);
	if (result != ippStsNoErr)
	{
		return result;
	}

	result = ippiConvert_16u32f_C1R((Ipp16u*)oprImageData, iOprStep, pOprBuffer, iDstStep, roiSize);
	if (result != ippStsNoErr)
	{
		return result;
	}
	
	// Divides pixel values of an image by pixel values of
	// another image and places the results in the dividend source image.
	result = ippiDiv_32f_C1IR(pOprBuffer, iDstStep, pSrcBuffer, iDstStep, roiSize);
	if (result != ippStsNoErr)
	{
		return result;
	}

	Ipp32f pixelMax = 0;
	ippiMax_32f_C1R((const Ipp32f*)pSrcBuffer, iDstStep, roiSize, &pixelMax);
	Ipp32f scaled = iUpperLimit / pixelMax;

	// multiplies pixel values of an image and a constant,
	// and places the results in the same image.
	result = ippiMulC_32f_C1IR(scaled, pSrcBuffer, iDstStep, roiSize);
	if (ippStsNoErr != result)
	{
		return result;
	}

	*resImageData = &pSrcBuffer[0];
	//delete[] pSrcBuffer;
	delete[] pOprBuffer;

	return result;
}***/

ABIAPI DWORD WINAPI MVC_BUF_DivAndScaleWithROI(PBYTE srcImageData, int iSrcStep, PBYTE oprImageData, int iOprStep, float* resImageData, int iDstStep, int iWidth, int iHeight, int iUpperLimit, int iOffset)
{
	ASSERT(srcImageData);
	ASSERT(oprImageData);

	if (srcImageData == NULL || oprImageData == NULL)
	{
		return ippStsNullPtrErr;
	}

	int result = ippStsNoErr;
	IppiSize roiSize = { iWidth, iHeight};
	Ipp32f* pSrcBuffer = new Ipp32f[iWidth * iHeight];
	Ipp32f* pOprBuffer = new Ipp32f[iWidth * iHeight];

	result = ippiConvert_16u32f_C1R((Ipp16u*)srcImageData, iSrcStep, pSrcBuffer, iDstStep, roiSize);
	if (result != ippStsNoErr)
	{
		return result;
	}

	result = ippiConvert_16u32f_C1R((Ipp16u*)oprImageData, iOprStep, pOprBuffer, iDstStep, roiSize);
	if (result != ippStsNoErr)
	{
		return result;
	}

	result = ippiDiv_32f_C1R(pOprBuffer, iDstStep, pSrcBuffer, iDstStep, resImageData, iDstStep, roiSize);
	if (result != ippStsNoErr)
	{
		return result;
	}

	Ipp32f pixelMax = 0;
	IppiSize roiSizeOffset = { iWidth - (2*iOffset), iHeight - (2*iOffset) };
	ippiMax_32f_C1R(resImageData + ((iWidth * iOffset) + iOffset), iDstStep, roiSizeOffset, &pixelMax);
	float scaled = iUpperLimit / pixelMax;

	result = ippiMulC_32f_C1R((const Ipp32f*)resImageData, iDstStep, scaled, (Ipp32f*)resImageData, iDstStep, roiSize);
	if (ippStsNoErr != result)
	{
		return result;
	}

	delete[] pSrcBuffer;
	delete[] pOprBuffer;

	return result;
}

/***ABIAPI DWORD WINAPI MVC_BUF_DivAndScaleWithROI(PBYTE srcImageData, int iSrcStep, PBYTE oprImageData, int iOprStep, float** resImageData, int iDstStep, int iWidth, int iHeight, int iUpperLimit, int iOffset)
{
	ASSERT(srcImageData);
	ASSERT(oprImageData);

	if (srcImageData == NULL || oprImageData == NULL)
	{
		return ippStsNullPtrErr;
	}

	int result = ippStsNoErr;
	IppiSize roiSize = { iWidth, iHeight};
	Ipp32f* pSrcBuffer = new Ipp32f[iWidth * iHeight];
	Ipp32f* pOprBuffer = new Ipp32f[iWidth * iHeight];

	result = ippiConvert_16u32f_C1R((Ipp16u*)srcImageData, iSrcStep, pSrcBuffer, iDstStep, roiSize);
	if (result != ippStsNoErr)
	{
		return result;
	}

	result = ippiConvert_16u32f_C1R((Ipp16u*)oprImageData, iOprStep, pOprBuffer, iDstStep, roiSize);
	if (result != ippStsNoErr)
	{
		return result;
	}

	// Divides pixel values of an image by pixel values of
	// another image and places the results in the dividend source image.
	result = ippiDiv_32f_C1IR(pOprBuffer, iDstStep, pSrcBuffer, iDstStep, roiSize);
	if (result != ippStsNoErr)
	{
		return result;
	}

	Ipp32f pixelMax = 0;
	IppiSize roiSizeOffset = { iWidth - (2*iOffset), iHeight - (2*iOffset) };
	ippiMax_32f_C1R((const Ipp32f*)pSrcBuffer + ((iWidth * iOffset) + iOffset), iDstStep, roiSizeOffset, &pixelMax);
	//ippiMax_32f_C1R((const Ipp32f*)pSrcBuffer, iDstStep, roiSizeOffset, &pixelMax);
	Ipp32f scaled = iUpperLimit / pixelMax;

	// multiplies pixel values of an image and a constant,
	// and places the results in the same image.
	result = ippiMulC_32f_C1IR(scaled, pSrcBuffer, iDstStep, roiSize);
	if (ippStsNoErr != result)
	{
		return result;
	}

	*resImageData = &pSrcBuffer[0];
	//delete[] pSrcBuffer;
	delete[] pOprBuffer;

	return result;
}***/

ABIAPI DWORD WINAPI MVC_BUF_CopyReplicateBorder(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData/*OUT*/, int iDstStep, int iBorderSize)
{
	ASSERT(lSrcData);
	ASSERT(lDstData);
	IppStatus ret = ippStsNoErr;

	IppiSize srcRoi = { iWidth, iHeight };
	IppiSize dstRoi = { iWidth + (iBorderSize * 2), iHeight + (iBorderSize * 2)};
	int topBorderHeight  = iBorderSize;
	int leftBorderWidth  = iBorderSize;
	BYTE* pSrc, *pDst;
	
	switch (iSrcType)
	{
	case P8uC1:
		{
		pSrc = lSrcData;
		pDst = lDstData;
		ippiCopyReplicateBorder_16u_C1R((Ipp16u*)pSrc, iSrcStep, srcRoi, (Ipp16u*)pDst, iDstStep, dstRoi, topBorderHeight, leftBorderWidth);
		break;
		}
	case P16uC1:
		{
		pSrc = lSrcData;
		pDst = lDstData;
		ippiCopyReplicateBorder_16u_C1R((Ipp16u*)pSrc, iSrcStep, srcRoi, (Ipp16u*)pDst, iDstStep, dstRoi, topBorderHeight, leftBorderWidth);
		break;
		}
	case P8uC3:
	case P8uC3_1:
		{
		pSrc = lSrcData;
		pDst = lDstData;
		ippiCopyReplicateBorder_16u_C1R((Ipp16u*)pSrc, iSrcStep, srcRoi, (Ipp16u*)pDst, iDstStep, dstRoi, topBorderHeight, leftBorderWidth);
		break;
		}
	case P16uC3:
		{
		pSrc = lSrcData;
		pDst = lDstData;
		ippiCopyReplicateBorder_16u_C1R((Ipp16u*)pSrc, iSrcStep, srcRoi, (Ipp16u*)pDst, iDstStep, dstRoi, topBorderHeight, leftBorderWidth);
		break;
		}
	}

	return ret;
}

ABIAPI DWORD WINAPI MVC_BUF_CopyConstBorder(PBYTE lSrcData/*IN*/, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE lDstData/*OUT*/, int iDstStep, int iBorderSize, int constVal)
{
	ASSERT(lSrcData);
	ASSERT(lDstData);
	IppStatus ret = ippStsNoErr;

	IppiSize srcRoi = { iWidth, iHeight };
	IppiSize dstRoi = { iWidth + (iBorderSize * 2), iHeight + (iBorderSize * 2)};
	int topBorderHeight  = iBorderSize;
	int leftBorderWidth  = iBorderSize;
	BYTE* pSrc, *pDst;
	
	switch (iSrcType)
	{
	case P8uC1:
		{
		pSrc = lSrcData;
		pDst = lDstData;
		ippiCopyConstBorder_16u_C1R((Ipp16u*)pSrc, iSrcStep, srcRoi, (Ipp16u*)pDst, iDstStep, dstRoi, topBorderHeight, leftBorderWidth, constVal);
		break;
		}
	case P16uC1:
		{
		pSrc = lSrcData;
		pDst = lDstData;
		ippiCopyConstBorder_16u_C1R((Ipp16u*)pSrc, iSrcStep, srcRoi, (Ipp16u*)pDst, iDstStep, dstRoi, topBorderHeight, leftBorderWidth, constVal);
		break;
		}
	case P8uC3:
	case P8uC3_1:
		{
		pSrc = lSrcData;
		pDst = lDstData;
		ippiCopyConstBorder_16u_C1R((Ipp16u*)pSrc, iSrcStep, srcRoi, (Ipp16u*)pDst, iDstStep, dstRoi, topBorderHeight, leftBorderWidth, constVal);
		break;
		}
	case P16uC3:
		{
		pSrc = lSrcData;
		pDst = lDstData;
		ippiCopyConstBorder_16u_C1R((Ipp16u*)pSrc, iSrcStep, srcRoi, (Ipp16u*)pDst, iDstStep, dstRoi, topBorderHeight, leftBorderWidth, constVal);
		break;
		}
	}

	return ret;
}

ABIAPI DWORD WINAPI MVC_BUF_Convert8bppTo16bpp(PBYTE srcImageData, int iSrcType, int iSrcStep, int iWidth, int iHeight, PBYTE dstImageData, int iDstStep)
{
	ASSERT(srcImageData);
	ASSERT(dstImageData);

	if (srcImageData == NULL || dstImageData == NULL)
	{
		return ippStsNullPtrErr;
	}

	int result = ippStsNoErr;
	IppiSize roiSize = { iWidth, iHeight};
	BYTE* pSrc, *pDst;
	
	pSrc = srcImageData;
	pDst = dstImageData;

	switch (iSrcType)
	{
		case P8uC1:
			result = ippiConvert_8u16u_C1R((Ipp8u*)pSrc, iSrcStep, (Ipp16u*)pDst, iDstStep, roiSize);
			break;
			
		case P8uC3:
		case P8uC3_1:
			result = ippiConvert_8u16u_C3R((Ipp8u*)pSrc, iSrcStep, (Ipp16u*)pDst, iDstStep, roiSize);	
			break;
	}

	return result;
}

ABIAPI DWORD WINAPI MVC_BUF_MeanStdDev(PBYTE srcData, int iSrcType, int iSrcStep, int iWidth, int iHeight, double* pMean, double* pStdDev)
{
	ASSERT(srcData);

	if (srcData == NULL)
	{
		return ippStsNullPtrErr;
	}

	int result = ippStsNoErr;
	IppiSize roiSize = { iWidth, iHeight};
	BYTE* pSrc= srcData;

	switch (iSrcType)
	{
	case P8uC1:
		{
			ippiMean_StdDev_8u_C1R(pSrc, iSrcStep, roiSize, (Ipp64f*)pMean, (Ipp64f*)pStdDev);
			break;
		}
	case P16uC1:
		{
			ippiMean_StdDev_16u_C1R((const Ipp16u*)pSrc, iSrcStep, roiSize, (Ipp64f*)pMean, (Ipp64f*)pStdDev);
			break;
		}
	}

	return result;
}
