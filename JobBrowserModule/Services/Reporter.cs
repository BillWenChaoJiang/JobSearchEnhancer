﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JobBrowserModule.ViewModels;
using Model.Entities.JobMine;

namespace JobBrowserModule.Services
{

    public class Reporter : IReporter
    {
        public Action<IList<FilterViewModel>> FilterChanged { get; set; }
        public Action<Job> SelectedJobChanged { get; set; }
    }

    public interface IReporter
    {
        Action<IList<FilterViewModel>> FilterChanged { get; set; }
        Action<Job> SelectedJobChanged { get; set; }
    }
}
