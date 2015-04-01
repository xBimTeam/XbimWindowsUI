using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace Bonghi.FileIO
{
    class FileTextParser : IDisposable
    {
        private StreamReader _srReader = null;
        private int _iLineNumber = 0;
        
        public FileTextParser(string FileName) {
            try
            {
                _srReader = File.OpenText(FileName);
                _iLineNumber = 0;
            }
            catch (SystemException exc){
                Debug.WriteLine("Err: " + exc.Message);
                _srReader = null;
            }
        }

        public int LinesRead() {
            return _iLineNumber;
        }

        public long Position
        { 
            get 
            {
                return _srReader.BaseStream.Position;
            }
            set
            {
                _srReader.BaseStream.Position = value;
            }
        }

        public string NextLine()
        {
            if (_srReader == null) return null;
            _iLineNumber++;
            // _srReader.BaseStream.Position = 12;
            return _srReader.ReadLine();
        }

        public double[] NextLineSpaceArray(double multiplier)
        {
            string[] sA = NextLineSpaceArray();
            int ub = sA.GetLength(0);
            double[] dA = new double[ub];
            for (int i = 0; i < ub; i++)
            {
                dA[i] = Convert.ToDouble(sA[i]) * multiplier;
            }
            return dA;
        }

        public string[] NextLineSpaceArray()
        {
            string tmp = NextLine().Trim();
            if (tmp == null) return null;
            while (tmp.Contains("  ")) {
                tmp = tmp.Replace("  ", " ");
            }
            return tmp.Split(" ".ToCharArray());
        }

        public void Close()
        {
            try
            {
                _srReader.Close();
                _srReader.Dispose();
            }
            catch {
                Debug.WriteLine("Warning: already closed?");
                }
            finally {
                _srReader = null;
            }
            
        }
        public string ReadUntil(string MatchRegex) {
            return ReadUntil(MatchRegex, "");
        }

        public string ReadUntil(string MatchRegex, string ExitCondition) {
            string tOut = null;
            Regex reMatch = new Regex(MatchRegex);
            Regex reExit = null;
            if (ExitCondition != "") {
                reExit = new Regex(ExitCondition);
            }
            string lastread;
            while ((lastread = NextLine()) != null)
            {
                if (reExit != null && reExit.IsMatch(lastread))
                {
                    tOut = null;
                    break;
                }
                if (reMatch.IsMatch(lastread))
                {
                    tOut = lastread;
                    break;
                }
            }
            return tOut;
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (_srReader != null)
            {
                _srReader.Dispose();
                _srReader = null;
            }
        }

        #endregion
    }
}
