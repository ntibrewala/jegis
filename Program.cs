using System;
using GIS.Framework.Helpers;

class Program {
    static void Main(string[] args) {
        var dbHelper = new DatabaseHelper("BHAVYA_POLYFILMS_01");
        try {
            var result = PayloadGenerator.GenerateEWayBillPayload(dbHelper, "67", "941");
            Console.WriteLine("---URL---");
            Console.WriteLine(result.Item2);
            Console.WriteLine("---PAYLOAD---");
            Console.WriteLine(result.Item1);
        } catch(Exception e) {
            Console.WriteLine(e.Message);
        }
    }
}
