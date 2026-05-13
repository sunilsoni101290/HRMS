using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace APP.Helpers
{
    public static class AlertHelper
    {
        public static void Success(ITempDataDictionary tempData, string message)
        {
            tempData["Success"] = message;
        }

        public static void Error(ITempDataDictionary tempData, string message)
        {
            tempData["Error"] = message;
        }
    }
}
