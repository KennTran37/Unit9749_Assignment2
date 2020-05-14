using System;
using System.Linq;
using System.Threading.Tasks;

namespace u3184875_9746_Assignment2
{
    //This class holds the algorithm that will control the agent when it's main job is a Constructor
    public class Constructor : Agent
    {
        bool lookAtStorage = false;

        public Constructor(Agent agent) : base(agent) =>
            mainJob = new Job(JobName.Constructor, new Delivery(inventory, Form1.inst.GetSite(JobName.Constructor)), agent.mainJob.skillLevel);

        protected override void FindJob()
        {
            if (lookAtStorage)
                SetTargetSite(NodeType.StorageSite);
            else if (deliveringMaterial)
            {
                if (currentJob.jobName == JobName.Constructor)
                    SetTargetSite(NodeType.MainSite);
                else
                    SetDeliverySite();
            }
            else if (blackListJobs.Contains(mainJob.jobName))
            {   //if agent can't do main job search through the sub jobs
                Job[] sortedSub = subJobs.OrderByDescending(o => o.skillLevel).ToArray();
                foreach (var job in sortedSub)
                    if (!blackListJobs.Contains(job.jobName))
                    {
                        Site jobSite = Form1.inst.GetSite(job.jobName);
                        targetSite = new Destination<Site>(jobSite, Form1.inst.GetNodeLocation(jobSite.nodeType));
                        CurrentJob = job;
                        break;
                    }
            }
            else  //assign the agent's job to its main job
            {
                Site jobSite = Form1.inst.GetSite(mainJob.jobName);
                targetSite = new Destination<Site>(jobSite, Form1.inst.GetNodeLocation(jobSite.nodeType));
                CurrentJob = mainJob;
            }

            PathFinding();
        }

        //checks whether the agent is at storage site or main site
        //if agent is at storage site, try take out ingots or planks
        //if at main site try doing it's job
        protected override void StartJob()
        {
            try
            {
                if (currentJob.jobName == JobName.Constructor)
                {
                    //At Storage Site
                    if (lookAtStorage)
                    {   
                        //Constructors won't take up site space
                        //look inside the storage's inventory to see if it has ingots or planks
                        if (StorageHasIngotOrPlank(out MaterialBox box))
                        {
                            currentJob.jobClass.MaterialToDeliver = box.materialType;
                            Task.Run(() => currentJob.jobClass.TakeOutMaterial(updateProgressHandler, Form1.inst.cts.Token)).Wait();
                            deliveringMaterial = true;
                        }
                        else if (subJobs != null)
                            BlackListCurrentJob();
                        lookAtStorage = false;
                    }
                    else if (currentJob.jobClass.SpaceForAgentMaterial())
                    {   
                        //At Main Site
                        currentJob.jobClass.jobSite.AddAgent(this);
                        if (deliveringMaterial)
                        {
                            Task.Run(() => currentJob.jobClass.DeliverMaterial(updateProgressHandler, Form1.inst.cts.Token)).Wait();
                            deliveringMaterial = false;
                        }
                        //once the agent delivers the materials (if it is) then get started with its job
                        if (currentJob.jobClass.HasEnoughMaterial())
                        {   //check if site has enough materials
                            Task.Run(() => currentJob.jobClass.ProgressJob(updateProgressHandler, Form1.inst.cts.Token));
                            //Task.Run(currentJob.jobClass.ProgressJob).Wait();
                            blackListJobs.Clear();
                        }
                        else //go to storage site to get materials
                            lookAtStorage = true;
                        currentJob.jobClass.jobSite.RemoveAgent(this);
                    }
                    else
                    {
                        if (subJobs == null)
                            WaitForSpace().Wait();
                        else
                            BlackListCurrentJob();
                    }

                    FindJob();
                    return;
                }

                base.StartJob();
            }
            catch (Exception) { }
        }

        //check to see if storage site has ingots or planks and return whichever has the highest amount
        bool StorageHasIngotOrPlank(out MaterialBox matBox)
        {
            if (currentJob.SiteIngot.HasAmount(currentJob.jobClass.TakeOutAmount) && currentJob.SitePlank.HasAmount(currentJob.jobClass.TakeOutAmount))
            {
                matBox = currentJob.SitePlank.Current >= currentJob.SiteIngot.Current ? currentJob.SitePlank : currentJob.SiteIngot;
                return true;
            }

            matBox = new MaterialBox();
            return false;
        }
    }
}