using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Copier
    {
    class Program
        {
        static void Main(string[] args)
            {
            string UpdateInfoPath = AppDomain.CurrentDomain.GetData("Path") as string;
            if ( File.Exists(UpdateInfoPath) )
                {
                string[] result = File.ReadAllLines(UpdateInfoPath);
                for ( int i = 0; i < result.Length; i++ )
                    {
                    try
                        {
                        string[] filepaths = result[i].Split('|');
                        if ( File.Exists(filepaths[0]) )
                            {
                            File.Copy(filepaths[0], filepaths[1], true);
                            //File.Delete(filepaths[0]);
                            }
                        }
                    catch ( Exception exp )
                        {
                        //exp.Message.Error();
                        //Error = true;
                        }
                    }
                //File.Delete(UpdateInfoPath);
                }
            }
        }
    }
