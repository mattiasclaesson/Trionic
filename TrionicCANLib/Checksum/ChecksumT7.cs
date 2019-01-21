using System;
using System.Collections.Generic;
using System.IO;
using NLog;
using System.Windows.Forms;
using TrionicCANLib.Firmware;

namespace TrionicCANLib.Checksum
{
    public class ChecksumT7
    {
        public const bool DO_NOT_AUTOFIXFOOTER = false;
        public const bool DO_NOT_AUTOCORRECT = false;

        static private Logger logger = LogManager.GetCurrentClassLogger();

        class csum_area_t
        {
            public long addr;
            public short length;
        };

        class CheckSum
        {
            public int checksumValue;
            public int checksumAddress;
        }

        static csum_area_t[] csumArea;

        static public ChecksumResult VerifyChecksum(string filename, bool autocorrect, bool autofixFooter, ChecksumDelegate.ChecksumUpdate delegateShouldUpdate)
        {
            csumArea = new csum_area_t[16];
            for (int i = 0; i < csumArea.Length; i++)
                csumArea[i] = new csum_area_t();

            ChecksumResult result = ChecksumResult.Ok;

            T7FileHeader t7InfoHeader = new T7FileHeader();
            t7InfoHeader.init(filename, autofixFooter);

            int sramOffset = t7InfoHeader.getSramOffset();
            int fwLength = t7InfoHeader.getFWLength();
            int calculatedFWChecksum = calculateFWChecksum(filename, sramOffset);
            uint calculatedF2Checksum = calculateF2Checksum(filename, 0, fwLength);
            int calculatedFBChecksum = calculateFBChecksum(filename, 0, fwLength);

            int readF2checksum = t7InfoHeader.getChecksumF2();
            int readFBchecksum = t7InfoHeader.getChecksumFB();

            if (readF2checksum != 0)
            {
                if (t7InfoHeader.getChecksumF2() != (int)calculatedF2Checksum)
                {
                    result = ChecksumResult.ChecksumF2Failed;
                }
            }
            if (t7InfoHeader.getChecksumFB() != calculatedFBChecksum)
            {
                result = ChecksumResult.ChecksumFBFailed;
            }
            if (getFWChecksum(filename, sramOffset) != calculatedFWChecksum)
            {
                result = ChecksumResult.ChecksumFWFailed;
            }

            if (result != ChecksumResult.Ok)
            {
                logger.Debug("Checksum was invalid, should be updated!");
                logger.Debug("calculatedF2Checksum = " + calculatedF2Checksum.ToString("X8") + " readF2checksum = " + readF2checksum.ToString("X8"));
                logger.Debug("calculatedFBChecksum = " + calculatedFBChecksum.ToString("X8") + " readFBchecksum = " + readFBchecksum.ToString("X8"));
                logger.Debug("calculatedFWChecksum = " + calculatedFWChecksum.ToString("X8") + " getFWChecksum = " + getFWChecksum(filename, sramOffset).ToString("X8"));

                if (autocorrect)
                {
                    result = updateChecksum(filename, t7InfoHeader, calculatedFWChecksum, calculatedF2Checksum, calculatedFBChecksum, sramOffset);
                }
                else
                {
                    if (delegateShouldUpdate(null, null, null))
                    {
                        result = updateChecksum(filename, t7InfoHeader, calculatedFWChecksum, calculatedF2Checksum, calculatedFBChecksum, sramOffset);
                    }
                }
            }

            return result;
        }

        static private ChecksumResult updateChecksum(string filename, T7FileHeader t7InfoHeader, int calculatedFWChecksum, uint calculatedF2Checksum, int calculatedFBChecksum, int sramOffset)
        {
            setFWChecksum(filename, calculatedFWChecksum, sramOffset);
            t7InfoHeader.setChecksumF2((int)calculatedF2Checksum);
            t7InfoHeader.setChecksumFB(calculatedFBChecksum);
            t7InfoHeader.save(filename);
            logger.Debug("Checksums updated");
            return ChecksumResult.Ok;
        }

        static public void UpdateChecksum(string filename, bool autoFixFooter)
        {
            csumArea = new csum_area_t[16];
            for (int i = 0; i < csumArea.Length; i++)
                csumArea[i] = new csum_area_t();

            T7FileHeader t7InfoHeader = new T7FileHeader();
            t7InfoHeader.init(filename, autoFixFooter);

            int sramOffset = t7InfoHeader.getSramOffset();
            int fwLength = t7InfoHeader.getFWLength();
            int calculatedFWChecksum = calculateFWChecksum(filename, sramOffset);
            uint calculatedF2Checksum = calculateF2Checksum(filename, 0, fwLength);
            int calculatedFBChecksum = calculateFBChecksum(filename, 0, fwLength);

            logger.Debug("calculatedF2Checksum = " + calculatedF2Checksum.ToString("X8"));
            logger.Debug("calculatedFBChecksum = " + calculatedFBChecksum.ToString("X8"));
            logger.Debug("calculatedFWChecksum = " + calculatedFWChecksum.ToString("X8"));

            updateChecksum(filename, t7InfoHeader, calculatedFWChecksum, calculatedF2Checksum, calculatedFBChecksum, sramOffset);
        }

