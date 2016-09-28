using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DGS.Content.Controllers
{
    public class UserContainer
    {
        public string StorageServerName
        {
            set;
            get;

        }

        public bool ContainerCreateForUser
        {
            get;
            set;

        }

        public bool ContainerAlreadyExistForUser
        {
            get;
            set;

        }


        public string UserName
        {
            get;
            set;
        }



    }
}