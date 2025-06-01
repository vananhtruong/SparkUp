using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkUp.Business
{
    public class EmailTemplate
    {
        public int Id { get; set; }
        public string Purpose { get; set; }
        public string Header { get; set; }
        public string Body { get; set; }
    }
}
