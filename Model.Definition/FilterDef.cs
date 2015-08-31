﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Definition
{
    public enum ValueSearchTarget
    {
        [Description("Unknown (Please Pick a Search Target)")]
        Unknown = 0,
        [Description("Number Of Openings")]
        NumberOfOpenings,
        [Description("Number Of Applied")]
        NumberOfApplied
    }


    public enum StringSearchTarget
    {
        [Description("Unknown (Please Pick a Search Target)")]
        Unknown = 0,
        [Description("Employer Name")]
        EmployerName,
        [Description("Full Address")]
        FullAddress,
        [Description("Region")]
        Region,
        [Description("Job Title")]
        JobTitle,
        [Description("Number Of Openings")]
        NumberOfOpening,
        [Description("Number Of Applicants Applied")]
        NumberOfApplied,
        [Description("Last Date To Apply")]
        LastDateToApply,
        [Description("Levels")]
        Levels,
        [Description("Disciplines")]
        Disciplines,
        [Description("Comment")]
        Comment,
        [Description("Job Description")]
        JobDescription,
        [Description("All Job Detail")]
        Job
    }

    public enum FilterCategory
    {
        [Description("String Search")]
        StringSearch,
        [Description("Level Selection")]
        LevelSelection,
        [Description("Discipline Selection")]
        DisciplineSelection,
        [Description("Value Filter (Numbers & Etc)")]
        ValueFilter,
        [Description("Review Filter (Beta - RateCoopJob.com Data)")]
        ReviewFilter,
        [Description("Location Filter")]
        LocationFilter
    }
}
