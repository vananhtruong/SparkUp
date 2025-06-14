using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkUp.Business
{
    public class WorkerCertificate
    {
        [Key]
        public int WorkerProdileId { get; set; }
        public string CertificateImgUrl { get; set; }
        public string Certificate { get; set; }
        public string ExperienceDescription { get; set; }
        //navigation property
        public WorkerProfile? WorkerProfile { get; set; }
    }
}
