using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DGS.Models
{
 
    [Serializable]
    public class StorageConnectionObject
    {
        public string AccountName
        {
            get;
            set;
        }

        public string AccountKey
        {
            get;
            set;
        }
        public string Server_URL
        {
            get;
            set;
        }
    }
}