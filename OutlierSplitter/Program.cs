using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace OutlierSplitter
{
    class Program
    {
        static void Main(string[] args)
        {
            List<string> allLines = new List<string>();
            double avg;
            double stddev;
            double sensitivity;

            // Check for filename parameter
            if (args.Length < 2)
            {
                Console.WriteLine("USAGE: OutlierSplitter <filename.csv> <column> <sensitivity>");
                Console.WriteLine("<filename.csv> = File Name of the CSV file to input data");
                Console.WriteLine("<column> = Column number of the data to be validated starting at 0");
                Console.WriteLine("<sensitivity> = Number between 0 and 1 that shapes the outlier test. 0.5 is the standard");
                Exit();
            }

            // Validate the sensitivity
            try
            {
                sensitivity = Convert.ToDouble(args[2]);
            }
            catch
            {
                sensitivity = 0.5;
            }

            if (sensitivity < 1e-3 || sensitivity > 1)
            {
                Console.WriteLine("Sensitivity is out of bounds (0.001-1.0)");
                Exit();
            }

            // Read in all of the lines into memory
            try
            {
                using (StreamReader r = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = r.ReadLine()) != null)
                    {
                        double n;
                        string[] parsedLine = ParseLine(line);
                        
                        //Only add lines where desired column has a numeric value
                        if (double.TryParse(parsedLine[Convert.ToInt32(args[1])],out n)) allLines.Add(line);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot open file: " + args[0]);
                Exit();
            }

            // Make space for the values
            List<double> values = new List<double>();
            List<string> good = new List<string>();
            List<string> bad = new List<string>();

            // Go through lines and get just one column to fill in the values array
            foreach (string line in allLines)
            {
                string[] parsedLine = ParseLine(line);

                values.Add(Convert.ToDouble(parsedLine[Convert.ToInt32(args[1])]));
            }

            // Calculate and Split
            avg = values.Average();
            stddev = StandardDeviation(values);
            double threshold = Convert.ToDouble(args[2]);
            
            int x = 0;
            foreach (double value in values)
            {
                double test = values.Count * SpecialFunction.erfc(Math.Abs(value - avg) / stddev);
                if (test < sensitivity)
                    bad.Add(allLines[x]);
                else
                    good.Add(allLines[x]);
                x++;
            }

            // Write to the new files
            try
            {
                using (TextWriter tw = new StreamWriter(args[0].Replace(".csv", "-good.csv")))
                {
                    foreach (String s in good)
                        tw.WriteLine(s);
                }

                using (TextWriter tw = new StreamWriter(args[0].Replace(".csv", "-bad.csv")))
                {
                    foreach (String s in bad)
                        tw.WriteLine(s);
                }
            } 
            catch
            {
                Console.WriteLine("Unable to write output files\n");
                Exit();
            }

            Exit();
        }

        // Calculate Standard Deviation
        static double StandardDeviation(List<double> doubleList)
        {
            double average = doubleList.Average();
            double sumOfDerivation = 0;

            foreach (double value in doubleList)
            {
                sumOfDerivation += Math.Pow(value - average,2);
            }

           return Math.Sqrt(sumOfDerivation / (doubleList.Count - 1));
        }

        // Graceful exit on command line and in debugger
        static void Exit()
        {
            // Keep the console window open if we are debugging!
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Console.WriteLine("\n\nHit Enter to Exit");
                Console.ReadLine();
            }

            // Close the program.
            Environment.Exit(0);
        }

        // Parse an individual line of CSV
        static string[] ParseLine(string lines)
        {
            string[] fields;
            string[] lineArray;

            try
            {
                //Split the data string
                fields = Regex.Split(lines, ",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");

                lineArray = new string[fields.Length];

                // Remove the outer quotes from the split fields
                int x = 0;
                foreach (string value in fields)
                {
                    string check = Regex.Replace(value, "^\"|\"$", "");
                    lineArray[x] = check;
                    x++;
                }
            }
            catch //(Exception ex)
            {
                return null;
            }

            return lineArray;
        }
    }
}
