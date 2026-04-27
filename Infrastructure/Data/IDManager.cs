using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

namespace Infrastructure.Data
{
    public static class IDManager
    {

        const long begin_Ids = 4235268274000000000;
        private static long lastTime = DateTime.UtcNow.Ticks - begin_Ids;


        // unique id generator each and every time
        public static string Next
        {
            get
            {
                long firstvalues, LastValues;
                do
                {
                    //here you can also take Guid Key , this is also given a specific key... each and every time

                    firstvalues = lastTime;
                    long now = DateTime.UtcNow.Ticks - begin_Ids;
                    LastValues = Math.Max(now, firstvalues + 1);

                }
                while (Interlocked.CompareExchange(ref lastTime, LastValues, firstvalues) != firstvalues);
                return LastValues.ToString("");
            }
        }



        public static string GetNewId(this IEntity enitity)
        {
            return GetNewId(enitity, null);
        }


        //that unique id is call here ... getsequence is Start characture
        public static string GetNewId(this IEntity enitity, string Id)
        {
            return enitity.GetKeyPrefix() + (Id ?? Next);
        }

    }
}
