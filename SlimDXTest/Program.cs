using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimDXTest
{
    static class Program
    {
        static void Main()
        {
            using (var view = new View())
            {
                view.Run();
            }
            //XLoader xloader = new XLoader();
            //xloader.LoadFile("./Teimoku.x");
        }
    }
}
