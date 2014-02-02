/*
 * The ADO.NET SQL provider for Transbase (tb.net20.dll) is installed with
 * the ADO.NET package and must be available via the project dependencies.
 */
using System;
using System.Data.Common;
using System.Data;

using Transaction.Transbase;

namespace ConsoleApplication
{
    class Program
    {
        private static DataSet set = new DataSet();

      private static void DisplayData(System.Data.DataTable table)
      {
         foreach (System.Data.DataRow row in table.Rows)
         {
            foreach (System.Data.DataColumn col in table.Columns)
            {
               Console.WriteLine("{0} = {1}", col.ColumnName, row[col]);
            }
         Console.WriteLine("============================");
         }
      }

      private static void ShowTable(DataTable table)
      {
          foreach (DataColumn col in table.Columns)
          {
              Console.Write("{0,-14}", col.ColumnName);
          }
          Console.WriteLine();

          foreach (DataRow row in table.Rows)
          {
              foreach (DataColumn col in table.Columns)
              {
                  if (col.DataType.Equals(typeof(DateTime)))
                      Console.Write("{0,-14:d}", row[col]);
                  else if (col.DataType.Equals(typeof(Decimal)))
                      Console.Write("{0,-14:C}", row[col]);
                  else
                      Console.Write("{0,-14}", row[col]);
              }
              Console.WriteLine();
          }
          Console.WriteLine();
      }


      private static void GetDataSetSpsHwdescription(TbConnection con, string command)
      {
          
          DbCommand com = con.CreateCommand();
          com.CommandText = command;

          DataTable table = new DataTable("sps_hwdescription");
          DataColumn workCol = table.Columns.Add("hwid", typeof(Int32));
          workCol.AllowDBNull = false;
          workCol.Unique = true;

          table.Columns.Add("hwlocid", typeof(Int32));
          table.Columns.Add("hwname", typeof(String));

          table.Load(com.ExecuteReader());
          set.Tables.Add(table);

          workCol.Dispose();
          com.Dispose();
          
          /*
          TbDataAdapter adapter = new TbDataAdapter();

          DataTable sampleTable = new DataTable("sps_hwdescription");
          TbCommand com = (TbCommand)con.CreateCommand();
          com.CommandText = command;
          adapter.SelectCommand = com;
          adapter.Fill(sampleTable);
          //adapter.Fill(sampleTable,"sps_schema");
           
          adapter.Dispose();
          com.Dispose();
          */
      }

      private static void GetDataSetSpsSchema(TbConnection con, string command)
      {
          DbCommand com = con.CreateCommand();
          com.CommandText = command;

          DataTable table = new DataTable("sps_schema");

          DataColumn workCol = table.Columns.Add("schemaid", typeof(Int32));
          workCol.AllowDBNull = false;
          workCol.Unique = true;

          table.Columns.Add("wmi", typeof(String));
          table.Columns.Add("salesmakecode", typeof(Int32));
          table.Columns.Add("protocol", typeof(Int32));
          table.Columns.Add("systemtype", typeof(Int32));
          table.Columns.Add("modelyearcode", typeof(Int32));
          table.Columns.Add("vinpattern", typeof(String));
          table.Columns.Add("snfrom", typeof(String));
          table.Columns.Add("snto", typeof(String));
          table.Columns.Add("hwid", typeof(Int32));
          table.Columns.Add("identtype", typeof(Int32));

          table.Load(com.ExecuteReader());
          set.Tables.Add(table);

          workCol.Dispose();
          com.Dispose();
      }

      private static void GetDataSetSpsHardware(TbConnection con, string command)
      {
          DbCommand com = con.CreateCommand();
          com.CommandText = command;

          DataTable table = new DataTable("sps_hardware");

          DataColumn workCol = table.Columns.Add("ecuid", typeof(Int32));
          //workCol.AllowDBNull = false;
          //workCol.Unique = true;

          DataColumn workCol2 = table.Columns.Add("hwid", typeof(Int32));
          //workCol2.AllowDBNull = false;
          //workCol2.Unique = true;

          table.Load(com.ExecuteReader());
          set.Tables.Add(table);

          workCol.Dispose();
          workCol2.Dispose();
          com.Dispose();
      }

      private static void GetDataSetSpsModules(TbConnection con, string command)
      {
          DbCommand com = con.CreateCommand();
          com.CommandText = command;

          DataTable table = new DataTable("sps_modules");

          DataColumn workCol = table.Columns.Add("ecuid", typeof(Int32));
          //workCol.AllowDBNull = false;
          //workCol.Unique = true;

          table.Columns.Add("moduleid", typeof(Int32));
          table.Columns.Add("modulename", typeof(String));

          table.Load(com.ExecuteReader());
          set.Tables.Add(table);

          workCol.Dispose();
          com.Dispose();
      }


      private static void GetDataSetSpsConfiguration(TbConnection con, string command)
      {
          DbCommand com = con.CreateCommand();
          com.CommandText = command;

          DataTable table = new DataTable("sps_configuration");

          DataColumn workCol = table.Columns.Add("vehicleid", typeof(Int32));
          //workCol.AllowDBNull = false;
          //workCol.Unique = true;

          table.Columns.Add("ecuid", typeof(Int32));

          table.Load(com.ExecuteReader());
          set.Tables.Add(table);

          workCol.Dispose();
          com.Dispose();
      }

