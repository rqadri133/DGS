using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DGS.Models
{
    public class TranslationAccount 
    {
        //
        // GET: /TranslationAccount/

        public string SecretKey
        {
            get;
            set;

        }

        public int MarketIndex
        {
            get;
            set;
        }

        public string ClientID
        {

            get;
            set;

        }

        public int CharactersUsed
        {
            get;
            set;

        }

        public bool MarkedFull
        {

            get;
            set;
        }



    }
}
