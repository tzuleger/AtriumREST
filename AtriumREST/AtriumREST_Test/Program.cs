using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreeRiversTech.Zuleger.Atrium.REST;
using ThreeRiversTech.Zuleger.Atrium.REST.Objects;

namespace AtriumREST_Test
{
    class Program
    {
        static void Main(string[] args)
        {
            AtriumController ctrl = new AtriumController("admin", "Holmen2019", "http://192.168.1.218:2000/");

            List<User> users = ctrl.GetAllByIndex<User>(0, 10);

            Console.WriteLine(users);
        }
    }
}
