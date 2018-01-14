using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySQLDataToJson
{
    public class FieldStructure
    {
        public string FieldName { get; set; }
        public string UpdatedFieldName { get; set; }
        public string FieldDataType { get; set; }
    }
}