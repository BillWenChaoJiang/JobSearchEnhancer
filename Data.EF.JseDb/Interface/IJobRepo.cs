﻿using System.Collections.Generic;
using Model.Entities.JobMine;

namespace Data.EF.JseDb.Interface
{
    public interface IJobRepo : IBaseRepository<Job>
    {
        List<Job> GetJobsByEmployerId(int employerId);
        List<Job> GetJobsByLocationId(int locationId);
    }
}