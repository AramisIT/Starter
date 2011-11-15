using System;
using System.Collections.Generic;
using System.IO;

namespace Starter
    {
    static class Program
        {
        const string LOADER_PATH = @"w:\Work\Temp Dreamering\C# projects\Starter\Loader\bin\Release\Loader.exe";

        private static string[] Args
            {
            get;
            set;
            }

        enum LoaderResult
            {
            Exit,
            Update,
            Error
            }

        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
            {
            Program.Args = args;
            LoaderResult loaderResult;
            do
                {
                loaderResult = RunLoader();
                } while ( loaderResult == LoaderResult.Update );
            }

        private static LoaderResult RunLoader()
            {
            AppDomain loaderDomain = AppDomain.CreateDomain("LoaderDomain", null, new AppDomainSetup { ApplicationBase = Environment.CurrentDirectory });
            loaderDomain.ExecuteAssembly(LOADER_PATH, Args);               

            object resultObj = loaderDomain.GetData("Result");
            LoaderResult result = LoaderResult.Error;

            if ( resultObj == null || TryResultConvert(resultObj, out result) )
                {
                LogError("Wrong loader result");
                AppDomain.Unload(loaderDomain);
                return result;
                }
            else
                {
                switch ( result )
                    {
                    case LoaderResult.Update:
                        object updateLoaderFilesInfoObj = loaderDomain.GetData("UpdateLoaderFilesInfo");
                        AppDomain.Unload(loaderDomain);
                        if ( updateLoaderFilesInfoObj == null || !( updateLoaderFilesInfoObj is SortedDictionary<string, string> ) )
                            {
                            LogError("Wrong loader update info");
                            return LoaderResult.Error;
                            }
                        SortedDictionary<string, string> updateLoaderFilesInfo = updateLoaderFilesInfoObj as SortedDictionary<string, string>;
                        UpdateFiles(updateLoaderFilesInfo);
                        break;

                    case LoaderResult.Error:
                        object errorMessage = loaderDomain.GetData("ErrorDescription");
                        if ( errorMessage != null && errorMessage is string )
                            {
                            LogError(errorMessage as string);
                            }
                        AppDomain.Unload(loaderDomain);
                        break;

                    default:
                        AppDomain.Unload(loaderDomain);
                        break;
                    }
                return result;
                }
            }

        private static void UpdateFiles( SortedDictionary<string, string> updateLoaderFilesInfo )
            {
            foreach ( KeyValuePair<string, string> pair in updateLoaderFilesInfo )
                {
                if ( pair.Value == null )
                    {
                    File.Delete(pair.Key);
                    }
                else
                    {
                    File.Move(pair.Key, pair.Value);
                    }
                }
            }

        private static bool TryResultConvert( object resultObj, out LoaderResult result )
            {
            result = LoaderResult.Error;
            if ( !( resultObj is Int32 ) )
                {
                return false;
                }

            int resultInt = ( int ) resultObj;
            if ( resultInt < 0 || Enum.GetValues(typeof(LoaderResult)).Length <= resultInt )
                {
                return false;
                }

            result = ( LoaderResult ) resultInt;
            return true;
            }

        private static void LogError( string errorMessage )
            {
            throw new NotImplementedException();
            }
        }
    }
