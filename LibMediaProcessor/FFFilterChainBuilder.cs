using System.Collections.Generic;
using System.Linq;

namespace LibMediaProcessor
{
    /// <summary>
    /// Manages the creation of filter chains for use with FFMPEG
    /// </summary>
    public class FilterChain
    {
        private string filterChain = "";
        private readonly List<string> filters;
        private string inputPad = string.Empty;
        private string outputPad = string.Empty;

        public FilterChain()
        {
            this.filters = new List<string>();
        }

        /// <summary>
        /// Adds a single filter like "idet" (no quotes) to the set.
        /// </summary>
        /// <param name="filter"> the filter and arguments for it.</param>
        public void AddFilter(string filter)
        {
            this.filters.Add(filter);
        }

        /// <summary>
        /// Removes a filter from the chain
        /// </summary>
        /// <param name="filter"> the filter to be removed.</param>
        public void RemoveFilter(string filter)
        {
            for (var i = 0; i < this.filters.Count(); i++)
            {
                if (this.filters[i].Contains(filter))
                {
                    this.filters.Remove(this.filters[i]);
                }
            }
        }
        public void DeleteAll()
        {
            this.filters.Clear();
        }

        /// <summary>
        /// Replace one filter with another.
        /// </summary>
        /// <param name="oldFilter"></param>
        /// <param name="newFilter"></param>
        public void ReplaceFilter(string oldFilter, string newFilter)
        {
            for (var i = 0; i < this.filters.Count(); i++)
            {
                if (this.filters[i].Contains(oldFilter))
                {
                    this.filters[i] = newFilter;
                }
            }
        }

        /// <summary>
        /// Sets the "input pad" for the filter chain.
        /// See FFEMPG documentation on filtering for an explainer.
        /// </summary>
        /// <param name="iPad"></param>
        public void SetInputPad(string iPad)
        {
            this.inputPad = iPad;
        }

        public List<string> GetFilters()
        {
            return this.filters;
        }

        public int GetFilterCount()
        {
            return this.filters.Count;
        }

        /// <summary>
        /// Formats and returns filters, assuming use with the -vf video filter command.
        /// </summary>
        public string GetVideoFilters()
        {
            var index = 1;
            this.filterChain = "";
            this.filterChain = " -vf ";

            //no filters...
            if (this.filters.Count == 0)
            {
                return string.Empty;
            }

            foreach (string s in this.filters)
            {
                string ftag1 = "[F" + index + "]";

                string ftag2;
                if (this.filters.Count != index)
                {
                    ftag2 = "[F" + (index + 1) + "];";
                }
                else
                {
                    ftag2 = "[V]";
                }

                this.filterChain += ftag1 + s + ftag2;
                index++;
            }

            return this.filterChain;
        }

        /// <summary>
        /// Formats and the filters, assuming use with the -filter_complex command in FFMPEG
        /// </summary>
        /// <param name="fileIndex"> The file index on input to FFEMPG where 0 == the first file</param>
        /// <param name="streamIndex"> Stream index. where 0 indicates the first stream in the file.</param>
        /// <param name="padRoot"> A string representing the root of the filter chain pads. </param>
        /// <param name="oPad"> The name of the output pad.</param>
        public string GetFilterChainComplex(int fileIndex, int streamIndex, string padRoot, string oPad)
        {
            var index = 1;
            this.filterChain = "";
            this.outputPad = oPad;

            this.inputPad = "[" + fileIndex + ":" + streamIndex + "]";

            //no filters...
            if (this.filters.Count == 0)
            {
                return string.Empty;
            }

            foreach (string s in this.filters)
            {
                string fTag1;
                string ftag2;

                if (index == 1)
                {
                    fTag1 = this.inputPad;
                }
                else
                {
                    fTag1 = padRoot + index + "]";
                }

                if (this.filters.Count != index)
                {
                    ftag2 = padRoot + (index + 1) + "];";
                }
                else
                {
                    ftag2 = this.outputPad;
                }

                this.filterChain += fTag1 + s + ftag2;
                index++;
            }

            return this.filterChain;
        }

        /// <summary>
        /// Formats and the filters, assuming use with the -filter_complex command in FFMPEG
        /// </summary>
        /// <param name="padRoot"> A string representing the root of the filter chain pads. </param>
        /// <param name="oPad"> The name of the output pad.</param>
        public string GetFilterChainComplex(string padRoot, string oPad)
        {
            var index = 1;
            this.filterChain = "";
            this.outputPad = oPad;

            //in the event the input pad has not been implicitly set...
            if (this.inputPad == string.Empty)
            {
                return null;
            }

            //no filters...
            if (this.filters.Count == 0)
            {
                return string.Empty;
            }

            foreach (string s in this.filters)
            {
                string fTag1;
                string ftag2;

                if (index == 1)
                {
                    fTag1 = this.inputPad;
                }
                else
                {
                    fTag1 = "[" + padRoot + index + "]";
                }

                if (this.filters.Count != index)
                {
                    ftag2 = "[" + padRoot + (index + 1) + "];";
                }
                else
                {
                    if (oPad == null)
                    {
                        ftag2 = "";
                    }
                    else
                    {
                        ftag2 = "[" + this.outputPad + "]";
                    }
                }
                this.filterChain += fTag1 + s + ftag2;
                index++;
            }

            return this.filterChain;
        }
    }
}