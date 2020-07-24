using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace u3184875_9746_Assignment2
{
    //This class holds the algorithm that will control the agent when it's main job is a Constructor
    public class Constructor : Agent
    {
        bool lookAtStorage = false;

        public Constructor(Agent agent) : base(agent) =>
            mainJob = new Job(JobName.Constructor, new Delivery(inventory, Form1.inst.GetSite(JobName.Constructor)), IconPath.constructor, agent.mainJob.SkillLevel);

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
                Job[] sortedSub = subJobs.OrderByDescending(o => o.SkillLevel).ToArray();
                foreach (var job in sortedSub)
                    if (!blackListJobs.Contains(job.jobName))
                    {
                        Site jobSite = job.jobClass.jobSite;
                        targetSite = new Destination<Site>(jobSite, jobSite.position);
                        CurrentJob = job;
                        break;
                    }
            }
            else  //assign the agent's job to its main job
            {
                Site jobSite = mainJob.jobClass.jobSite;
                targetSite = new Destination<Site>(jobSite, jobSite.position);
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
                    if (lookAtStorage)       //At Storage Site
                        LookInsideStorage();
                    else if (currentJob.jobClass.jobSite.HasSpace())  //At Main Site
                        ProgressConstruction();
                    else
                    {
                        if (subJobs == null)
                        {
                            Task.Run(WaitForSpace).Wait();
                            StartJob();
                            return;
                        }
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

        //when the agent is at the main site, check if they are delivering back materials first and put materials into site
        //then check if there are enough materials to use
        void ProgressConstruction()
        {
            currentJob.jobClass.jobSite.AddAgent(this);
            if (deliveringMaterial)
            {
                Task.Run(() => currentJob.jobClass.DeliverMaterial(updateProgressHandler, Form1.inst.cts.Token)).Wait();
                deliveringMaterial = false;
            }
            //once the agent delivers the materials (if it is) then get started with its job
            if (currentJob.jobClass.HasEnoughMaterial())
            {   //check if site has enough materials
                Task.Run(() => currentJob.jobClass.ProgressJob(updateProgressHandler, Form1.inst.cts.Token)).Wait();
                blackListJobs.Clear();
            }
            else //go to storage site to get materials
                lookAtStorage = true;
            currentJob.jobClass.jobSite.RemoveAgent(this);
        }

        //selects whichever material has to most amount and takes it out
        //if there isn't any, blacklist the job and go to the next job, 
        //unless the agent doesnt have any, then go back to the main site
        void LookInsideStorage()
        {
            //Constructors won't take up site space
            //look inside the storage's inventory to see if it has ingots or planks
            if (StorageHasIngotOrPlank(out MaterialBox box))
            {
                currentJob.jobClass.MaterialToDeliver = box.materialType;
                Task.Run(() => currentJob.jobClass.TakeOutMaterial(updateProgressHandler, Form1.inst.cts.Token)).Wait();
                deliveringMaterial = true;
            }
            else
            {
                if (subJobs == null)
                    SetTargetSite(NodeType.MainSite);
                else
                {
                    mainJob.jobClass.jobSite = Form1.inst.GetSite(JobName.Constructor);
                    BlackListCurrentJob();
                }
            }
            lookAtStorage = false;
        }

        //check to see if storage site has ingots or planks and return whichever has the highest amount
        bool StorageHasIngotOrPlank(out MaterialBox matBox)
        {

            if (currentJob.SiteIngot.HasAmount(currentJob.jobClass.TakeOutAmount) || currentJob.SitePlank.HasAmount(currentJob.jobClass.TakeOutAmount))
            {
                matBox = currentJob.SitePlank.Current >= currentJob.SiteIngot.Current ? currentJob.SitePlank : currentJob.SiteIngot;
                return true;
            }

            matBox = new MaterialBox();
            return false;
        }
    }
}