using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace MusicApp.Models
{
    public class Sample
    {
        /// <summary>
        /// Sample ID
        /// </summary>
        [Key]
        public string SampleID { get; set; }

        /// <summary>
        /// Title of sample
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Name of Artist
        /// </summary>
        public string Artist { get; set; }

        /// <summary>
        /// Web service resource URL of mp3 sample
        /// </summary>
        public string SampleMp3URL { get; set; }



    }
}