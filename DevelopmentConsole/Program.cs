﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Runtime.InteropServices;
using Business.DataBaseSeeder;
using Business.Manager;
using Data.EF.JseDb;
using Data.IO.Local;
using Model.Definition;
using Model.Entities.JobMine;

namespace DevelopmentConsole
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //foreach (var msg in MasterSeeder.SeedAll("1155", JobStatus.Posted, new JseLocalRepo().GetAccount(), false, false, "ottawa"))
            //{
            //    Console.WriteLine(msg);
            //}
            RateMyCoopJobSeeder.SeedDb();
            Console.ReadKey();
        }
    }
}