        static private int getFWChecksum(string a_fileName, int sramOffset)
        {
            int returnValue = -1;
            FileStream fs = new FileStream(a_fileName, FileMode.Open, FileAccess.Read);
            int checksumArea = findChecksumArea(fs);
            if (checksumArea < 0)
            {
                fs.Close();
                return -1;
            }

            if (checksumArea > FileT7.Length)
            {
                checksumArea = checksumArea - sramOffset;
            }
            int positionInFile = findFWChecksum(fs, checksumArea).checksumAddress;
            logger.Debug("positionInfile: " + positionInFile.ToString("X8"));
            if (positionInFile > FileT7.Length)
            {
                positionInFile = positionInFile - sramOffset;
            }
            fs.Position = positionInFile;
            // read the checksum
            returnValue = Convert.ToInt32(fs.ReadByte()) << 24;
            returnValue += Convert.ToInt32(fs.ReadByte()) << 16;
            returnValue += Convert.ToInt32(fs.ReadByte()) << 8;
            returnValue += Convert.ToInt32(fs.ReadByte());
            logger.Debug("Checksum GS: " + returnValue.ToString("X8"));

            // if open software, maybe set to internal address?

            fs.Close();
            return returnValue;
        }

        static private void setFWChecksum(string a_fileName, int a_checksum, int sramOffset)
        {
            try
            {
                FileStream fs = new FileStream(a_fileName, FileMode.Open, FileAccess.ReadWrite);
                int checksumArea = findChecksumArea(fs);
                int positionInFile = findFWChecksum(fs, checksumArea).checksumAddress;
                if (positionInFile > FileT7.Length)
                {
                    positionInFile = positionInFile - sramOffset;
                }
                fs.Position = positionInFile;
                byte aByte;
                if (fs.Position < FileT7.Length)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        aByte = (byte)(a_checksum >> (24 - i * 8));
                        fs.WriteByte(aByte);
                    }
                }
                
