Sapphire Biomolecular Imager capture software release notes.

What's New:

07/09/2019 (v1.2.0709.0)
===========================
-Bug fixed: Image resize - added parameters validation: DPI value must be 96 or greater. New image width and/or height cannot be 0.
-Bug fixed: Save as PUB error - Save an inverted Chemi image as PUB throws an exception error (Error trying to invert an 8-bit display image).

06/13/2019 (v1.2.0613.0)
===========================
-Modified: Changed 658/638/685 channel LaserIntensity to be the same as 784 & 529 level position settings in config.xml. 

06/05/2019 (v1.2.0605.0)
===========================
-Added: PMT Compensation in individual parameters.

04/25/2019 (v1.2.0425.0)
===========================
-Bug fixed: User cancelled file save operation closes unsaved image.
    1. User attempt to close an unsaved image
    2. The application prompt the user to save a modified/unsaved file
    3. The user select 'Yes'
    4. File Save Dialog box popup for user input (for File location, file name, and file type)
    5. User selected 'Cancel' on the File Save Dialog box
    6. The unsaved image closes.

04/23/2019 (v1.2.0423.0)
===========================
-Bug fixed: Image contrasting display error on a merged image.
    1. Split an RGB image.
    2. Merge the image channels previously split.
    3. Select an image channel
    4. Change the Black/White contrast value
    5. Error creating the display image.
    (NOTE: to reproduce this error on the previous version - one of the original RGB channels must have auto-contrast enable, and when you do a merged, don't include the image channel that has auto-contrast enabled)
-Bug fixed: Stop sequential scan crashes the application.
    1. Start sequential scan
    2. Stop the sequential scan
    3. Prompt to save scanned image
    4. Select 'Yes'
    5. Application pop up an error message, or in the worse case, the application crashes (application closed).
-Bug fixed: Unexpected switch to Fluorescence when exit/close application in chemi mode
    1. Exit application in Chemi mode
    2. Re-launch the application
    3. Go to Gallery tab
    4. Go to Imaging tab, the application switch Fluorescence tab but the Chemi is still selected.


04/15/2019 (v1.2.0415.0)
===========================
-Bug fixed: Phosphor imaging tab not showing on Phosphor only system.
-Bug fixed: Removed laser's intensity check before setting (Phosphor only system without the D laser intensity value set in the system does not set the PGA and the APD gain when scanning).
-Modified: Wait 3 seconds after turning the lasers to allow them to warm up and comes to full set intensity before start scanning.
-Modified: Don't generate a new file name on image save, use the image tab title as the file name.
-Added: Display stack trace when an exception is caught.


03/15/2019 (v1.1.0315.0) [Released]
===========================
-Modified: Set the display image to auto-contrast when the scanned/captured image is sent to Gallery.


03/07/2019 (v1.1.0307.0)
===========================
-Bug fixed: AutoScan calculated a wrong signal level scanning on a sample with low signal (i.e. Laser A (784) on the Azure's black sample target (green Azure's logo)).
-Modified: Set Phosphor imaging tab in the same section as the lasers power setup (there's a multiple reads here to make we're able data from the scanner - now the Phosphor image tab will not show until the laser heads are homed).
-Modified: Display error messages in front of the main application window (so it doesn't get covered up by the main application window).


02/28/2019 (v1.1.0228.0)
===========================
-Added: ScanType (Sequential/Smartscan/Simultaneous) to image info.
-Modified: Renamed "Method" to "Protocol" (UI and message boxes only).
-Modified: Display sample type focus offset in the Sample Type dropdown menu.
-Modified: Correct the sequential scan's grayscale image generated file name (GeneratedName.tif_lasertype to GeneratedName_lasertype.tif)
-Bug fixed: Settings: Add Sample Type - 'Edit' function doesn't work if the focus position a decimal (Gel (0.50)).


02/01/2019 (v1.1.0201.0)
===========================
-Bug fixed (artf156968): Changing the application window from maximized to normal window and back to maximized window caused the scanning area selection to not be able to selection the full scan area (the size of the scanning area allowed to select depends on the grid window size before the application window maximized).


01/03/2019 (v1.1.0103.0)
===========================
-Modified: Changed sequential scan file name (or image tab title): Now uses the Azure's automatic generated file name format with laser's wavelength (YEAR-MONTHDAY-TIME_wavelength).

12/28/2018 (v1.1.0.1228)
=======================
-Modified: Improved SmartScan algorithm for automatically finding the 'optimal' laser intensity level.

12/26/2018 (v1.1.0.1226)
=======================
-Added: Sequential scan option in Fluorescence scan; once the "Sequential Scan" is selected, the laser channels will be scanned one by one sequentially, and the scanned images would be stored into gallery as gray scale images (but not saved to hard disk), the merged image is as before.

12/19/2018 (v1.1.0.1219)
=======================
- Added: AutoScan/SmartScan: automatically calculate the intensity level of the selected dyes. (3 total scans: 2 scans at 500um to calculate the intensity level + the final scan with the calculated intensity level).
- Added: Multiple attempts at reading the scanner's settings saved on the scanner (loop around until the settings are read or the number of tries reached (10 total attempts, 1 second delay between each attempt).  

11/16/2018 (v1.0.5.1116)
=======================
- Bug fixed: Chemi/camera mode: Flat correction not applied - error reading and setting the flat correction flag (config.xml).


09/26/2018 (v1.0.4.0926)
=======================
- Added: Added auto-save feature to Chemi and Visible imaging mode.
- Bug fixed: Stop/cancel visible image capture and stop/cancel darkmaster generation locked up the application.
- Modified: Make the edges of the selection box touches/connected when select the smallest/minimum area allowed.
- Modified: Remove the jagged edge around the grid (you see it when zoomed in) by adding another border around the grid and also align the left header (A-Z marking) with the grid lines.
- Modified: Make the scan region grid fit the window (based on the height on the client area - now the grid behave like Chemi mode) [previously on a high resolution monitor > 1920x1080 the grid does not fill the available area].
            The scan region grid will now also resizes when you resized the application window (except while it's scanning).


09/18/2018 (v1.0.4.0918)
=======================
- Bug fixed: Adding a new dye to a method pops up an exception error (non-fatal error - you can ignore the error message and continue). Bug re-introduced in the previous build.
- Bug fixed: Select "Close", "Close All", and "Print Report" does not closes the File menu.
             Select "Copy Area" does not closes the Copy popup menu.
- Bug fixed: Select the "Select" button then immediately select "Copy Area" throws an "Value does not fall within the expected range." exception error. Added an error checking and it will now prompt to reselect the region of interest.
- Modified: UI changes:
            - Changed the background color of the Gallery panel on the right to match the navigation panel's gray background color on the left.
            - Align Gallery panel with image viewer.
	    - Modified alignment and added blue border for "Advanced", "Create Darkmasters" and "Create Flats"

09/07/2018 (v1.0.3.0907)
=======================
- Modified: Setup - added an installation option "NIR-Q" (NIR + 520 [784/658/520]). Also removed "4 Channel Western" and "Visible Fluorescent Western" from default methods.


08/20/2018 (v1.0.3.0820)
=======================
- Modified: PMT protection. When the lid is opened while PMT gain is set higher than 5000, the software automatically reduce the gain to 5000; and automatically recover the gain value when the lid is closed; the software will wait for PMT gain setup stable (20 seconds after a new setting) before it starts image scan process.


08/14/2018 (v1.0.3.0814)
=======================
- Bug fixed: Chemi marker overlay - the blob finder has a problem finding blobs with the white blots and the white tray (it especially has a problem finding the blobs when the blots is placed in between the transparent plastic sheets). The blob finder will now only look for the blobs in the selected area, and if it failed to find any blob, it will make the selected region the blob. For the color marker, it will now also uses the green image channel to find the blob instead of the blue channel.


08/01/2018 (v1.0.3.0801)
=======================
- Bug fixed: Phosphor only system - select Phosphor Imaging tab on application launched.

07/18/2018 (v1.0.3.0718)
=======================
- Modified: Make sure PVCAM gain is set when collecting dark masters files (default value: 3).

07/10/2018 (v1.0.3.0710)
=======================
- Bug fixed: Setup - the software installer did not correctly remove laser types and imaging tabs when selecting certain installation options when it’s installed on a PC that has not run the Sapphire software on it before (or if the configurations files do not exist on the Sapphire ProgramData folder (C:\ProgramData\Azure Biosystems\Sapphire).

07/01/2018 (v1.0.3.0701)
=======================
- Added: Password protect Engineering UI software. Prompt for password on application launch. (Azure.ScannerEUI)
- Modified: Disabled Phosphor imaging field in 'Parameter Setup.' Setting Phosphor imaging flag currently not allowed in Engineering UI. (Azure.ScannerEUI)

06/29/2018 (v1.0.3.0629)
=======================
- Bug fixed: 'Black bar' fix (changed the USB transaction module, use synchronous reading method).

06/26/2018 (v1.0.3.0626)
=======================
- Bug fixed: PVCAM gain value incorrect set.
- Modified: Reorganize camera mode configuration settings.

06/21/2018 (v1.0.3.0621)
=======================
- Bug fixed: Error saving an image that's larger than 2GB. If estimated total file size is greater than 2GB (when the images are merged) - the software will not alert the user and scanned images will be save and place in Gallery as individual channels instead of automatically merging the scanned images (Only affecting 10 micron scan).
- Modified: Allow user to change the image DPI value below 96 (for actual size printing of scanned resolution of 500 and 1000).

06/14/2018 (v1.0.3.0614)
=======================
- Added: Scanner serial number field (Engineering UI).
- Bug fixed: Firmware read error (added back compatibility) (Engineering UI).
- Bug fixed: Incorrectly displaying the scan region in image info panel.
- Modified: Display image color channel along with the laser channel on image info panel.
- Modified: Setup - Updated Silicon Labs CP210x USB to UART Bridge driver (v10.1.3).

06/06/2018 (v1.0.3.0606)
=======================
- Bug Fixed: when scanning with settings: dx = 270mm, Quality = 2, resolution = 300 um, the data package will be lost. 
- Added: Engineering UI - added image channels merge feature.

04/19/2018 (v1.0.2.0419)
=======================
- Bug fixed: Adding a new laser/signal to a method then select 'Scan' throws an exception; will now pop up a message box if dye/intensity/color channel options are not selected.

04/17/2018 (v1.0.2.0417)
=======================
- Bug fixed: Turned off prescan channel(s) before prescan completed causes that image channel(s) to have an incomplete/partial image after the prescan is completed. 
- Bug fixed: Incorrectly display the estimated scan when the estimated scan time is greater than 24 hours.  

04/11/2018 (v1.0.2.0411)
=======================
- Added in EUI: automatically read the individual parameters when users open the 'parameters' window;
- Bug fixed: when scanning parameters set to dx = 200 mm, resolution = 200 um, quality = 8, data package would be lost on the USB transaction, which results in a bad ‘line lost’ image.

04/09/2018 (v1.0.2.0409)
=======================
- Bug fixed: Setup - if RGB or NIR installation option is selected, comment out the "4 Channel Western" method.
- Bug fixed: Creating multiplex image with any of the 4 channel is empty (no image file selected), and "Keep source images" option is unchecked - the application throws an exception because it's referencing image channel that doesn't point to any image while trying to close the source image.
- Modified: Allow the user to merge chemi image and the marker images as a 4 channel image (split the marker images and set chemi in the gray channel) to allow the user to contrast a faint chemi image channel with the marker image. 

03/28/2018 (v1.0.2.0328)
=======================
- Bug fixed: Error collecting/generating bias and dark masters. Bias and dark masters are also collected/generated with Gain: 3.
- Bug fixed: Image scaling error - now find the max pixel in the whole image instead of the 500x500 region in the middle of the image (Previously image scaling error occurred at 1x1 binning mode when the sample is not placed in the middle of the camera field of view (or when the brightest pixel is not within the 500x500 region).
- Bug fixed: Red and blue color channel swapped when pasting color marker to chemi image.
- Modified: Chemi auto-exposure - "Overexposure" now acquire the image with double calculated "Wide Dynamic Range" exposure time.
- Modified: Chemi marker - the marker image is now acquired with auto exposure (this is because of the change in the LED's). You will now need to create "fast" bias in all the binning modes (1x1, 2x2, 3x3, 4x4, 6x6, 8x8).
- Modified: Updated laser signal settings in the configuration file (config.xml).

03/21/2018 (v1.0.2.0321)
=======================
- Bug fixed: Saving unsaved file during image merge cause the wait animation to stay opened.
- Bug fixed: File saving error - 'Save' option does not bring up file save dialog (newly scanned image with auto save turned turned off)
- Bug fixed: File saving error - error saving 4-channel image (error extracting image channels to save as multipage ITFF - bug introduced in the previous build).
- Bug fixed: Allow the user set 0 as the focus position when editing a sample type.

03/19/2018 (v1.0.2.0319)
=======================
- Modified: Validate the focus position value. Prompt the user to power cycle the scanner on focus position read error.

03/14/2018 (v1.0.2.0314)
=======================
- Bug fixed: Enable/disable phosphor imaging on the scanner throws an exception. (Casting error: enable/disable phosphor is now in the 'Advanced' tab).

03/13/2018 (v1.0.2.0313)
=======================
- Bug fixed: Restrict the custom focus to 0 to 4.
- Bug fixed: Prompt the user to select a sample type or create a new sample type when saving a new method with 'Custom' sample type selected.
- Bug fixed: Allow the user to create a new sample type with 0 as focus position.
- Bug fixed: Set image DPI to 300 (if less than or equal to 96), otherwise set the DPI to the value specified.

03/09/2018 (v1.0.2.0309)
=======================
- Modified: Changed the size of the corner of the region selection tool so the user can select a smaller region of interest.

03/08/2018 (v1.0.2.0308)
=======================
- Added: 'None' to Multiplex to allow the user to deselect a selected channel.
- Added: 'Advanced' tab in 'Settings' 
- Added: 'Add Dye' to the 'General' tab in 'Settings' to allow user to add custom dye. (Deletion of the default dyes are not allowed, the user can only delete the dyes that they've added). Custom dyes will be save to 'CustSettings.xml'
- Added: 'Delete' option to allow user to delete the sample type.
- Added: 'Custom' to 'Sample Type' dropdown menu, when selected brings up custom slider.
- Added: Save as PUB in 'Save As' (Save the display as .TIFF file format).
- Bug fixed: Multiplex (or image merge) error.
- Bug fixed: Error displaying very large image. A very large multi-channel image contrast can overflow the integer arithmetic.
- Bug fixed: Phosphor imaging: incorrectly displaying the preview image when changing/resizing the scan area.
- Modified: Moved default scan signal table from 'SysSettings.xml' to 'Config.xml'
- Modified: Scan signal's 'LaserIntensity' in the 'Config.xml' is now in milliwatt (mW) and the actual intensity value will read from the scanner's firmware.
- Modified: Align the images in a different thread (try to avoid UI lagging when scanning a large area).
- Modified: 'General' tab is not password protected. Advanced/Create Darkmasters/Create Flats are still password protected.
- Modified: Hide the file name and folder selection when 'Auto Save' is turned off.
- Modified: Check if the 'auto save' folder exists before scanning.
- Modified: Print more image info when print with 'Print Report'
- Modified: Save Phosphor scan image with '_PI' instead of '_488'

02/15/2018 (v1.0.2.0215)
=======================
- Bug fixed: Incorrectly merged the scanned channels (automatic merged after the scan is completed).
- Modified: Changed the micron symbol from µM to µm.

02/14/2018 (v1.0.2.0214)
=======================
- Modified: Carry over the contrast values for the following image manipulation: Crop, Rotate (rotate 90, flip horizontal, flip vertical), and resize.
- Modified: Image alignment settings - don't prompt for application restart after applying the new settings.

02/13/2018 (v1.0.2.0213)
=======================
- Bug fixed: 3-channel RGB composite image not correctly displayed (select a channel, contrast it, then select the 'Composite' button - the merged channels not correctly displayed) (Introduced in the previous build).
- Bug fixed: Split/extract an 3-channel RGB image error. (Introduced in the previous build).

02/12/2018 (v1.0.2.0212)
=======================
- Added: 4-channel image support (Preview scan image and Gallery). [the 4 color channels are Red/Green/Blue/Gray]
- Added: Multiplex - added an option to keep the merged images opened or close after the images are merged (trying free up some memory; just in case we're working with large images).
- Bug fixed: Image resize - display image not corresponding to the resized image (throws exception if resized down, wrong pixel intensity value when resized up (when mouse-over a pixel)).
- Modified: When a multi-channel (or a single channel and the selected color channel is not gray) is scanned; the image will be added and display as color image in Gallery.
- Modified: 4-channel images will be saved as multi-page TIFF file. 3-channel RGB image are still saved as still save as 48-bit RGB image.
- Modified: Saturated pixels are now showing as pink-ish (R:255, G:159, B:217) instead of solid white. For single channel grayscale image, it's still displaying solid red as the saturated pixels.
- Modified: Setup - don't install the camera if chemi option is not selected.

01/17/2018 (v1.0.1.0117)
=======================
- Modified: Convert image pixel format Bgr32, Bgr24, Bgra32 to Rgb24 on image load. This will allow the capture software to open an image edit & saved with other imaging software.

01/16/2018 (v1.0.1.0116)
=======================
- Added: Two new flags 'Fluorescence2LinesAvgScan' and 'Phosphor2LinesAvgScan' that replaces 'IsUnidirectionalScan' in config.xml. This will allow 'unidirectional scan'/'two line average scan' to be enable either for Fluorescence or Phosphor or both.
- Added: Enable 'Phosphor2LinesAvgScan' by default (apply 2 lines average (or unidirectional) on Phosphor imaging scan).
- Added: Auto-exposure type on image info for Chemi image.
- Bug fixed: DeltaX out of range exception error when selected scan region is 0 to 25 (bug introduced in v1.0.1.0110).
- Bug fixed: Rotate an 90 degree throws an exception. When a rectangular image is rotated the image size changed, but our new image display scheme, the display image buffer is allocated once (so we had an image dimension mismatch), now when the image is rotated and the image width/height changed, a new display buffer is re-allocated (bug introduced in v1.0.0.1128).
- Modified: Don't apply LightShadeFix if 'Unidirectional' is enabled.
- Modified: Don't allow 32-bit image to be opened, currently not supported.

01/10/2018 (v1.0.1.0110)
=======================
- Added: Align the preview image (while scanning).
- Bug fixed: Scanned image stretches (y-axis) scanning at a scan speed other than 'Highest.'
- Bug fixed: Incorrectly display BGR color order .JPG file.
- Bug fixed: Moving the contrast slider bar with the left or right arrow key on the keyboard does not update the display image.

01/03/2018 (v1.0.1.0103)
=======================
- Added: Enable/disable Phosphor imaging in the firmware (the flag is written to the scanner). (Select any textbox, then press key combination: Ctrl + Shift + P to bring up the option) (Settings: General tab)
- Added: Transport lock option to lock the scan head. (Settings: General tab)
- Added: Artifact artf146474 : Ability to add notes/comments post image capture in the gallery section (option available under image info pulldown menu) (Gallery tab)
- Bug fixed: Artifact artf147033 : default image name doesn't populate after first scan

12/26/2017 (v1.0.0.1226)
=======================
- Modified: Methods dropdown menu not properly displayed.

12/22/2017 (v1.0.0.1222)
=======================
- Added: Added option to enable/disable Phosphor Imaging tab in the 'Settings' tab (Currently reading and writing to the flag registry).
- Added: CCD cooled indicator in Chemi tab. NOTE: in the chemi tab, the CCD cooled goes green at -10, but the CCD cooled indicator will go green at -19 and below in 'Create Darkmasters' tab.
- Modified: Removed 'Phosphor' and 'Chemi + Phosphor' from the software installation setup. Phosphor imaging will now be enabled/disabled from the a hidden control in the 'General' setup tab.
- Bug fixed: bug fixed in the 'Create Darkmasters' tab.

12/13/2017 (v1.0.0.1213)
=======================
- Bug fixed: File size estimate and estimate total scan time not displaying initially after application launched.
- Bug fixed: Modifying a default method without saving does not restore protocol settings when switching back to the protocol/method.
- Bug fixed: Setup - enable Phosphor imaging only when specified in setup.
- Bug fixed: In release build, the software sometimes has a problem detecting the hardware mode (scanner vs camera mode) when the software launch. When it has a problem detecting the hardware mode, it will now launch/switch to the scanner/fluorescence mode.
- Modified: Greying out (or disable) controls during scanning/chemi capture.
- Modified: Removed binning mode slider in Visible imaging (replicating the cSeries - the cSeries software does not a binning selection Gel/Visible image capture).

12/11/2017 (v1.0.0.1211)
=======================
- Added: Visible imaging tab.
- Added: Visible imaging settings in config.xml.
- Added: Display file size estimate before scanning.
- Added: "UnidirectionalScan" settings in Config.xml. The function averages two lines for a new line, aiming to reduce alternating bright & dark lines. Note that the function would double the scan time.
- Bug fixed: Error running Engineering UI in release build (*).
- Bug fixed: Error applying dark correction (bug introduced in the previous build).
- Bug fixed: Image info panel sometimes does not updating the image info when active image is changed.
- Bug fixed: Wrong image info in the merged/multiplexed image.
- Bug fixed: CCD temperature check window not displaying when clicking 5 times on the CCD cooled circular button in the 'Create Darkmasters' tab.
- Modified: Display an error message when clicking on Create Darkmasters or Create Flats when the camera is not connected.
- Modified: Scale the marker image to level the image intensity (make the background more white). The "imaging mat" must be white.
- Modified: Removed more force garbage collection (Seems to behave differently when running as x86 vs x64).
- Modified: Changed laser A wavelength from 780 to 784
- Modified: Increased signal levels from 5 to 10 (Default values based on 17091103)
- (*) NOTE: This version is built with Visual Studio 2015. When built with Visual Studio 2010, there's Common Language Runtime (CLR) run time error. The run time error is fixed when built with Visual Studio 2015.

11/30/2017 (v1.0.0.1130)
=======================
- Bug fixed: A very large image > 20000 x 20000 crashes the application with new image contrasting scheme - as work-around, in this extreme case, we're reverting to the way we previous contrast the image (Bug introduced in the previous build).
- Modified: Changed 785nm to 780nm (in the application and config file).
- Modified: Excitation and emission information in config.xml.
- Modified: Removed unneeded library (ControlzEx and Fluent Ribbon) from the project solution.
- Modified: Using new Xceed WPF Toolkit, Expression.Blend.Sdk (System.Windows.Interactivity.dll), ToggleSwitch and WPFFolderBrowser packages.

11/28/2017 (v1.0.0.1128)
=======================
- Modified: Added more field to image info (sample type/scan speed/intensity level/channels).
- Modified: Improving memory usage on image contrasting in Gallery.
- Modified: Allow user to add more than 5 signal intensity levels (NOTE: the software will assume that all the lasers will have the same number of signal/intensity levels).
- Modified: Remove .NET force garbage collection (may have cause application crashes).
- Bug fixed: Viewing a single channel on an RGB image, then select "Saturation", the display image is displaying in grayscale.
- Bug fixed: Allow the user manual keyboard entry of the contrast values (black/white/gamma). The value doesn't take effect until the enter is pressed.
- Bug fixed: Added 'LightShadeFix' flags to Phosphor imaging scan (previously setting the 'LightShadeFix' flags in config.xml does not affect Phosphor imaging scan).
- Bug fixed: Correct Phosphor imaging image info.

11/20/2017 (v1.0.0.1120)
=======================
- Bug fixed: PGA setting is different between Engineer GUI and User GUI; now it is unified.
- Bug fixed: Plug in USB devices while scanning causes missing of scanner; judge whether the device plugged in is the scanner or not before initialization of USB.
- Bug fixed: Setup - setup program not correctly modifying the SysSettings.xml.
- Modified: Changed the installation option from "RGBIR" to "RGBNIR"
- Modified: Turn off all lasers before starting a new scan (just in case the lasers were previously left on).

11/16/2017 (v1.0.0.1116)
=======================
- Modified: Changed installation option label "RGBIR" to "RGBNIR"

11/15/2017 (v1.0.0.1115)
=======================
- Added: A splash screen on application launched.
- Added: Azure.UpdateConfig project - a utility tool to update the config file during installation.
- Modified: Engineering GUI now uses the same 'config.xml' as the User's GUI. [C:\ProgramData\Azure Biosystems\Sapphire\config.xml]

11/14/2017 (v1.0.0.1114)
=======================
- Bug fixed: 4-channel scan image preview error.
- Modified: Removed signal/dye numbering (currently hiding to keep the formatting in tact).
- Modified: Added option to remove the Create Darkmasters and Create Flats when Chemi imaging is not available/visible.
