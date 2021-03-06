﻿using System.Collections.Generic;
using Model.Entities.JobMine;

namespace Data.Contract.JseDb.Interface
{
    public interface IJobRepo : IBaseRepository<Job>
    {
        List<Job> GetJobsByEmployerId(int employerId);
        List<Job> GetJobsByLocationId(int locationId);
        Job GetFullJob(int id);
        IEnumerable<int> GetJobIds();
        bool UpdateWithJov(JobOverView jov);
        bool SeedJobAndRelatedEntities(Job job);
    }
}