      static void Main(string[] args)
        {
            try
            {
                TbConnection con = new TbConnection("t2w_spssbdb:5024", "tbadmin", "dgs");
                
                con.Open();

                //----- SPS_VERSION
                //{
                //    DbCommand com = con.CreateCommand();
                //    com.CommandText = "select item,version from sps_version";
                //    DbDataReader dr = com.ExecuteReader();

                //    Console.Out.WriteLine(com.CommandText);
                //    while (dr.Read())
                //    {
                //        string tab = dr.GetString(0) + " " + dr.GetString(1);
                //        Console.Out.WriteLine(tab);
                //    }
                //    dr.Close();
                //    dr.Dispose();
                //    com.Dispose();
                //}
                //--------------------------

                //----- SPS_BLOBS
                //{
                //    DbCommand com = con.CreateCommand();
                //    com.CommandText = "select moduleid,moduleblob,blobsize from sps_blobs";
                //    DbDataReader dr = com.ExecuteReader();

                //    Console.Out.WriteLine(com.CommandText);
                //    while (dr.Read())
                //    {
                //        string tab = dr.GetString(0) + " " + dr.GetString(1) + " " + dr.GetString(2);
                //        Console.Out.WriteLine(tab);

                //        int size = dr.GetInt32(2);
                //        byte[] binary = new byte[size];
                //        dr.GetBytes(1, 0, binary,0,size);
                //    }
                //    dr.Close();
                //    dr.Dispose();
                //    com.Dispose();
                //}
                //--------------------------

                //----- SPS_SCHEMA
                //{
                //    DbCommand com = con.CreateCommand();
                //    com.CommandText = "select schemaid,wmi,salesmakecode,protocol,systemtype,modelyearcode,vinpattern,snfrom,snto,hwid,identtype from sps_schema";
                //    DbDataReader dr = com.ExecuteReader();

                //    Console.Out.WriteLine(com.CommandText);
                //    while (dr.Read())
                //    {
                //        string tab = dr.GetString(0) + " " + dr.GetString(1) + " " + dr.GetString(2) + " " + dr.GetString(3) + " " + dr.GetString(4) + " " + dr.GetString(5) + " " + dr.GetString(6) + " " + dr.GetString(7) + " " + dr.GetString(8) + " " + dr.GetString(9) + " " + dr.GetString(10) + " " + dr.GetString(10);
                //        Console.Out.WriteLine(tab);
                //    }
                //    dr.Close();
                //    dr.Dispose();
                //    com.Dispose();
                //}
                //--------------------------

                // Now with DataSet instead
                string command = "select hwid,hwlocid,hwname from sps_hwdescription";
                GetDataSetSpsHwdescription(con, command);
                
                command = "select ecuid,hwid from sps_hardware";
                GetDataSetSpsHardware(con,command);

                command = "select ecuid,moduleid,modulename from sps_modules";
                GetDataSetSpsModules(con, command);

                command = "select vehicleid,ecuid from sps_configuration";
                GetDataSetSpsConfiguration(con, command);

                foreach (DataRow myDataRowC in set.Tables["sps_configuration"].Rows)
                {
                    Console.WriteLine("vehicleid: " + myDataRowC["vehicleid"]);

                    foreach (DataRow myDataRow1 in set.Tables["sps_hardware"].Rows)
                    {
                        if (myDataRow1["ecuid"].Equals(myDataRowC["ecuid"]))
                        {
                            Console.Write("\t\tecuid: " + myDataRow1["ecuid"]);
                            if (myDataRow1["hwid"] != null)
                            {
                                // Iterate to find name data.
                                foreach (DataRow myDataRow2 in set.Tables["sps_hwdescription"].Rows)
                                {
                                    if (myDataRow1["hwid"].Equals(myDataRow2["hwid"]))
                                    {
                                        Console.Write(" hwname: " + myDataRow2["hwname"]);
                                    }
                                }
                            }
                            else
                            {
                                Console.Write(" hwid: " + myDataRow1["hwid"]);
                            }

                            // Iterate to find name data.
                            foreach (DataRow myDataRow3 in set.Tables["sps_modules"].Rows)
                            {
                                if (myDataRow1["ecuid"].Equals(myDataRow3["ecuid"]))
                                {
                                    Console.WriteLine();
                                    Console.Write("\t\t\tmoduleid: " + myDataRow3["moduleid"]); // ------> GET binary from "select moduleid,moduleblob,blobsize from sps_blobs"
                                    Console.Write("\t\t\tmodulename: " + myDataRow3["modulename"]);
                                }
                            }

                            Console.WriteLine();
                            Console.WriteLine();
                        }
                    }
                }

                //
                set.Dispose();
                con.Close();
                con.Dispose();
            }

            catch (TbException tbx)
            {
                Console.Out.WriteLine("ErrorCode: " + tbx.Code);
                Console.Out.WriteLine(tbx.Message);
            }
            Console.Out.WriteLine("Application about to terminate. Press Enter to finish!");
            Console.In.ReadLine();
        }
    }

}