                fs.Close();
            }
            catch (Exception E)
            {
                logger.Debug(E.Message);
            }
        }

        static private int calculateFWChecksum(string a_fileName, int sramOffset)
        {
            getFWChecksum(a_fileName, sramOffset);
            FileStream fs = new FileStream(a_fileName, FileMode.Open, FileAccess.Read);
            int checksum32 = 0;
            int checksum = 0;
            for (int i = 0; i < 16; i++)
            {
                checksum = calculateChecksum(fs, csumArea[i].addr, csumArea[i].length);
                checksum32 += checksum;
            }
            fs.Close();
            return checksum32;
        }

        static private uint calculateF2Checksum(string a_fileName, int start, int length)
        {
            FileStream fs = new FileStream(a_fileName, FileMode.Open, FileAccess.Read);
            uint[] xorTable = new uint[8] { 0x81184224, 0x24421881, 0xc33c6666, 0x3cc3c3c3,
                                           0x11882244, 0x18241824, 0x84211248, 0x12345678 };
            byte[] data = new byte[4];
            byte xorCount;
            uint temp = 0;
            uint checksum = 0;
            uint count = 0;
             
            fs.Position = start;
            checksum = 0;
            count = 0;
            xorCount = 1;

            while( count < length && fs.Position < 0x7FFFF )
            {
                data[0] = (byte)fs.ReadByte();
                data[1] = (byte)fs.ReadByte();
                data[2] = (byte)fs.ReadByte();
                data[3] = (byte)fs.ReadByte();
                temp = (uint)(data[0] << 24 | data[1] << 16 | data[2] << 8 | data[3]);
                checksum += temp ^ xorTable[xorCount++];
                if( xorCount > 7 ) xorCount = 0;
                count += 4;
            }     

            checksum ^= 0x40314081;
            checksum -= 0x7FEFDFD0;
            fs.Close();
            return checksum;    
        }

        static private int calculateFBChecksum(string a_fileName, int start, int length)
        {
            FileStream fs = new FileStream(a_fileName, FileMode.Open, FileAccess.Read);
            int fbChecksum = calculateChecksum(fs, (long)start, length);
            fs.Close();
            return fbChecksum;
        }

        static private int findChecksumArea(FileStream a_fileStream)
        {
            byte[] sequence = new byte[24] {0x48, 0xE7, 0x00, 0x3C, 0x24, 0x7C, 0x00, 0xF0,
                                            0x00, 0x00, 0x26, 0x7C, 0x00, 0x00, 0x00, 0x00,
                                            0x28, 0x7C, 0x00, 0xF0, 0x00, 0x00, 0x2A, 0x7C};
            byte[] seq_mask = new byte[24] {1, 1, 1, 1, 1, 1, 1, 1,
                                            0, 0, 1, 1, 1, 0, 0, 0,   
                                            1, 1, 1, 1, 0, 0, 1, 1};
            byte data;
            int i, max;
            i = 0;
            max = 0;

            while (a_fileStream.Position < 0x7FFFF)
            {
                data = (byte)a_fileStream.ReadByte();
                if( data == sequence[i] || seq_mask[i] == 0 )
                {
                    i++;
                }
                else
                {
                    if( i > max ) max = i;
                    i = 0;
                }
                if( i == 24 ) break;            
            }
            if( i == 24 )
            {
                return ((int)a_fileStream.Position - 24);
            }
            else
            {
                return -1;
            }
        }

        static private CheckSum findFWChecksum(FileStream a_fileStream, int areaStart)
        {
            logger.Debug("findFWChecksum with areaStart: " + areaStart.ToString("X8"));
             byte[] data = new byte[4];
             byte areaNumber = 0;
             int baseAddr = 0;
             int ltemp = 0;
             int csumAddr = 0;
             short csumLength = 0;
             CheckSum r_checkSum = new CheckSum();
             if (areaStart > 0x7FFFF)
             {
                 r_checkSum.checksumAddress = -1;
                 r_checkSum.checksumValue = -1;
                 return r_checkSum;
             }
             
             a_fileStream.Position = (areaStart + 22);
             
             while( a_fileStream.Position < 0x7FFFF )
             {
                    data[0] = (byte)a_fileStream.ReadByte();
                    data[1] = (byte)a_fileStream.ReadByte();
                    if( data[0] == 0x48 )
                    {
                        switch( data[1] )
                        {
                            case 0x6D:
                                data[0] = (byte)a_fileStream.ReadByte();
                                data[1] = (byte)a_fileStream.ReadByte();
                                csumAddr = baseAddr + (int)(data[0] << 8 | data[1]);
                                csumArea[areaNumber].addr = csumAddr;
                                areaNumber++;
                                break;
                            case 0x78:
                                data[0] = (byte)a_fileStream.ReadByte();
                                data[1] = (byte)a_fileStream.ReadByte();
                                csumLength = (short)(data[0] << 8 | data[1]);
                                csumArea[areaNumber].length = csumLength;
                                break;
                            case 0x79:
                                data[0] = (byte)a_fileStream.ReadByte();
                                data[1] = (byte)a_fileStream.ReadByte();
                                data[2] = (byte)a_fileStream.ReadByte();
                                data[3] = (byte)a_fileStream.ReadByte();
                                csumAddr = (int)(data[0] << 24 | data[1] << 16 | data[2] << 8 | data[3]);
                                csumArea[areaNumber].addr = csumAddr;
                                areaNumber++;
                                break;
                            default:
                                break;
                        }
                    }
                    else if( data[0] == 0x2A && data[1] == 0x7C )
                    {
                        data[0] = (byte)a_fileStream.ReadByte();
                        data[1] = (byte)a_fileStream.ReadByte();
                        data[2] = (byte)a_fileStream.ReadByte();
                        data[3] = (byte)a_fileStream.ReadByte();
                        ltemp = (int)(data[0] << 24 | data[1] << 16 | data[2] << 8 | data[3]);
                        if( ltemp < 0xF00000L )
                        {
                            baseAddr = ltemp;
                        }
                    }
                    else if( data[0] == 0xB0 && data[1] == 0xB9 )
                    {
                        data[0] = (byte)a_fileStream.ReadByte();
                        data[1] = (byte)a_fileStream.ReadByte();
                        data[2] = (byte)a_fileStream.ReadByte();
                        data[3] = (byte)a_fileStream.ReadByte();
                        csumAddr = (int)(data[0] << 24 | data[1] << 16 | data[2] << 8 | data[3]);
                        r_checkSum.checksumAddress = csumAddr;

                        long tmpPos = a_fileStream.Position;
                        a_fileStream.Position = r_checkSum.checksumAddress;
                        data[0] = (byte)a_fileStream.ReadByte();
                        data[1] = (byte)a_fileStream.ReadByte();
                        data[2] = (byte)a_fileStream.ReadByte();
                        data[3] = (byte)a_fileStream.ReadByte();
                        a_fileStream.Position = tmpPos;
                        r_checkSum.checksumValue = (int)(data[0] << 24 | data[1] << 16 | data[2] << 8 | data[3]);
                        
                        break;
                    }
             }
             return r_checkSum;
        }

        static private int calculateChecksum(FileStream a_fileStream, long start, int length)
        {
            byte[] data = new byte[4];
            byte checksum8;
            int checksum;
            int count;
             
            a_fileStream.Position = start;
            checksum = 0;
            count = 0;

            while( count < (length >> 2) && a_fileStream.Position < 0x7FFFF )
            {
                data[0] = (byte)a_fileStream.ReadByte();
                data[1] = (byte)a_fileStream.ReadByte();
                data[2] = (byte)a_fileStream.ReadByte();
                data[3] = (byte)a_fileStream.ReadByte();
                checksum += (int)(data[0] << 24 | data[1] << 16 | data[2] << 8 | data[3]);
                count++;
            }     
            count = count << 2;
            checksum8 = 0;

            while (count < length && a_fileStream.Position < 0x7FFFF)
            {
                data[0] = (byte)a_fileStream.ReadByte();
                checksum8 += data[0];
                count++;
            }

            checksum += checksum8;
            
            return checksum;
        }
    }  
}
