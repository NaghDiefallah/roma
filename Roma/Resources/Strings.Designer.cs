using System.Globalization;

namespace Roma
{
    internal class Strings
    {
        private static System.Resources.ResourceManager? resourceMan;

        internal static System.Resources.ResourceManager ResourceManager
        {
            get
            {
                if (resourceMan == null)
                {
                    resourceMan = new System.Resources.ResourceManager("Roma.Resources.Strings", typeof(Strings).Assembly);
                }
                return resourceMan;
            }
        }

        internal static string GetString(string name, CultureInfo? culture = null)
        {
            return ResourceManager.GetString(name, culture) ?? name;
        }
    }
}
