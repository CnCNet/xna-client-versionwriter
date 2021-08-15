/*
 * Copyright © 2017-2021 Starkku
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute,
 * sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.IO;
using System.Reflection;
using Starkku.Utilities;

namespace VersionWriter
{
    class Program
    {
        static void Main(string[] args)
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            bool writeLogFile = false;
            bool verbose = false;
            bool quiet = false;
            bool supressInputs = false;

            foreach (string arg in args)
            {
                if (string.IsNullOrEmpty(arg))
                    continue;

                if (arg.StartsWith("/") || arg.StartsWith("-"))
                {
                    string option = arg.Substring(1).ToUpper();

                    switch (option)
                    {
                        case "L":
                        case "LOG":
                            writeLogFile = true;
                            break;
                        case "Q":
                        case "QUIET":
                            quiet = true;
                            break;
                        case "S":
                        case "SUPRESS":
                        case "SUPRESSINPUTS":
                            supressInputs = true;
                            break;
                        // Does not do anything right now.
                        case "V":
                        case "VERBOSE":
                            verbose = true;
                            break;
                        default:
                            break;
                    }
                }
                else if (Directory.Exists(arg))
                {
                    basePath = arg;

                    if (!basePath.EndsWith("\\"))
                        basePath += "\\";
                }
            }

            string titleLine = "CnCNet Client Version File Writer by Starkku v." + Assembly.GetEntryAssembly().GetName().Version;

            Console.WriteLine(titleLine);
            Console.WriteLine("Base directory: " + basePath);
            if (!supressInputs)
            {
                Console.WriteLine("");
                ConsoleColor defaultColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("WARNING: Continuing will generate new version info files and overwrites existing ones in the working directory.");
                Console.ForegroundColor = defaultColor;
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }

            Logger.Initialize(writeLogFile, verbose);
            Logger.WriteToConsole = false;

            Logger.Info(titleLine);
            Logger.Info("Base directory: " + basePath);

            Logger.WriteToConsole = !quiet;
            Logger.WriteTimestamps = false;
            Logger.WriteLogLevelLabels = false;

            VersionFileWriter versionWriter = new VersionFileWriter(basePath);

            if (!versionWriter.Initialized)
            {
                if (!supressInputs)
                    AskForExit();

                return;
            }

            if (!versionWriter.GenerateVersionFile(supressInputs))
            {
                if (!supressInputs)
                    AskForExit();

                return;
            }

            if (!supressInputs)
                AskForExit();
        }

        private static void AskForExit()
        {
            Console.WriteLine("");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
