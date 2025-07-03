The Trionic CAN Flasher is an Open Source tool used to read and write software in Trionic5, Trionic 7 and Trionic 8 based ECU’s. It can also read software out of Motronic 9.6 based ECU’s and write calibration.

The tool can also be used to modify parameters in the ECU, such as SAI, Convertible and High Output.

![alt text](https://github.com/mattiasclaesson/Trionic/blob/master/trioniccanflasher.png "trioniccanflasher image")

The Trionic CAN Flasher currently supports the following interfaces:
* Lawicel CANUSB
* CombiAdapter
* OBDLink SX (Not supported for ME9.6)
* Just4Tronic
* Kvaser HS
* J2534 (Beta)

## System Requirements
A PC running Windows XP, Windows Vista, Windows 7, Windows 8.1 or Windows 10.
Microsoft .NET 4.0
Microsoft Visual C++ 2010 Redistributable Package (x86)

Download zip from releases and extract TrionicCANFlash.msi and setup.exe. 

Run the bootstrap file setup.exe. It will check for required frameworks before installation is started.

## Disclaimer
This is Open Source software tools that pokes around in your car's control system. The authors of the tools shall not be held accountable for how you decide to use the tools. If you are not careful, you can easily brick your car with these tools so please use this software with care.

# Documentation
Is included in the setup file, and also available here:
<a href=https://github.com/mattiasclaesson/Trionic/blob/master/TrionicCANFlasher/TrionicCanFlasher.pdf>Pdf</a>

# Quick start guide
This is a quick guide to help you get started with the Trionic CAN Flasher.

The first step is to download the latest version of the Trionic CAN Flasher. When downloaded, you should have the TrionicCANFlash.msi and Setup.exe files. Run the Setup.exe to install Trionic CAN Flasher. Setup.exe is a bootstrap file that will check for and download required frameworks from Microsoft.

The next step is to install the device you use to connect your computer to your car. There are multiple options, but this guide will focus on the OBDLink SX based alternative. It's not the best, but the usually the cheapest. <a href="http://www.obdlink.com/sxusb/">ODBLink SX USB</a>. Please note that the Trionic CAN Flasher does not work with Bluetooth or WiFi devices, but requires a cable connection.

When you have connected the device to your PC's and drivers has been installed, it's important to set the latency to 2ms.

Start the windows device manager, find the serial port (e.g. COM7) under Ports. Right click and select settings. Select tab port settings and click Advanced... button. Here you find latency. Set it to 2 ms.

Now you can start the Trionic CAN Flasher. 
Next is to select your ECU type in main screen.
Click on Settings and select Adapter type, Adapter and COM speed. In this example we select Trionic 8, OBDLink SX, COM7 and set Com speed to 2Mbit.

If your cable is also connected to your car's ODB port, you should now have contact. Put your key in Off position and it is highly recommended to have an external charger connected and that you do not touch anything during this process. We want to minimize signaling on the bus, which may disturb the process.

Try now by pressing <strong>Get ECU Info</strong>. You should now see logs that indicate that the ECU is being read, and when done a new window will pop up with some information about your car and your software.

If all this went well, it's time to read the software.

The process is:
<ol>
	<li>Have your key in On position</li>
	<li>Initiate action (Read ECU / Write ECU)</li>
	<li>When you see message <em>Starting bootloader</em> then you turn key to Off position</li>
</ol>
When you have clicked <strong>Read ECU</strong>, select a filename and watch the log window. This should take around 10 minutes and the result is that you have downloaded you software.

Next is to take it to T8Suite, do your changes and write it back to the car. Which is done by clicking <strong>Flash ECU</strong>. 
Before writing the flash its recommended to use a 12V battery charger.

If something goes wrong during flashing, don't panic. Just try to <strong>Recover ECU</strong> and re-install your original software. You might have to shut down and disconnect everything, but it is not likely anything is broken. Only that you do not have any software in your ECU.
