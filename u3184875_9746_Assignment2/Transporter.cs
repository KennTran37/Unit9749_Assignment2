using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace u3184875_9746_Assignment2
{
    //This class holds the algorithm that will control the agent when it's main job is a Transporter
    public class Transporter : Agent
    {
        MaterialType materialToDeliver;
        public MaterialType MaterialToDeliver
        {   //created a get set to optimize on the assigning of materialToDeliver in the mainJob.jobClass
            get => materialToDeliver; set
            {
                mainJob.jobClass.MaterialToDeliver = value;
                materialToDeliver = value;
            }
        }

        //similar to the blackListJobs, this list is used so that the agent does not go back to the same site
        List<NodeType> blacklistSites = new List<NodeType>();

        public Transporter(Agent agent) : base(agent) =>
            mainJob = new Job(JobName.Transporter, new Delivery(inventory, Form1.inst.GetSite(JobName.Transporter)), IconPath.transporter, agent.mainJob.SkillLevel);

        public override void InitAgent()
        {
            agentIcon = Form1.inst.CreateAgentIcon();

            blacklistSites = new List<NodeType>();
            SetTargetSite(NodeType.StorageSite);
            currentNode = new CurrentNode(mainJob.jobClass.jobSite, mainJob.SitePosition);

            updateProgressHandler += UpdateProgressBars;
            FindJob();
        }

        //assigning the target site for the agent to go to
        protected override void FindJob()
        {
            if (deliveringMaterial)
            {   //checking which site the agent is at and go towards its target site
                switch (MaterialToDeliver)
                {
                    case MaterialType.Wood:
                        SetTargetSite(NodeType.CarpenterSite);
                        break;
                    case MaterialType.Ore:
                        SetTargetSite(NodeType.BlacksmithSite);
                        break;
                    case MaterialType.Plank:
                    case MaterialType.Ingot:
                        if (currentNode.node.nodeType == NodeType.StorageSite)
                            SetTargetSite(NodeType.MainSite);
                        else
                            SetTargetSite(NodeType.StorageSite);
                        break;
                }
                PathFinding();
                return;
            }

            //if all sites have been blacklisted
            if (blacklistSites.Count == Form1.inst.TakeOutSites.Length)
            {   //clear the list and go back to storage site
                blacklistSites.Clear();
                SetTargetSite(NodeType.StorageSite);
            }
            else  //go to the first available site which has not been blacklisted
                SetTargetSite(Form1.inst.TakeOutSites.First(f => !blacklistSites.Contains(f.nodeType)).nodeType);
            PathFinding();
        }

        protected override void SetTargetSite(NodeType type)
        {
            mainJob.jobClass.jobSite = Form1.inst.GetSite(type);
            targetSite = new Destination<Site>(mainJob.jobClass.jobSite, Form1.inst.GetNodeLocation(type));
        }

        //Checks if there is space for the agent and if the agent is at the site to deliver or take out materials
        protected override void StartJob()
        {
            try
            {
                if (mainJob.jobClass.jobSite.HasSpace())
                {
                    currentJob.jobClass.jobSite.AddAgent(this);
                    if (!deliveringMaterial)
                    {
                        if (mainJob.SiteNodeType == NodeType.MainSite) //if agent is at the main site then go back to the storage site
                            SetTargetSite(NodeType.StorageSite);
                        else if (CanSelectMaterial())
                        {
                            Task.Run(() => mainJob.jobClass.TakeOutMaterial(updateProgressHandler, Form1.inst.cts.Token)).Wait();
                            //blacklist the site so that the agent doesn't go back to it
                            blacklistSites.Add(currentNode.node.nodeType);
                            deliveringMaterial = true;
                        }
                        else
                            blacklistSites.Add(currentNode.node.nodeType);
                    }
                    else
                    {
                        Task.Run(() => mainJob.jobClass.DeliverMaterial(updateProgressHandler, Form1.inst.cts.Token)).Wait();
                        deliveringMaterial = false;
                    }
                    currentJob.jobClass.jobSite.RemoveAgent(this);
                }
                else
                    blacklistSites.Add(currentNode.node.nodeType);
                FindJob();
            }
            catch (Exception) { }
        }

        //returns true if the site has space and materials, else return false and blacklist the site
        bool CanSelectMaterial()
        {
            if (mainJob.SiteNodeType == NodeType.StorageSite && StorageHasMaterial(out MaterialBox box))   //taking out the material with the most amount
                MaterialToDeliver = box.materialType;
            else if (mainJob.jobClass.HasEnoughMaterial())
            {   //if site has space for materials and depending on the site (other then storageSite), assign the materials to take out
                switch (currentNode.node.nodeType)
                {
                    case NodeType.BlacksmithSite:
                        MaterialToDeliver = MaterialType.Ingot;
                        break;
                    case NodeType.CarpenterSite:
                        MaterialToDeliver = MaterialType.Plank;
                        break;
                    case NodeType.ForestSite:
                        MaterialToDeliver = MaterialType.Wood;
                        break;
                    case NodeType.MiningSite:
                        MaterialToDeliver = MaterialType.Ore;
                        break;
                }
            }
            else //if site does not have enough materials to take out and site is not storage site
            {
                blacklistSites.Add(currentNode.node.nodeType);
                return false;
            }
            return true;
        }
    }
}